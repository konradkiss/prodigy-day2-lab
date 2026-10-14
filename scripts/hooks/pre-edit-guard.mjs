#!/usr/bin/env node
/*
 * PreToolUse on Edit and Write: refuse a change that switches a check off instead of fixing its cause
 * (#pragma warning disable, [SuppressMessage], #nullable disable, the null-forgiving operator, NoWarn and friends in
 * a project file, secretlint-disable), and any edit to the checks' configuration, scripts/ or .claude/.
 */
import { existsSync, readFileSync } from 'node:fs';
import { join } from 'node:path';
import { ROOT, addedSuppressions, protectedReason, repoPath } from '../lib/guards.mjs';
import { block, readInput } from './hook-input.mjs';

const input = readInput();
const tool = input.tool_input ?? {};
const rel = tool.file_path ? repoPath(tool.file_path, input.cwd) : null;
if (rel === null) process.exit(0);

const reason = protectedReason(rel);
if (reason) {
  block(`Refused: ${reason}. The checks' configuration, scripts/ and .claude/ are not edited by the agent. `
    + 'Fix the code the check complains about; if you think a check is wrong, stop and tell the human why.');
}

/* What the edit replaces and what it puts in its place: Edit and MultiEdit change strings, Write the whole file. */
const current = existsSync(join(ROOT, rel)) ? readFileSync(join(ROOT, rel), 'utf8') : '';
const edits = tool.edits ?? (tool.content !== undefined ? [{ old_string: current, new_string: tool.content }] : [tool]);
const added = edits.flatMap((e) => addedSuppressions(rel, e.old_string ?? '', e.new_string ?? ''));
if (added.length) {
  block(`Refused: this edit to ${rel} switches a check off instead of fixing its cause: ${[...new Set(added)].join('; ')}. `
    + 'Fix what the check found (check for null, handle the case, change the design); if you think the finding is wrong, stop and say so.');
}
