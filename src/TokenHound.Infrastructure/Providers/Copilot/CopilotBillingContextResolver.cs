using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Core.Models;
using TokenHound.Infrastructure.Storage;

namespace TokenHound.Infrastructure.Providers.Copilot;

/// <summary>
/// Resolves and verifies the Copilot billing owner context from identity hints and seat assignments.
/// </summary>
public sealed class CopilotBillingContextResolver
{
    private static readonly Regex REPOSITORY_REGEX = new(
        @"repository:\s*[""']?([^/\s""']+)/",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );

    private readonly CopilotBillingClient _client;
    private readonly string _sessionStateDirectory;

    /// <summary>
    /// Initializes a resolver with the specified billing client and optional session state directory.
    /// </summary>
    /// <param name="client">The billing client for seat verification and identity lookup.</param>
    /// <param name="sessionStateDirectory">An optional directory path for Copilot session state.</param>
    public CopilotBillingContextResolver(
        CopilotBillingClient client,
        string? sessionStateDirectory = null)
    {

        ArgumentNullException.ThrowIfNull(client);

        _client = client;
        _sessionStateDirectory = sessionStateDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".copilot",
            "session-state"
        );
    }

    /// <summary>
    /// Resolves the verified billing context for the authenticated principal.
    /// </summary>
    /// <param name="accessToken">The borrowed bearer token.</param>
    /// <param name="quotaResponse">The internal quota response containing identity hints, if available.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A verified, ambiguous, or unknown billing context.</returns>
    public async Task<CopilotBillingContext> ResolveContextAsync(
        string accessToken,
        CopilotQuotaResponse? quotaResponse,
        CancellationToken cancellationToken = default)
    {

        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);
        cancellationToken.ThrowIfCancellationRequested();

        var principal = await ResolvePrincipalAsync(accessToken, quotaResponse, cancellationToken).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(principal))
            return CreateUnknownContext(string.Empty, CopilotPlanType.Unknown, quotaResponse?.CopilotPlan);

        var plan = NormalizePlan(quotaResponse?.CopilotPlan);
        var candidates = CollectCandidates(quotaResponse);

        if (candidates.Count == 0)
            return CreateUnknownContext(principal, plan, quotaResponse?.CopilotPlan);

        return await VerifyCandidatesAsync(
            principal,
            plan,
            quotaResponse?.CopilotPlan,
            candidates,
            accessToken,
            cancellationToken
        ).ConfigureAwait(false);
    }

    private async Task<string?> ResolvePrincipalAsync(
        string accessToken,
        CopilotQuotaResponse? quotaResponse,
        CancellationToken cancellationToken)
    {

        if (!string.IsNullOrWhiteSpace(quotaResponse?.Login))
            return quotaResponse.Login;

        try
        {

            return await _client.GetAuthenticatedUserLoginAsync(accessToken, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception)
        {

            return null;
        }
    }

    private List<string> CollectCandidates(CopilotQuotaResponse? quotaResponse)
    {

        var candidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (quotaResponse?.OrganizationLoginList is not null)
        {
            foreach (var org in quotaResponse.OrganizationLoginList)
            {
                if (!string.IsNullOrWhiteSpace(org))
                    candidates.Add(org.Trim());
            }
        }

        var workspaceHints = DiscoverWorkspaceOrganizationHints();

        foreach (var hint in workspaceHints)
            candidates.Add(hint);

        return [.. candidates];
    }

    private async Task<CopilotBillingContext> VerifyCandidatesAsync(
        string principal,
        CopilotPlanType plan,
        string? rawPlan,
        List<string> candidates,
        string accessToken,
        CancellationToken cancellationToken)
    {

        var verifiedOrgs = new List<string>();

        foreach (var candidate in candidates)
        {

            cancellationToken.ThrowIfCancellationRequested();

            var isVerified = await CheckSeatAssignmentAsync(
                candidate,
                principal,
                accessToken,
                cancellationToken
            ).ConfigureAwait(false);

            if (isVerified)
                verifiedOrgs.Add(candidate);
        }

        if (verifiedOrgs.Count == 1)
        {
            var verifiedOrg = verifiedOrgs[0];
            var effectivePlan = plan == CopilotPlanType.Unknown ? CopilotPlanType.Business : plan;

            return new CopilotBillingContext
            {
                PrincipalId = principal,
                Scope = CopilotBillingScope.Organization,
                OwnerId = verifiedOrg,
                OwnerName = verifiedOrg,
                Plan = effectivePlan,
                RawPlan = rawPlan,
                EvidenceKey = $"seats:{verifiedOrg}"
            };
        }

        if (verifiedOrgs.Count > 1)
        {
            return new CopilotBillingContext
            {
                PrincipalId = principal,
                Scope = CopilotBillingScope.Unknown,
                Plan = plan,
                RawPlan = rawPlan,
                EvidenceKey = "ambiguous"
            };
        }

        return CreateUnknownContext(principal, plan, rawPlan);
    }

    private async Task<bool> CheckSeatAssignmentAsync(
        string organization,
        string principal,
        string accessToken,
        CancellationToken cancellationToken)
    {

        try
        {

            var seatResponse = await _client.GetSeatAssignmentsAsync(organization, accessToken, cancellationToken).ConfigureAwait(false);

            if (seatResponse.Seats is null)
                return false;

            return seatResponse.Seats.Any(s =>
                string.Equals(s.Assignee?.Login, principal, StringComparison.OrdinalIgnoreCase)
                && (string.Equals(s.PlanType, "business", StringComparison.OrdinalIgnoreCase) || s.PlanType is not null));
        }
        catch (RateLimitBlockedException)
        {

            throw;
        }
        catch (Exception)
        {

            return false;
        }
    }

    private IReadOnlyList<string> DiscoverWorkspaceOrganizationHints()
    {

        if (!Directory.Exists(_sessionStateDirectory))
            return [];

        var hints = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        try
        {

            var workspaceFiles = Directory.EnumerateFiles(_sessionStateDirectory, "workspace.yaml", SearchOption.AllDirectories);

            foreach (var file in workspaceFiles)
            {

                var text = SharedFileReader.ReadAllTextAsync(file).GetAwaiter().GetResult();

                if (string.IsNullOrWhiteSpace(text))
                    continue;

                var match = REPOSITORY_REGEX.Match(text);

                if (match.Success)
                    hints.Add(match.Groups[1].Value);
            }
        }
        catch (Exception)
        {

            // Corroboration is best-effort.
        }

        return [.. hints];
    }

    private static CopilotPlanType NormalizePlan(string? planName)
    {

        if (string.IsNullOrWhiteSpace(planName))
            return CopilotPlanType.Unknown;

        return planName.Trim().ToLowerInvariant() switch
        {
            "business" => CopilotPlanType.Business,
            "enterprise" => CopilotPlanType.Enterprise,
            "pro" => CopilotPlanType.Pro,
            "pro_plus" or "pro+" or "proplus" => CopilotPlanType.ProPlus,
            "free" => CopilotPlanType.Free,
            "max" => CopilotPlanType.Max,
            _ => CopilotPlanType.Unknown
        };
    }

    private static CopilotBillingContext CreateUnknownContext(
        string principal,
        CopilotPlanType plan,
        string? rawPlan)
        => new()
        {
            PrincipalId = principal,
            Scope = CopilotBillingScope.Unknown,
            Plan = plan,
            RawPlan = rawPlan,
            EvidenceKey = null
        };
}
