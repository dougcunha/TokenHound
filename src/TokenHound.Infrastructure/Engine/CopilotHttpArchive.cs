using System;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace TokenHound.Infrastructure.Engine;

internal sealed class CopilotHttpArchive
{
    private const string BACKOFF_UNTIL_PROPERTY_NAME = "backoffUntil";
    private const string COPILOT_HTTP_PROPERTY_NAME = "copilotHttp";
    private const string COPILOT_PROVIDER_ID = "copilot";
    private const string DEADLINE_UTC_PROPERTY_NAME = "deadlineUtc";
    private const string CONSECUTIVE_FAILURES_PROPERTY_NAME = "consecutiveFailures";

    private static readonly JsonSerializerOptions SERIALIZER_OPTIONS = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private readonly string _directoryPath;
    private readonly string _statePath;
    private readonly SemaphoreSlim _writeLock;

    internal CopilotHttpArchive(
        string directoryPath,
        string statePath,
        SemaphoreSlim writeLock)
    {

        _directoryPath = directoryPath;
        _statePath = statePath;
        _writeLock = writeLock;
    }

    internal CopilotHttpGateState Load()
    {

        if (!File.Exists(_statePath))
            return new CopilotHttpGateState();

        try
        {
            var state = ReadState();
            var (copilotDeadline, failures) = ParseCopilotHttpElement(state);
            var legacyDeadline = ReadLegacyCopilotDeadline(state);
            var effectiveDeadline = ResolveConservativeDeadline(copilotDeadline, legacyDeadline);

            if (!copilotDeadline.HasValue && legacyDeadline.HasValue && failures == 0)
                failures = 1;

            return new CopilotHttpGateState
            {
                DeadlineUtc = effectiveDeadline,
                ConsecutiveFailures = failures
            };
        }
        catch (Exception ex) when (IsPersistenceException(ex))
        {
            return new CopilotHttpGateState();
        }
    }

    internal async Task SaveAsync(
        DateTimeOffset deadlineUtc,
        int consecutiveFailures,
        CancellationToken cancellationToken)
    {

        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            var state = ReadStateForWrite();
            var copilotHttp = state[COPILOT_HTTP_PROPERTY_NAME] as JsonObject ?? new JsonObject();

            copilotHttp[DEADLINE_UTC_PROPERTY_NAME] = deadlineUtc.ToString("O", CultureInfo.InvariantCulture);
            copilotHttp[CONSECUTIVE_FAILURES_PROPERTY_NAME] = consecutiveFailures;
            state[COPILOT_HTTP_PROPERTY_NAME] = copilotHttp;

            await WriteAsync(state, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    internal async Task ClearAsync(CancellationToken cancellationToken)
    {

        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (!File.Exists(_statePath))
                return;

            var state = ReadStateForWrite();
            var changed = state.Remove(COPILOT_HTTP_PROPERTY_NAME);

            if (state[BACKOFF_UNTIL_PROPERTY_NAME] is JsonObject backoffUntil
                && backoffUntil.Remove(COPILOT_PROVIDER_ID))
            {
                changed = true;
            }

            if (changed)
                await WriteAsync(state, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    private JsonObject ReadStateForWrite()
        => File.Exists(_statePath) ? ReadState() : new JsonObject();

    private JsonObject ReadState()
        => JsonNode.Parse(File.ReadAllText(_statePath)) as JsonObject
            ?? throw new InvalidDataException("The archive state root must be a JSON object.");

    private async Task WriteAsync(JsonObject state, CancellationToken cancellationToken)
        => await AtomicJsonFile.WriteAsync(
            _directoryPath,
            _statePath,
            state,
            SERIALIZER_OPTIONS,
            cancellationToken
        ).ConfigureAwait(false);

    private static (DateTimeOffset? Deadline, int Failures) ParseCopilotHttpElement(JsonObject state)
    {

        if (state[COPILOT_HTTP_PROPERTY_NAME] is not JsonObject copilotHttp)
            return (null, 0);

        var deadline = ReadDeadline(copilotHttp, DEADLINE_UTC_PROPERTY_NAME);
        var failures = copilotHttp[CONSECUTIVE_FAILURES_PROPERTY_NAME] is JsonValue failureValue
            && failureValue.TryGetValue<int>(out var parsedFailures)
                ? Math.Max(0, parsedFailures)
                : 0;

        return (deadline, failures);
    }

    private static DateTimeOffset? ReadLegacyCopilotDeadline(JsonObject state)
        => state[BACKOFF_UNTIL_PROPERTY_NAME] is JsonObject backoffUntil
            ? ReadDeadline(backoffUntil, COPILOT_PROVIDER_ID)
            : null;

    private static DateTimeOffset? ReadDeadline(JsonObject state, string propertyName)
    {

        if (state[propertyName] is JsonValue value
            && value.TryGetValue<string>(out var text)
            && DateTimeOffset.TryParse(
                text,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out var deadline))
        {
            return deadline;
        }

        return null;
    }

    private static DateTimeOffset? ResolveConservativeDeadline(
        DateTimeOffset? primary,
        DateTimeOffset? legacy)
    {

        if (primary is null)
            return legacy;

        if (legacy is null)
            return primary;

        return legacy.Value > primary.Value ? legacy : primary;
    }

    private static bool IsPersistenceException(Exception exception)
        => exception is JsonException
            or NotSupportedException
            or IOException
            or UnauthorizedAccessException
            or InvalidDataException;
}
