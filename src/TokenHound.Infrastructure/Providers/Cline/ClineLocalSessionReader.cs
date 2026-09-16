using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Core.Models;
using TokenHound.Infrastructure.Storage;

namespace TokenHound.Infrastructure.Providers.Cline;

/// <summary>
/// Aggregates locally recorded Cline session evidence without writing to the Cline data directory.
/// </summary>
/// <remarks>
/// Only message artifacts modified inside the sampling window are read, newest first and capped, so a
/// long history never turns into an unbounded scan.
/// </remarks>
public sealed class ClineLocalSessionReader
{
    /// <summary>The file suffix of the Cline session message artifacts.</summary>
    public const string MESSAGES_FILE_SUFFIX = ".messages.json";

    /// <summary>The default aggregation window (24 hours).</summary>
    public static readonly TimeSpan DEFAULT_SAMPLE_WINDOW = TimeSpan.FromHours(24);

    private const string CLINE_CONFIG_DIRECTORY = ".cline";
    private const string DATA_DIRECTORY = "data";
    private const int MAX_SAMPLE_FILES = 20;
    private const long MAX_SAMPLE_FILE_BYTES = 8L * 1024 * 1024;
    private const string SESSIONS_DIRECTORY = "sessions";

    private readonly TimeSpan _sampleWindow;
    private readonly string _sessionsDirectory;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClineLocalSessionReader"/> class.
    /// </summary>
    /// <param name="sessionsDirectory">An optional Cline sessions directory for testing.</param>
    /// <param name="timeProvider">An optional clock used to bound the sampling window.</param>
    /// <param name="sampleWindow">An optional aggregation window, defaulting to 24 hours.</param>
    public ClineLocalSessionReader(
        string? sessionsDirectory = null,
        TimeProvider? timeProvider = null,
        TimeSpan? sampleWindow = null)
    {

        _sessionsDirectory = string.IsNullOrWhiteSpace(sessionsDirectory)
            ? GetDefaultSessionsDirectory()
            : sessionsDirectory;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _sampleWindow = sampleWindow is { } window && window > TimeSpan.Zero
            ? window
            : DEFAULT_SAMPLE_WINDOW;
    }

    /// <summary>
    /// Gets the resolved Cline sessions directory sampled by this reader.
    /// </summary>
    public string SessionsDirectory
        => _sessionsDirectory;

    /// <summary>
    /// Samples the recent session artifacts and aggregates their token usage and free limit evidence.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to observe.</param>
    /// <returns>The aggregated local sample, or an empty sample when no evidence is available.</returns>
    public async Task<ClineLocalUsageSample> ReadAsync(CancellationToken cancellationToken = default)
    {

        cancellationToken.ThrowIfCancellationRequested();

        var windowStartUtc = _timeProvider.GetUtcNow() - _sampleWindow;
        var accumulator = new SampleAccumulator(windowStartUtc);

        foreach (var file in EnumerateSampleFiles(windowStartUtc))
        {

            cancellationToken.ThrowIfCancellationRequested();

            var content = await ReadFileAsync(file.FullName, cancellationToken).ConfigureAwait(false);

            accumulator.Add(ClineLocalSessionParser.ParseMessages(content, windowStartUtc));
        }

        return accumulator.ToSample();
    }

    /// <summary>
    /// Gets the default Cline sessions directory under the current user's profile.
    /// </summary>
    /// <returns>The fully qualified default path to the Cline sessions directory.</returns>
    public static string GetDefaultSessionsDirectory()
    {

        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        if (string.IsNullOrWhiteSpace(userProfile))
            userProfile = Environment.GetEnvironmentVariable("USERPROFILE") ?? ".";

        return Path.Combine(
            userProfile,
            CLINE_CONFIG_DIRECTORY,
            DATA_DIRECTORY,
            SESSIONS_DIRECTORY
        );
    }

    private IReadOnlyList<FileInfo> EnumerateSampleFiles(DateTimeOffset windowStartUtc)
    {

        try
        {

            var directory = new DirectoryInfo(_sessionsDirectory);

            if (!directory.Exists)
                return [];

            return directory
                .EnumerateFiles($"*{MESSAGES_FILE_SUFFIX}", SearchOption.AllDirectories)
                .Where(file => file.LastWriteTimeUtc >= windowStartUtc.UtcDateTime)
                .OrderByDescending(static file => file.LastWriteTimeUtc)
                .Take(MAX_SAMPLE_FILES)
                .ToList();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {

            return [];
        }
    }

    private static async Task<string?> ReadFileAsync(string path, CancellationToken cancellationToken)
    {

        try
        {

            var info = new FileInfo(path);

            if (!info.Exists || info.Length > MAX_SAMPLE_FILE_BYTES)
                return null;

            return await SharedFileReader.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {

            throw;
        }
        catch (Exception)
        {

            // A session artifact can be rewritten while it is read; another sample covers the gap.
            return null;
        }
    }

    private sealed class SampleAccumulator(DateTimeOffset windowStartUtc)
    {
        private long _cacheReadTokens;
        private long _cacheWriteTokens;
        private ClineFreeLimitHit? _freeLimit;
        private long _inputTokens;
        private DateTimeOffset? _lastActivityUtc;
        private int _modelCalls;
        private long _outputTokens;

        public void Add(ClineLocalUsageSample sample)
        {

            if (sample.Usage is { } usage)
            {

                _inputTokens += usage.InputTokens;
                _outputTokens += usage.OutputTokens;
                _cacheReadTokens += usage.CacheReadTokens;
                _cacheWriteTokens += usage.CacheWriteTokens;
                _modelCalls += usage.ModelCalls;

                if (_lastActivityUtc is null || usage.LastActivityUtc > _lastActivityUtc)
                    _lastActivityUtc = usage.LastActivityUtc;
            }

            if (sample.FreeLimit is { } hit && (_freeLimit is null || hit.DetectedAtUtc > _freeLimit.DetectedAtUtc))
                _freeLimit = hit;
        }

        public ClineLocalUsageSample ToSample()
        {

            if (_modelCalls == 0 && _freeLimit is null)
                return ClineLocalUsageSample.Empty;

            return new ClineLocalUsageSample
            {
                Usage = CreateUsage(),
                FreeLimit = _freeLimit
            };
        }

        private ClineLocalUsage? CreateUsage()
        {

            if (_modelCalls == 0)
                return null;

            return new ClineLocalUsage
            {
                InputTokens = _inputTokens,
                OutputTokens = _outputTokens,
                CacheReadTokens = _cacheReadTokens,
                CacheWriteTokens = _cacheWriteTokens,
                ModelCalls = _modelCalls,
                WindowStartUtc = windowStartUtc,
                LastActivityUtc = _lastActivityUtc ?? windowStartUtc
            };
        }
    }
}