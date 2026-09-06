using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Core.Contracts;
using TokenHound.Core.Models;

namespace TokenHound.Infrastructure.Providers.Cursor;

/// <summary>
/// Implements the usage provider for Cursor with official web API allowance telemetry.
/// </summary>
public sealed class CursorUsageProvider : IUsageProvider, IDisposable
{
    private const string PROVIDER_ID = "cursor";

    private readonly CursorSessionDiscovery _discovery;
    private readonly CursorApiClient _client;
    private readonly bool _disposeClient;

    /// <inheritdoc />
    public string ProviderId
        => PROVIDER_ID;

    /// <summary>
    /// Initializes a new instance of the <see cref="CursorUsageProvider"/> class.
    /// </summary>
    /// <param name="discovery">Optional session discovery instance.</param>
    /// <param name="client">Optional Cursor API client.</param>
    public CursorUsageProvider(
        CursorSessionDiscovery? discovery = null,
        CursorApiClient? client = null)
    {
        _discovery = discovery ?? new CursorSessionDiscovery();

        if (client is not null)
        {
            _client = client;
            _disposeClient = false;
        }
        else
        {
            _client = new CursorApiClient();
            _disposeClient = true;
        }
    }

    /// <inheritdoc />
    public async ValueTask<Snapshot> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var auth = await _discovery.DiscoverAuthAsync(cancellationToken).ConfigureAwait(false);

        if (auth is null || string.IsNullOrWhiteSpace(auth.AccessToken) || string.IsNullOrWhiteSpace(auth.StripeMembershipAuthId))
        {
            return CreateNeedsAuthSnapshot();
        }

        var usage = await _client.GetUsageSummaryAsync(
            auth.StripeMembershipAuthId,
            auth.AccessToken,
            cancellationToken
        ).ConfigureAwait(false);

        if (usage is null)
        {
            return CreateStaleSnapshot("Failed to retrieve usage telemetry from Cursor API.");
        }

        return CreateSnapshotFromUsage(usage);
    }

    /// <inheritdoc />
    public void Dispose()
    {

        if (_disposeClient)
        {
            _client.Dispose();
        }
    }

    private static Snapshot CreateSnapshotFromUsage(CursorUsageResponse usage)
    {

        var windows = new List<LimitWindow>();
        var plan = usage.IndividualUsage?.Plan;

        if (plan is not null && plan.Enabled == true)
        {
            AddPlanWindows(windows, plan, usage.BillingCycleEnd);
        }

        var onDemand = usage.IndividualUsage?.OnDemand;

        if (onDemand is not null && onDemand.Enabled == true && onDemand.Limit.HasValue && onDemand.Limit.Value > 0)
        {
            AddOnDemandWindow(windows, onDemand, usage.BillingCycleEnd);
        }

        return new Snapshot
        {
            ProviderId = PROVIDER_ID,
            Status = ProviderStatus.Ok,
            Fidelity = Fidelity.Official,
            FetchedAtUtc = DateTimeOffset.UtcNow,
            LimitWindows = windows
        };
    }

    private static void AddPlanWindows(
        List<LimitWindow> windows,
        CursorPlanUsage plan,
        DateTimeOffset? resetTimeUtc)
    {

        var totalPercent = plan.TotalPercentUsed ?? 0.0;
        var usedFraction = Math.Clamp(totalPercent / 100.0, 0.0, 1.0);
        var remaining = (long)Math.Max(0, Math.Round(100.0 - totalPercent));

        windows.Add(new LimitWindow
        {
            Name = "Plan allowance",
            TotalUnits = 100L,
            RemainingUnits = remaining,
            UsedFraction = usedFraction,
            ResetTimeUtc = resetTimeUtc
        });

        if (plan.ApiPercentUsed.HasValue && plan.ApiPercentUsed.Value > 0)
        {

            var apiPercent = plan.ApiPercentUsed.Value;
            var apiUsedFraction = Math.Clamp(apiPercent / 100.0, 0.0, 1.0);
            var apiRemaining = (long)Math.Max(0, Math.Round(100.0 - apiPercent));

            windows.Add(new LimitWindow
            {
                Name = "API usage",
                TotalUnits = 100L,
                RemainingUnits = apiRemaining,
                UsedFraction = apiUsedFraction,
                ResetTimeUtc = resetTimeUtc
            });
        }
    }

    private static void AddOnDemandWindow(
        List<LimitWindow> windows,
        CursorOnDemandUsage onDemand,
        DateTimeOffset? resetTimeUtc)
    {

        var limitVal = onDemand.Limit!.Value;
        var usedVal = onDemand.Used ?? 0.0;
        var fraction = limitVal > 0 ? Math.Clamp(usedVal / limitVal, 0.0, 1.0) : 0.0;
        var limitUnits = (long)Math.Max(0, Math.Round(limitVal));
        var remainingUnits = (long)Math.Max(0, Math.Round(limitVal - usedVal));

        windows.Add(new LimitWindow
        {
            Name = "On-demand",
            TotalUnits = limitUnits,
            RemainingUnits = remainingUnits,
            UsedFraction = fraction,
            ResetTimeUtc = resetTimeUtc
        });
    }

    private static Snapshot CreateNeedsAuthSnapshot()
        => new()
        {
            ProviderId = PROVIDER_ID,
            Status = ProviderStatus.NeedsAuth,
            Fidelity = Fidelity.Official,
            FetchedAtUtc = DateTimeOffset.UtcNow,
            LimitWindows = [],
            ErrorDescription = "Open Cursor and log in to authenticate."
        };

    private static Snapshot CreateStaleSnapshot(string description)
        => new()
        {
            ProviderId = PROVIDER_ID,
            Status = ProviderStatus.Stale,
            Fidelity = Fidelity.Official,
            FetchedAtUtc = DateTimeOffset.UtcNow,
            LimitWindows = [],
            ErrorDescription = description
        };
}
