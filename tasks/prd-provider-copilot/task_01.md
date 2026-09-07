# Stable execution context

Load in this exact order:

1. `tasks/prd-provider-copilot/prd.md`
2. `tasks/prd-provider-copilot/techspec.md`
3. This file

Use the approved PRD and TechSpec versions. Do not broaden the product contract.

---

# T01 - Establish pure Core quota and resilience policies

## Outcome

TokenHound.Core can represent Copilot's unsupported state and fractional remainder, retain last-good snapshots, schedule active/idle refreshes, and calculate persistent rate-limit deadlines without any UI, OS, HTTP, process, or file dependency.

## Dependencies and boundaries

- Depends on: —
- Unblocks: T02 and T03
- In scope: additive pure model fields/status, snapshot retention policy, refresh cadence, shared rate-limit floor/ceiling/jitter rules, and Core tests.
- Out of scope: Copilot HTTP or credential code, archive I/O, WPF registration, activity file inspection, and any product decision deferred by HIL1.

## Traceability

| Source | Section | Obligation covered |
| --- | --- | --- |
| FR-01, FR-05, FR-06 | `prd.md#functional-requirements` | Preserve provider fidelity values, fractional remainder, reset, and overage semantics in pure models. |
| FR-07, FR-11 | `prd.md#functional-requirements` | Provide pure status/history and refresh/deadline policy inputs. |
| NFR-02, NFR-03, NFR-06 | `prd.md#non-functional-requirements` | Avoid invented values, enforce future Retry-After deadlines, and keep Core pure. |
| AC-01, AC-06, AC-08 | `prd.md#acceptance-criteria` | Model finite fraction, zero Retry-After floor, and blocking/non-blocking overage. |
| DEC-02, DEC-06, DEC-09 | `techspec.md#technical-decisions` | Add Unsupported/RemainingValue and align shared rate-limit policy. |
| CMP-09 | `techspec.md#components-and-flow` | Own pure models and policies. |
| TC-07, TC-10 | `techspec.md#test-approach` | Prove rate-limit and serialization/purity behavior. |

## Context to recover on demand

- Applicable skills: `dotnet-efficient-validation`, `repository-cli-efficiency`, `no-workarounds`.
- Existing code: `src/TokenHound.Core/Models/ProviderStatus.cs`, `LimitWindow.cs`, `Snapshot.cs`, `UsageBlock.cs`, and `src/TokenHound.Core/Policies/RateLimitPolicy.cs`, `BackoffCalculator.cs`, `RefreshSchedulePolicy.cs`.
- Contract or integration: TechSpec sections `Pure model changes`, `Status and history matrix`, and `Errors, security, and recovery`.

## Work

- [x] T01.1 Add `ProviderStatus.Unsupported` and nullable `LimitWindow.RemainingValue` without changing existing provider call contracts.
- [x] T01.2 Add `SnapshotRetentionPolicy` and encode success replacement, stale last-good retention, and NeedsAuth/Unsupported clearing as pure decisions.
- [x] T01.3 Align `RateLimitPolicy` and `BackoffCalculator` with the shared 60-second floor, 900-second ceiling, positive jitter, server Retry-After floor-raising, and future deadline for zero/negative values.
- [x] T01.4 Preserve existing 60-second active and 300-second idle cadence behavior in `RefreshSchedulePolicy`.
- [x] T01.5 Add focused Core tests for optional serialization, Unsupported, RemainingValue, retention outcomes, cadence, and every Retry-After edge.
- [x] T01.6 Inspect the Core project references and changed files to confirm no OS, UI, HTTP, filesystem, process, or WPF dependency was introduced.

## Acceptance criteria

- `ProviderStatus.Unsupported` is distinct from `NeedsAuth` and `Stale`.
- `RemainingValue` round-trips as nullable JSON and existing providers remain source-compatible.
- Retention never turns stale/auth/unsupported data into a fabricated current zero and preserves the original successful fetch time for stale history.
- Rate-limit policy produces a future deadline for `Retry-After: 0`, respects the configured floor/ceiling, and never dispatches immediately because of zero or negative input.
- Core remains free of external integration references and all focused tests execute at least one test.

## Verification

- Unit: model, retention, cadence, and rate-limit tests cover the acceptance criteria and use deterministic clocks/randomness where needed.
- Integration: None; external I/O is out of scope for Core.
- E2E: omitted by .NET desktop policy.
- Manual: None.
- Commands: `rtk dotnet build tests/TokenHound.Core.Tests/TokenHound.Core.Tests.csproj --no-restore --nologo --verbosity:minimal`; `rtk dotnet test --project tests/TokenHound.Core.Tests/TokenHound.Core.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1`; focused reruns may use `--filter-class "*DomainModels*"`, `--filter-class "*RateLimitPolicy*"`, and `--filter-class "*RefreshSchedule*"` after `--`.
- Environment dependency: .NET SDK 10.0.400 and the existing native Microsoft.Testing.Platform runner; no live service or credential.
- Expected evidence: build exit code 0, non-zero executed test count, focused test output, and a dependency inspection showing Core purity.

## Affected files

- Modify: `src/TokenHound.Core/Models/ProviderStatus.cs`
- Modify: `src/TokenHound.Core/Models/LimitWindow.cs`
- Modify: `src/TokenHound.Core/Policies/RateLimitPolicy.cs`
- Modify: `src/TokenHound.Core/Policies/BackoffCalculator.cs`
- Modify: `src/TokenHound.Core/Policies/RefreshSchedulePolicy.cs`
- Create: `src/TokenHound.Core/Policies/SnapshotRetentionPolicy.cs`
- Modify: `tests/TokenHound.Core.Tests/Models/DomainModelsTests.cs`
- Modify: `tests/TokenHound.Core.Tests/Policies/BackoffCalculatorTests.cs`
- Modify: `tests/TokenHound.Core.Tests/Policies/RateLimitPolicyTests.cs`
- Create: `tests/TokenHound.Core.Tests/Policies/SnapshotRetentionPolicyTests.cs`

## Observability and recovery

- Operational signal: policy outputs and test evidence; no new production telemetry.
- Recovery: revert only the T01-owned Core/test changes if review finds a contract defect. Do not change provider or UI files to mask a failing policy.

## Handoff

> Updated by `sdd-execute-task` during implementation.

- Produced result: T01 complete. Core now exposes additive Unsupported and fractional remainder seams, pure retention decisions, and the shared 60/120/240/480/900-second deadline policy with positive one-to-five-second jitter.
- Changed files: `src/TokenHound.Core/Models/ProviderStatus.cs`; `src/TokenHound.Core/Models/LimitWindow.cs`; `src/TokenHound.Core/Policies/BackoffCalculator.cs`; `src/TokenHound.Core/Policies/RateLimitPolicy.cs`; `src/TokenHound.Core/Policies/SnapshotRetentionPolicy.cs`; `tests/TokenHound.Core.Tests/Models/DomainModelsTests.cs`; `tests/TokenHound.Core.Tests/Policies/BackoffCalculatorTests.cs`; `tests/TokenHound.Core.Tests/Policies/RateLimitPolicyTests.cs`; `tests/TokenHound.Core.Tests/Policies/SnapshotRetentionPolicyTests.cs`.
- Checks: `rtk dotnet restore tests/TokenHound.Core.Tests/TokenHound.Core.Tests.csproj --nologo --verbosity:minimal` exit 0; `rtk dotnet build tests/TokenHound.Core.Tests/TokenHound.Core.Tests.csproj --no-restore --nologo --verbosity:minimal` exit 0; `rtk dotnet test --project tests/TokenHound.Core.Tests/TokenHound.Core.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1` exit 0 with 43 tests passed; `git diff --check` passed; Core project and dependency inspection passed.
- Validated state: T01-owned code and tests are implemented and validated on .NET SDK 10.0.400 with native Microsoft.Testing.Platform. Existing 60-second active and 300-second idle cadence tests pass unchanged. Core has no project/package references or UI, OS, HTTP, filesystem, process, or WPF dependency.
- Open items: None for T01. T02 may consume `SnapshotRetentionPolicy.Apply`; IP-01 remains scoped to T03's plaintext credential fallback.

### ADR candidates

None - direct TechSpec implementation or local decision.
