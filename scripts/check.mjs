#!/usr/bin/env node
/*
 * The one check of the repository. Exit 0 when everything is clean, 1 on any finding.
 *
 *   node scripts/check.mjs            everything: suppressions, secret scan of the working tree, build of the
 *                                     solution (nullable, analyzers, code style, complexity, NuGet audit), tests
 *   node scripts/check.mjs <file>     the fast checks on one file, in a few seconds: suppressions and secret scan
 *                                     of the file, build of the project it belongs to (with its analyzers)
 *
 * The rules themselves live in Directory.Build.props, .editorconfig, CodeMetricsConfig.txt and .secretlintrc.json.
 */
import { spawnSync } from 'node:child_process';
import { existsSync, readdirSync, readFileSync, statSync } from 'node:fs';
import { dirname, join, resolve } from 'node:path';
import { ROOT, protectedReason, repoPath, suppressions } from './lib/guards.mjs';

const ENV = { ...process.env, DOTNET_NOLOGO: '1', DOTNET_CLI_TELEMETRY_OPTOUT: '1', DOTNET_SKIP_FIRST_TIME_EXPERIENCE: '1' };
const BUILD_FLAGS = ['-nologo', '-v', 'q', '-tl:off', '-clp:NoSummary'];
const failures = [];

const run = (cmd, args) => {
  const r = spawnSync(cmd, args, { cwd: ROOT, env: ENV, encoding: 'utf8', maxBuffer: 64 * 1024 * 1024 });
  if (r.error) return { ok: false, out: `${cmd}: ${r.error.message}` };
  return { ok: r.status === 0, out: `${r.stdout ?? ''}${r.stderr ?? ''}` };
};
const fail = (step, details) => failures.push(`✗ ${step}\n${details.trim().replace(/^/gm, '  ')}`);

/* MSBuild prints each diagnostic with the absolute path and the project; keep it short, once per diagnostic. */
function diagnostics(out) {
  const lines = out.split(/\r?\n/)
    .filter((l) => /\b(?:error|warning)\s+[A-Z]+\d+|:\s*error\b/.test(l))
    .map((l) => l.replace(/\s+\[[^\]]*\.(?:cs|fs|vb)proj\]$/, '').replaceAll(`${ROOT}/`, '').replaceAll(`${ROOT}\\`, '').trim());
  return [...new Set(lines)].join('\n') || out.trim().split(/\r?\n/).slice(-40).join('\n');
}

/* The protected configuration is where the checks are set, so it is not scanned; the hooks guard it instead. */
function scanSuppressions(files) {
  const found = [];
  for (const rel of files) {
    const full = join(ROOT, rel);
    if (protectedReason(rel) || !existsSync(full) || !statSync(full).isFile()) continue;
    for (const s of suppressions(rel, readFileSync(full, 'utf8'))) found.push(`${rel}:${s.line}: ${s.what}: fix the cause instead`);
  }
  if (found.length) fail('suppressions (a check switched off instead of fixed)', found.join('\n'));
}

/* The secretlint config, with the repository's own rules (ids starting with ./) resolved to absolute paths. */
function secretlint(targets) {
  const config = JSON.parse(readFileSync(join(ROOT, '.secretlintrc.json'), 'utf8'));
  config.rules = config.rules.map((r) => (r.id.startsWith('.') ? { ...r, id: resolve(ROOT, r.id) } : r));
  const bin = join(ROOT, 'node_modules', 'secretlint', 'bin', 'secretlint.js');
  if (!existsSync(bin)) { fail('secret scan', 'secretlint is not installed: run npm ci'); return; }
  const r = run(process.execPath, [bin, '--no-color', '--format', 'unix', '--secretlintrcJSON', JSON.stringify(config), ...targets]);
  if (!r.ok) fail('secret scan (secretlint)', r.out.replaceAll(`${ROOT}/`, '').replaceAll(`${ROOT}\\`, ''));
}

function build(target, restore = true) {
  const r = run('dotnet', ['build', target, ...BUILD_FLAGS, ...(restore ? [] : ['--no-restore'])]);
  if (!r.ok) fail(`build ${target} (nullable, analyzers, code style, complexity, NuGet audit)`, diagnostics(r.out));
  return r.ok;
}

function test() {
  const r = run('dotnet', ['test', 'Orders.sln', '--no-build', '-nologo', '-v', 'q']);
  if (!r.ok) fail('tests', r.out.trim().split(/\r?\n/).slice(-120).join('\n'));
}

/* The project a file belongs to: the nearest directory upwards with a .csproj in it. */
function projectOf(rel) {
  for (let dir = dirname(join(ROOT, rel)); dir.startsWith(ROOT) && dir !== ROOT; dir = dirname(dir)) {
    const proj = readdirSync(dir).find((f) => f.endsWith('.csproj'));
    if (proj) return repoPath(join(dir, proj));
  }
  return null;
}

function checkFile(file) {
  const rel = repoPath(file);
  if (rel === null) { console.log(`${file} is outside the repository; nothing to check`); return; }
  scanSuppressions([rel]);
  if (existsSync(join(ROOT, rel))) secretlint([rel]);
  if (/\.(?:cs|csproj)$/i.test(rel)) {
    const project = projectOf(rel) ?? 'Orders.sln';
    const restored = existsSync(join(ROOT, dirname(project), 'obj', 'project.assets.json'));
    build(project, rel.endsWith('.csproj') || !restored);
  } else if (/(?:^|\/)(?:Directory\.[\w.]+|\.editorconfig|[\w.]*\.globalconfig|CodeMetricsConfig\.txt)$/i.test(rel)) {
    build('Orders.sln');
  }
}

function checkAll() {
  const r = run('git', ['ls-files', '-co', '--exclude-standard']);
  scanSuppressions(r.ok ? r.out.split('\n').filter(Boolean) : []);
  secretlint(['**/*']);
  if (build('Orders.sln')) test();
}

const [file] = process.argv.slice(2);
if (file) checkFile(file);
else checkAll();

if (failures.length) {
  console.error(failures.join('\n\n'));
  process.exit(1);
}
console.log(file ? `check ${file}: clean` : 'check: all clean (suppressions, secrets, build, analyzers, audit, tests)');
