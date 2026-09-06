using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Core.Contracts;
using TokenHound.Core.Models;

namespace TokenHound.Infrastructure.Providers.Codex;

/// <summary>Adapts Codex app-server and rollout telemetry into usage snapshots.</summary>
public sealed class CodexUsageProvider : IUsageProvider
{
    /// <summary>The unique provider identifier for Codex.</summary>
    public const string PROVIDER_ID = "codex";

    /// <summary>The actionable message used when Codex authentication is unavailable.</summary>
    public const string NEEDS_AUTH_MESSAGE = "Execute 'codex login' in terminal";

    private const string PRIMARY_WINDOW_NAME = "Primary (5h)";
    private const string SECONDARY_WINDOW_NAME = "Secondary (Weekly)";
    private const string NO_TELEMETRY_MESSAGE = "Codex app server and rollout logs returned no rate limits.";

    private readonly CodexAppServerClient _appServerClient;
    private readonly CodexAuthDiscovery _authDiscovery;
    private readonly CodexRolloutLogReader _rolloutLogReader;

    /// <summary>Initializes a provider with default Codex discovery services.</summary>
    public CodexUsageProvider(
        CodexAppServerClient? appServerClient = null,
        CodexRolloutLogReader? rolloutLogReader = null,
        CodexAuthDiscovery? authDiscovery = null)
    {

        _appServerClient = appServerClient ?? new CodexAppServerClient();
        _rolloutLogReader = rolloutLogReader ?? new CodexRolloutLogReader();
        _authDiscovery = authDiscovery ?? new CodexAuthDiscovery();
    }

    /// <inheritdoc />
    public string ProviderId
        => PROVIDER_ID;

    /// <inheritdoc />
    public async ValueTask<Snapshot> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {

        cancellationToken.ThrowIfCancellationRequested();

        var appServerLimits = await _appServerClient.GetRateLimitsAsync(cancellationToken).ConfigureAwait(false);

        if (appServerLimits is not null)
            return CreateTelemetrySnapshot(appServerLimits, Fidelity.Official);

        var rolloutLimits = await _rolloutLogReader.ReadLatestRateLimitsAsync(cancellationToken).ConfigureAwait(false);

        if (rolloutLimits is not null)
            return CreateTelemetrySnapshot(rolloutLimits, Fidelity.Derived);

        var account = await _authDiscovery.DiscoverAsync(cancellationToken).ConfigureAwait(false);

        return account is null
            ? CreateNeedsAuthSnapshot()
            : CreateNoTelemetrySnapshot();
    }

    private static Snapshot CreateTelemetrySnapshot(CodexRateLimitsDto limits, Fidelity fidelity)
    {

        var windows = MapWindows(limits);
        var rateLimitType = limits.RateLimitReachedType;
        var activeBlock = string.IsNullOrWhiteSpace(rateLimitType)
            ? null
            : CreateUsageBlock(rateLimitType, limits);

        return new Snapshot
        {
            ProviderId = PROVIDER_ID,
            Status = activeBlock is null ? ProviderStatus.Ok : ProviderStatus.RateLimited,
            Fidelity = fidelity,
            FetchedAtUtc = DateTimeOffset.UtcNow,
            LimitWindows = windows,
            ActiveBlock = activeBlock,
            ErrorDescription = null
        };
    }

    private static IReadOnlyList<LimitWindow> MapWindows(CodexRateLimitsDto limits)
    {

        var windows = new List<LimitWindow>(2);
        var primary = MapWindow(PRIMARY_WINDOW_NAME, limits.Primary);
        var secondary = MapWindow(SECONDARY_WINDOW_NAME, limits.Secondary);

        if (primary is not null)
            windows.Add(primary);

        if (secondary is not null)
            windows.Add(secondary);

        return windows;
    }

    private static LimitWindow? MapWindow(
        string name,
        CodexRateLimitsDto.RateLimitWindow? source)
    {

        if (source?.UsedPercent is not double usedPercent || usedPercent is < 0 or > 100)
            return null;

        return new LimitWindow
        {
            Name = name,
            UsedFraction = usedPercent / 100.0,
            TotalUnits = 100,
            RemainingUnits = (long)Math.Round(100.0 - usedPercent),
            ResetTimeUtc = ConvertResetTime(source.ResetsAt),
            Period = ConvertPeriod(source.WindowDurationMins)
        };
    }

    private static UsageBlock CreateUsageBlock(
        string rateLimitType,
        CodexRateLimitsDto limits)
        => new()
        {
            Reason = rateLimitType,
            IsBlocked = true,
            ResetTimeUtc = ConvertResetTime(limits.Primary?.ResetsAt) ?? ConvertResetTime(limits.Secondary?.ResetsAt),
            RetryAfterSeconds = null
        };

    private static DateTimeOffset? ConvertResetTime(long? unixSeconds)
    {

        if (unixSeconds is null)
            return null;

        try
        {

            return DateTimeOffset.FromUnixTimeSeconds(unixSeconds.Value);
        }
        catch (ArgumentOutOfRangeException)
        {

            return null;
        }
    }

    private static TimeSpan? ConvertPeriod(int? windowDurationMins)
    {

        if (windowDurationMins is null or < 0)
            return null;

        try
        {

            return TimeSpan.FromMinutes(windowDurationMins.Value);
        }
        catch (OverflowException)
        {

            return null;
        }
    }

    private static Snapshot CreateNeedsAuthSnapshot()
        => new()
        {
            ProviderId = PROVIDER_ID,
            Status = ProviderStatus.NeedsAuth,
            Fidelity = Fidelity.Official,
            FetchedAtUtc = DateTimeOffset.UtcNow,
            LimitWindows = [],
            ActiveBlock = null,
            ErrorDescription = NEEDS_AUTH_MESSAGE
        };

    private static Snapshot CreateNoTelemetrySnapshot()
        => new()
        {
            ProviderId = PROVIDER_ID,
            Status = ProviderStatus.Stale,
            Fidelity = Fidelity.Derived,
            FetchedAtUtc = DateTimeOffset.UtcNow,
            LimitWindows = [],
            ActiveBlock = null,
            ErrorDescription = NO_TELEMETRY_MESSAGE
        };
}
