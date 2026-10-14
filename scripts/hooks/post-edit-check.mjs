#!/usr/bin/env node
/*
 * PostToolUse on Edit and Write: the fast check on the file just edited. On a finding, exit 2 with the
 * errors on stderr, so the agent sees them at once and fixes them in its next step.
 */
import { spawnSync } from 'node:child_process';
import { join } from 'node:path';
import { ROOT, repoPath } from '../lib/guards.mjs';
import { block, readInput } from './hook-input.mjs';

const input = readInput();
const file = input.tool_input?.file_path;
if (!file || repoPath(file, input.cwd) === null) process.exit(0);

const r = spawnSync(process.execPath, [join(ROOT, 'scripts', 'check.mjs'), repoPath(file, input.cwd)], { cwd: ROOT, encoding: 'utf8' });
if (r.status !== 0) block(`The check found problems after this edit. Fix their cause (do not silence them):\n\n${r.stderr || r.stdout}`);
