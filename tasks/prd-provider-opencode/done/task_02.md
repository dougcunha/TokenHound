# Stable execution context

Load in this exact order:

1. `tasks/prd-provider-opencode/prd.md`
2. `tasks/prd-provider-opencode/techspec.md`
3. This file

---

# T02 — HTTP API client and quota response DTOs

## Outcome

Implements `OpenCodeApiClient` and strongly typed DTOs to query `https://opencode.ai/zen/go/v1/usage`, handling Bearer authentication, timeout constraints (<= 10s), HTTP 429 `Retry-After` headers, and defensive error parsing.

## Dependencies and boundaries

- Depends on: T01
- Unblocks: T03
- In scope:
  - `OpenCodeUsageDto.cs`: `OpenCodeUsageResponse`, `OpenCodeUsageData`, `OpenCodeLimitWindowDto`, `OpenCodeErrorResponse`.
  - `OpenCodeApiClient.cs` with custom `HttpClient` or injected `HttpMessageHandler`.
  - Parses HTTP 200 payload with 3 limit windows.
  - Extracts `Retry-After` header and parses `GoUsageLimitError` on HTTP 429.
  - Handles HTTP 401 (`AuthError`) and HTTP 403 (`EntitlementError`).
- Out of scope:
  - Constructing `Snapshot` domain records (handled in T03).
  - Calling live networks during unit test execution.

## Traceability

| Source | Section | Obligation covered |
| --- | --- | --- |
| FR-03 | `prd.md#functional-requirements` | Quota Endpoint Telemetry (`zen/go/v1/usage`) |
| FR-04 | `prd.md#functional-requirements` | Multi-Window Parsing (rolling, weekly, monthly) |
| FR-05 | `prd.md#functional-requirements` | Rate-Limit & 429 Handling (`Retry-After`) |
| NFR-04 | `prd.md#non-functional-requirements` | Network Resilience & Timeout (<= 10s) |
| NFR-05 | `prd.md#non-functional-requirements` | Testability via Mock HTTP Handlers |
| CMP-02 | `techspec.md#components-and-flow` | `OpenCodeUsageDto` |
| CMP-04 | `techspec.md#components-and-flow` | `OpenCodeApiClient` |
| TC-02 | `techspec.md#test-approach` | API client usage parsing unit tests |
| TC-03 | `techspec.md#test-approach` | API client 429 rate limit unit tests |

## Context to recover on demand

- Existing reference: [CursorApiClient.cs](file:///D:/MyProjects/TokenHound/src/TokenHound.Infrastructure/Providers/Cursor/CursorApiClient.cs)
- TechSpec Contracts: [techspec.md#contracts-and-data](file:///D:/MyProjects/TokenHound/tasks/prd-provider-opencode/techspec.md#contracts-and-data)

## Work

- [x] T02.1 Create `src/TokenHound.Infrastructure/Providers/OpenCode/OpenCodeUsageDto.cs` with JSON property attributes.
- [x] T02.2 Create `src/TokenHound.Infrastructure/Providers/OpenCode/OpenCodeApiClient.cs` implementing `GetUsageAsync(string apiKey, CancellationToken ct)`.
- [x] T02.3 Create unit tests in `tests/TokenHound.Infrastructure.Tests/Providers/OpenCode/OpenCodeApiClientTests.cs` using `MockHttpMessageHandler` covering 200 OK, 401 Unauthorized, 403 EntitlementError, 429 Too Many Requests (with `Retry-After`), and transient socket timeouts.

## Acceptance criteria

- `GetUsageAsync` attaches `Authorization: Bearer <key>` and `User-Agent: TokenHound`.
- Returns deserialized `OpenCodeUsageResponse` on HTTP 200 with valid `rolling`, `weekly`, and `monthly` sections.
- Throws custom `OpenCodeRateLimitException` containing `RetryAfterSeconds` on HTTP 429.
- Throws custom `OpenCodeAuthException` on HTTP 401 and `OpenCodeEntitlementException` on HTTP 403.
- Enforces request timeout <= 10 seconds.

## Verification

- Unit: Test suite `OpenCodeApiClientTests` passes with 100% assertions.
- Integration: Mock HTTP response payload matches real output captured from `opencode.ai/zen/go/v1/usage`.
- E2E: Omitted by desktop .NET policy.
- Commands: `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests --no-build --no-restore -- --filter-class "*OpenCodeApiClientTests*" --minimum-expected-tests 1`
- Environment dependency: None.

## Affected files

- Create: `src/TokenHound.Infrastructure/Providers/OpenCode/OpenCodeUsageDto.cs`
- Create: `src/TokenHound.Infrastructure/Providers/OpenCode/OpenCodeRateLimitException.cs`
- Create: `src/TokenHound.Infrastructure/Providers/OpenCode/OpenCodeAuthException.cs`
- Create: `src/TokenHound.Infrastructure/Providers/OpenCode/OpenCodeEntitlementException.cs`
- Create: `src/TokenHound.Infrastructure/Providers/OpenCode/OpenCodeTimeoutException.cs`
- Create: `src/TokenHound.Infrastructure/Providers/OpenCode/OpenCodeApiClient.cs`
- Create: `tests/TokenHound.Infrastructure.Tests/Providers/OpenCode/OpenCodeApiClientTests.cs`

## Observability and recovery

- Operational signal: Structured error logging for HTTP response statuses.
- Recovery: Non-fatal exceptions caught and translated by usage provider.

## Handoff

> Updated by `sdd-execute-task` during implementation.

- Produced result: Implemented `OpenCodeApiClient`, strongly typed DTOs (`OpenCodeUsageResponse`, `OpenCodeUsageData`, `OpenCodeLimitWindowDto`, `OpenCodeErrorResponse`), and typed exceptions (`OpenCodeRateLimitException`, `OpenCodeAuthException`, `OpenCodeEntitlementException`, `OpenCodeTimeoutException`). All requests enforce <= 10s timeouts, Bearer authentication, User-Agent `TokenHound`, and HTTP 429 `Retry-After` seconds and date header parsing. Unit test suite includes 13 passing tests with 100% assertion pass rate.
- Changed files:
  - `src/TokenHound.Infrastructure/Providers/OpenCode/OpenCodeUsageDto.cs`
  - `src/TokenHound.Infrastructure/Providers/OpenCode/OpenCodeRateLimitException.cs`
  - `src/TokenHound.Infrastructure/Providers/OpenCode/OpenCodeAuthException.cs`
  - `src/TokenHound.Infrastructure/Providers/OpenCode/OpenCodeEntitlementException.cs`
  - `src/TokenHound.Infrastructure/Providers/OpenCode/OpenCodeTimeoutException.cs`
  - `src/TokenHound.Infrastructure/Providers/OpenCode/OpenCodeApiClient.cs`
  - `tests/TokenHound.Infrastructure.Tests/Providers/OpenCode/OpenCodeApiClientTests.cs`
  - `tasks/prd-provider-opencode/task_02.md`
- Checks:
  - `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-restore --nologo --verbosity:minimal` -> 0 errors, 0 warnings.
  - `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --filter-class "*OpenCodeApiClientTests*" --minimum-expected-tests 1` -> 13 passed, 0 warnings (1.0 s).
  - OpenCode suite: `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --filter-class "*OpenCode*" --minimum-expected-tests 1` -> 33 passed, 0 warnings (1.0 s).
  - Full suite: `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1` -> 639 passed, 0 warnings (3.9 s).
- Validated state: .NET 10 (`net10.0`), all acceptance criteria satisfied, zero regressions.
- Open items: None for T02. Unblocks T03 (OpenCode usage provider adapter and snapshot mapping).

### ADR candidates

None. Implementation strictly adheres to TechSpec DEC-02, DEC-03, DEC-05, and repository architectural invariants.
