#!/usr/bin/env node
/* PreToolUse on Edit and Write: while .phase-implement exists, the test files under tests/ are read-only. */
import { spawnSync } from 'node:child_process';
import { ROOT, repoPath } from '../lib/guards.mjs';
import { block, readInput } from './hook-input.mjs';

const TEST_FILE = /^tests?\//i;

const input = readInput({ strict: true });
const file = input.tool_input?.file_path ?? '';
const rel = file ? repoPath(file, input.cwd) : null;
const r = spawnSync('git', ['ls-files', '--cached', '--others', '--', '.phase-implement'], { cwd: ROOT, encoding: 'utf8' });
if (r.error || r.status !== 0) block('Refused: cannot tell whether the tests are frozen (git failed).');
if (r.stdout.trim() && rel !== null && TEST_FILE.test(rel)) {
  block('Refused: the tests are frozen while you implement. If a test is wrong, tell the user.');
}
