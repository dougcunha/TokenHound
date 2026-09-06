# Stable execution context

Load in this exact order:

1. `tasks/prd-provider-cursor/prd.md`
2. `tasks/prd-provider-cursor/techspec.md`
3. This file

---

# T02 — Cursor Api Client with Workos Session Cookie

## Outcome

Implements `CursorApiClient` to execute GET requests against `https://cursor.com/api/usage-summary` with the required `Cookie: WorkosCursorSessionToken=<stripeMembershipAuthId>::<accessToken>` header, deserializing usage plans and quotas.

## Work

- [x] T02.1 Implement `CursorUsageResponse.cs` (and nested DTOs) under `TokenHound.Infrastructure/Providers/Cursor/`.
- [x] T02.2 Implement `CursorApiClient.cs` accepting `HttpClient` or `IHttpClientFactory`.
- [x] T02.3 Inject the mandatory `WorkosCursorSessionToken` cookie into requests.
- [x] T02.4 Implement `CursorApiClientTests.cs` using mock `HttpMessageHandler`.

## Acceptance criteria

- Correctly formats the cookie as `<stripeMembershipAuthId>::<accessToken>`.
- Deserializes `individualUsage.plan.totalPercentUsed`, `apiPercentUsed`, and billing dates.
- Handles HTTP 401/403/429 gracefully without throwing unhandled exceptions.
- Tests pass with `rtk dotnet test`.

## Verification

- Command: `rtk dotnet test tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*CursorApiClientTests*"`

## Handoff

- Produced result: Implemented `CursorApiClient` to fetch `https://cursor.com/api/usage-summary` using `WorkosCursorSessionToken=<stripeMembershipAuthId>::<accessToken>` with typed JSON deserialization (`CursorUsageResponse`) and resilient 401/403/429 handling.
- Changed files:
  - `src/TokenHound.Infrastructure/Providers/Cursor/CursorUsageResponse.cs`
  - `src/TokenHound.Infrastructure/Providers/Cursor/CursorApiClient.cs`
  - `tests/TokenHound.Infrastructure.Tests/Providers/Cursor/CursorApiClientTests.cs`
- Checks: `rtk dotnet test tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*CursorApiClientTests*"` (3 tests passed, 0 warnings, exit code 0).
- Validated state: Passed 3 unit tests with mock HTTP handler.
- Open items: None.

### ADR candidates

None.
