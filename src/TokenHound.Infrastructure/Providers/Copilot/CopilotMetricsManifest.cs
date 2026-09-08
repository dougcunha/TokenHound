using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TokenHound.Infrastructure.Providers.Copilot;

/// <summary>
/// Manifest response for Copilot daily user metrics reports containing download links.
/// </summary>
public sealed record CopilotMetricsManifest
{
    /// <summary>
    /// Gets the date of the metrics report.
    /// </summary>
    [JsonPropertyName("report_day")]
    public DateOnly? ReportDay { get; init; }

    /// <summary>
    /// Gets the signed download URLs for report partitions.
    /// </summary>
    [JsonPropertyName("download_links")]
    public IReadOnlyList<string> DownloadLinks { get; init; } = [];
}
