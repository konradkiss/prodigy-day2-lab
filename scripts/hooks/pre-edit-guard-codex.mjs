#!/usr/bin/env node
/*
 * Codex PreToolUse hook for apply_patch: the same refusals as pre-edit-guard.mjs, read out of the patch text.
 * Fails closed: a patch the parser cannot read is refused rather than let through. An input in the Claude Code
 * shape (file_path, edits or content) is handed to pre-edit-guard.mjs itself.
 */
import { spawnSync } from 'node:child_process';
import { join } from 'node:path';
import { ROOT, addedSuppressions, protectedReason, repoPath } from '../lib/guards.mjs';
import { parsePatch } from './codex-patch.mjs';
import { block, readInput } from './hook-input.mjs';

const input = readInput({ strict: true });
const tool = input.tool_input ?? {};

if (tool.command === undefined) {
  const r = spawnSync(process.execPath, [join(ROOT, 'scripts', 'hooks', 'pre-edit-guard.mjs')], { input: JSON.stringify(input), encoding: 'utf8' });
  process.stderr.write(r.stderr ?? '');
  process.exit(r.status ?? 2);
}

const patch = parsePatch(Array.isArray(tool.command) ? tool.command.join('\n') : String(tool.command));
if (patch.unreadable) block('Refused: this edit names no file the hook can read, so it cannot be checked. Use a plain apply_patch with "*** Update File:" headers.');

const ADVICE = 'Fix the code the check complains about; if you think a check is wrong, stop and tell the human why.';
for (const { file } of patch.ops) {
  const rel = repoPath(file, input.cwd);
  const reason = protectedReason(rel);
  if (reason) block(`Refused: ${reason}. The checks' configuration, scripts/, .claude/ and .codex/ are not edited by the agent. ${ADVICE}`);
  const added = rel === null ? [] : addedSuppressions(rel, patch.removed, patch.added);
  if (added.length) block(`Refused: this edit to ${rel} switches a check off instead of fixing its cause: ${added.join('; ')}. ${ADVICE}`);
}
