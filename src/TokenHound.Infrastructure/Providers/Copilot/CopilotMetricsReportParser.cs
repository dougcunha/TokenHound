using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Core.Models;

namespace TokenHound.Infrastructure.Providers.Copilot;

/// <summary>
/// Streams, validates, deduplicates, and aggregates Copilot daily metrics NDJSON reports.
/// </summary>
public static class CopilotMetricsReportParser
{
    private static readonly JsonSerializerOptions JSON_OPTIONS = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Parses an NDJSON report from a text reader for the specified day and scope.
    /// </summary>
    public static CopilotMetricsReportParseResult Parse(
        TextReader reader,
        DateOnly expectedDay,
        string? principalLogin = null,
        string? expectedOwner = null,
        CopilotBillingScope expectedScope = CopilotBillingScope.Organization)
    {

        ArgumentNullException.ThrowIfNull(reader);

        var users = new Dictionary<string, CopilotMetricsUserRow>(StringComparer.OrdinalIgnoreCase);
        var hasInvalidRows = false;
        var isDayInvalid = false;
        CopilotMetricsUserRow? principalRow = null;

        string? line;

        while ((line = reader.ReadLine()) != null)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            if (!TryParseRow(line, out var row))
            {
                hasInvalidRows = true;
                continue;
            }

            if (!ValidateRow(row!, expectedDay, expectedOwner, expectedScope))
            {
                hasInvalidRows = true;
                continue;
            }

            ProcessUserRow(row!, users, ref isDayInvalid, ref hasInvalidRows);

            if (IsPrincipalMatch(row!, principalLogin))
                principalRow = row;
        }

        return BuildParseResult(expectedDay, users, hasInvalidRows, isDayInvalid, principalRow);
    }

    /// <summary>
    /// Asynchronously parses an NDJSON report from a stream for the specified day and scope.
    /// </summary>
    public static async Task<CopilotMetricsReportParseResult> ParseAsync(
        Stream stream,
        DateOnly expectedDay,
        string? principalLogin = null,
        string? expectedOwner = null,
        CopilotBillingScope expectedScope = CopilotBillingScope.Organization,
        CancellationToken cancellationToken = default)
    {

        ArgumentNullException.ThrowIfNull(stream);
        cancellationToken.ThrowIfCancellationRequested();

        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 4096, leaveOpen: true);

        return await Task.Run(
            () => Parse(reader, expectedDay, principalLogin, expectedOwner, expectedScope),
            cancellationToken
        ).ConfigureAwait(false);
    }

    /// <summary>
    /// Merges multiple partition parse results for the same day, validating duplicate consistency.
    /// </summary>
    public static CopilotMetricsReportParseResult Merge(
        DateOnly day,
        IEnumerable<CopilotMetricsReportParseResult> partitionResults,
        string? principalLogin = null)
    {

        ArgumentNullException.ThrowIfNull(partitionResults);

        var users = new Dictionary<string, CopilotMetricsUserRow>(StringComparer.OrdinalIgnoreCase);
        var hasInvalidRows = false;
        var isDayInvalid = false;
        CopilotMetricsUserRow? principalRow = null;

        foreach (var partition in partitionResults)
        {
            if (partition.Day != day || partition.HasInvalidRows)
                hasInvalidRows = true;

            if (partition.IsDayInvalid)
                isDayInvalid = true;

            foreach (var row in partition.UserRows)
            {
                ProcessUserRow(row, users, ref isDayInvalid, ref hasInvalidRows);

                if (IsPrincipalMatch(row, principalLogin))
                    principalRow = row;
            }
        }

        return BuildParseResult(day, users, hasInvalidRows, isDayInvalid, principalRow);
    }

    private static bool TryParseRow(string line, out CopilotMetricsUserRow? row)
    {

        try
        {
            row = JsonSerializer.Deserialize<CopilotMetricsUserRow>(line, JSON_OPTIONS);

            return row is not null;
        }
        catch (Exception)
        {
            row = null;

            return false;
        }
    }

    private static bool ValidateRow(
        CopilotMetricsUserRow row,
        DateOnly expectedDay,
        string? expectedOwner,
        CopilotBillingScope expectedScope)
    {

        if (row.Day is null || row.Day != expectedDay)
            return false;

        if (string.IsNullOrWhiteSpace(row.UserLogin) && !row.UserId.HasValue)
            return false;

        if (row.AiCreditsUsed is null || row.AiCreditsUsed.Value < 0)
            return false;

        return ValidateScope(row, expectedOwner, expectedScope);
    }

    private static bool ValidateScope(
        CopilotMetricsUserRow row,
        string? expectedOwner,
        CopilotBillingScope expectedScope)
    {

        if (string.IsNullOrWhiteSpace(expectedOwner))
            return true;

        if (expectedScope == CopilotBillingScope.Enterprise)
            return string.IsNullOrWhiteSpace(row.EnterpriseId)
                || string.Equals(row.EnterpriseId, expectedOwner, StringComparison.OrdinalIgnoreCase);

        return string.IsNullOrWhiteSpace(row.OrganizationId)
            || string.Equals(row.OrganizationId, expectedOwner, StringComparison.OrdinalIgnoreCase);
    }

    private static void ProcessUserRow(
        CopilotMetricsUserRow row,
        Dictionary<string, CopilotMetricsUserRow> users,
        ref bool isDayInvalid,
        ref bool hasInvalidRows)
    {

        var userKey = !string.IsNullOrWhiteSpace(row.UserLogin)
            ? row.UserLogin.Trim()
            : row.UserId?.ToString(CultureInfo.InvariantCulture);

        if (string.IsNullOrWhiteSpace(userKey))
        {
            hasInvalidRows = true;

            return;
        }

        if (users.TryGetValue(userKey, out var existing))
        {
            if (existing.AiCreditsUsed == row.AiCreditsUsed)
                return;

            isDayInvalid = true;
            hasInvalidRows = true;

            return;
        }

        users[userKey] = row;
    }

    private static bool IsPrincipalMatch(CopilotMetricsUserRow row, string? principalLogin)
        => !string.IsNullOrWhiteSpace(principalLogin)
            && string.Equals(row.UserLogin, principalLogin.Trim(), StringComparison.OrdinalIgnoreCase);

    private static CopilotMetricsReportParseResult BuildParseResult(
        DateOnly day,
        Dictionary<string, CopilotMetricsUserRow> users,
        bool hasInvalidRows,
        bool isDayInvalid,
        CopilotMetricsUserRow? principalRow)
    {

        var totalCredits = isDayInvalid ? 0m : users.Values.Sum(static r => r.AiCreditsUsed ?? 0m);

        return new CopilotMetricsReportParseResult
        {
            Day = day,
            TotalCreditsUsed = totalCredits,
            UserCount = users.Count,
            HasInvalidRows = hasInvalidRows,
            IsDayInvalid = isDayInvalid,
            PrincipalRow = principalRow,
            UserRows = [.. users.Values]
        };
    }
}
