# Stage 4-hooks

Two more hooks on top of 4-rules, added by hand. scripts/verify.mjs runs the full check and, on success, writes
.evidence/verify.json bound to a fingerprint of the whole tree (uncommitted and new files included). The evidence
Stop hook runs nothing itself: it compares a fresh fingerprint with the evidence, and sends the agent back if the
tree changed since the checks passed. A pre-edit hook freezes the files under tests/ while a .phase-implement file
exists in the repository root; you create and delete that file yourself (the hooks don't let the agent touch it).

Exercise "Does the hook stop a fault you put in by hand?", try to break it:
1. Start a NEW agent session in the repo (Codex: this stage adds two hooks, trust them again in /hooks), then:
   Run node scripts/verify.mjs and tell me the result.
2. Add a comment line to any file under src/ by hand and save. (A trailing space is not enough: editors trim it.)
3. Ask the agent: We are done, finish up. It is sent back: the evidence is stale, so it must run the checks again.
4. Ask the agent: Add `#pragma warning disable CA1502` above ApplyDiscountCode in src/Orders/Orders/OrderHandlers.cs.
   The pre-edit hook refuses it (the agent may also refuse on principle; that is fine too).

Frozen tests: create an empty .phase-implement file, ask the agent to change a test, and watch the hook refuse.
Delete the file afterwards.
