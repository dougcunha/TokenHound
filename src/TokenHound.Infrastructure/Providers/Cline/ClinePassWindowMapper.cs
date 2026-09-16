using System;
using System.Collections.Generic;
using System.Linq;
using TokenHound.Core.Models;

namespace TokenHound.Infrastructure.Providers.Cline;

/// <summary>
/// Maps the rolling Cline Pass inference caps and usage transactions onto limit windows.
/// </summary>
/// <remarks>
/// The published caps share the unit of the <c>costUsd</c> field, so their ratio is a valid utilization
/// fraction and the raw cap total is an honest denominator. A window is emitted only from usable
/// evidence: any transaction without a timestamp invalidates it, and the sampled transactions must
/// provably cover the whole window, because the account API returns a bounded page.
/// </remarks>
public static class ClinePassWindowMapper
{
    /// <summary>The provider-defined group that contains every Cline Pass window.</summary>
    public const string GROUP_NAME = "Cline Pass";

    private const string FIVE_HOUR_WINDOW_NAME = "Cline Pass (5h)";
    private const string THIRTY_DAY_WINDOW_NAME = "Cline Pass (30d)";
    private const string SEVEN_DAY_WINDOW_NAME = "Cline Pass (7d)";

    /// <summary>
    /// Builds the rolling Cline Pass limit windows for the supplied caps and usage transactions.
    /// </summary>
    /// <param name="caps">The published inference caps, or <see langword="null"/> when the plan carries none.</param>
    /// <param name="transactions">The newest usage transactions returned by the account API.</param>
    /// <param name="nowUtc">The reference current time.</param>
    /// <returns>The mapped limit windows, possibly empty.</returns>
    public static IReadOnlyList<LimitWindow> Map(
        ClineInferenceCapThreshold? caps,
        IReadOnlyList<ClineUsageTransaction> transactions,
        DateTimeOffset nowUtc)
    {

        if (caps is null)
            return [];

        var windows = new List<LimitWindow>();

        AddWindow(
            windows,
            FIVE_HOUR_WINDOW_NAME,
            TimeSpan.FromHours(5),
            caps.Last5HoursCost,
            transactions,
            nowUtc
        );
        AddWindow(
            windows,
            SEVEN_DAY_WINDOW_NAME,
            TimeSpan.FromDays(7),
            caps.Last7DaysCost,
            transactions,
            nowUtc
        );
        AddWindow(
            windows,
            THIRTY_DAY_WINDOW_NAME,
            TimeSpan.FromDays(30),
            caps.Last30DaysCost,
            transactions,
            nowUtc
        );

        return windows;
    }

    private static void AddWindow(
        List<LimitWindow> windows,
        string name,
        TimeSpan period,
        double? cap,
        IReadOnlyList<ClineUsageTransaction> transactions,
        DateTimeOffset nowUtc)
    {

        if (cap is not { } capValue || capValue <= 0)
            return;

        var windowStartUtc = nowUtc - period;
        var coverageComplete = transactions.All(static transaction => transaction.CreatedAt.HasValue) &&
            (transactions.Count == 0 ||
                transactions.Min(static transaction => transaction.CreatedAt!.Value) <= windowStartUtc);

        if (!coverageComplete)
            return;

        var consumed = transactions
            .Where(transaction => transaction.CreatedAt!.Value >= windowStartUtc)
            .Sum(static transaction => transaction.CostUsd ?? 0.0);

        windows.Add(new LimitWindow
        {
            Name = name,
            GroupName = GROUP_NAME,
            Period = period,
            UsedFraction = Math.Clamp(consumed / capValue, 0.0, 1.0),
            RemainingUnits = (long)Math.Max(0.0, Math.Round(capValue - consumed)),
            TotalUnits = (long)Math.Max(1.0, Math.Round(capValue))
        });
    }
}