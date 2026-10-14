#!/usr/bin/env node
/*
 * Codex PostToolUse hook for apply_patch: the fast check on every file the patch touched, findings fed back.
 * Codex reads a JSON decision on stdout for this event.
 */
import { spawnSync } from 'node:child_process';
import { existsSync } from 'node:fs';
import { join } from 'node:path';
import { ROOT, repoPath } from '../lib/guards.mjs';
import { parsePatch } from './codex-patch.mjs';
import { readInput } from './hook-input.mjs';

const input = readInput();
const command = input.tool_input?.command;
const { files } = parsePatch(Array.isArray(command) ? command.join('\n') : String(command ?? ''));
const failures = files
  .map((f) => repoPath(f, input.cwd))
  .filter((rel) => rel !== null && existsSync(join(ROOT, rel)))
  .map((rel) => ({ rel, r: spawnSync(process.execPath, [join(ROOT, 'scripts', 'check.mjs'), rel], { cwd: ROOT, encoding: 'utf8' }) }))
  .filter(({ r }) => r.status !== 0);

if (failures.length) {
  const reason = failures.map(({ rel, r }) => `The check found problems in ${rel}. Fix their cause (do not silence them):\n${r.stderr || r.stdout}`).join('\n\n');
  console.log(JSON.stringify({ decision: 'block', reason }));
}
