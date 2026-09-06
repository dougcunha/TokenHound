# Stable execution context

Load in this exact order:

1. `tasks/prd-tracer-bullet-claude/prd.md`
2. `tasks/prd-tracer-bullet-claude/techspec.md`
3. This file

---

# T02 — Claude OAuth Client (Network & DTOs)

## Outcome

Implements `ClaudeOAuthClient` in `TokenHound.Infrastructure/Providers/Claude/` to query `https://api.anthropic.com/api/oauth/usage` with the required beta header `anthropic-beta: oauth-2025-04-20` and deserializes usage responses.

## Dependencies and boundaries

- Depends on: —
- Unblocks: T03
- In scope: Network client, DTOs (`ClaudeUsageResponse`, `ClaudeWindowDto`), header configuration, and unit tests using `MockHttpMessageHandler`.
- Out of scope: Credential discovery on disk or domain snapshot mapping.

## Traceability

| Source | Section | Obligation covered |
| --- | --- | --- |
| FR-02 | `prd.md#functional-requirements` | Anthropic OAuth usage telemetry query |
| DEC-02 | `techspec.md#technical-decisions` | ClaudeOAuthClient with typed DTOs |
| CMP-02 | `techspec.md#components-and-flow` | src/TokenHound.Infrastructure/Providers/Claude/ClaudeOAuthClient.cs |

## Context to recover on demand

- Applicable skills: `dotnet-efficient-validation`
- Spec: `docs/specs/03-PROVIDER-CLAUDE-CODE.md`

## Work

- [ ] T02.1 Define `ClaudeUsageResponse.cs` and `ClaudeWindowDto.cs` records under `TokenHound.Infrastructure/Providers/Claude/`.
- [ ] T02.2 Implement `ClaudeOAuthClient.cs` making GET requests with bearer token and `anthropic-beta: oauth-2025-04-20`.
- [ ] T02.3 Handle HTTP status codes (200 OK, 401 Unauthorized, 429 RateLimited).
- [ ] T02.4 Implement `ClaudeOAuthClientTests.cs` using mock HTTP responses for success, auth failure, and rate limiting.

## Acceptance criteria

- Sends `Authorization: Bearer <token>` and `anthropic-beta: oauth-2025-04-20` headers.
- Correctly parses `five_hour` and `seven_day` utilization fractions and reset timestamps.
- Returns clear status or throws typed exceptions on network/auth failure.

## Verification

- Unit: Test HTTP 200 payload parsing, 401 response handling, and 429 Retry-After header parsing.
- Commands: `rtk dotnet test tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*ClaudeOAuthClientTests*"`
- Expected evidence: MTP test suite passes.

## Affected files

- Create:
  - `src/TokenHound.Infrastructure/Providers/Claude/ClaudeUsageResponse.cs`
  - `src/TokenHound.Infrastructure/Providers/Claude/ClaudeWindowDto.cs`
  - `src/TokenHound.Infrastructure/Providers/Claude/ClaudeOAuthClient.cs`
  - `tests/TokenHound.Infrastructure.Tests/Providers/ClaudeOAuthClientTests.cs`

## Observability and recovery

- Operational signal: Unit test execution.
- Recovery: Revert client logic if Anthropic header requirements change.

## Handoff

- Produced result: Implemented `ClaudeOAuthClient` with HTTP GET requests to `https://api.anthropic.com/api/oauth/usage`, injecting required `Authorization: Bearer <token>`, `anthropic-beta: oauth-2025-04-20`, and `User-Agent: TokenHound/1.0` headers. Deserializes telemetry into `ClaudeUsageResponse` and `ClaudeWindowDto` records with snake_case mapping. Handles HTTP error statuses, including 401 Unauthorized, 403 Forbidden, and 429 Too Many Requests with typed `RateLimitException` extracting `Retry-After` header. 11 MTP unit tests created and passing.
- Changed files:
  - `src/TokenHound.Infrastructure/Providers/Claude/ClaudeUsageResponse.cs`
  - `src/TokenHound.Infrastructure/Providers/Claude/ClaudeWindowDto.cs`
  - `src/TokenHound.Infrastructure/Providers/Claude/ClaudeOAuthClient.cs`
  - `tests/TokenHound.Infrastructure.Tests/Providers/ClaudeOAuthClientTests.cs`
  - `tasks/prd-tracer-bullet-claude/task_02.md`
- Checks:
  - `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj` (Passed: 0 errors)
  - `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*ClaudeOAuthClientTests*"` (Passed: 11 tests passed, 0 warnings)
  - `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1` (Passed: 92 tests passed, 0 warnings)
- Validated state: .NET 10 (`net10.0`), all acceptance criteria satisfied, zero regressions in existing test suite.
- Open items: None. Ready for T03 (`ClaudeOAuthProvider`).

### ADR candidates

None. Implementation conforms to TechSpec DEC-02 and CMP-02 without architectural deviations.
