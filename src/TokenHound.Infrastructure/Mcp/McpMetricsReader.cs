using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using TokenHound.Core.Models;
using TokenHound.Infrastructure.Engine;

namespace TokenHound.Infrastructure.Mcp;

/// <summary>Projects current store readings into safe MCP metrics.</summary>
public sealed partial class McpMetricsReader
{
    private const string MOCK_PROVIDER_ID = "mock";

    private readonly UsageStore _store;
    private readonly TimeProvider _timeProvider;

    /// <summary>Creates a reader over the existing usage store.</summary>
    public McpMetricsReader(UsageStore store, TimeProvider? timeProvider = null)
    {

        _store = store ?? throw new ArgumentNullException(nameof(store));
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>Lists current metrics for registered, enabled real providers.</summary>
    public McpMetricsListResult ListMetrics()
    {

        var providers = new List<McpProviderMetrics>();

        foreach (var providerId in _store.RegisteredProviderIds.OrderBy(static id => id, StringComparer.OrdinalIgnoreCase))
        {

            if (IsSynthetic(providerId) || !_store.IsProviderEnabled(providerId))
                continue;

            if (_store.GetMetricsState(providerId) is { } state)
                providers.Add(MapProvider(state));
        }

        return new McpMetricsListResult
        {
            ObservedAtUtc = _timeProvider.GetUtcNow(),
            Providers = providers
        };
    }

    /// <summary>Gets metrics for one provider or a precise unavailability state.</summary>
    public McpMetricsLookupResult GetMetrics(string providerId)
    {

        ArgumentException.ThrowIfNullOrWhiteSpace(providerId);

        var lookupState = GetLookupState(providerId);
        var state = lookupState == "available" ? _store.GetMetricsState(providerId) : null;
        var provider = lookupState == "available"
            && state is { } available
                ? MapProvider(available)
                : null;

        return new McpMetricsLookupResult
        {
            LookupState = provider is null && lookupState == "available" ? "pending" : lookupState,
            ObservedAtUtc = _timeProvider.GetUtcNow(),
            Provider = provider
        };
    }

    private string GetLookupState(string providerId)
    {

        if (IsSynthetic(providerId))
            return "synthetic";

        if (!_store.RegisteredProviderIds.Contains(providerId, StringComparer.OrdinalIgnoreCase))
            return "unknown";

        if (!_store.IsProviderEnabled(providerId))
            return "disabled";

        return _store.GetMetricsState(providerId) is null ? "pending" : "available";
    }

    private static McpProviderMetrics MapProvider(UsageStore.MetricsState state)
    {

        var snapshot = state.Snapshot;

        return new McpProviderMetrics
        {
            ProviderId = snapshot.ProviderId,
            Status = FormatEnum(snapshot.Status),
            Fidelity = FormatEnum(snapshot.Fidelity),
            SnapshotFetchedAtUtc = snapshot.FetchedAtUtc,
            LastSuccessfulAtUtc = state.LastSuccessfulAtUtc,
            LimitWindows = snapshot.LimitWindows.Select(MapWindow).ToArray(),
            ActiveBlock = MapBlock(snapshot.ActiveBlock),
            CopilotBilling = MapCopilotBilling(snapshot.CopilotBilling),
            ClineAccount = MapClineAccount(snapshot.ClineAccount),
            ClineLocal = MapClineLocal(snapshot.ClineLocal)
        };
    }

    private static McpLimitWindowMetrics MapWindow(LimitWindow window)
    {

        return new McpLimitWindowMetrics
        {
            Name = window.Name,
            GroupName = window.GroupName,
            UsedFraction = window.UsedFraction,
            UsedUnits = window.UsedUnits,
            RemainingUnits = window.RemainingUnits,
            RemainingValue = window.RemainingValue,
            TotalUnits = window.TotalUnits,
            ResetTimeUtc = window.ResetTimeUtc,
            PeriodSeconds = window.Period?.TotalSeconds
        };
    }

    private static McpBlockMetrics? MapBlock(UsageBlock? block)
    {

        return block is null ? null : new McpBlockMetrics
        {
            IsBlocked = block.IsBlocked,
            ResetTimeUtc = block.ResetTimeUtc,
            RetryAfterSeconds = block.RetryAfterSeconds
        };
    }

    private static bool IsSynthetic(string providerId)
        => string.Equals(providerId, MOCK_PROVIDER_ID, StringComparison.OrdinalIgnoreCase);

    private static string FormatEnum<T>(T value) where T : struct, Enum
        => JsonNamingPolicy.CamelCase.ConvertName(value.ToString());
}
