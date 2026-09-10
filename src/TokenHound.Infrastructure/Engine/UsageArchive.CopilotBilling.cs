using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Core.Models;

namespace TokenHound.Infrastructure.Engine;

public sealed partial class UsageArchive
{
    /// <summary>Loads cached Copilot credit usage for the specified context, period, and filters.</summary>
    public CopilotCreditUsage? LoadCopilotBilling(
        CopilotBillingContext context,
        CopilotBillingPeriod period,
        IReadOnlyDictionary<string, string>? filters = null)
    {

        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(period);

        return _copilotBillingArchive.Load(context, period, filters);
    }

    /// <summary>Loads cached Copilot credit usage for the specified parameters.</summary>
    public CopilotCreditUsage? LoadCopilotBilling(
        string principalId,
        CopilotBillingScope scope,
        string? ownerId,
        int year,
        int month,
        IReadOnlyDictionary<string, string>? filters = null)
    {

        ArgumentException.ThrowIfNullOrWhiteSpace(principalId);

        return _copilotBillingArchive.Load(
            principalId,
            scope,
            ownerId,
            year,
            month,
            filters
        );
    }

    /// <summary>Loads cached Copilot daily summaries for the specified context, period, and filters.</summary>
    public IReadOnlyList<CopilotDailyUsageSummary> LoadCopilotDailySummaries(
        CopilotBillingContext context,
        CopilotBillingPeriod period,
        IReadOnlyDictionary<string, string>? filters = null)
    {

        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(period);

        return _copilotBillingArchive.LoadDailySummaries(context, period, filters);
    }

    /// <summary>Persists Copilot credit usage using the archive's shared writer coordination.</summary>
    public Task SaveCopilotBillingAsync(
        CopilotCreditUsage usage,
        CancellationToken cancellationToken = default)
    {

        ArgumentNullException.ThrowIfNull(usage);

        return _copilotBillingArchive.SaveAsync(usage, cancellationToken);
    }

    /// <summary>Persists daily usage summaries using the archive's shared writer coordination.</summary>
    public Task SaveCopilotDailySummariesAsync(
        CopilotBillingContext context,
        CopilotBillingPeriod period,
        IReadOnlyCollection<CopilotDailyUsageSummary> summaries,
        IReadOnlyDictionary<string, string>? filters = null,
        CancellationToken cancellationToken = default)
    {

        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(period);
        ArgumentNullException.ThrowIfNull(summaries);

        return _copilotBillingArchive.SaveDailySummariesAsync(
            context,
            period,
            summaries,
            filters,
            cancellationToken
        );
    }

    /// <summary>Persists one daily usage summary using the archive's shared writer coordination.</summary>
    public Task SaveCopilotDailySummaryAsync(
        CopilotBillingContext context,
        CopilotBillingPeriod period,
        CopilotDailyUsageSummary summary,
        IReadOnlyDictionary<string, string>? filters = null,
        CancellationToken cancellationToken = default)
        => SaveCopilotDailySummariesAsync(
            context,
            period,
            [summary],
            filters,
            cancellationToken
        );
}
