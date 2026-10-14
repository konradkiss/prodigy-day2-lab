# Stage 1-bare

The agent implemented TICKET-142 without a CLAUDE.md, checks or hooks.
The ticket's ten points are correct, and it wrote 14 new tests (25 in all). It left everything else as it was.
Ask your agent to review the change (the prompt is on the slide) and compare its list with the referee.

Exercises here: "Find what's wrong" (your agent reviews, `npm run referee` scores), "Testing the tests"
(`npm run mutate`, `dotnet test`, `npm run restore`; then `npm run stryker` for all such changes at once),
"Run the reviewer" (start your agent in the repo, paste the review prompt).
Score it: `npm run referee` (18 of 23). Next: `npm run stage -- 3-guidance`.
