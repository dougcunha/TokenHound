using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Core.Models;

namespace TokenHound.Infrastructure.Engine;

/// <summary>
/// Persists TokenHound-owned snapshots and provider backoff deadlines.
/// </summary>
public sealed partial class UsageArchive : IDisposable
{
    private const string LAST_READINGS_FILE_NAME = "last_readings.json";
    private const string STATE_FILE_NAME = "state.json";
    private const string BACKOFF_UNTIL_PROPERTY_NAME = "backoffUntil";
    private const string COPILOT_BILLING_FILE_NAME = "copilot_billing.json";

    private static readonly JsonSerializerOptions SERIALIZER_OPTIONS = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private readonly CopilotBillingArchive _copilotBillingArchive;
    private readonly CopilotHttpArchive _copilotHttpArchive;
    private bool _disposed;

    /// <summary>
    /// Initializes a new archive using the TokenHound local application directory.
    /// </summary>
    /// <param name="directoryPath">An optional directory used for isolated archive state.</param>
    public UsageArchive(string? directoryPath = null)
    {
        DirectoryPath = string.IsNullOrWhiteSpace(directoryPath)
            ? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "TokenHound")
            : directoryPath;

        LastReadingsPath = Path.Combine(DirectoryPath, LAST_READINGS_FILE_NAME);
        StatePath = Path.Combine(DirectoryPath, STATE_FILE_NAME);
        CopilotBillingPath = Path.Combine(DirectoryPath, COPILOT_BILLING_FILE_NAME);
        _copilotBillingArchive = new CopilotBillingArchive(
            DirectoryPath,
            CopilotBillingPath,
            _writeLock
        );
        _copilotHttpArchive = new CopilotHttpArchive(
            DirectoryPath,
            StatePath,
            _writeLock
        );
    }

    /// <summary>
    /// Gets the directory owned by this archive.
    /// </summary>
    public string DirectoryPath { get; }

    /// <summary>
    /// Gets the path of the last-good snapshot file.
    /// </summary>
    public string LastReadingsPath { get; }

    /// <summary>
    /// Gets the path of the provider deadline state file.
    /// </summary>
    public string StatePath { get; }

    /// <summary>
    /// Gets the path of the Copilot billing state file.
    /// </summary>
    public string CopilotBillingPath { get; }

    /// <summary>
    /// Loads available snapshots and deadlines without creating missing files.
    /// </summary>
    /// <returns>The loaded archive state and any local persistence diagnostic.</returns>
    public State Load()
    {

        var errors = new List<string>();
        var readings = ReadReadings(errors);
        var deadlines = ReadDeadlines(errors);

        return new State
        {
            LastReadings = readings,
            BackoffDeadlines = deadlines,
            ErrorDescription = errors.Count == 0 ? null : string.Join("; ", errors)
        };
    }

    /// <summary>
    /// Replaces one provider's last-good snapshot using an atomic serialized write.
    /// </summary>
    /// <param name="snapshot">The successful snapshot to archive.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task SaveSnapshotAsync(
        Snapshot snapshot,
        CancellationToken cancellationToken = default)
    {

        ArgumentNullException.ThrowIfNull(snapshot);
        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            var readings = ReadReadingsForWrite();
            readings[snapshot.ProviderId] = snapshot.CopilotBilling is null
                ? snapshot
                : snapshot with { CopilotBilling = null };
            await WriteJsonAtomicallyAsync(LastReadingsPath, readings, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    /// <summary>
    /// Removes one provider's archived snapshot using an atomic serialized write.
    /// </summary>
    /// <param name="providerId">The provider identifier to remove.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task ClearSnapshotAsync(
        string providerId,
        CancellationToken cancellationToken = default)
    {

        ArgumentException.ThrowIfNullOrWhiteSpace(providerId);
        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (!File.Exists(LastReadingsPath))
                return;

            var readings = ReadReadingsForWrite();

            if (!readings.Remove(providerId))
                return;

            await WriteJsonAtomicallyAsync(LastReadingsPath, readings, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    /// <summary>
    /// Persists one provider's absolute backoff deadline while preserving unrelated state keys.
    /// </summary>
    /// <param name="providerId">The provider identifier.</param>
    /// <param name="deadlineUtc">The absolute UTC deadline.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task SaveBackoffDeadlineAsync(
        string providerId,
        DateTimeOffset deadlineUtc,
        CancellationToken cancellationToken = default)
    {

        ArgumentException.ThrowIfNullOrWhiteSpace(providerId);
        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            var state = ReadStateObjectForWrite();
            var backoffUntil = GetBackoffObjectForWrite(state);
            backoffUntil[providerId] = deadlineUtc.ToString("O", CultureInfo.InvariantCulture);
            await WriteJsonAtomicallyAsync(StatePath, state, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    /// <summary>
    /// Removes one provider's deadline while preserving unrelated state keys.
    /// </summary>
    /// <param name="providerId">The provider identifier to remove.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task ClearBackoffDeadlineAsync(
        string providerId,
        CancellationToken cancellationToken = default)
    {

        ArgumentException.ThrowIfNullOrWhiteSpace(providerId);
        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (!File.Exists(StatePath))
                return;

            var state = ReadStateObjectForWrite();

            if (!TryRemoveBackoffDeadline(state, providerId))
                return;

            await WriteJsonAtomicallyAsync(StatePath, state, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    private static bool TryRemoveBackoffDeadline(JsonObject state, string providerId)
    {

        if (state[BACKOFF_UNTIL_PROPERTY_NAME] is null)
            return false;

        if (state[BACKOFF_UNTIL_PROPERTY_NAME] is not JsonObject backoffUntil)
            throw new InvalidDataException("The backoffUntil archive value must be a JSON object.");

        return backoffUntil.Remove(providerId);
    }

    /// <inheritdoc />
    public void Dispose()
    {

        if (_disposed)
            return;

        _disposed = true;
        _writeLock.Dispose();
    }

}
