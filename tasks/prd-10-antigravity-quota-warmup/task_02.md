# Task 02: Integrate Quota Warmup and Retry in AntigravityUsageProvider

## Objective
Update `AntigravityUsageProvider.TryGetLanguageServerSnapshotAsync` to invoke `WarmupSessionAsync` when the initial quota summary returns null or empty groups, retrying `RetrieveUserQuotaSummaryAsync` before continuing along the waterfall.

## Acceptance Criteria
- [x] If `RetrieveUserQuotaSummaryAsync` returns null or has empty `Groups`, `WarmupSessionAsync` is called on the endpoint.
- [x] If warmup returns `true`, `RetrieveUserQuotaSummaryAsync` is retried once.
- [x] If the retried quota contains groups, it is mapped to limit windows and returned as an `Official` `Snapshot` with status `Ok`.
- [x] If warmup fails or the retried quota is still empty, the provider gracefully continues to Layer 2/Layer 3 fallback without throwing.
- [x] Unit tests in `AntigravityUsageProviderTests` verify the empty-to-warmup-to-retry recovery path and the persistent-failure fallback path.

## Files Touched
- `src/TokenHound.Infrastructure/Providers/Antigravity/AntigravityUsageProvider.cs`
- `tests/TokenHound.Infrastructure.Tests/Providers/Antigravity/AntigravityUsageProviderTests.Warmup.cs`

## Verification
- `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*AntigravityUsageProviderTests*"`: Passed (13 passed, 0 failed).
- All Antigravity tests: Passed (46 passed, 0 failed).

## Handoff
- `AntigravityUsageProvider.TryGetLanguageServerSnapshotAsync` uses `GetOrWarmupQuotaSummaryAsync` to probe the language server and transparently warm up the session with `WarmupSessionAsync` upon empty quota groups, retrying once before cascading down the multi-tier waterfall.
- Comprehensive unit tests added in `AntigravityUsageProviderTests.Warmup.cs` covering empty-to-warmup recovery, failed warmup fallback to transcripts, and populated initial quota bypass.
- Task completed and ready for final code review.
