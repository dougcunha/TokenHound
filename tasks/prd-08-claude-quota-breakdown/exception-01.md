# EX-01 — Preserve an API-reported percentage without a unit denominator

## Finding

The approved TechSpec requires Claude windows to have `UsedFraction = reported percent / 100` while `TotalUnits` and `RemainingUnits` remain null. `LimitWindow.UsedFraction` currently returns null whenever `TotalUnits` is null (`src/TokenHound.Core/Models/LimitWindow.cs:23-29`), even when a non-null fraction was supplied. Therefore T01 cannot meet FR-01, FR-06, and NFR-03 under the approved technical plan.

`tests/TokenHound.Core.Tests/Models/DomainModelsTests.cs:15-35` explicitly asserts that an assigned fraction is ignored without `TotalUnits`. The current Claude mapper sets `TotalUnits = 100` and a computed `RemainingUnits`, which represents percentages as invented counts. Continuing with that pattern would conceal the contract defect and contradict the approved PRD.

## Evidence and impact

- `LimitWindow.cs` is 64 lines. `DomainModelsTests.cs` is 302 lines and already exceeds the project 300-line limit by two lines.
- A scoped audit of provider assignments found no existing production provider that sets a non-null `UsedFraction` with null `TotalUnits`. Existing remaining-only paths set `UsedFraction = null`. This limits current behavior changes but does not prove external consumers never construct that combination.
- This is a Core model contract change, whereas approved TechSpec DEC-06 says no Core model change and T01 names only Infrastructure files. The HIL 2 approval therefore does not cover it.
- No implementation code or tests have been changed under T01, and no build or test has run in this implementation turn.

## Proposed amendment (recommended)

1. Change `LimitWindow.UsedFraction` to return the explicitly assigned nullable fraction independently of `TotalUnits`; update its XML contract. Keep `null` when no fraction was supplied. Providers with only a remaining count must continue assigning `null`.
2. Move the existing `LimitWindow` tests from `DomainModelsTests.cs` into a new `LimitWindowTests.cs` so each touched C# file stays under 300 lines. Replace the contradictory expectation with two cases: remaining-only yields null, and an API-supplied fraction with null total is preserved. Keep tests with a real total and record copy semantics.
3. Amend TechSpec DEC-03/CMP-02/contracts, its Terrain baseline and TC-03, plus T01 scope/affected files and validation. Add a focused `TokenHound.Core.Tests` build and native MTP run with `--minimum-expected-tests 1`, then run the Infrastructure checks already planned. T02 and the product scope remain unchanged.
4. Execute the amended T01 only after this exception is approved. Re-hash the amended TechSpec and task contract in the checkpoint, linking this decision in `workflow.md`.

## Verification and reversal

- Verify the Core model's explicit-percentage and remaining-only cases, existing Core model tests, Claude percentage-only snapshots and rows, and the affected provider tests. `TotalUnits` and `RemainingUnits` must remain null for Claude percent-only windows.
- Review all changed files against the 300-line project limit and TechSpec quality profile. There is no data migration or credential change. Reversal is a source rollback of this Core contract amendment and its dependent Claude mapping.

## Alternatives considered

- Set `TotalUnits = 100` for a percentage: rejected because it presents a percentage scale as an API-published unit limit and causes fabricated remaining-count text.
- Add a second percentage property beside `UsedFraction`: possible, but it duplicates the same measurement and would require extra projection logic in Core and App without improving the contract.

## Decision requested

Approve the proposed Core contract amendment and its focused test extraction as an exception to the approved TechSpec/T01. No broader refactoring or product-scope change is requested.
