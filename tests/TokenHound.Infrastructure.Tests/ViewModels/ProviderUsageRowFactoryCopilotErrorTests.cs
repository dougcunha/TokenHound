using AwesomeAssertions;
using System;
using System.Collections.Generic;
using TokenHound.App.ViewModels;
using TokenHound.Core.Models;
using Xunit;

namespace TokenHound.Infrastructure.Tests.ViewModels;

/// <summary>
/// Verifies error, rate limit, stale, and provider isolation behavior in <see cref="ProviderUsageRowFactory"/>.
/// </summary>
public sealed class ProviderUsageRowFactoryCopilotErrorTests
{
    private static readonly DateTimeOffset FIXED_NOW = new(2026, 9, 8, 12, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// Verifies that rate-limited stale billing displays retry countdown and stale provenance.
    /// </summary>
    [Fact]
    public void CreateRows_WhenStaleRateLimitedBilling_DisplaysRetryCountdownAndStaleFlag()
    {

        var timeProvider = new FixedTimeProvider(FIXED_NOW);
        var retryAfter = FIXED_NOW.AddSeconds(45);
        var snapshot = new Snapshot
        {
            ProviderId = "copilot",
            Status = ProviderStatus.Ok,
            Fidelity = Fidelity.Official,
            FetchedAtUtc = FIXED_NOW,
            LimitWindows = [],
            CopilotBilling = new CopilotBillingStatus
            {
                State = CopilotBillingState.Stale,
                Reason = CopilotBillingReason.RateLimited,
                AttemptedAtUtc = FIXED_NOW,
                NextRequestAtUtc = retryAfter,
                Usage = new CopilotCreditUsage
                {
                    Context = new CopilotBillingContext
                    {
                        PrincipalId = "user-1",
                        Scope = CopilotBillingScope.Organization,
                        OwnerName = "ColibriAgile",
                        Plan = CopilotPlanType.Business
                    },
                    Period = new CopilotBillingPeriod
                    {
                        RequestedYear = 2026,
                        RequestedMonth = 9,
                        IsVerified = true
                    },
                    Coverage = new CopilotReportCoverage
                    {
                        IsComplete = true,
                        MissingDays = [],
                        Filters = new Dictionary<string, string>(),
                        HasMissingPartitions = false,
                        HasInvalidRows = false
                    },
                    GrossUsed = 500m,
                    Source = CopilotCreditSource.BillingApi,
                    FetchedAtUtc = FIXED_NOW,
                    IsEstimated = false
                }
            }
        };

        var rows = ProviderUsageRowFactory.CreateRows(snapshot, timeProvider);

        var creditRow = rows.Should().ContainSingle(r => r.Key == "copilot:credits").Subject;
        creditRow.ProvenanceText.Should().Contain("Stale");
        creditRow.ErrorText.Should().Be("Rate limited: retrying after 45s");
    }

    /// <summary>
    /// Verifies that unavailable billing without usage outputs specific guidance text.
    /// </summary>
    [Theory]
    [InlineData(CopilotBillingReason.AccessDenied, "Access denied to billing API (check permissions)")]
    [InlineData(CopilotBillingReason.UnknownScope, "Billing organization or account could not be resolved")]
    [InlineData(CopilotBillingReason.AmbiguousScope, "Multiple candidate billing scopes found")]
    [InlineData(CopilotBillingReason.MissingCredential, "GitHub credentials not found")]
    public void CreateRows_WhenUnavailableBilling_SurfacesStructuredErrorReason(
        CopilotBillingReason reason,
        string expectedError)
    {

        var snapshot = new Snapshot
        {
            ProviderId = "copilot",
            Status = ProviderStatus.Ok,
            Fidelity = Fidelity.Official,
            FetchedAtUtc = FIXED_NOW,
            LimitWindows = [],
            CopilotBilling = new CopilotBillingStatus
            {
                State = CopilotBillingState.Unavailable,
                Reason = reason,
                AttemptedAtUtc = FIXED_NOW
            }
        };

        var rows = ProviderUsageRowFactory.CreateRows(snapshot);

        var creditRow = rows.Should().ContainSingle(r => r.Key == "copilot:credits").Subject;
        creditRow.PrimaryQuantityText.Should().Be("Unavailable");
        creditRow.ErrorText.Should().Be(expectedError);
    }

    /// <summary>
    /// Verifies that non-Copilot providers do not produce an AI credits row.
    /// </summary>
    [Fact]
    public void CreateRows_WhenClaudeProvider_DoesNotProduceAiCreditsRow()
    {

        var snapshot = new Snapshot
        {
            ProviderId = "claude",
            Status = ProviderStatus.Ok,
            Fidelity = Fidelity.Official,
            FetchedAtUtc = FIXED_NOW,
            LimitWindows =
            [
                new LimitWindow
                {
                    Name = "Session",
                    UsedFraction = 0.20
                }
            ]
        };

        var rows = ProviderUsageRowFactory.CreateRows(snapshot);

        rows.Should().ContainSingle(r => r.Key == "quota:0");
        rows.Should().NotContain(r => r.Key == "copilot:credits");
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _now;

        public FixedTimeProvider(DateTimeOffset now)
        {

            _now = now;
        }

        public override DateTimeOffset GetUtcNow()
            => _now;
    }
}
