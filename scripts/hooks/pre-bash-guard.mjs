#!/usr/bin/env node
/*
 * PreToolUse on Bash: refuse --no-verify (and git commit -n), switching a check off from the command line
 * (-p:NoWarn=..., TreatWarningsAsErrors=false, -warnaserror-), and commands that modify the protected files:
 * the checks' configuration, scripts/, .claude/ (see scripts/lib/guards.mjs). Reading them stays allowed.
 * A heuristic, not a sandbox: it looks at each part of the command line for a write and a protected path.
 */
import { basename } from 'node:path';
import { protectedNames, protectedPaths, protectedReason, repoPath } from '../lib/guards.mjs';
import { block, readInput } from './hook-input.mjs';

const input = readInput();
const command = String(input.tool_input?.command ?? '');
if (!command.trim()) process.exit(0);

const ADVICE = 'Fix the cause of what the check finds; if you think a check is wrong, stop and tell the human why.';

if (/--no-verify\b/i.test(command) || /\bgit\b[^;&|\n]*\scommit\b[^;&|\n]*\s-[a-z]*n[a-z]*\b/i.test(command)) {
  block(`Refused: --no-verify (or git commit -n) skips the checks. ${ADVICE}`);
}

const CHECK_PROPERTIES = String.raw`(?:MSBuild|CodeAnalysis)?TreatWarningsAsErrors|Nullable|NoWarn|WarningsNotAsErrors|WarningLevel|RunAnalyzers(?:DuringBuild)?|EnableNETAnalyzers|EnforceCodeStyleInBuild|GenerateDocumentationFile|Analysis(?:Level|Mode)\w*|NuGetAudit\w*`;
if (new RegExp(String.raw`\b(?:${CHECK_PROPERTIES})\s*=`, 'i').test(command) || /(?:^|\s)[-/](?:warnaserror-|nowarn\b|noWarn\b)/i.test(command)) {
  block(`Refused: this command overrides a check's setting (MSBuild property or warning switch). ${ADVICE}`);
}

/* Commands that change the checks' tooling without naming a protected file. */
const TOOLING = [
  /\bnpm\s+(?:pkg\s+(?:set|delete)|set-script)\b/i,
  /\bdotnet\s+tool\s+(?:install|uninstall|update)\b(?![^;&|\n]*\s(?:-g|--global)\b)/i,
  /\bdotnet\s+new\s+(?:editorconfig|globaljson|buildprops|buildtargets|tool-manifest)\b/i,
];
if (TOOLING.some((re) => re.test(command))) block(`Refused: this command changes the checks' configuration (package.json scripts or the tool manifest). ${ADVICE}`);

const escape = (s) => s.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
const mentions = [...protectedPaths(), ...protectedNames()].map((p) => {
  const dir = p.endsWith('/');
  const name = dir ? p.slice(0, -1) : p;
  const last = escape(dir ? name : basename(name));
  return new RegExp(String.raw`(?:^|[\s"'=:/\\(])${dir ? escape(name).replaceAll('/', String.raw`[/\\]`) : last}(?=$|[\s"';|&)/\\])`, 'i');
});
mentions.push(/\.globalconfig\b/i);

const WRITES = [
  /\b(?:rm|rmdir|unlink|mv|cp|tee|touch|truncate|chmod|chown|ln|dd|install|del|erase|copy|move|ren|rename|xcopy|robocopy|patch)\b/i,
  /\bsed\b[^|]*\s(?:-[a-z]*i|--in-place)/i,
  /\bperl\b[^|]*\s-[a-z]*i/i,
  /\b(?:Set-Content|Add-Content|Out-File|Remove-Item|Move-Item|Copy-Item|New-Item|Rename-Item|Clear-Content)\b/i,
  /\bgit\s+(?:checkout|restore|rm|mv|apply|am|update-index|reset|clean|stash)\b/i,
  /\b(?:node|python3?|py|perl|ruby|pwsh|powershell|deno|bun)\b.*\s(?:-e|--eval|-p|--print|-c|-Command)\b/i,
  /\bnpm\s+(?:uninstall|remove|rm|un|r|install|i|add|update|up)\b/i,
  /\[System\.IO\.File\]::|\bWriteAllText\b|\bwriteFileSync\b/i,
];
const REDIRECT = /(?:^|[^<>&\d])>{1,2}\|?\s*(["']?)([^\s"'|;&<>]+)\1/g;

const parts = command.split(/&&|\|\||[;|\n]/);
let inProtectedDir = false;
for (const part of parts) {
  const cd = part.match(/^\s*(?:cd|pushd|Set-Location|sl)\s+(["']?)([^\s"']+)\1/i);
  if (cd) inProtectedDir = Boolean(protectedReason(repoPath(cd[2], input.cwd)));

  for (const [, , target] of part.matchAll(REDIRECT)) {
    const reason = protectedReason(repoPath(target, input.cwd));
    if (reason) block(`Refused: this command writes to ${target}: ${reason}. ${ADVICE}`);
  }
  const touchesProtected = inProtectedDir || mentions.some((re) => re.test(part));
  if (touchesProtected && WRITES.some((re) => re.test(part))) {
    block(`Refused: this command may modify a protected file (the checks' configuration, scripts/, .claude/): ${part.trim()}. ${ADVICE}`);
  }
}
