# Stage 0-start

The untouched order service: customers, orders, a bulk discount, shipping, an in-memory database.
The open ticket is docs/TICKET-142.md. Nothing checks the agent's work yet.

Exercises here: "Is your doctor green?" (`npm run doctor`), "The ticket", "Find the ambiguities, write Gherkin"
(start your agent in the repo, paste the prompt).
Next: `npm run stage -- 1-bare` to read what the agent did with no guardrails; after the Gherkin exercise here,
`npm run stage -- 4-guarded`.

Command approvals: Claude Code asks before it runs a command such as `dotnet test`, `dotnet build` or `npm run check`.
When it asks, choose "Yes, and don't ask again": it then runs that command in this project without asking, and the
exercises go faster.
