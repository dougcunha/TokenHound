using System;
using System.Collections.Generic;
using TokenHound.Core.Models;

namespace TokenHound.Infrastructure.Providers.Cline;

/// <summary>
/// Builds domain snapshots from borrowed Cline evidence.
/// </summary>
public sealed partial class ClineUsageProvider
{
    private sealed record ClineEvidence(
        ClineAccountUsage? Account,
        IReadOnlyList<LimitWindow> Windows,
        ClineLocalUsageSample Local);

    private Snapshot CreateOkSnapshot(DateTimeOffset nowUtc, ClineEvidence evidence)
    {

        var block = ResolveFreeLimitBlock(evidence.Local, nowUtc);

        return new()
        {
            ProviderId = PROVIDER_ID,
            Status = block is null ? ProviderStatus.Ok : ProviderStatus.RateLimited,
            Fidelity = Fidelity.Official,
            FetchedAtUtc = nowUtc,
            LimitWindows = evidence.Windows,
            ClineAccount = evidence.Account,
            ClineLocal = evidence.Local.Usage,
            ActiveBlock = block,
            ErrorDescription = block?.Reason
        };
    }

    private static UsageBlock? ResolveFreeLimitBlock(ClineLocalUsageSample local, DateTimeOffset nowUtc)
    {

        var hit = local.FreeLimit;

        if (hit is null || !hit.IsActive(nowUtc))
            return null;

        return new UsageBlock
        {
            Reason = FREE_LIMIT_REASON,
            IsBlocked = true,
            ResetTimeUtc = hit.ResetTimeUtc
        };
    }

    private Snapshot CreateRateLimitedSnapshot(
        DateTimeOffset nowUtc,
        ClineEvidence? evidence = null,
        string? errorDescription = null)
        => new()
        {
            ProviderId = PROVIDER_ID,
            Status = ProviderStatus.RateLimited,
            Fidelity = Fidelity.Official,
            FetchedAtUtc = nowUtc,
            LimitWindows = evidence?.Windows ?? _lastSuccessfulSnapshot?.LimitWindows ?? [],
            ClineAccount = evidence?.Account,
            ClineLocal = evidence?.Local.Usage,
            ActiveBlock = _activeRateLimitBlock,
            ErrorDescription = errorDescription ?? _activeRateLimitBlock?.Reason
        };

    private static Snapshot CreateNeedsAuthSnapshot(DateTimeOffset nowUtc, string? errorDescription = null)
        => new()
        {
            ProviderId = PROVIDER_ID,
            Status = ProviderStatus.NeedsAuth,
            Fidelity = Fidelity.Official,
            FetchedAtUtc = nowUtc,
            LimitWindows = [],
            ActiveBlock = null,
            ErrorDescription = errorDescription ?? NEEDS_AUTH_MESSAGE
        };

    private static Snapshot CreateAccessDeniedSnapshot(DateTimeOffset nowUtc)
        => new()
        {
            ProviderId = PROVIDER_ID,
            Status = ProviderStatus.AccessDenied,
            Fidelity = Fidelity.Official,
            FetchedAtUtc = nowUtc,
            LimitWindows = [],
            ActiveBlock = null,
            ErrorDescription = "Cline account access denied (HTTP 403)."
        };

    private Snapshot CreateStaleSnapshot(
        DateTimeOffset nowUtc,
        string description,
        ClineEvidence? evidence = null)
        => new()
        {
            ProviderId = PROVIDER_ID,
            Status = ProviderStatus.Stale,
            Fidelity = ResolveStaleFidelity(evidence),
            FetchedAtUtc = nowUtc,
            LimitWindows = evidence?.Windows is { Count: > 0 } windows
                ? windows
                : _lastSuccessfulSnapshot?.LimitWindows ?? [],
            ClineAccount = evidence?.Account,
            ClineLocal = evidence?.Local.Usage,
            ActiveBlock = _activeRateLimitBlock,
            ErrorDescription = description
        };

    private static Fidelity ResolveStaleFidelity(ClineEvidence? evidence)
        => evidence?.Account is null && evidence?.Local.Usage is not null
            ? Fidelity.Derived
            : Fidelity.Official;
}