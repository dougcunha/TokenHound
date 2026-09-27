# Technical Specification: Antigravity Quota Retrieval Warmup

## 1. Overview & Architecture

### Context
When the Antigravity Language Server starts, its internal `QuotaSummaryCache` can be empty until a user status check occurs. TokenHound communicates with the Language Server via local HTTPS/HTTP RPC. Adding a session warmup step via `GetUserStatus` ensures the server populates its internal cache before TokenHound concludes Layer 1 is empty and degrades to transcripts.

### Component Design
The change is localized to `TokenHound.Infrastructure.Providers.Antigravity`:

1. **`AntigravityLanguageServerClient`**:
   - Introduce `WarmupSessionAsync(AntigravityEndpoint endpoint, CancellationToken cancellationToken = default)` returning `ValueTask<bool>`.
   - Sends a POST request with `{}` body to:
     `{scheme}://127.0.0.1:{port}/exa.language_server_pb.LanguageServerService/GetUserStatus`
     with `x-codeium-csrf-token` header.
   - Probes `endpoint.CandidatePorts` (HTTPS then HTTP fallback), returning `true` on the first HTTP success (200), or `false` if all fail.

2. **`AntigravityUsageProvider`**:
   - In `TryGetLanguageServerSnapshotAsync`:
     If `RetrieveUserQuotaSummaryAsync` returns `null` or empty groups, execute `WarmupSessionAsync(endpoint, cancellationToken)`.
     If warmup succeeds, retry `RetrieveUserQuotaSummaryAsync` once.
     If the retried response contains quota groups, proceed to map `LimitWindow`s.
     If still empty or if warmup failed, proceed along the existing waterfall.

---

## 2. Contracts and Method Signatures

### `AntigravityLanguageServerClient.cs`
```csharp
/// <summary>
/// Warms up the Antigravity session by querying user status across candidate ports.
/// </summary>
/// <param name="endpoint">The discovered Language Server endpoint.</param>
/// <param name="cancellationToken">Cancellation token.</param>
/// <returns>True if any candidate port responded with success; otherwise, false.</returns>
public async ValueTask<bool> WarmupSessionAsync(
    AntigravityEndpoint endpoint,
    CancellationToken cancellationToken = default)
```

### Flow in `AntigravityUsageProvider.cs`
```csharp
private async ValueTask<(Snapshot? Snapshot, bool IsRunning)> TryGetLanguageServerSnapshotAsync(
    CancellationToken cancellationToken)
{
    var endpoint = _discovery.DiscoverEndpoint();

    if (endpoint is null)
    {
        return (null, false);
    }

    var quota = await _client.RetrieveUserQuotaSummaryAsync(endpoint, cancellationToken).ConfigureAwait(false);

    if (quota?.Groups is null || quota.Groups.Count == 0)
    {
        var warmedUp = await _client.WarmupSessionAsync(endpoint, cancellationToken).ConfigureAwait(false);

        if (warmedUp)
        {
            quota = await _client.RetrieveUserQuotaSummaryAsync(endpoint, cancellationToken).ConfigureAwait(false);
        }
    }

    if (quota?.Groups is null || quota.Groups.Count == 0)
    {
        return (null, true);
    }

    var windows = MapQuotaGroups(quota.Groups);

    if (windows.Count == 0)
        return (null, true);

    _consecutiveRateLimits = 0;

    return (CreateOfficialSnapshot(windows), true);
}
```

---

## 3. Invariants & Code Standards
- **Pure Core**: No modifications to `TokenHound.Core`.
- **Async Pattern**: All async calls propagate `cancellationToken` and use `.ConfigureAwait(false)`.
- **Style Rules**: Methods <= 30 lines, classes <= 300 lines, XML docs on public members, file-scoped namespaces.
- **Error Handling**: Network exceptions inside `WarmupSessionAsync` are trapped and return `false`, preventing unhandled exceptions.

---

## 4. Test Strategy

### Unit Testing (`TokenHound.Infrastructure.Tests`)
- `WarmupSessionAsync_WhenPortRespondsOk_ReturnsTrue`
- `WarmupSessionAsync_WhenAllPortsFail_ReturnsFalse`
- `GetSnapshotAsync_WhenFirstCallEmptyAndWarmupSucceeds_ReturnsOfficialSnapshot`
- `GetSnapshotAsync_WhenFirstCallEmptyAndWarmupFails_FallsBackToDerivedTranscript`

### Manual Acceptance Script
1. Verify unit tests pass via `rtk dotnet test`.
2. Run live integration test `AntigravityLiveUsageProviderTests` against the running environment to ensure `GetSnapshotAsync` resolves without errors.
