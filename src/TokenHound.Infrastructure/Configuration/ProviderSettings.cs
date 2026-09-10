using System;
using System.Collections.Generic;

namespace TokenHound.Infrastructure.Configuration;

/// <summary>
/// Persisted monitoring enablement of every provider, keyed by runtime provider identifier.
/// </summary>
public sealed record ProviderSettings
{
    private const string ALIAS_PROVIDER_KEY = "antigravity";
    private const string CANONICAL_PROVIDER_KEY = "gemini";

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
        var hasCanonicalState = TryGetCanonicalState(states, out var canonicalState);

        foreach (var state in states)
            AddNormalizedState(normalized, state, hasCanonicalState);

        if (hasCanonicalState)
            normalized[CANONICAL_PROVIDER_KEY] = canonicalState;

        return normalized;
    }

    private static void AddNormalizedState(
        Dictionary<string, bool> normalized,
        KeyValuePair<string, bool> state,
        bool hasCanonicalState)
    {

        if (string.IsNullOrWhiteSpace(state.Key))
            return;

        var providerId = state.Key.Trim();

        if (string.Equals(providerId, ALIAS_PROVIDER_KEY, StringComparison.OrdinalIgnoreCase))
        {
            if (!hasCanonicalState)
                normalized[CANONICAL_PROVIDER_KEY] = state.Value;

            return;
        }

        normalized[providerId] = state.Value;
    }

    private static bool TryGetCanonicalState(
        IReadOnlyDictionary<string, bool> states,
        out bool isEnabled)
    {

        foreach (var state in states)
        {

            if (!string.Equals(state.Key?.Trim(), CANONICAL_PROVIDER_KEY, StringComparison.OrdinalIgnoreCase))
                continue;

            isEnabled = state.Value;

            return true;
        }

        isEnabled = true;

        return false;
    }
}
