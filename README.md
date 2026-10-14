# Guardrails lab · order service in C#

The hands-on repository of session 2 of Agentic Coding (agentcode.page/2): one small order service (customers,
orders, a bulk discount, shipping, an in-memory SQLite database), one open ticket (`docs/TICKET-142.md`), and the
guardrails we put around one coding agent, stage by stage.

## Before the session

You need:

- the .NET 10 SDK (`dotnet --version` prints 10.x);
- Node 22.18 or later, with npm (Node 24 LTS recommended): the lab's own commands and two check tools run on it;
- git;
- your coding agent (Claude Code or Codex CLI), signed in and working.

No admin rights and nothing global beyond these. Then:

```bash
git clone https://github.com/konradkiss/prodigy-day2-lab.git
cd prodigy-day2-lab
npm ci
npm run doctor
```

`doctor` prints one line per check and ends with "All green. You are ready." If something is red, tell us before
the session.

The build warns that `System.Linq.Dynamic.Core` 1.5.1 has a known vulnerability. That is expected: the vulnerable
package is one of the planted problems. Don't update it by hand; the session builds on it.

## In the session: every step is one command

| Command | What it does |
|---|---|
| `npm run stages` | lists the stages of the session and marks where you are |
| `npm run stage -- <name>` | switches to a stage (e.g. `npm run stage -- 4-guarded`); your current work is committed first, nothing is lost |
| `npm run referee` | scores the service with the referee: 23 black-box checks over HTTP |
| `npm run mutate` | a colleague's one-character change at the free-shipping threshold; then `dotnet test`: do the tests notice? |
| `npm run restore` | undoes the mutation |
| `npm run stryker` | mutation testing with Stryker.NET: hundreds of such small changes, and which ones the tests notice |
| `npm run lock-rules` | after the rules exercise: the hooks protect `rules/`, `rule-tests/` and `sgconfig.yml`, so the agent can't loosen a rule |
| `npm run doctor` | is this laptop ready? |
| `dotnet test`, `npm start` | the unit tests; the service on http://localhost:3000 |

Every stage prints its own short note (`docs/STAGE.md`) when you switch to it: what is new, which exercise runs
here, what comes next.

Start your agent in the repository root, and start a new session after every `npm run stage`: guidance files are
read when a session starts. From the `4-guarded` stage on, the hooks are wired for both agents: Claude Code reads
`.claude/settings.json`, Codex CLI reads `.codex/hooks.json`. Codex skips new hooks until you trust them once: type
`/hooks` in Codex and approve them.

Use your own agent as you normally do. Your personal configuration (a global CLAUDE.md, rules, hooks) also shapes
what the agent does here, so your result may differ from the reference. Every stage branch holds the reference
result of its step, so you can always compare.

## The service

An HTTP API, no user interface. `npm start` serves it on http://localhost:3000. The database is in memory and
starts from the same seed every time: three customers (Alice, Bob, Carol), five products (mug, tee, cap, hoodie,
socks), seven orders, three discount codes. Signing in is a header: `X-Customer-Id: 1` means you are Alice.

| Request | What it does |
|---|---|
| `GET /orders/1002` | the order, priced: items, subtotal, bulk discount, shipping, total |
| `POST /orders/1002/items` `{"sku":"CAP","qty":1}` | adds an item to an open order |
| `DELETE /orders/1002/items/MUG` | removes an item |
| `GET /orders/1002/receipt` | a plain-text receipt with the customer's name and email |
| `GET /warehouse/orders?status=open` | the packing team's list, with an `X-Staff-Token` header |
| `POST /orders/1002/discount` `{"code":"SAVE15"}` | applies a discount code: this is the ticket, and it exists only on the stages where the ticket is done |

Pricing: ten or more of one product gives 5% off that line, five or more socks 10%; shipping is €4.90 and free from
€50.00 of goods; a discount code takes its percentage off the goods after the bulk discount, and free shipping is
decided after that. Every amount is a whole number of cents, rounded half up (`docs/money.md`).

```bash
npm start
curl -H 'X-Customer-Id: 1' localhost:3000/orders/1002
curl -H 'X-Customer-Id: 1' -H 'Content-Type: application/json' \
     -d '{"sku":"CAP","qty":1}' localhost:3000/orders/1002/items
```

The lab is small on purpose, under 600 lines at the start, so you can take it in within minutes, and the planted
problems (missing ownership checks, SQL built from strings, two crashes, a vulnerable package, a key in the code)
can be found by reading.

## Credits

The stage design follows Felhő's (Gergely Hodicska) guardrails lab for TypeScript,
[github.com/felho/guardrails-lab](https://github.com/felho/guardrails-lab), which is in turn derived from Konrad
Kiss's six-language lab. The referee is kept outside the repository on purpose, so that agents can't read it.

## For organisers

How the branches relate, and how to change things without breaking a stage:

- `main` is the C# lab exactly as downloaded, one commit, never edited.
- `stage/0-start` is the root of the session: the lab plus the shared tooling (`scripts/lab.mjs`,
  `scripts/doctor.mjs`, `package.json`, the Stryker.NET tool manifest, this README). The stages build on each
  other in a chain: `0-start` → `1-bare` and `3-guidance` → `4-guarded` → `4-guarded-ticket` → `4-rules` →
  `4-hooks`. `messy` and `nice` branch from `main`.
- **Shared tooling changes go on `stage/0-start`, then merge forward through the whole chain.** After each merge,
  check `git diff --name-only --diff-filter=U` before adding anything. `docs/STAGE.md` differs on every stage on
  purpose: on a conflict, keep the stage's own version.
- **From `4-guarded` on, the hooks protect `scripts/`.** A script that is fine on `0-start` must also pass the
  checks of the later stages, because the agent can't fix it there.
- The agent-made stages (`1-bare`, `4-guarded`, `4-guarded-ticket`, `4-rules`) are headless Claude Code runs
  (Opus 5.5) with only the repository's configuration loaded (`--setting-sources project,local`, no MCP servers).
  Each stage note says what the agent did and what a human added. If the ticket text changes, those stages must be
  regenerated, because they are answers to that exact text.
