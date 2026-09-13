# Stable execution context

Load in this exact order:

1. `tasks/prd-arch-20260912-01-dead-exports/prd.md`
2. `tasks/prd-arch-20260912-01-dead-exports/techspec.md`
3. This file

Use the current PRD and TechSpec versions already loaded; recover only missing or changed sources. This order does not guarantee a host cache hit.

---

# T02 — Validate removals

## Outcome

The build and the focused Copilot/Codex test classes pass with the four members removed, proving no remaining caller existed.

## Dependencies and boundaries

- Depends on: T01
- Unblocks: —
- In scope: build and run focused MTP tests for the affected providers.
- Out of scope: any further source change.

## Traceability

| Source | Section | Obligation covered |
| --- | --- | --- |
| R-01..R-04 | `prd.md#behaviors-to-preserve` | Provider behavior unchanged |
| TC-01..TC-03 | `techspec.md#safety-net` | Build + focused tests |

## Context to recover on demand

- Applicable skills: `dotnet-efficient-validation` (including `references/mtp.md`).
- Existing code: affected provider classes and their test classes.
- Contract or integration: TechSpec `#safety-net`.

## Work

- [ ] T02.1 Build: `rtk dotnet build`.
- [ ] T02.2 Run focused Copilot tests: `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*CopilotApiClientTests*"`.
- [ ] T02.3 Run focused credential/Codex tests with equivalent `--filter-class`.

## Acceptance criteria

- [ ] Build succeeds with 0 errors.
- [ ] Every focused command reports at least one executed test and passes.
- [ ] No test references the removed members.

## Verification

- Unit: focused provider classes pass.
- Integration: not applicable.
- E2E: omitted by .NET desktop policy.
- Manual: none.
- Commands: as in Work.
- Environment dependency: none.
- Expected evidence: pass counts per class with `--minimum-expected-tests 1`.

## Affected files

- Modify: none (verification only).

## Observability and recovery

- Operational signal: test output.
- Recovery: n/a.

## Handoff

> Updated by `sdd-execute-task` during implementation.

- Produced result: T02 verification complete (pass). T01's four orphaned members are absent from the working tree (30 deletions across 4 files); the build is green and all four focused Copilot/Codex test classes pass, proving no remaining caller existed. No source files were changed by this task.
- Changed files: none (verification only). Only this Handoff section was edited. Pre-existing T01 edits remain uncommitted in the working tree: `src/TokenHound.Infrastructure/Providers/Copilot/CopilotPassDispatchBudget.cs`, `.../Copilot/CopilotApiClient.cs`, `.../Copilot/CopilotCredentialDiscovery.cs`, `.../Codex/CodexAuthDiscovery.cs`.
- Checks:
  - Build (`TC-01`, `R-05`): `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-restore --nologo --verbosity:minimal` -> exit 0; 3 projects, 0 errors, 0 warnings.
  - `R-02` (`CopilotApiClientTests`): `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*CopilotApiClientTests*"` -> exit 0, 4 passed.
  - `R-03` (`CopilotCredentialDiscoveryTests`): same command with `--filter-class "*CopilotCredentialDiscoveryTests*"` -> exit 0, 5 passed.
  - `R-04` (`CodexAuthDiscoveryTests`): same command with `--filter-class "*CodexAuthDiscoveryTests*"` -> exit 0, 7 passed.
  - `R-01` (`CopilotMetricsClientTests`): same command with `--filter-class "*CopilotMetricsClientTests*"` -> exit 0, 17 passed.
  - Quality profile: `rtk rg -n "RemainingDispatches" src`, `rtk rg -n "GetUsageAsync" src/TokenHound.Infrastructure/Providers/Copilot`, `rtk rg -n "DiscoverCredentialAsync" src/TokenHound.Infrastructure/Providers/Copilot`, `rtk rg -n "DiscoverAccountAsync" src` -> 0 hits each. `rtk rg -n "RemainingDispatches|DiscoverAccountAsync|CopilotApiClient.*GetUsageAsync|GetUsageAsync.*Copilot" tests` -> 0 hits.
- Validated state: code/diff = T01 removals present in the working tree; configuration = `global.json` SDK 10.0.400, `test.runner: Microsoft.Testing.Platform` (native MTP via `--project`, filters after `--`); projects = `TokenHound.Infrastructure.Tests` (build transitively covers Infrastructure); environment = Windows/pwsh, `--no-restore` valid (no package changes). Desktop/UI, DB, and E2E not applicable per .NET desktop policy.
- Open items: none blocking. Work checklist boxes were intentionally left unticked because the T02 authorization restricted writes to this Handoff section only; all Work items were executed and evidenced above.

### ADR candidates

None - direct TechSpec implementation or local decision.
