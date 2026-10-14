# Stage 4-guarded

One prompt built this (docs/guardrail-prompt.md): Directory.Build.props (nullable, warnings as errors, the .NET
analyzers at latest-all with code style in the build, complexity at most 10 per method, the NuGet audit as an
error), secretlint with a small rule of its own for the planted key, scripts/check.mjs (`npm run check`), and four
hooks under scripts/hooks/ (check after every edit, no silencing, no --no-verify, the full check before finishing).
The existing code was fixed where the checks complained. The ticket is NOT done yet.

What the checks found in the existing code: the vulnerable System.Linq.Dynamic.Core (removed), the hard-coded staff
token (now only from STAFF_TOKEN), SQL built from strings in two places (CA2100: now parameters), the two crashes
(nullable warnings: now 401 and 404), one method over the complexity limit. They did not find the two missing
owner checks (remove-item and the receipt): nothing generic knows whose order it is. The agent listed both as out
of scope.

Its own hooks went live during the run and refused its later edits to scripts/, so it handed three hook fixes to
the human as a patch; they are the next commit. Codex hooks (.codex/hooks.json) were added by hand.

Exercises here: "The guardrail prompt, part by part" (read the prompt and the diff), "The ticket on the guarded
stage" (start a NEW agent session in the repo, then: Implement docs/TICKET-142.md.)
Codex users: trust the hooks once with /hooks, or Codex skips them.
If the agent asks a question, answer it; that is the CLAUDE.md working. Score: `npm run referee` (14 of 23: the
ticket is not done yet). Next: `npm run stage -- 4-guarded-ticket`.
