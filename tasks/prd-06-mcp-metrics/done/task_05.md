# Stable execution context

Load in this exact order:

1. `tasks/prd-06-mcp-metrics/workflow.md` — DEC-23 and ACC-01
2. `tasks/prd-06-mcp-metrics/techspec.md` — DEC-24, CMP-09, window projection, TC-07
3. This file

---

# T05 — Separate used counts from remaining units

## Outcome

No provider stores a used-request count in `LimitWindow.RemainingUnits`. Antigravity's daily request count reaches the HUD with identical text and reaches MCP clients as `usedUnits`, with `remainingUnits` absent.

## Dependencies and boundaries

- Depends on: T01–T03 (complete), `codereview_2` (APPROVED), and exception HIL approval of DEC-24 and this contract.
- Unblocks: independent re-review of the T05 change, then HIL 3.
- In scope: `LimitWindow.UsedUnits`, the Antigravity transcript window, the HUD's derived used-count branches, the MCP window DTO and mapping, `docs/MCP.md`, and tests.
- Out of scope: other provider adapters, polling cadence, HUD text and geometry, archive migration, the Mock provider's official remaining-count window, and new MCP tools.

## Traceability

| Source | Section | Obligation |
| --- | --- | --- |
| ACC-01 | `workflow.md#acc-01--gemini-request-count-exposed-as-remainingunits` | Stop reporting a used count as remaining. |
| DEC-23, DEC-24 | `workflow.md#dec-23`, `techspec.md#technical-decisions` | Root fix through Core, adapter, HUD, and MCP. |
| NFR-03, FR-05 | `prd.md` | Truthful window values. |
| CMP-09, TC-07 | `techspec.md` | Components and unit evidence. |

## Work

- [x] T05.1 Add `long? UsedUnits { get; init; }` to `LimitWindow` with an XML comment (units consumed in the window when the provider reports a count, typically without a published limit).
- [x] T05.2 `AntigravityUsageProvider.Snapshots.cs:CreateTranscriptSnapshot`: set `UsedUnits = requestsToday`, `RemainingUnits = null`; leave the fidelity, status, and name unchanged.
- [x] T05.3 HUD: `ProviderRingViewModel.Status.cs:ResolveStatusMessage`, `ProviderRingViewModel.cs` session text, and `ProviderUsageRowFactory.cs:ResolveQuotaPrimaryText` read `UsedUnits` for the derived used-count case; keep their text and every other branch.
- [x] T05.4 MCP: add `UsedUnits` to `McpLimitWindowMetrics` (XML-documented, camel-case `usedUnits`) and map it in `McpMetricsReader.MapWindow`; update `docs/MCP.md` window fields and semantics.
- [x] T05.5 Tests (TC-07): update the existing Antigravity assertions (`RemainingUnits` → `UsedUnits`, `RemainingUnits` null); add or update HUD tests proving unchanged text for the derived used-count window; add an MCP projection assertion for `usedUnits` with `remainingUnits` null.
- [x] T05.6 Build App and Infrastructure tests; run scoped MTP classes (`*Mcp*`, `*Antigravity*`, `*ProviderRingViewModel*`, `*ProviderUsageRowFactory*`, `*DomainModels*`); run QA-01–QA-07 over the touched files.

## Acceptance criteria

- The Antigravity snapshot carries its count only in `UsedUnits`; `RemainingUnits` is null.
- The HUD shows the same Antigravity text as before ("~N requests today · no limit published" and "~N requests").
- MCP `list_provider_metrics` for `gemini` returns `usedUnits: N` with no `remainingUnits`; other providers' windows are unchanged.
- `docs/MCP.md` documents `usedUnits` and states that it is never a remaining count.
- Scoped tests pass with `--minimum-expected-tests 1`; builds have zero warnings; no new blocking QA hit.

## Verification

- Unit: TC-07 plus the existing TC-01 projection tests.
- Integration: the scoped `*Mcp*` run includes the real SSE client tests.
- E2E: omitted by .NET desktop policy.
- Manual: re-run TC-06 step 2 for the Antigravity ring (HUD tooltip against `list_provider_metrics`); the other manual steps remain valid from DEC-22.
- Commands: `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-restore --nologo --verbosity:minimal`; `rtk dotnet build src/TokenHound.App/TokenHound.App.csproj --no-restore --nologo --verbosity:minimal`; `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "<pattern>"` per pattern above; Core tests if `DomainModelsTests` covers `LimitWindow`. Preserve `$LASTEXITCODE`.

## Affected files

- Modify: `src/TokenHound.Core/Models/LimitWindow.cs`, `src/TokenHound.Infrastructure/Providers/Antigravity/AntigravityUsageProvider.Snapshots.cs`, `src/TokenHound.App/ViewModels/ProviderRingViewModel.cs`, `ProviderRingViewModel.Status.cs`, `ProviderUsageRowFactory.cs`, `src/TokenHound.Infrastructure/Mcp/McpLimitWindowMetrics.cs`, `McpMetricsReader.cs`, `docs/MCP.md`, and the tests named in T05.5.
- Create: none required.

## Observability and recovery

- No new logging. Reverting T05 restores the prior field usage; the archive needs no migration (see the TechSpec risk on pre-fix archived readings).

## Handoff

> Updated by the executing coordinator session under `sdd-orchestrate-tasks`.

- Produced result: `LimitWindow.UsedUnits` (nullable, XML-documented) carries used counts. The Antigravity transcript window sets `UsedUnits = requestsToday` and `RemainingUnits = null`. The three HUD derived-count branches read `UsedUnits` with unchanged text. `McpLimitWindowMetrics.UsedUnits` (`usedUnits`) is mapped in `McpMetricsReader.MapWindow`. `docs/MCP.md` documents `usedUnits` and says it is never a remaining count.
- Changed files: `src/TokenHound.Core/Models/LimitWindow.cs`, `src/TokenHound.Infrastructure/Providers/Antigravity/AntigravityUsageProvider.Snapshots.cs`, `src/TokenHound.App/ViewModels/ProviderRingViewModel.cs`, `ProviderRingViewModel.Status.cs`, `ProviderUsageRowFactory.cs`, `src/TokenHound.Infrastructure/Mcp/McpLimitWindowMetrics.cs`, `McpMetricsReader.cs`, `docs/MCP.md`, `tests/TokenHound.Infrastructure.Tests/Providers/Antigravity/AntigravityUsageProviderTests.cs`, `ViewModels/ProviderRingViewModelTests.cs`, `ViewModels/ProviderUsageRowFactoryTests.cs`; created `tests/TokenHound.Infrastructure.Tests/Mcp/McpWindowProjectionTests.cs`.
- Checks: builds of `TokenHound.Infrastructure.Tests`, `TokenHound.App`, and `TokenHound.Core.Tests` with `--no-restore` passed with 0 errors and 0 warnings. Scoped MTP runs (`--no-build --no-restore -- --minimum-expected-tests 1 --filter-class`): `*Mcp*` 22 passed (20 existing + 2 TC-07); `*ProviderRingViewModel*` 19 passed; `*ProviderUsageRowFactory*` 23 passed; Core `*DomainModels*` 10 passed; `*Antigravity*` 40 passed and 1 failed. The failure is the pre-existing live test `GetSnapshotAsync_LiveIntegration_WhenAgyRunning_ReturnsOfficialMetrics`: it asserts `Official` fidelity (line 55) against the real running `agy`, got `Derived`, and was already recorded in `done/task_02.md`. T05 does not touch fidelity.
- Manual: re-ran TC-06 step 2 for Antigravity on the rebuilt Debug App (2026-09-22 10:43). `acceptance/t05_gemini.txt` shows `Requests Today` with `usedUnits: 0` and no `remainingUnits`. The HUD tooltip still reads "~0 requests", "No limit published", and "~0 requests today · no limit published". The dev instance was exited from the tray; the user's installed copy remains closed.
- Quality profile: QA-01–QA-05 had no hits in added lines or the new test file. QA-06 matched only the new test's `DateTimeOffset` constructor (lexical false positive). QA-07 reservation: `AntigravityUsageProviderTests.cs` went from 311 to 314 lines, above the 300-line style target before T05 and still below the 500-line profile threshold; every other touched file is at or below 282 lines. `git diff --check` passed.
- Validated state: current uncommitted worktree on Git base `97f17c7`, Debug, .NET SDK 10.0.401, Windows.
- Open items: archived pre-T05 Antigravity readings keep the count in `RemainingUnits` until the first refresh after upgrade (TechSpec risk, accepted in DEC-25). This session authored T05, so the re-review must run in another session.

### ADR candidates

None; DEC-24 is a direct TechSpec decision.
