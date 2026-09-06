# Stable execution context

Load in this exact order:

1. `tasks/prd-provider-antigravity/prd.md`
2. `tasks/prd-provider-antigravity/techspec.md`
3. This file

---

# T02 — Antigravity Language Server HTTPS Client

## Outcome

Implements `AntigravityLanguageServerClient` to query local endpoint `/exa.language_server_pb.LanguageServerService/RetrieveUserQuotaSummary` with `forceRefresh: true` and header `x-codeium-csrf-token`, handling self-signed localhost SSL certs and applying the fraction inversion rule ($1.0 - \text{remainingFraction}$).

## Work

- [x] T02.1 Implement `AntigravityLanguageServerDto.cs` (or records) under `TokenHound.Infrastructure/Providers/Antigravity/`.
- [x] T02.2 Implement `AntigravityLanguageServerClient.cs` accepting `HttpClient` or `IHttpClientFactory`.
- [x] T02.3 Enforce fraction inversion: `usedFraction = 1.0 - remainingFraction`.
- [x] T02.4 Implement candidate port fallback (trying listening ports until response is received or exhausted).
- [x] T02.5 Implement `AntigravityLanguageServerClientTests.cs` verifying happy path parsing, fraction inversion, and error responses.

## Acceptance criteria

- Sends POST with `{"forceRefresh": true}` and header `x-codeium-csrf-token`.
- Calculates consumed fraction by inverting remaining fraction.
- Parses bucket models (`displayName`, `resetTime`, `bucketId`).
- Returns null safely on network error or invalid JSON.
- Tests pass with `rtk dotnet test`.

## Verification

- Command: `rtk dotnet test tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*AntigravityLanguageServerClientTests*"`

## Handoff

- Produced result: `AntigravityLanguageServerDto.cs` and `AntigravityLanguageServerClient` with fraction inversion calculation, candidate port retry, and JSON-RPC over HTTPS.
- Changed files:
  - `src/TokenHound.Infrastructure/Providers/Antigravity/AntigravityLanguageServerDto.cs`
  - `src/TokenHound.Infrastructure/Providers/Antigravity/AntigravityLanguageServerClient.cs`
  - `tests/TokenHound.Infrastructure.Tests/Providers/Antigravity/AntigravityLanguageServerClientTests.cs`
- Checks: `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*AntigravityLanguageServerClientTests*"` (8 tests passed, exit code 0).
- Validated state: Validated SSL bypass callback, header injection, port iterating fallback, fraction inversion, and null safety.
- Open items: None.

### ADR candidates

None.
