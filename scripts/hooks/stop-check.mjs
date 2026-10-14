#!/usr/bin/env node
/*
 * Stop: the full check before the agent may finish. If it fails, exit 2 with the failures on stderr so the
 * agent keeps working; at most five rounds in a row (counted in a temp file per session), then the human takes over.
 */
import { spawnSync } from 'node:child_process';
import { existsSync, readFileSync, rmSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { ROOT } from '../lib/guards.mjs';
import { block, readInput } from './hook-input.mjs';

const MAX_ROUNDS = 5;
const input = readInput();
const session = String(input.session_id ?? 'unknown').replace(/[^\w-]/g, '_');
const counter = join(tmpdir(), `orders-lab-stop-check-${session}.json`);

/* A stop that does not follow a blocked stop starts a new series of rounds. */
const previous = input.stop_hook_active && existsSync(counter) ? Number(JSON.parse(readFileSync(counter, 'utf8')).rounds) || 0 : 0;

const r = spawnSync(process.execPath, [join(ROOT, 'scripts', 'check.mjs')], { cwd: ROOT, encoding: 'utf8' });
if (r.status === 0) {
  rmSync(counter, { force: true });
  process.exit(0);
}

const rounds = previous + 1;
const failures = (r.stderr || r.stdout).trim();
if (rounds > MAX_ROUNDS) {
  rmSync(counter, { force: true });
  process.stdout.write(`${JSON.stringify({ systemMessage: `npm run check still fails after ${MAX_ROUNDS} rounds; over to you.\n\n${failures}` })}\n`);
  process.exit(0);
}
writeFileSync(counter, JSON.stringify({ rounds }));
block(`npm run check fails (round ${rounds} of ${MAX_ROUNDS}); the work is not done. Fix the cause of every failure:\n\n${failures}`);
