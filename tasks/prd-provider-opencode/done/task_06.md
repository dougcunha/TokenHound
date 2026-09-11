# Stable execution context

Load in this exact order:

1. `tasks/prd-provider-opencode/codereview_1/codereview.md`
2. This file

---

# T06 — Honor HTTP 429 reset-time fallback

## Outcome

When OpenCode returns HTTP 429 without a usable `Retry-After` header, the provider carries and honors the server's authoritative reset timestamp instead of retrying after only the local floor.

## Classification

- Actionable: `codereview_1/CR-02`.

## Dependencies and boundaries

- Depends on: T05
- Unblocks: —
- In scope: Preserve a 429 response reset timestamp through DTO/exception/client boundaries, calculate the provider block from that absolute deadline, and test the no-cache fallback.
- Out of scope: The HTTP 200 rate-limited status path covered by T05, SQLite activity, credential discovery, live network calls, and changes to the review report.

## Traceability

| Source | Section | Finding covered |
| --- | --- | --- |
| `codereview_1/CR-02` | `codereview.md#findings` | The client drops a 429 reset timestamp and the provider falls back to a local floor when no cached window exists. |
| FR-05 | `prd.md#functional-requirements` | Parse `Retry-After` or `resetsAt` and create the active block. |
| NFR-04 | `prd.md#non-functional-requirements` | Enforce resilient timeout and backoff behavior. |
| DEC-03 | `techspec.md#technical-decisions` | Respect the server deadline and policy floor. |
| TC-03 | `techspec.md#test-approach` | Verify 429 rate-limit handling. |

## Requirements

- Preserve the server reset timestamp from the 429 payload through the API client to the usage provider.
- Prefer a valid `Retry-After` duration when supplied; otherwise use the authoritative reset timestamp.
- `Retry-After: 0` must still be subject to the repository's non-immediate retry floor.
- A 429 without a reset timestamp must retain the existing safe policy-floor behavior and must not invent a quota reset.

## Context to recover on demand

- TechSpec: `techspec.md#errors-security-and-recovery`, `techspec.md#contracts-and-data`
- Rules/skills: `AGENTS.md` rate-limit persistence and no immediate retry; `dotnet-efficient-validation`
- Code: `src/TokenHound.Infrastructure/Providers/OpenCode/OpenCodeApiClient.cs` — 429 response parsing and exception construction
- Code: `src/TokenHound.Infrastructure/Providers/OpenCode/OpenCodeUsageDto.cs` — error payload contract
- Code: `src/TokenHound.Infrastructure/Providers/OpenCode/OpenCodeUsageProvider.cs` — retry resolution and deadline calculation

## Work

- [x] T06.1 Extend the error/exception contract to retain a valid server reset timestamp from a 429 response.
- [x] T06.2 Update retry resolution to use that timestamp when `Retry-After` is absent, while preserving the policy floor and existing cached-window fallback.
- [x] T06.3 Add API-client and provider tests for no-header reset fallback, `Retry-After: 0`, invalid/past reset values, and no-cache behavior.

## Acceptance criteria

- A 429 with no `Retry-After`, no prior successful snapshot, and a valid server `resetsAt` produces an active block ending at the server reset, subject to the configured policy minimum.
- A valid `Retry-After` remains authoritative as a duration, with the policy floor preventing immediate retry for zero seconds.
- Missing or invalid reset data falls back safely without inventing a reset timestamp.
- Existing 429 seconds/date parsing and the full OpenCode/infrastructure test suites remain green.

## Verification

- Unit: Mock 429 payloads with and without reset timestamps and assert exception data, deadline, and dispatch lockout.
- Integration: Not required; the HTTP response and provider boundary are covered by in-memory handlers.
- E2E: Omitted by desktop .NET policy.
- Manual: Not required for this transport correction.
- Environment dependency: None.
- Commands: `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --filter-class "*OpenCodeApiClientTests*" --minimum-expected-tests 1`; `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --filter-class "*OpenCodeUsageProviderTests*" --minimum-expected-tests 1`; `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1`
- Expected evidence: Tests prove both server-reset and header paths, zero-second flooring, and no-cache safety.

## Affected files

- Modify: `src/TokenHound.Infrastructure/Providers/OpenCode/OpenCodeUsageDto.cs`
- Modify: `src/TokenHound.Infrastructure/Providers/OpenCode/OpenCodeApiClient.cs`
- Modify: `src/TokenHound.Infrastructure/Providers/OpenCode/OpenCodeRateLimitException.cs`
- Modify: `src/TokenHound.Infrastructure/Providers/OpenCode/OpenCodeUsageProvider.cs`
- Modify: `tests/TokenHound.Infrastructure.Tests/Providers/OpenCode/OpenCodeApiClientTests.cs`
- Modify: `tests/TokenHound.Infrastructure.Tests/Providers/OpenCode/OpenCodeUsageProviderTests.cs`

## Observability and recovery

- Operational signal: Existing rate-limit warning logs the final calculated deadline; preserve structured deadline fields.
- Recovery: Revert the transport/provider/test changes together if the API payload contract is disproven; no third-party state is modified.

## Handoff

> Updated by `sdd-execute-corrections` during implementation.

- Produced result: Implemented CR-02. HTTP 429 `resetsAt` is deserialized, preserved on `OpenCodeRateLimitException`, and used by the provider when no valid `Retry-After` duration is available. Valid `Retry-After` values, including zero, remain authoritative and continue through `RateLimitPolicy`; missing, malformed, and past reset values fall back to the existing cached-window or policy-floor behavior.
- Changed files: `src/TokenHound.Infrastructure/Providers/OpenCode/OpenCodeUsageDto.cs`; `src/TokenHound.Infrastructure/Providers/OpenCode/OpenCodeApiClient.cs`; `src/TokenHound.Infrastructure/Providers/OpenCode/OpenCodeRateLimitException.cs`; `src/TokenHound.Infrastructure/Providers/OpenCode/OpenCodeUsageProvider.cs`; `tests/TokenHound.Infrastructure.Tests/Providers/OpenCode/OpenCodeApiClientTests.cs`; `tests/TokenHound.Infrastructure.Tests/Providers/OpenCode/OpenCodeUsageProviderTests.cs`.
- Checks: `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-restore --nologo --verbosity:minimal` passed, 3 projects, 0 errors, 0 warnings. `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --filter-class "*OpenCodeApiClientTests*" --minimum-expected-tests 1` passed, 15 tests. `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --filter-class "*OpenCodeUsageProviderTests*" --minimum-expected-tests 1` passed, 18 tests. `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1` passed, 670 tests. Scoped `rtk git diff --check --` for the six changed implementation/test files passed.
- Validated state: `codereview_1` / T06 current worktree, .NET SDK 10.0.400, `net10.0`, native Microsoft.Testing.Platform, infrastructure project built and tested without restore. Focused API/provider tests cover boundary preservation, no-cache server reset, `Retry-After: 0`, and missing/invalid/past reset fallback. E2E and live OpenCode network calls were omitted under the desktop .NET policy and task scope.
- Open items: No T06 implementation or test items remain. CR-03 and CR-04 remain outside this task; no manual transport check was required.
