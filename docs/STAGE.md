# Stage 4-guarded-ticket

The ticket, implemented under the hooks of 4-guarded. The two questions the agent asked in the TypeScript lab were answered in
the prompt up front: the minimum is re-checked every time the order is priced, a code that no longer qualifies stays
with a discount of 0, but applying a code that does not qualify right now is still refused; dates are in UTC.
npm run check is green; 40 tests (22 new).

Worth knowing: the agent wrote every file through Bash, never with its Edit or Write tools, so the after-edit hooks
never saw a change. Only the Stop hook's full check, at the end, ran on its work.

The referee gives 21 of 23. The two open items are the ones no general check can see: ownership of remove-item and
of the receipt. (In the TypeScript lab the warehouse SQL was open here too; in C# the .NET analyzers' CA2100 caught
it on 4-guarded.)

Exercises here: "A sentence becomes a check" (start your agent, paste the rules prompt). The agent can't edit
scripts/check.mjs, the hooks protect it, so it will tell you what to add: you run `npm run wire-rules`, which adds the
rule tests and the scan to the check and locks the rules. Then `npm run check`. Next: `npm run stage -- 4-rules`.
