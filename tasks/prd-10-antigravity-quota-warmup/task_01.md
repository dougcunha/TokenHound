# Task 01: Implement WarmupSessionAsync in AntigravityLanguageServerClient

## Objective
Implement `WarmupSessionAsync` in `AntigravityLanguageServerClient` that issues a `GetUserStatus` request across candidate ports with the CSRF header, returning `true` on success and `false` on failure.

## Acceptance Criteria
- [x] `AntigravityLanguageServerClient` exposes `public async ValueTask<bool> WarmupSessionAsync(AntigravityEndpoint endpoint, CancellationToken cancellationToken = default)`.
- [x] It sends a POST request with `{}` body to `{scheme}://127.0.0.1:{port}/exa.language_server_pb.LanguageServerService/GetUserStatus` with `x-codeium-csrf-token` header.
- [x] Probes HTTPS first then HTTP for each candidate port in `endpoint.CandidatePorts`.
- [x] Returns `true` immediately upon receiving a 2xx success status code; returns `false` if all candidate ports fail or on network exceptions.
- [x] Unit tests in `AntigravityLanguageServerClientTests` cover success and failure scenarios.

## Files Touched
- `src/TokenHound.Infrastructure/Providers/Antigravity/AntigravityLanguageServerClient.cs`
- `tests/TokenHound.Infrastructure.Tests/Providers/Antigravity/AntigravityLanguageServerClientTests.cs`

## Verification
- `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*AntigravityLanguageServerClientTests*"`: Passed (8 tests, 0 warnings).

## Handoff
- `WarmupSessionAsync` implemented in `AntigravityLanguageServerClient.cs` with helper methods `WarmupPortAsync` and `WarmupSchemeAsync`.
- Tested in `AntigravityLanguageServerClientTests.cs` with `WarmupSessionAsync_WhenPortRespondsOk_ReturnsTrue` and `WarmupSessionAsync_WhenAllPortsFail_ReturnsFalse`.
- Ready for Task 02 (`AntigravityUsageProvider` integration).
