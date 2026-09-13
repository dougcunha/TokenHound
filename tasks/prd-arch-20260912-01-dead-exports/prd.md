# PRD — Refactoring orphaned public members

## Context and motivation

The 2026-09-12 architectural analysis (`.audits/architectural-analysis-20260912-174200.md`, AA-01..AA-04) confirmed four `public` members in `TokenHound.Infrastructure` that are never referenced anywhere in `src/` or `tests/`. They are aliases or convenience surfaces with no caller, so they enlarge the maintainable API without carrying behavior. Removing them in one small refactoring keeps the provider surface honest.

## Scope

- Target: four orphaned members — `CopilotPassDispatchBudget.RemainingDispatches`, `CopilotApiClient.GetUsageAsync`, `CopilotCredentialDiscovery.DiscoverCredentialAsync`, `CodexAuthDiscovery.DiscoverAccountAsync`.
- Allowed structural change: delete exactly these members (adjusting any now-unused `using`/doc references). No other signature changes.
- Out of scope: `ApplicationInfo.Get` (AA-05) and `installer/build-installer.ps1` (AA-06) are pending owner decisions; exception hierarchies (AA-11) and every duplication group are separate workstreams.

## Behaviors to preserve

| ID | Observable behavior | Source and evidence | Verification |
| --- | --- | --- | --- |
| R-01 | `CopilotPassDispatchBudget.TryAcquire` / `CanDispatch` / `DispatchesUsed` keep their current semantics; only `RemainingDispatches` is removed. | `CopilotHistoricalReportCollector.cs:67,151`; `CopilotMetricsClientTests.cs:191`; `CopilotPassDispatchBudget.cs:29,41,48` | Focused Copilot test classes pass. |
| R-02 | `CopilotApiClient.GetQuotaAsync` remains the public quota entry with an unchanged result; only the `GetUsageAsync` alias is removed. | `CopilotUsageProvider.cs:178`; `CopilotApiClientTests.cs` | Copilot API-client tests pass. |
| R-03 | `CopilotCredentialDiscovery.DiscoverAsync` is unchanged; only the `DiscoverCredentialAsync` alias is removed. | `CopilotUsageProvider.cs:128`; `CopilotCredentialDiscoveryTests.cs` | Copilot credential tests pass. |
| R-04 | `CodexAuthDiscovery.DiscoverAsync` is unchanged; only the `DiscoverAccountAsync` alias is removed. | `CodexUsageProvider.cs:59`; `CodexAuthDiscoveryTests.cs` | Codex tests pass. |
| R-05 | No external consumer references the removed members; the assembly is an application dependency, not a shipped library. | exhaustive `rg` over `src` + `tests`; no NuGet packaging in the solution | `dotnet build` succeeds with zero errors. |

## Constraints

- Public-API caveat: each member is `public`. Removal is valid only because the repository ships an application. If out-of-repo consumers exist, stop and keep the member.
- Preserve XML documentation on the remaining members.
- No behavioral change; the target is removal only.

## Acceptance criteria

- [ ] All `R-NN` items were checked after refactoring.
- [ ] No new behavior entered the scope silently.
- [ ] Infrastructure and Core test projects pass with `--minimum-expected-tests 1`.
- [ ] Reversal is a single `git revert` of the removal commit.

## Assumptions and open items

- Assumption: no external consumer depends on these `Infrastructure` public members.
- Open item: none blocking; AA-05/AA-06 remain owner decisions outside this refactoring.
