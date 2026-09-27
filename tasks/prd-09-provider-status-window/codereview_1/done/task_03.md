# Stable execution context

Load in this exact order:

1. `tasks/prd-09-provider-status-window/codereview_1/codereview.md`
2. This file

Use the already loaded report version; recover relevant contracts and code afterward. This order does not guarantee a host cache hit.

---

# T03 — Copilot and Cline scenarios for TC-05 and TC-09

## Outcome

Unit tests prove two things for Copilot and Cline snapshots, as they already do for quota snapshots. First, the status window's columns mirror `ProviderUsageRowFactory.CreateRows` in count, order, key, and label (TC-05). Second, `ProviderUsageRow.ResetTimeUtc` equals the instant behind `ResetText` at the Copilot and Cline reset sites (TC-09). This completes T01's acceptance.

## Dependencies and boundaries

- Depends on: — (T01 and T02 are in `done/`)
- Unblocks: re-review of the feature (`codereview_2`)
- In scope: new or extended test methods in `tests/TokenHound.Infrastructure.Tests/ViewModels/`, reusing the existing Copilot and Cline snapshot builders.
- Out of scope: any production code change (the sites are correct by inspection); CR-02 (alert colour for blocked accounts) and CR-03 (earliest vs latest comeback), which are HIL 3 decisions; T01/T02 contracts.

## Traceability

| Source | Section | Finding covered |
| --- | --- | --- |
| codereview_1/CR-01 | `codereview.md#findings` | TC-05 and TC-09 lack the Copilot and Cline scenarios; T01 acceptance incomplete |
| TC-05, TC-09 | `techspec.md#test-approach` | Planned unit scenarios |
| FR-06, FR-08, DEC-02 | `prd.md#functional-requirements`, `techspec.md#technical-decisions` | Obligations the tests prove |

## Requirements

- TC-05: a Copilot credit snapshot and a Cline snapshot projected with `ProviderStatusProjection.CreateAccount` produce columns whose `Key` and `Label` sequences equal `ProviderUsageRowFactory.CreateRows` for the same snapshot and clock. A Copilot row without `UsedFraction` shows its `PrimaryQuantityText` and no bar.
- TC-09: for a Copilot snapshot with `usage.Period.ResetUtc` set, the credit row's `ResetTimeUtc` equals that instant. For a Cline free-limit snapshot with `ActiveBlock.ResetTimeUtc` set, the `Free model limit` row's `ResetTimeUtc` equals that instant. A null reset gives a null `ResetTimeUtc`.
- Tests use fixed clocks and no `DateTime.Now`/`UtcNow`; they follow AGENTS.md test style and the existing file conventions.

## Context to recover on demand

- TechSpec: `techspec.md#test-approach` (TC-05, TC-09), `#quality-profile`.
- Rules and skills: `AGENTS.md` C# rules and MTP commands; `dotnet-efficient-validation`.
- Code:
  - `src/TokenHound.App/ViewModels/ProviderUsageRowFactory.Copilot.cs:33` and `ProviderUsageRowFactory.Cline.cs:63`: the reset sites under test.
  - `tests/TokenHound.Infrastructure.Tests/ViewModels/ProviderUsageRowFactoryCopilotTests.cs:CreateCopilotSnapshot` and `ProviderUsageRowFactoryClineTests.cs:CreateRows_WithClineAccountAndLocal_RendersDedicatedRows`: existing snapshot builders to reuse.
  - `tests/TokenHound.Infrastructure.Tests/ViewModels/ProviderStatusProjectionTests.cs:CreateAccount_WhenSnapshotHasWindows_ColumnsMirrorHudRows`: pattern for the mirroring assertion.

## Work

- [x] T03.1 Add `ResetTimeUtc` assertions for Copilot (period reset set) in `ProviderUsageRowFactoryCopilotTests` and for Cline (free-limit `ActiveBlock` reset set) in `ProviderUsageRowFactoryClineTests`, extending existing scenarios where they already set those resets (TC-09).
- [x] T03.2 Add Copilot and Cline mirroring tests to `ProviderStatusProjectionTests` (TC-05), including the Copilot usage-only case (no bar, quantity text).
- [x] T03.3 Build the test project; run the focused classes, then the full Infrastructure and Core test projects; run QA-01..QA-04 over the touched test files.

## Acceptance criteria

- New or extended tests exist and pass for each TC-05 and TC-09 scenario named in Requirements. Each one fails if its asserted field is changed (checked by reasoning over the assertion, not by mutating production code).
- The full `tests/TokenHound.Infrastructure.Tests` run passes with more than 819 tests, and `tests/TokenHound.Core.Tests` passes with 92.
- No file under `src/` changes.

## Verification

- Unit: the TC-05 and TC-09 scenarios above.
- Integration: none.
- E2E: omitted by .NET desktop policy.
- Manual: none.
- Environment dependency: none.
- Commands:
  - `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-restore --nologo --verbosity:minimal`
  - `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*ProviderStatusProjectionTests*"` (and the same with `*ProviderUsageRowFactoryCopilotTests*`, `*ProviderUsageRowFactoryClineTests*`)
  - `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests --no-build --no-restore -- --minimum-expected-tests 1`
  - `rtk dotnet test --project tests/TokenHound.Core.Tests --no-build --no-restore -- --minimum-expected-tests 1`
- Expected evidence: exit codes 0, test counts per command, `git status -- src` unchanged from the reviewed state.

## Affected files

- Modify: `tests/TokenHound.Infrastructure.Tests/ViewModels/ProviderStatusProjectionTests.cs`, `tests/TokenHound.Infrastructure.Tests/ViewModels/ProviderUsageRowFactoryCopilotTests.cs`, `tests/TokenHound.Infrastructure.Tests/ViewModels/ProviderUsageRowFactoryClineTests.cs`
- Create: —

## Observability and recovery

- Operational signal: none (tests only).
- Recovery: revert the test changes.

## Handoff

> Updated by `sdd-execute-corrections` during implementation.

- Produced result:
  - TC-09: `ResetTimeUtc` is asserted for the Copilot credit row (period reset set → equals `FIXED_NOW.AddDays(22)`; usage-only with null reset → null) and for the Cline rows (free-limit row → `ActiveBlock.ResetTimeUtc`; credits row → null), extending the existing scenarios.
  - TC-05: `CreateAccount_WhenCopilotCredits_ColumnsMirrorHudRows` (credit row plus a quota window) and `CreateAccount_WhenClineFreeLimit_ColumnsMirrorHudRows` (credits plus free-limit block) prove that the column keys and labels equal `CreateRows`. They also check that no-fraction columns show quantity text without a bar, and the reset lines `in 22 days · 10/18, 22:23` and `in 1 hour · 09/26, 23:53`.
- Changed files: modified `tests/TokenHound.Infrastructure.Tests/ViewModels/ProviderStatusProjectionTests.cs` (+2 tests, Copilot snapshot helper, `System.Collections.Generic` using), `ProviderUsageRowFactoryCopilotTests.cs` (+2 asserts), `ProviderUsageRowFactoryClineTests.cs` (+2 asserts; the file's existing CRLF working-copy endings were preserved). No `src/` file changed (`git status --porcelain -- src` count unchanged at 25).
- Checks:
  - `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-restore --nologo --verbosity:minimal`: 0 errors, 0 warnings.
  - Focused `--filter-class`: `*ProviderStatusProjectionTests*` 9 passed; `*ProviderUsageRowFactoryCopilotTests*` 4 passed; `*ProviderUsageRowFactoryClineTests*` 3 passed.
  - Full `tests/TokenHound.Infrastructure.Tests`: 821 passed (819 + 2), exit 0. `tests/TokenHound.Core.Tests`: 92 passed, exit 0.
  - QA-01..QA-04 over the three touched files: no hits.
- Validated state: git base `53da181` + T01/T02 working tree + this correction; Debug; net10.0 tests; run on 2026-09-27.
- Open items: none for CR-01. CR-02 and CR-03 stay HIL 3 decisions. Observation for CR-03's decision: a Cline free-limit block has no used fraction, so under DEC-06 the account is neither dimmed nor given `back in` (the reset line still shows the countdown).
