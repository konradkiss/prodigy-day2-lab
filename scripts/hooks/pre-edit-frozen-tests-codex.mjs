#!/usr/bin/env node
/*
 * Codex PreToolUse on apply_patch: while .phase-implement exists, the test files are read-only (the Codex twin of
 * pre-edit-frozen-tests.mjs). Fails closed: an unreadable patch or any error refuses the edit. An input in the
 * Claude Code shape (file_path) is handed to pre-edit-frozen-tests.mjs itself.
 */
import { spawnSync } from 'node:child_process';
import { join } from 'node:path';
import { ROOT, repoPath } from '../lib/guards.mjs';
import { parsePatch } from './codex-patch.mjs';
import { block, readInput } from './hook-input.mjs';

const TEST_FILE = /^tests?\//i;

const input = readInput({ strict: true });
const tool = input.tool_input ?? {};
if (tool.command === undefined) {
  const r = spawnSync(process.execPath, [join(ROOT, 'scripts', 'hooks', 'pre-edit-frozen-tests.mjs')], { input: JSON.stringify(input), encoding: 'utf8' });
  process.stderr.write(r.stderr ?? '');
  process.exit(r.status ?? 2);
}
const patch = parsePatch(Array.isArray(tool.command) ? tool.command.join('\n') : String(tool.command));
if (patch.unreadable) block('Refused: this edit names no file the hook can read, so it cannot be checked.');
const r = spawnSync('git', ['ls-files', '--cached', '--others', '--', '.phase-implement'], { cwd: ROOT, encoding: 'utf8' });
if (r.error || r.status !== 0) block('Refused: cannot tell whether the tests are frozen (git failed).');
if (r.stdout.trim() && patch.files.some((f) => { const rel = repoPath(f, input.cwd); return rel !== null && TEST_FILE.test(rel); })) {
  block('Refused: the tests are frozen while you implement. If a test is wrong, tell the user.');
}
