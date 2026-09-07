using System;
using System.Collections.Generic;
using TokenHound.Infrastructure.Providers.Copilot;
using Xunit;

namespace TokenHound.Infrastructure.Tests.Providers.Copilot;

/// <summary>Verifies honest parsing of Copilot's open quota map.</summary>
public sealed class CopilotQuotaParserTests
{
    [Fact]
    public void Parse_PrefersFinitePremiumInteractionsAndPreservesFractionalRemainder()
    {
        var response = CreateResponse(
            new Dictionary<string, CopilotQuotaSnapshotDto>
            {
                ["chat"] = CreateQuota(false, 0, 0, 100, false, false),
                ["premium_interactions"] = CreateQuota(true, 3000, 252, 8.4, true, false)
            });

        var result = CopilotQuotaParser.Parse(response);

        Assert.Equal(CopilotQuotaParseStatus.Success, result.Status);
        Assert.Equal("Monthly Premium Interactions", result.LimitWindow!.Name);
        Assert.Equal(252.6, result.LimitWindow.RemainingValue);
        Assert.Equal(252L, result.LimitWindow.RemainingUnits);
        Assert.Equal(3000L, result.LimitWindow.TotalUnits);
        Assert.Equal(0.916, result.LimitWindow.UsedFraction!.Value, 3);
        Assert.Equal(DateTimeOffset.Parse("2026-10-01T00:00:00Z"), result.ResetTimeUtc);
    }

    [Fact]
    public void Parse_FallsBackToAnotherFiniteOpenMapCategory()
    {
        var response = CreateResponse(
            new Dictionary<string, CopilotQuotaSnapshotDto>
            {
                ["new_category"] = CreateQuota(true, 100, 20, 20, false, false)
            });

        var result = CopilotQuotaParser.Parse(response);

        Assert.Equal(CopilotQuotaParseStatus.Success, result.Status);
        Assert.Equal(20L, result.LimitWindow!.RemainingUnits);
    }

    [Fact]
    public void Parse_OnlyUnlimitedOrNonEntitledCategoriesIsUnsupported()
    {
        var response = CreateResponse(
            new Dictionary<string, CopilotQuotaSnapshotDto>
            {
                ["chat"] = CreateQuota(false, 0, 0, 100, false, true),
                ["completions"] = CreateQuota(false, 0, 0, 100, false, false)
            });

        var result = CopilotQuotaParser.Parse(response);

        Assert.Equal(CopilotQuotaParseStatus.NoFiniteQuota, result.Status);
        Assert.Null(result.LimitWindow);
    }

    [Fact]
    public void Parse_MissingMapOrMalformedFiniteEntryIsSchemaDrift()
    {
        Assert.Equal(
            CopilotQuotaParseStatus.SchemaDrift,
            CopilotQuotaParser.Parse(new CopilotQuotaResponse()).Status);

        var malformed = CreateResponse(
            new Dictionary<string, CopilotQuotaSnapshotDto>
            {
                ["premium_interactions"] = new()
                {
                    HasQuota = true,
                    Unlimited = false,
                    Entitlement = 0,
                    Remaining = 0,
                    QuotaRemaining = 0,
                    PercentRemaining = 100,
                    OveragePermitted = false
                }
            });

        Assert.Equal(CopilotQuotaParseStatus.SchemaDrift, CopilotQuotaParser.Parse(malformed).Status);
    }

    [Fact]
    public void Parse_InvalidResetIsSchemaDriftAndDoesNotInventOne()
    {
        var response = new CopilotQuotaResponse
        {
            QuotaResetDateUtc = "not-a-date",
            QuotaSnapshots = new Dictionary<string, CopilotQuotaSnapshotDto>
            {
                ["premium_interactions"] = CreateQuota(true, 100, 0, 0, false, false)
            }
        };

        var result = CopilotQuotaParser.Parse(response);

        Assert.Equal(CopilotQuotaParseStatus.SchemaDrift, result.Status);
        Assert.Null(result.ResetTimeUtc);
    }

    private static CopilotQuotaResponse CreateResponse(
        Dictionary<string, CopilotQuotaSnapshotDto> snapshots)
        => new()
        {
            QuotaResetDateUtc = "2026-10-01T00:00:00Z",
            QuotaSnapshots = snapshots
        };

    private static CopilotQuotaSnapshotDto CreateQuota(
        bool hasQuota,
        int entitlement,
        int remaining,
        double percentRemaining,
        bool overagePermitted,
        bool unlimited)
        => new()
        {
            QuotaId = "test",
            HasQuota = hasQuota,
            Unlimited = unlimited,
            Entitlement = entitlement,
            Remaining = remaining,
            QuotaRemaining = remaining + 0.6,
            PercentRemaining = percentRemaining,
            OveragePermitted = overagePermitted
        };
}
