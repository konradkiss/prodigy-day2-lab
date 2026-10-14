/* Shared by the hooks: read the hook's JSON from stdin, and block (exit 2, reason on stderr) or allow. */
import { readFileSync } from 'node:fs';

/* A guard must not let a change through because it could not read what the change is: strict blocks instead. */
export function readInput({ strict = false } = {}) {
  try {
    return JSON.parse(readFileSync(0, 'utf8') || '{}');
  } catch (e) {
    if (strict) block(`Refused: the hook could not read its input (${e.message}).`);
    return {};
  }
}

export function block(message) {
  process.stderr.write(`${message.trim()}\n`);
  process.exit(2);
}
