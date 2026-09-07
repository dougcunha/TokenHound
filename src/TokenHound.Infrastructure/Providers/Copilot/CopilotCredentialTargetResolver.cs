using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace TokenHound.Infrastructure.Providers.Copilot;

/// <summary>
/// Derives Copilot CLI credential targets from validated host and login state.
/// </summary>
public sealed partial class CopilotCredentialTargetResolver
{
    /// <summary>The documented Copilot CLI service target.</summary>
    public const string COPILOT_SERVICE_TARGET = "copilot-cli";

    /// <summary>
    /// Resolves the service target and account-qualified targets present in JSONC state.
    /// </summary>
    /// <param name="jsonContent">Copilot config JSONC content.</param>
    /// <returns>Distinct, non-arbitrary target candidates in probe order.</returns>
    public IReadOnlyList<string> ResolveTargets(string? jsonContent)
        => ResolveTargets(CopilotConfigReader.ParseLoginState(jsonContent));

    /// <summary>
    /// Resolves the service target and account-qualified targets from parsed state.
    /// </summary>
    /// <param name="state">Validated Copilot login state.</param>
    /// <returns>Distinct target candidates in probe order.</returns>
    public IReadOnlyList<string> ResolveTargets(CopilotConfigReader.LoginState? state)
    {
        var targets = new List<string> { COPILOT_SERVICE_TARGET };

        if (state?.LastLoggedInUser is not null)
            AddQualifiedTarget(targets, state.LastLoggedInUser);

        if (state is not null)
        {
            foreach (var identity in state.LoggedInUsers)
                AddQualifiedTarget(targets, identity);
        }

        return targets;
    }

    private static void AddQualifiedTarget(
        ICollection<string> targets,
        CopilotConfigReader.LoginIdentity identity)
    {
        var host = NormalizeHost(identity.Host);

        if (host is null
            || string.IsNullOrWhiteSpace(identity.Login)
            || !SAFE_COMPONENT_REGEX.IsMatch(identity.Login))
            return;

        var target = $"https://{host}:{identity.Login}.copilot-cli";

        foreach (var existing in targets)
        {
            if (string.Equals(existing, target, StringComparison.OrdinalIgnoreCase))
                return;
        }

        if (!targets.Contains(target))
            targets.Add(target);
    }

    private static string? NormalizeHost(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var host = value.Trim();

        if (host.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            || host.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
        {
            if (!Uri.TryCreate(host, UriKind.Absolute, out var uri)
                || !string.IsNullOrWhiteSpace(uri.UserInfo)
                || uri.AbsolutePath != "/"
                || !string.IsNullOrWhiteSpace(uri.Query)
                || !string.IsNullOrWhiteSpace(uri.Fragment))
                return null;

            host = uri.Host;
        }

        return SAFE_COMPONENT_REGEX.IsMatch(host) ? host : null;
    }

    [GeneratedRegex("^[A-Za-z0-9][A-Za-z0-9.-]*$")]
    private static partial Regex SafeComponentRegex();

    private static Regex SAFE_COMPONENT_REGEX
        => SafeComponentRegex();
}
