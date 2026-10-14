/*
 * The referee for the lab's own commands (npm run referee, npm run doctor). It is downloaded once into .lab/, outside
 * git, so an agent never reads it as part of the repository. To score the service, this script starts the built
 * service itself on a free port, waits until it answers, points the referee at it, and stops it again: on Windows
 * with taskkill, which also ends anything the service started.
 */
import { spawn, spawnSync } from 'node:child_process';
import { existsSync, mkdirSync, readFileSync, renameSync, rmSync, writeFileSync } from 'node:fs';
import net from 'node:net';

export const REFEREE_URL = 'https://agentcode.page/lab/referee.mjs';
export const REFEREE = '.lab/referee.mjs';
const SERVICE_DLL = 'src/Orders/bin/Debug/net10.0/Orders.dll';
const WIN = process.platform === 'win32';

/* A downloaded file is the referee only if it looks like it: a proxy's login page must not be saved as the referee. */
const looksLikeReferee = (text) => text.includes('const checks') && text.includes('async function main');

function save(text) {
  mkdirSync('.lab', { recursive: true });
  writeFileSync(`${REFEREE}.part`, text);
  renameSync(`${REFEREE}.part`, REFEREE);
}

async function viaFetch() {
  try {
    const res = await fetch(REFEREE_URL, { signal: AbortSignal.timeout(20_000) });
    if (!res.ok) return `HTTP ${res.status}`;
    const text = await res.text();
    if (!looksLikeReferee(text)) return 'the answer is not the referee (a proxy or a login page?)';
    save(text);
    return null;
  } catch (e) {
    return e.cause?.code ?? e.message;
  }
}

/* curl reads the proxy settings (HTTPS_PROXY) that Node's fetch ignores; Windows 10 and later ship it as curl.exe. */
function viaCurl() {
  mkdirSync('.lab', { recursive: true });
  const r = spawnSync('curl', ['-fsSL', '--max-time', '30', '-o', `${REFEREE}.part`, REFEREE_URL], { encoding: 'utf8' });
  if (r.error) return `curl: ${r.error.code ?? r.error.message}`;
  if (r.status !== 0) { rmSync(`${REFEREE}.part`, { force: true }); return (r.stderr || `curl: exit ${r.status}`).trim(); }
  const text = readFileSync(`${REFEREE}.part`, 'utf8');
  rmSync(`${REFEREE}.part`, { force: true });
  if (!looksLikeReferee(text)) return 'curl: the answer is not the referee (a proxy or a login page?)';
  save(text);
  return null;
}

/* The referee in .lab/, downloaded if it is not there yet: { ok, how } or { ok: false, why }. */
export async function ensureReferee() {
  if (existsSync(REFEREE) && looksLikeReferee(readFileSync(REFEREE, 'utf8'))) return { ok: true, how: 'already in .lab/' };
  const fetchError = await viaFetch();
  if (!fetchError) return { ok: true, how: `downloaded from ${REFEREE_URL}` };
  const curlError = viaCurl();
  if (!curlError) return { ok: true, how: `downloaded with curl from ${REFEREE_URL}` };
  return {
    ok: false,
    why: `cannot download ${REFEREE_URL} (Node: ${fetchError}; ${curlError}). `
      + `Open the address in a browser, save the file as .lab/referee.mjs in this folder, and run this again.`,
  };
}

/* The referee parses and starts: without arguments it prints its usage and exits 2, without touching anything. */
export function refereeRuns() {
  const parse = spawnSync(process.execPath, ['--check', REFEREE], { encoding: 'utf8' });
  if (parse.status !== 0) {
    const error = parse.stderr.split('\n').find((l) => /Error/.test(l)) ?? `exit ${parse.status}`;
    return { ok: false, why: `.lab/referee.mjs does not parse (${error.trim()}). Delete it and run this again.` };
  }
  const usage = spawnSync(process.execPath, [REFEREE], { encoding: 'utf8', timeout: 20_000 });
  if (usage.status !== 2 || !usage.stderr.includes('usage:')) return { ok: false, why: `.lab/referee.mjs did not start as expected (exit ${usage.status}). Delete it and run this again.` };
  return { ok: true };
}

function freePort() {
  return new Promise((resolve, reject) => {
    const s = net.createServer();
    s.once('error', reject);
    s.listen(0, () => { const { port } = s.address(); s.close(() => resolve(port)); });
  });
}

export function portInUse(port) {
  return new Promise((resolve) => {
    const s = net.createServer();
    s.once('error', (e) => resolve(e.code === 'EADDRINUSE'));
    s.listen(port, () => s.close(() => resolve(false)));
  });
}

export const HOW_TO_FREE_3000 = WIN
  ? 'To free it: netstat -ano | findstr :3000, then taskkill /pid <the number in the last column> /T /F'
  : 'To free it: lsof -ti tcp:3000 | xargs kill';

/* Stops the service and everything it started. */
export function stopService(child) {
  if (child.exitCode !== null || child.signalCode !== null) return;
  if (WIN) spawnSync('taskkill', ['/pid', String(child.pid), '/T', '/F'], { stdio: 'ignore' });
  else child.kill('SIGTERM');
}

async function answers(base) {
  try {
    await fetch(`${base}/orders/1001`, { headers: { 'X-Customer-Id': '1' }, signal: AbortSignal.timeout(2_000) });
    return true;
  } catch {
    return false;
  }
}

/* Starts the built service with fresh seed data on a free port; resolves when it answers, rejects with its log if it doesn't. */
export async function startService() {
  const port = await freePort();
  const staff = /const STAFF = '([^']+)'/.exec(readFileSync(REFEREE, 'utf8'))?.[1];
  const env = { ...process.env, PORT: String(port), ...(staff ? { STAFF_TOKEN: staff } : {}) };
  const child = spawn('dotnet', [SERVICE_DLL], { env, stdio: ['ignore', 'pipe', 'pipe'] });
  let log = '';
  child.stdout.on('data', (d) => { log += d; });
  child.stderr.on('data', (d) => { log += d; });
  const exited = new Promise((resolve) => { child.once('exit', resolve); child.once('error', (e) => { log += e.message; resolve(); }); });
  const base = `http://127.0.0.1:${port}`;
  for (const until = Date.now() + 60_000; Date.now() < until;) {
    if (await answers(base)) return { child, base, port };
    const gone = await Promise.race([exited.then(() => true), new Promise((r) => { setTimeout(() => r(false), 250); })]);
    if (gone) throw new Error(`the service stopped before it answered:\n${log.trim()}`);
  }
  stopService(child);
  throw new Error(`the service did not answer within a minute:\n${log.trim()}`);
}
