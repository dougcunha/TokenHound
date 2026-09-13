# Stable execution context

Load in this exact order:

1. `tasks/prd-arch-20260912-03-http-failure-factory/prd.md`
2. `tasks/prd-arch-20260912-03-http-failure-factory/techspec.md`
3. This file

Use the current PRD and TechSpec versions already loaded; recover only missing or changed sources. This order does not guarantee a host cache hit.

---

# T02 — Delete Retry-After pass-through; retarget Copilot call sites

## Outcome

`CopilotRateLimitExtractor.ExtractRetryAfterSeconds` no longer exists; the four Copilot call sites call `HttpRetryAfterParser.ExtractSeconds` directly and parse identically.

## Dependencies and boundaries

- Depends on: — within this plan. External: `arch-20260912-01-dead-exports` must be complete (shared `CopilotApiClient.cs`).
- Unblocks: —
- In scope: delete the wrapper; retarget `CopilotApiClient.cs:176`, `CopilotBillingClient.cs:231`, `CopilotMetricsClient.cs:283`, `CopilotRequestGate.cs:245`.
- Out of scope: `TryExtractRateLimit` (kept); exception hierarchy (AA-11).

## Traceability

| Source | Section | Obligation covered |
| --- | --- | --- |
| R-03, R-04 | `prd.md#behaviors-to-preserve` | Identical parsing at the four sites |
| DEC-04 | `techspec.md#technical-decisions` | Delete wrapper, retarget sites |
| QA-02 | `techspec.md#quality-profile` | Pass-through count 5 → 0 |

## Context to recover on demand

- Applicable skills: `dotnet-efficient-validation`, `repository-cli-efficiency`.
- Existing code: `CopilotRateLimitExtractor.cs:42-48`, `HttpRetryAfterParser.cs:10-32`, four call sites listed above.
- Contract or integration: TechSpec `#technical-decisions`.

## Work

- [x] T02.1 Delete `ExtractRetryAfterSeconds` from `CopilotRateLimitExtractor.cs`.
- [x] T02.2 Retarget the four call sites to `HttpRetryAfterParser.ExtractSeconds(response, timeProvider)`.
- [x] T02.3 Remove now-unused usings; keep `TryExtractRateLimit` intact.

## Acceptance criteria

- [x] `rtk rg -n "ExtractRetryAfterSeconds" src` returns 0 hits.
- [x] Delta/date/raw Retry-After inputs parse to the same values as before at all four sites.
- [x] No change to `TryExtractRateLimit` behavior.

## Verification

- Unit: Copilot billing client, metrics client, request gate Retry-After scenarios.
- Integration: not applicable.
- E2E: omitted by .NET desktop policy.
- Manual: none.
- Commands: `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*CopilotBillingClientTests*"` (and `*CopilotMetricsClientTests*`, `*CopilotRequestGateTests*`).
- Environment dependency: none.
- Expected evidence: pass counts plus the zero-hit `rg`.

## Affected files

- Modify: `src/TokenHound.Infrastructure/Providers/Copilot/CopilotRateLimitExtractor.cs`
- Modify: `src/TokenHound.Infrastructure/Providers/Copilot/CopilotApiClient.cs`
- Modify: `src/TokenHound.Infrastructure/Providers/Copilot/CopilotBillingClient.cs`
- Modify: `src/TokenHound.Infrastructure/Providers/Copilot/CopilotMetricsClient.cs`
- Modify: `src/TokenHound.Infrastructure/Providers/Copilot/CopilotRequestGate.cs`

## Observability and recovery

- Operational signal: none.
- Recovery: `git revert`.

## Handoff

> Updated by `sdd-execute-task` during implementation.

- Produced result: `CopilotRateLimitExtractor.ExtractRetryAfterSeconds` deleted; `TryExtractRateLimit` kept intact. The four Copilot call sites now call `HttpRetryAfterParser.ExtractSeconds(response, timeProvider)` directly with the same time provider as before (`_timeProvider` in the gate, `TimeProvider.System` in the three clients). Added `using TokenHound.Infrastructure.Providers;` to the four call-site files and removed the now-unused parent-namespace using from `CopilotRateLimitExtractor.cs`.
- Changed files:
  - `src/TokenHound.Infrastructure/Providers/Copilot/CopilotRateLimitExtractor.cs` (deleted method, removed unused using)
  - `src/TokenHound.Infrastructure/Providers/Copilot/CopilotApiClient.cs`
  - `src/TokenHound.Infrastructure/Providers/Copilot/CopilotBillingClient.cs`
  - `src/TokenHound.Infrastructure/Providers/Copilot/CopilotMetricsClient.cs`
  - `src/TokenHound.Infrastructure/Providers/Copilot/CopilotRequestGate.cs`
- Checks:
  - `rtk dotnet build --nologo --verbosity:minimal` → 7 projects, 0 errors, 0 warnings, exit 0.
  - `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*CopilotBillingClientTests*"` → 9 tests passed, exit 0.
  - Same command with `--filter-class "*CopilotMetricsClientTests*"` → 17 tests passed, exit 0.
  - Same command with `--filter-class "*CopilotRequestGateTests*"` → 10 tests passed, exit 0.
  - Same command with `--filter-class "*CopilotApiClientTests*"` → 4 tests passed, exit 0.
  - QA-02: `rtk rg -n "ExtractRetryAfterSeconds" src` → 0 hits (exit 1 = no matches).
  - QA-03: `rtk rg -n "ExtractSeconds\(" src/TokenHound.Infrastructure/Providers/HttpRetryAfterParser.cs` → 1 hit (`HttpRetryAfterParser.cs:10`).
- Validated state: build green; 40 focused tests passed (4 Copilot test classes); TechSpec QA-02 5→0 and QA-03 1→1. Pre-existing uncommitted changes (completed `arch-20260912-01-dead-exports` removals such as `CopilotApiClient.GetUsageAsync`, and the T01 factory migration) were preserved and not reverted/staged/committed.
- Open items: none.

### ADR candidates

None - direct TechSpec implementation or local decision.
