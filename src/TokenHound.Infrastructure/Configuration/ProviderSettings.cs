using System;
using System.Collections.Generic;

namespace TokenHound.Infrastructure.Configuration;

/// <summary>
/// Persisted monitoring enablement of every provider, keyed by runtime provider identifier.
/// </summary>
public sealed record ProviderSettings
{
    private static readonly IReadOnlyDictionary<string, bool> EMPTY_STATES
        = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

    private readonly IReadOnlyDictionary<string, bool> _enabledStates = EMPTY_STATES;

    /// <summary>
    /// Gets the stored enablement flag of every persisted provider identifier, compared case-insensitively.
    /// </summary>
    public IReadOnlyDictionary<string, bool> EnabledStates
    {
        get => _enabledStates;
        init => _enabledStates = Normalize(value);
    }

    /// <summary>
    /// Reports whether the supplied provider is monitored, defaulting to enabled when no preference is stored.
    /// </summary>
    /// <param name="providerId">The runtime provider identifier to inspect.</param>
    /// <returns><see langword="true"/> when monitoring is enabled; otherwise <see langword="false"/>.</returns>
    public bool IsEnabled(string providerId)
    {

        if (string.IsNullOrWhiteSpace(providerId))
            return true;

        return !_enabledStates.TryGetValue(providerId, out var isEnabled) || isEnabled;
    }

    private static IReadOnlyDictionary<string, bool> Normalize(IReadOnlyDictionary<string, bool>? states)
    {

        if (states is null || states.Count == 0)
            return EMPTY_STATES;

        var normalized = new Dictionary<string, bool>(states.Count, StringComparer.OrdinalIgnoreCase);

        foreach (var state in states)
        {

            if (string.IsNullOrWhiteSpace(state.Key))
                continue;

            normalized[state.Key.Trim()] = state.Value;
        }

        return normalized;
    }
}
