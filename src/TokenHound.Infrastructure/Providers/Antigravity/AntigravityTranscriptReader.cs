using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace TokenHound.Infrastructure.Providers.Antigravity;

/// <summary>
/// Aggregates Antigravity activity by parsing local transcript files.
/// </summary>
public sealed class AntigravityTranscriptReader
{
    private readonly IReadOnlyList<string> _searchDirectories;
    private readonly TimeProvider _timeProvider;

    private sealed record StepEntry(string? source, DateTimeOffset? created_at);

    /// <summary>
    /// Initializes a new instance of the <see cref="AntigravityTranscriptReader"/> class.
    /// </summary>
    /// <param name="searchDirectories">Optional list of directories to scan for brain transcripts.</param>
    /// <param name="timeProvider">Optional time provider for testing.</param>
    public AntigravityTranscriptReader(
        IEnumerable<string>? searchDirectories = null,
        TimeProvider? timeProvider = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;

        if (searchDirectories is not null)
        {
            _searchDirectories = [.. searchDirectories];
        }
        else
        {
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            _searchDirectories =
            [
                Path.Combine(userProfile, ".gemini", "antigravity", "brain"),
                Path.Combine(userProfile, ".gemini", "antigravity-cli", "brain")
            ];
        }
    }

    /// <summary>
    /// Counts the total number of model turns executed during the current local calendar day.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The count of model turns, or 0 if none found.</returns>
    public async ValueTask<int> CountTodayModelRequestsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var localNow = _timeProvider.GetLocalNow();
        var today = DateOnly.FromDateTime(localNow.DateTime);
        var transcriptFiles = EnumerateTranscriptFiles();
        int totalRequests = 0;

        foreach (var file in transcriptFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();
            totalRequests += await CountRequestsInFileAsync(file, today, cancellationToken).ConfigureAwait(false);
        }

        return totalRequests;
    }

    /// <summary>
    /// Gets the most recent write timestamp across all discovered transcript files.
    /// </summary>
    /// <returns>Latest UTC timestamp, or null if no transcripts exist.</returns>
    public DateTimeOffset? GetLatestTranscriptWriteTimeUtc()
    {
        DateTimeOffset? latest = null;

        foreach (var file in EnumerateTranscriptFiles())
        {
            try
            {
                var writeTime = File.GetLastWriteTimeUtc(file);

                if (latest is null || writeTime > latest.Value.UtcDateTime)
                {
                    latest = new DateTimeOffset(writeTime, TimeSpan.Zero);
                }
            }
            catch
            {
                // Ignore file access exceptions
            }
        }

        return latest;
    }

    private IEnumerable<string> EnumerateTranscriptFiles()
    {
        foreach (var baseDir in _searchDirectories)
        {

            if (!Directory.Exists(baseDir))
            {
                continue;
            }

            IEnumerable<string> files;

            try
            {
                files = Directory.EnumerateFiles(
                    baseDir,
                    "transcript.jsonl",
                    SearchOption.AllDirectories);
            }
            catch
            {
                continue;
            }

            foreach (var file in files)
            {
                yield return file;
            }
        }
    }

    private static async ValueTask<int> CountRequestsInFileAsync(
        string filePath,
        DateOnly targetDate,
        CancellationToken cancellationToken)
    {
        int count = 0;

        try
        {
            using var stream = new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete);

            using var reader = new StreamReader(stream);

            while (await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false) is { } line)
            {

                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                if (IsMatchingModelTurn(line, targetDate))
                {
                    count++;
                }
            }
        }
        catch
        {
            return 0;
        }

        return count;
    }

    private static bool IsMatchingModelTurn(string jsonLine, DateOnly targetDate)
    {
        try
        {
            var step = JsonSerializer.Deserialize<StepEntry>(jsonLine);

            if (step is null || !string.Equals(step.source, "MODEL", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!step.created_at.HasValue)
            {
                return false;
            }

            var stepDate = DateOnly.FromDateTime(step.created_at.Value.ToLocalTime().DateTime);

            return stepDate == targetDate;
        }
        catch
        {
            return false;
        }
    }
}
