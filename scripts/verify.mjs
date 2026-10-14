#!/usr/bin/env node
/*
 * Runs the mandatory checks and, on success, writes evidence bound to a fingerprint of the working
 * tree. The Stop hook compares a fresh fingerprint against it: a green run on older code is no proof.
 *
 *   node scripts/verify.mjs            # run checks, write .evidence/verify.json on success
 *   node scripts/verify.mjs --check    # exit 0 if valid evidence exists for the current tree
 */
import { createHash } from 'node:crypto';
import { execFileSync, spawnSync } from 'node:child_process';
import { existsSync, mkdirSync, readFileSync, writeFileSync } from 'node:fs';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const EVIDENCE_DIR = join(ROOT, '.evidence');
const EVIDENCE = join(EVIDENCE_DIR, 'verify.json');
const CHECKS = [[process.execPath, ['scripts/check.mjs'], 'node scripts/check.mjs']];

/* Every tracked and untracked file except the evidence itself and ignored paths, hashed by git. */
function fingerprint() {
  const git = (args, input) => execFileSync('git', args, { cwd: ROOT, input, maxBuffer: 64 * 1024 * 1024 }).toString();
  const deleted = new Set(git(['ls-files', '--deleted', '-z']).split('\0').filter(Boolean));
  const list = git(['ls-files', '--cached', '--others', '--exclude-standard', '-z']).split('\0')
    .filter((f) => f && !f.startsWith('.evidence/') && !deleted.has(f)).sort();
  const hashes = list.length ? git(['hash-object', '--stdin-paths'], `${list.join('\n')}\n`).trim().split('\n') : [];
  const hash = createHash('sha256');
  for (const [i, h] of hashes.entries()) hash.update(`${list.at(i)} ${h}\n`);
  return hash.digest('hex');
}

function checkEvidence() {
  if (!existsSync(EVIDENCE)) { console.error('no evidence: run node scripts/verify.mjs'); process.exit(1); }
  const e = JSON.parse(readFileSync(EVIDENCE, 'utf8'));
  if (e.fingerprint !== fingerprint()) { console.error(`evidence is stale: the tree changed since the checks passed (${e.at})`); process.exit(1); }
  console.log(`evidence valid: checks passed at ${e.at} on this exact tree`);
  process.exit(0);
}

if (process.argv.includes('--check')) checkEvidence();

const before = fingerprint();
for (const [cmd, args, name] of CHECKS) {
  const r = spawnSync(cmd, args, { cwd: ROOT, encoding: 'utf8' });
  if (r.status !== 0) { console.error(`${name} failed:\n${`${r.stdout}${r.stderr}`.trim().split('\n').slice(-30).join('\n')}`); process.exit(1); }
  console.log(`✓ ${name}`);
}
if (fingerprint() !== before) { console.error('the tree changed while the checks ran; run again'); process.exit(1); }
mkdirSync(EVIDENCE_DIR, { recursive: true });
writeFileSync(EVIDENCE, `${JSON.stringify({ at: new Date().toISOString(), fingerprint: before, checks: CHECKS.map(([, , name]) => name) }, null, 2)}\n`);
console.log('evidence written to .evidence/verify.json');
