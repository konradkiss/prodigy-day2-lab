#!/usr/bin/env node
/* Stop: the agent may finish only with valid evidence for the current tree (scripts/verify.mjs). It runs no check itself. */
import { spawnSync } from 'node:child_process';
import { join } from 'node:path';
import { ROOT } from '../lib/guards.mjs';
import { readInput } from './hook-input.mjs';

const input = readInput();
const r = spawnSync(process.execPath, [join(ROOT, 'scripts', 'verify.mjs'), '--check'], { cwd: ROOT, encoding: 'utf8' });
if (r.status === 0) process.exit(0);
if (input.stop_hook_active === true) {
  process.stderr.write('still no valid evidence after one round; handing over to the human\n');
  process.exit(0);
}
process.stderr.write(`You cannot finish yet. ${r.stderr.trim()}\nRun: node scripts/verify.mjs\n`);
process.exit(2);
