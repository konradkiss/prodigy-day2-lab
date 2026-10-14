#!/usr/bin/env node
/*
 * Lab navigation. Everything a participant runs goes through npm scripts, never raw git:
 *
 *   npm run stages              list the stages of the session
 *   npm run stage -- 4-guarded  switch to a stage (your current work is committed first, nothing is lost)
 *   npm run referee             score the service with the referee (downloaded on first use, kept in .lab/)
 *   npm run mutate              a colleague's one-character change at the free-shipping threshold (then: dotnet test)
 *   npm run restore             undo the mutation
 *   npm run wire-rules          after the rules exercise: the rule tests and the scan join scripts/check.mjs, then lock-rules
 *   npm run lock-rules          after you wrote your own rules: the hooks protect rules/, rule-tests/ and sgconfig.yml
 *   npm run doctor              is this laptop ready?
 */
import { spawnSync } from 'node:child_process';
import { existsSync, readdirSync, readFileSync, writeFileSync } from 'node:fs';
import { ensureReferee, HOW_TO_FREE_3000, portInUse, REFEREE, startService, stopService } from './lab-referee.mjs';

const STAGES = [
  ['0-start', 'the untouched lab, the ticket, the doctor'],
  ['1-bare', 'the agent did the ticket with no guardrails (read the diff: what is wrong?)'],
  ['3-guidance', 'a CLAUDE.md: what done means, scope, ask do not guess'],
  ['4-guarded', 'check script, hooks, strict build, analyzers, audit, secret scan (run the ticket here)'],
  ['4-guarded-ticket', 'the ticket done under the hooks (write your rules here)'],
  ['4-rules', 'ast-grep rules with their own tests, wired into the check'],
  ['4-hooks', 'evidence bound to the tree, frozen tests (try to break it)'],
];
const WIN = process.platform === 'win32';

const root = spawnSync('git', ['rev-parse', '--show-toplevel'], { encoding: 'utf8' }).stdout.trim();
if (!root) { console.error('not inside the lab repository'); process.exit(1); }
process.chdir(root);

const git = (...args) => spawnSync('git', args, { encoding: 'utf8' });
const run = (cmd, args, env = {}) => spawnSync(cmd, args, { stdio: 'inherit', env: { ...process.env, ...env }, shell: WIN }).status ?? 1;
const quiet = (cmd, args) => spawnSync(cmd, args, { encoding: 'utf8', shell: WIN });

function stages() {
  const current = git('branch', '--show-current').stdout.trim();
  for (const [name, what] of STAGES) console.log(`${current === name || current === `stage/${name}` ? '→' : ' '} ${name.padEnd(18)} ${what}`);
  console.log('\nswitch with: npm run stage -- <name>');
}

/* Where a stage lives: origin first, then local, under stage/ or as a plain branch. */
function resolveRef(name) {
  const candidates = [`origin/stage/${name}`, `stage/${name}`, `origin/${name}`, name];
  return candidates.find((ref) => git('rev-parse', '--verify', '--quiet', ref).status === 0) ?? null;
}

/* Uncommitted work is committed on the current branch before switching, so nothing is lost. */
function commitWorkInProgress(next) {
  if (!git('status', '--porcelain').stdout.trim()) return;
  const current = git('branch', '--show-current').stdout.trim() || 'detached';
  git('add', '-A');
  git('commit', '-q', '--no-verify', '-m', `WIP on ${current} before switching to ${next}`);
  console.log(`your changes were committed on ${current}, nothing is lost (return with: npm run stage -- ${current.replace(/^stage\//, '')})`);
}

/* A participant's own branch for a stage: the one npm run stage created, or the clone's default stage/<name>. */
function localBranch(name) {
  return [name, `stage/${name}`].find((b) => git('rev-parse', '--verify', '--quiet', `refs/heads/${b}`).status === 0) ?? null;
}

function checkout(name, ref) {
  const local = localBranch(name);
  const r = local ? git('checkout', '-q', local) : git('checkout', '-q', '-B', name, ref);
  if (r.status !== 0) { console.error(r.stderr); process.exit(1); }
  const ownWork = local && git('rev-list', '--count', `${ref}..${local}`).stdout.trim() !== '0';
  console.log(local ? `back on your branch ${local}${ownWork ? ' (with your saved work)' : ''}` : `on ${name} (fresh from ${ref})`);
}

/* The stage's tools: the npm check tools, the local dotnet tools (Stryker.NET), the NuGet packages. */
function install() {
  if (run('npm', ['ci', '--no-audit', '--no-fund', '--loglevel=error']) !== 0) process.exit(1);
  if (existsSync('.config/dotnet-tools.json') && run('dotnet', ['tool', 'restore', '-v', 'q']) !== 0) process.exit(1);
  const restore = quiet('dotnet', ['restore', '-v', 'q']);
  if (restore.status !== 0) console.log(`dotnet restore reported problems (the build will show them):\n${(restore.stdout + restore.stderr).trim()}`);
}

function stage(name) {
  if (!name) { stages(); process.exit(1); }
  const ref = resolveRef(name);
  if (!ref) { console.error(`no such stage: ${name}`); stages(); process.exit(1); }
  undoMutationIfLeft();
  commitWorkInProgress(name);
  checkout(name, ref);
  install();
  if (existsSync('docs/STAGE.md')) console.log(`\n${readFileSync('docs/STAGE.md', 'utf8')}`);
}

/*
 * The referee: this script builds the service, starts it on a free port with fresh data, runs the referee against it,
 * and stops it again, also on Windows (taskkill), so no service is left running between rounds.
 */
async function referee() {
  const got = await ensureReferee();
  if (!got.ok) { console.error(got.why); process.exit(1); }
  if (got.how !== 'already in .lab/') console.log(`referee ${got.how} to ${REFEREE}`);
  if (await portInUse(3000)) {
    console.log(`note: port 3000 is in use (a service started with npm start, or one left over). The referee uses its own port, so this run is fine.\n${HOW_TO_FREE_3000}\n`);
  }
  const build = quiet('dotnet', ['build', 'src/Orders', '-c', 'Debug', '-v', 'q', '-nologo']);
  if (build.status !== 0) { console.error(`the service does not build, so the referee cannot start it:\n${(build.stdout + build.stderr).trim()}`); process.exit(1); }
  let service;
  try {
    service = await startService();
  } catch (e) {
    console.error(e.message);
    process.exit(1);
  }
  const stop = () => stopService(service.child);
  process.once('SIGINT', () => { stop(); process.exit(130); });
  const status = run('node', [REFEREE, service.base, ...process.argv.slice(3)]);
  stop();
  process.exit(status);
}

/* The one-character mutation of the testing exercise: >= becomes > at the free-shipping threshold. Run the tests yourself. */
const PRICING = 'src/Orders/Orders/Pricing.cs';
const AT_THRESHOLD = /(\w+) >= FreeShippingFrom/;
const MUTATED = /(\w+) > FreeShippingFrom/;

function mutate() {
  const before = readFileSync(PRICING, 'utf8');
  const hit = before.match(AT_THRESHOLD);
  if (!hit) { console.error(`${PRICING} has no "<goods> >= FreeShippingFrom" comparison; nothing to mutate (already mutated? npm run restore)`); process.exit(1); }
  writeFileSync(PRICING, before.replace(AT_THRESHOLD, `${hit[1]} > FreeShippingFrom`));
  console.log(`mutated ${PRICING}: "${hit[0]}" is now "${hit[1]} > FreeShippingFrom" (free shipping no longer at exactly 50.00)\n`);
  run('git', ['status', '--short']);
  console.log('\nnow: dotnet test     do the tests notice?\nthen: npm run restore');
}

/* The exact reverse of mutate(), so it works whatever git state the file is in (a WIP commit may contain the mutation). */
function unmutate() {
  if (!existsSync(PRICING)) return null;
  const before = readFileSync(PRICING, 'utf8');
  const hit = before.match(MUTATED);
  if (!hit) return null;
  writeFileSync(PRICING, before.replace(MUTATED, `${hit[1]} >= FreeShippingFrom`));
  return hit;
}

/* A switch must not save the colleague's mutation as the participant's own work: undo it first, and say so. */
function undoMutationIfLeft() {
  if (unmutate()) console.log(`the colleague's mutation was still in ${PRICING}; restored it before switching (npm run restore does the same)`);
}

function restore() {
  const hit = unmutate();
  if (!hit) { console.log(`${PRICING} is not mutated; nothing to restore.`); return; }
  console.log(`${PRICING} restored: "${hit[0]}" is again "${hit[1]} >= FreeShippingFrom".`);
  run('git', ['status', '--short']);
}

/*
 * After the rules exercise: the agent wrote rules/, rule-tests/ and sgconfig.yml, but it cannot add them to
 * scripts/check.mjs (the hooks protect it), so the human runs this. It adds exactly the lines of the 4-rules stage:
 * the rule tests and a scan in the full check, a scan of the edited .cs file in file mode. Then it locks the rules
 * like lock-rules. Idempotent: on a stage where the rules are wired it says so and changes nothing in the check.
 */
const CHECK = 'scripts/check.mjs';
const AST_GREP_FUNCTION = `/* The repository's own ast-grep rules (rules/, sgconfig.yml): first their tests, then a scan of the project. */
const AST_GREP = join(ROOT, 'node_modules', '@ast-grep', 'cli', process.platform === 'win32' ? 'ast-grep.exe' : 'ast-grep');
function astGrep() {
  if (!existsSync(AST_GREP)) { fail('ast-grep rules', 'ast-grep is not installed: run npm ci'); return; }
  const t = run(AST_GREP, ['test']);
  if (!t.ok) { fail('ast-grep rule tests', t.out); return; }
  const s = run(AST_GREP, ['scan', '--color', 'never']);
  if (!s.ok) fail('ast-grep rules (SQL as parameters, order ownership)', s.out);
}

`;
const AST_GREP_FILE = `    if (rel.endsWith('.cs')) {
      const s = run(AST_GREP, ['scan', '--color', 'never', rel]);
      if (!s.ok) fail(\`ast-grep rules on \${rel}\`, s.out);
    }
`;
const WIRING = [
  { what: 'astGrep(): the rule tests, then a scan of the project', anchor: '/* The project a file belongs to', text: AST_GREP_FUNCTION, before: true },
  { what: 'the full check (checkAll) runs astGrep()', anchor: "  secretlint(['**/*']);\n", text: '  astGrep();\n' },
  { what: 'file mode (checkFile) scans the edited .cs file', anchor: '  if (/\\.(?:cs|csproj)$/i.test(rel)) {\n', text: AST_GREP_FILE },
];

function wireRules() {
  if (!existsSync(CHECK)) { console.error(`no ${CHECK} on this stage: the check arrives on 4-guarded. Nothing to wire.`); process.exit(1); }
  const rules = existsSync('rules') ? readdirSync('rules').filter((f) => /\.ya?ml$/.test(f)) : [];
  if (!existsSync('sgconfig.yml') || rules.length === 0) {
    console.error('no rules yet: the rules exercise writes rules/*.yml and sgconfig.yml. Run npm run wire-rules after it.');
    process.exit(1);
  }
  let check = readFileSync(CHECK, 'utf8');
  if (check.includes('function astGrep(')) {
    console.log(`already wired: ${CHECK} runs the rule tests and the scan (${rules.join(', ')}).`);
  } else {
    const missing = WIRING.filter((w) => !check.includes(w.anchor));
    if (missing.length) {
      console.error(`${CHECK} is not the lab's own (cannot find: ${missing.map((w) => JSON.stringify(w.anchor.trim())).join(', ')}). Nothing changed; add the lines by hand.`);
      process.exit(1);
    }
    for (const w of WIRING) check = check.replace(w.anchor, w.before ? w.text + w.anchor : w.anchor + w.text);
    writeFileSync(CHECK, check);
    console.log(`wired ${rules.join(', ')} into ${CHECK}:`);
    for (const w of WIRING) console.log(`  + ${w.what}`);
  }
  lockRules();
  console.log('\nnow: npm run check');
}

/*
 * After the rules exercise: the agent wrote rules/ and rule-tests/; from now on the hooks must treat them like the
 * check's own configuration, so no agent can loosen a rule to make code pass. Idempotent; only works where the
 * hooks exist (from the 4-guarded stage on). The hooks read the extra protected paths from this file.
 */
const LOCKED = 'scripts/hooks/locked-paths.json';

function lockRules() {
  if (!existsSync('scripts/hooks')) { console.error('no hooks on this stage (scripts/hooks/); switch to 4-guarded or later'); process.exit(1); }
  const paths = ['rules/', 'rule-tests/', 'sgconfig.yml'];
  const now = existsSync(LOCKED) ? JSON.parse(readFileSync(LOCKED, 'utf8')) : [];
  const missing = paths.filter((p) => !now.includes(p));
  if (!missing.length) { console.log('the rules are already locked'); return; }
  writeFileSync(LOCKED, `${JSON.stringify([...now, ...missing], null, 2)}\n`);
  console.log(`locked: the hooks now protect ${missing.join(', ')} (written to ${LOCKED}; that is expected).\nTry it: ask your agent to loosen a rule, and watch the hook refuse.`);
}

const [, , command, arg] = process.argv;
if (command === 'stages') stages();
else if (command === 'stage') stage(arg);
else if (command === 'referee') await referee();
else if (command === 'mutate') mutate();
else if (command === 'restore') restore();
else if (command === 'lock-rules') lockRules();
else if (command === 'wire-rules') wireRules();
else { console.error('usage: lab.mjs stages | stage <name> | referee | mutate | restore | wire-rules | lock-rules'); process.exit(1); }
