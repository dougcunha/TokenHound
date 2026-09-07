using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using TokenHound.Core.Models;

namespace TokenHound.Infrastructure.Providers.Copilot;

/// <summary>
/// Parses Copilot's open quota map without inventing a quota category or denominator.
/// </summary>
public static class CopilotQuotaParser
{
    private const string PREMIUM_CATEGORY = "premium_interactions";
    private const string WINDOW_NAME = "Monthly Premium Interactions";

    /// <summary>
    /// Parses a deserialized Copilot quota response.
    /// </summary>
    /// <param name="response">The response to validate and parse.</param>
    /// <returns>A finite quota, no-entitlement result, or schema-drift result.</returns>
    public static CopilotQuotaParseResult Parse(CopilotQuotaResponse? response)
    {
        if (response?.QuotaSnapshots is null)
            return CopilotQuotaParseResult.SchemaDrift("Copilot quota_snapshots is missing.");

        var candidate = FindCandidate(response.QuotaSnapshots);

        if (candidate.Status == CandidateStatus.NoFiniteQuota)
            return CopilotQuotaParseResult.NoFiniteQuota();

        if (candidate.Snapshot is null)
            return CopilotQuotaParseResult.SchemaDrift("Copilot quota category fields are invalid.");

        if (!TryParseReset(response.QuotaResetDateUtc, out var resetTimeUtc))
            return CopilotQuotaParseResult.SchemaDrift("Copilot quota reset date is invalid.");

        var quota = candidate.Snapshot;
        var remaining = quota.Remaining!.Value;
        var entitlement = quota.Entitlement!.Value;
        var percentRemaining = quota.PercentRemaining!.Value;

        return CopilotQuotaParseResult.Success(
            new LimitWindow
            {
                Name = WINDOW_NAME,
                UsedFraction = 1.0 - (percentRemaining / 100.0),
                RemainingUnits = remaining,
                RemainingValue = quota.QuotaRemaining!.Value,
                TotalUnits = entitlement,
                ResetTimeUtc = resetTimeUtc
            },
            remaining,
            quota.OveragePermitted!.Value,
            resetTimeUtc
        );
    }

    /// <summary>Deserializes and parses a Copilot quota JSON response.</summary>
    /// <param name="jsonContent">The response JSON.</param>
    /// <returns>A parser outcome that never fabricates a quota value.</returns>
    public static CopilotQuotaParseResult ParseJson(string? jsonContent)
    {
        if (string.IsNullOrWhiteSpace(jsonContent))
            return CopilotQuotaParseResult.SchemaDrift("Copilot quota response was empty.");

        try
        {
            return Parse(JsonSerializer.Deserialize<CopilotQuotaResponse>(jsonContent, JSON_OPTIONS));
        }
        catch (JsonException)
        {
            return CopilotQuotaParseResult.SchemaDrift("Copilot quota response schema was not recognized.");
        }
    }

    private static Candidate FindCandidate(
        IReadOnlyDictionary<string, CopilotQuotaSnapshotDto> snapshots)
    {
        var candidateSeen = false;

        if (TryGetIgnoreCase(snapshots, PREMIUM_CATEGORY, out var premium))
        {
            var premiumResult = ValidateCandidate(PREMIUM_CATEGORY, premium);

            if (premiumResult.Status == CandidateStatus.Valid)
                return premiumResult;

            candidateSeen |= premiumResult.Status == CandidateStatus.Invalid;
        }

        foreach (var snapshot in snapshots)
        {
            if (string.Equals(snapshot.Key, PREMIUM_CATEGORY, StringComparison.OrdinalIgnoreCase))
                continue;

            var result = ValidateCandidate(snapshot.Key, snapshot.Value);

            if (result.Status == CandidateStatus.Valid)
                return result;

            candidateSeen |= result.Status == CandidateStatus.Invalid;
        }

        return candidateSeen
            ? new Candidate(CandidateStatus.Invalid, null)
            : new Candidate(CandidateStatus.NoFiniteQuota, null);
    }

    private static Candidate ValidateCandidate(
        string mapKey,
        CopilotQuotaSnapshotDto? snapshot)
    {
        if (snapshot is null)
            return new Candidate(CandidateStatus.Invalid, null);

        if (snapshot.HasQuota == false || snapshot.Unlimited == true)
            return new Candidate(CandidateStatus.NoFiniteQuota, null);

        if (snapshot.HasQuota is null || snapshot.Unlimited is null)
            return new Candidate(CandidateStatus.Invalid, null);

        if (snapshot.Entitlement is not > 0
            || string.IsNullOrWhiteSpace(snapshot.QuotaId) && string.IsNullOrWhiteSpace(mapKey)
            || snapshot.Remaining is null
            || !IsFinite(snapshot.QuotaRemaining)
            || !IsFinite(snapshot.PercentRemaining)
            || snapshot.PercentRemaining is < 0 or > 100
            || snapshot.OveragePermitted is null)
            return new Candidate(CandidateStatus.Invalid, null);

        return new Candidate(CandidateStatus.Valid, snapshot);
    }

    private static bool TryGetIgnoreCase(
        IReadOnlyDictionary<string, CopilotQuotaSnapshotDto> snapshots,
        string key,
        out CopilotQuotaSnapshotDto? snapshot)
    {
        foreach (var pair in snapshots)
        {
            if (string.Equals(pair.Key, key, StringComparison.OrdinalIgnoreCase))
            {
                snapshot = pair.Value;
                return true;
            }
        }

        snapshot = null;
        return false;
    }

    private static bool IsFinite(double? value)
        => value.HasValue && double.IsFinite(value.Value);

    private static bool TryParseReset(
        string? value,
        out DateTimeOffset resetTimeUtc)
    {
        if (DateTimeOffset.TryParse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind,
            out var parsed)
            && parsed.Offset == TimeSpan.Zero)
        {
            resetTimeUtc = parsed.ToUniversalTime();
            return true;
        }

        resetTimeUtc = default;
        return false;
    }

    private enum CandidateStatus
    {
        Valid,
        Invalid,
        NoFiniteQuota
    }

    private sealed record Candidate(
        CandidateStatus Status,
        CopilotQuotaSnapshotDto? Snapshot);

    private static readonly JsonSerializerOptions JSON_OPTIONS = new()
    {
        PropertyNameCaseInsensitive = true
    };
}

/// <summary>
/// Describes the outcome of parsing the Copilot quota map.
/// </summary>
public sealed record CopilotQuotaParseResult
{
    private CopilotQuotaParseResult()
    {
    }

    /// <summary>Gets the parser outcome.</summary>
    public required CopilotQuotaParseStatus Status { get; init; }

    /// <summary>Gets the finite quota window, when parsing succeeded.</summary>
    public LimitWindow? LimitWindow { get; init; }

    /// <summary>Gets the integer remainder used for overage classification.</summary>
    public int? Remaining { get; init; }

    /// <summary>Gets whether metered overage continues after exhaustion.</summary>
    public bool? OveragePermitted { get; init; }

    /// <summary>Gets the validated quota reset instant.</summary>
    public DateTimeOffset? ResetTimeUtc { get; init; }

    /// <summary>Gets a non-sensitive schema diagnostic.</summary>
    public string? ErrorDescription { get; init; }

    internal static CopilotQuotaParseResult Success(
        LimitWindow limitWindow,
        int remaining,
        bool overagePermitted,
        DateTimeOffset resetTimeUtc)
        => new()
        {
            Status = CopilotQuotaParseStatus.Success,
            LimitWindow = limitWindow,
            Remaining = remaining,
            OveragePermitted = overagePermitted,
            ResetTimeUtc = resetTimeUtc
        };

    internal static CopilotQuotaParseResult NoFiniteQuota()
        => new() { Status = CopilotQuotaParseStatus.NoFiniteQuota };

    internal static CopilotQuotaParseResult SchemaDrift(string description)
        => new()
        {
            Status = CopilotQuotaParseStatus.SchemaDrift,
            ErrorDescription = description
        };
}

/// <summary>
/// Classifies Copilot quota parsing outcomes.
/// </summary>
public enum CopilotQuotaParseStatus
{
    /// <summary>A valid finite quota category was found.</summary>
    Success,

    /// <summary>The response map is valid but has no finite entitled category.</summary>
    NoFiniteQuota,

    /// <summary>The response cannot be interpreted without inventing data.</summary>
    SchemaDrift
}
