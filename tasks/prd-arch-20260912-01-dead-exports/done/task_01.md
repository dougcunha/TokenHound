# Stable execution context

Load in this exact order:

1. `tasks/prd-arch-20260912-01-dead-exports/prd.md`
2. `tasks/prd-arch-20260912-01-dead-exports/techspec.md`
3. This file

Use the current PRD and TechSpec versions already loaded; recover only missing or changed sources. This order does not guarantee a host cache hit.

---

# T01 — Remove orphaned public members

## Outcome

The four unreferenced members no longer exist in `TokenHound.Infrastructure`, and the solution still builds.

## Dependencies and boundaries

- Depends on: —
- Unblocks: T02
- In scope: delete `CopilotPassDispatchBudget.RemainingDispatches`, `CopilotApiClient.GetUsageAsync`, `CopilotCredentialDiscovery.DiscoverCredentialAsync`, `CodexAuthDiscovery.DiscoverAccountAsync`.
- Out of scope: `ApplicationInfo.Get` (AA-05), `build-installer.ps1` (AA-06), all duplication groups.

## Traceability

| Source | Section | Obligation covered |
| --- | --- | --- |
| R-05 | `prd.md#behaviors-to-preserve` | Only unreferenced members removed |
| DEC-01..DEC-04 | `techspec.md#technical-decisions` | Delete each member |
| QA-01..QA-04 | `techspec.md#quality-profile` | Reference counts reach 0 |

## Context to recover on demand

- Applicable skills: `dotnet-efficient-validation`, `repository-cli-efficiency`.
- Existing code: `CopilotPassDispatchBudget.cs:32-36`, `CopilotApiClient.cs:148-155`, `CopilotCredentialDiscovery.cs:93-95`, `CodexAuthDiscovery.cs:66-68`.
- Contract or integration: TechSpec `#affected-components`.

## Work

- [ ] T01.1 Delete `RemainingDispatches` from `CopilotPassDispatchBudget.cs`.
- [ ] T01.2 Delete the `GetUsageAsync` alias (and its `<see cref>` doc) from `CopilotApiClient.cs`.
- [ ] T01.3 Delete `DiscoverCredentialAsync` from `CopilotCredentialDiscovery.cs`.
- [ ] T01.4 Delete `DiscoverAccountAsync` from `CodexAuthDiscovery.cs`.
- [ ] T01.5 Remove any now-unused `using` directives introduced by the deletions.

## Acceptance criteria

- [ ] `rtk rg -n "RemainingDispatches" src` returns 0 hits.
- [ ] `rtk rg -n "GetUsageAsync" src/TokenHound.Infrastructure/Providers/Copilot` returns 0 hits.
- [ ] `rtk rg -n "DiscoverCredentialAsync" src/TokenHound.Infrastructure/Providers/Copilot` returns 0 hits.
- [ ] `rtk rg -n "DiscoverAccountAsync" src` returns 0 hits.
- [ ] Solution builds with no new warnings.

## Verification

- Unit: not applicable (members had no behavior contract).
- Integration: not applicable.
- E2E: omitted by .NET desktop policy.
- Manual: none.
- Commands: `rtk dotnet build`; scoped `rtk rg` above.
- Environment dependency: none.
- Expected evidence: zero-hit `rg` output and a clean build.

## Affected files

- Modify: `src/TokenHound.Infrastructure/Providers/Copilot/CopilotPassDispatchBudget.cs`
- Modify: `src/TokenHound.Infrastructure/Providers/Copilot/CopilotApiClient.cs`
- Modify: `src/TokenHound.Infrastructure/Providers/Copilot/CopilotCredentialDiscovery.cs`
- Modify: `src/TokenHound.Infrastructure/Providers/Codex/CodexAuthDiscovery.cs`

## Observability and recovery

- Operational signal: none (compile-time surface).
- Recovery: `git revert` the removal commit.

## Handoff

> Updated by `sdd-execute-task` during implementation.

- Produced result: Removed all four orphaned `public` members from `TokenHound.Infrastructure` (T01.1–T01.4); no now-unused `using` directives were introduced (T01.5). The change is deletion-only; all remaining members and behaviors are unchanged.
- Changed files (4, all authorized; 30 deletions, 0 insertions):
  - `src/TokenHound.Infrastructure/Providers/Copilot/CopilotPassDispatchBudget.cs` — removed `RemainingDispatches`.
  - `src/TokenHound.Infrastructure/Providers/Copilot/CopilotApiClient.cs` — removed the `GetUsageAsync` alias and its `<see cref>` doc; `GetQuotaAsync` unchanged.
  - `src/TokenHound.Infrastructure/Providers/Copilot/CopilotCredentialDiscovery.cs` — removed `DiscoverCredentialAsync`; `DiscoverAsync` unchanged.
  - `src/TokenHound.Infrastructure/Providers/Codex/CodexAuthDiscovery.cs` — removed `DiscoverAccountAsync`; `DiscoverAsync` unchanged.
- Checks (exact commands and results):
  - `rtk rg -n "RemainingDispatches" src` → 0 hits (rg exit 1).
  - `rtk rg -n "GetUsageAsync" src/TokenHound.Infrastructure/Providers/Copilot` → 0 hits.
  - `rtk rg -n "DiscoverCredentialAsync" src/TokenHound.Infrastructure/Providers/Copilot` → 0 hits.
  - `rtk rg -n "DiscoverAccountAsync" src` → 0 hits.
  - Baseline before edits: each of the four scoped patterns had exactly 1 hit (its own declaration only).
  - `rtk dotnet build --nologo --verbosity:minimal` → 7 projects, 0 errors, 0 warnings.
  - `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1` → 673 passed, 0 failed.
  - `rtk dotnet test --project tests/TokenHound.Core.Tests/TokenHound.Core.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1` → 91 passed, 0 failed.
  - Focused (TC-02/TC-03): `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*CopilotMetricsClientTests*" --filter-class "*CopilotApiClientTests*" --filter-class "*CopilotCredentialDiscoveryTests*" --filter-class "*CodexAuthDiscoveryTests*"` → 33 passed, 0 failed.
  - `rtk git --no-pager diff --stat -- src` → exactly the 4 files above, 30 deletions; `rtk git --no-pager diff --check -- src` → clean.
- Validated state: code/diff at working tree over current `HEAD`, Debug build, `net10.0` / xUnit v3 MTP runner, .NET SDK 10.0.400. Zero tests or listing were not used as success. No pre-existing source changes were present in `src`; unrelated pre-existing worktree changes (skills, task moves) were left untouched. E2E omitted by the .NET desktop policy (no UI surface).
- Open items: none blocking. TechSpec CMP-02 (CopilotApiClient) is also edited by `arch-20260912-03` and CMP-03/CMP-04 by `arch-20260912-02` (deferred); those must land after this task to avoid conflicts.

### ADR candidates

None - direct TechSpec implementation (DEC-01..DEC-04) with no new durable decision.
