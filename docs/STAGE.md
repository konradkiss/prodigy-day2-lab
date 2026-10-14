# Stage 4-rules

Two sentences became checks: "SQL is never built from strings; values always travel as parameters" and "if a method
in src/Orders/Orders/ loads an order, it checks that the order belongs to the signed-in customer". Each is an
ast-grep rule in rules/ with its own must-flag and must-pass case in rule-tests/. The agent could not edit
scripts/check.mjs (the hooks protect it), so it handed over the lines and the human added them, then ran
`npm run lock-rules`, so the hooks now protect the rules too.

The scan found three methods that load an order without checking its owner: RemoveItemFromOrder,
Receipts.BuildReceipt (the helper behind GetReceipt: GetReceipt never calls FindOrder, its helper does) and ShowOrder.
The agent fixed all three and added four tests (44 in all). The SQL rule found nothing: the .NET analyzers had already
removed SQL built from strings on 4-guarded.

The referee now passes all 23 checks.

Exercises here: "GetReceipt never calls FindOrder" (the helper gap), "Testing the tests": `npm run mutate`, then
`dotnet test`. Even here the 44 tests stay green: none of them prices an order of exactly €50.00. `npm run stryker`
shows that mutant and the others that survive. `npm run restore` afterwards.
Score: `npm run referee`. Next: `npm run stage -- 4-hooks`.
