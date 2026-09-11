# Stable execution context

Load in this exact order:

1. `tasks/prd-provider-opencode/codereview_1/codereview.md`
2. This file

---

# T05 — Handle HTTP 200 rate-limited usage status

## Outcome

An OpenCode usage response whose window status is `rate-limited` produces a `RateLimited` snapshot with an active deadline instead of being reported as a healthy `Ok` snapshot.

## Classification

- Actionable: `codereview_1/CR-01`.

## Dependencies and boundaries

- Depends on: T02, T03
- Unblocks: T06
- In scope: Interpret rate-limited status values in the successful usage DTO, create the existing `RateLimitPolicy`/`UsageBlock` result, preserve authoritative reset data, and add focused tests.
- Out of scope: HTTP 429 reset-header parsing, SQLite activity detection, changes to the review report, and live OpenCode calls.

## Traceability

| Source | Section | Finding covered |
| --- | --- | --- |
| `codereview_1/CR-01` | `codereview.md#findings` | A 200 `status: "rate-limited"` payload is currently mapped to `ProviderStatus.Ok` without a block. |
| FR-05 | `prd.md#functional-requirements` | Handle `status == "rate-limited"` and apply `RateLimitPolicy`. |
| FR-07 | `prd.md#functional-requirements` | Resolve the rate-limited provider state. |
| DEC-03 | `techspec.md#technical-decisions` | Enforce the rate-limit policy for rate-limited responses. |
| TC-04 | `techspec.md#test-approach` | Verify provider status and active-block mapping. |

## Requirements

- A response with any authoritative rate-limited usage window must not create an `Ok` snapshot.
- The result must use `ProviderStatus.RateLimited`, set `ActiveBlock.IsBlocked`, and calculate a deadline through the existing `RateLimitPolicy`.
- The authoritative reset timestamp must be used when it is available; no synthetic quota or reset value may be introduced.
- Existing successful three-window mapping, 401/403 handling, cancellation, and cached stale behavior must remain unchanged.

## Context to recover on demand

- TechSpec: `techspec.md#errors-security-and-recovery`, `techspec.md#test-approach`
- Rules/skills: `AGENTS.md` rate-limit deadlines and no fake data; `dotnet-efficient-validation`
- Code: `src/TokenHound.Infrastructure/Providers/OpenCode/OpenCodeUsageProvider.cs` — status mapping and block creation
- Code: `src/TokenHound.Infrastructure/Providers/OpenCode/OpenCodeUsageDto.cs` — deserialized window status and reset data

## Work

- [x] T05.1 Define the rate-limited success-response contract and select the authoritative reset timestamp without changing the repository's existing `ProviderStatus.RateLimited` enum.
- [x] T05.2 Update the provider response path to route a rate-limited usage status through `RateLimitPolicy` and return an active `UsageBlock`.
- [x] T05.3 Add provider tests for 200 rate-limited responses, reset deadline behavior, no network dispatch before the deadline, and unchanged successful mapping.

## Acceptance criteria

- A 200 usage payload with `status: "rate-limited"` returns `ProviderStatus.RateLimited`, not `ProviderStatus.Ok`.
- The returned snapshot contains an active block whose deadline is based on the response reset timestamp and the configured policy floor.
- Repeated snapshot requests before the deadline do not dispatch another HTTP request.
- Existing OpenCode tests and the full infrastructure suite remain green.

## Verification

- Unit: Mock a 200 rate-limited payload and assert status, block, deadline, and dispatch count.
- Integration: Not required; the client/provider boundary is covered with the existing in-memory HTTP handler.
- E2E: Omitted by desktop .NET policy.
- Manual: Not required for this provider-state correction.
- Environment dependency: None.
- Commands: `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --filter-class "*OpenCodeUsageProviderTests*" --minimum-expected-tests 1`; `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --filter-class "*OpenCode*" --minimum-expected-tests 1`; `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1`
- Expected evidence: Focused tests prove the previously missing status path and the full suite reports no regressions.

## Affected files

- Modify: `src/TokenHound.Infrastructure/Providers/OpenCode/OpenCodeUsageProvider.cs`
- Modify: `tests/TokenHound.Infrastructure.Tests/Providers/OpenCode/OpenCodeUsageProviderTests.cs`

## Observability and recovery

- Operational signal: Existing rate-limit warning log includes the calculated deadline.
- Recovery: Revert the provider/test changes together if the policy contract is rejected; no credential or external state is modified.

## Handoff

> Updated by `sdd-execute-corrections` during implementation.

- Produced result: Implemented CR-01. A successful usage payload with any `status: "rate-limited"` window now returns `ProviderStatus.RateLimited`, creates an active `UsageBlock`, preserves the authoritative reset deadline subject to the existing `RateLimitPolicy` floor, and suppresses dispatch until that deadline.
- Changed files: `src/TokenHound.Infrastructure/Providers/OpenCode/OpenCodeUsageProvider.cs`; `tests/TokenHound.Infrastructure.Tests/Providers/OpenCode/OpenCodeUsageProviderTests.cs`.
- Checks: `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-restore --nologo --verbosity:minimal` passed, 0 errors and 0 warnings. `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --filter-class "*OpenCodeUsageProviderTests*" --minimum-expected-tests 1` passed, 14 tests. `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --filter-class "*OpenCode*" --minimum-expected-tests 1` passed, 58 tests. `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1` passed, 664 tests. `rtk git diff --check -- src/TokenHound.Infrastructure/Providers/OpenCode/OpenCodeUsageProvider.cs tests/TokenHound.Infrastructure.Tests/Providers/OpenCode/OpenCodeUsageProviderTests.cs` passed.
- Validated state: `codereview_1` / T05 current worktree, .NET SDK 10.0.400, `net10.0`, native Microsoft.Testing.Platform, infrastructure project built and tested without restore. E2E omitted under desktop .NET policy.
- Open items: No CR-01 implementation or test items remain. CR-02, CR-03, and CR-04 remain outside T05 scope; no live OpenCode call was performed.
