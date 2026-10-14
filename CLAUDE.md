# Order service

Read `docs/money.md` before touching prices.

## Done means

- `npm run check` is green (build with analyzers, tests).
- Every point of the ticket has a test that would fail without the change.
- Nothing outside the ticket's scope changed. If you see an unrelated problem, list it at the end; do not fix it silently.

## Scope

- While implementing, add the ticket's tests under `tests/` and leave the existing tests as they are. If an existing test is wrong, say so and stop.
- Treat `scripts/`, `.claude/`, `.codex/` and the check configuration as read-only.

## When unsure

When a ticket point allows two readings, ask the user which one they mean before you implement it.
