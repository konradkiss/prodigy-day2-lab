#!/usr/bin/env node
/*
 * Doctor: is this laptop ready for the lab? One line per check, exit 1 if anything is red.
 *   npm run doctor
 */
import { spawnSync } from 'node:child_process';
import { existsSync } from 'node:fs';
import { ensureReferee, REFEREE_URL, refereeRuns } from './lab-referee.mjs';

const WIN = process.platform === 'win32';
const checks = [];
const check = (name, fn) => { try { const r = fn(); checks.push([name, true, r]); } catch (e) { checks.push([name, false, e.message]); } };
const run = (cmd, args) => {
  const r = spawnSync(cmd, args, { encoding: 'utf8', shell: WIN });
  if (r.status !== 0) throw new Error((r.stderr || r.stdout || `${cmd} failed`).trim().split('\n').slice(-3).join(' '));
  return (r.stdout || '').trim();
};

check('node >= 22.18', () => { const [maj, min] = process.versions.node.split('.').map(Number); if (maj < 22 || (maj === 22 && min < 18)) throw new Error(`node ${process.versions.node}`); return process.versions.node; });
check('npm', () => run('npm', ['--version']));
check('git', () => run('git', ['--version']));
check('.NET 10 SDK', () => { const v = run('dotnet', ['--version']); if (Number(v.split('.')[0]) < 10) throw new Error(`dotnet ${v}; install the .NET 10 SDK`); return v; });
check('node_modules installed', () => { if (!existsSync('node_modules')) throw new Error('run: npm ci'); return 'ok'; });
check('dotnet tools (Stryker.NET)', () => { run('dotnet', ['tool', 'restore', '-v', 'q']); return 'ok'; });
check('build', () => { run('dotnet', ['build', '-v', 'q', '-nologo']); return 'ok'; });
check('tests', () => run('dotnet', ['test', '--no-build', '-v', 'q', '-nologo']).match(/(Passed!|Failed!).*?Total:\s*\d+/)?.[0] ?? 'ok');
check('ast-grep (npm package)', () => run('npx', ['--no-install', 'ast-grep', '--version']));
check('coding agent (claude or codex)', () => {
  for (const c of ['claude', 'codex']) {
    const r = spawnSync(c, ['--version'], { encoding: 'utf8', shell: WIN });
    if (r.status === 0) return `${c} ${r.stdout.trim()}`;
  }
  throw new Error('neither claude nor codex on PATH');
});

/* The referee comes from agentcode.page: a blocked site or a proxy shows up here, before the session. */
const referee = await ensureReferee();
if (!referee.ok) checks.push([`referee (${REFEREE_URL})`, false, referee.why]);
else {
  const runs = refereeRuns();
  checks.push([`referee (${REFEREE_URL})`, runs.ok, runs.ok ? `${referee.how}, runs` : runs.why]);
}

let red = 0;
for (const [name, ok, detail] of checks) { if (!ok) red++; console.log(`${ok ? '✓' : '✗'} ${name}${detail ? `  ${detail}` : ''}`); }
console.log(red ? `\n${red} check(s) red.` : '\nAll green. You are ready.');
process.exit(red ? 1 : 0);
