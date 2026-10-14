/* Shared by the hooks: read the hook's JSON from stdin, and block (exit 2, reason on stderr) or allow. */
import { readFileSync } from 'node:fs';

export function readInput() {
  try {
    return JSON.parse(readFileSync(0, 'utf8') || '{}');
  } catch {
    return {};
  }
}

export function block(message) {
  process.stderr.write(`${message.trim()}\n`);
  process.exit(2);
}
