using System;
using System.Globalization;
using System.IO;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace TokenHound.Infrastructure.Engine;

public sealed partial class UsageArchive
{
    /// <summary>
    /// Loads the Copilot HTTP rate-limit gate state, honoring legacy deadlines conservatively.
    /// </summary>
    /// <returns>The persisted or default Copilot HTTP gate state.</returns>
    public CopilotHttpGateState LoadCopilotHttpDeadline()
    {

        if (!File.Exists(StatePath))
            return new CopilotHttpGateState();

        try
        {

            var state = ParseStateObject(File.ReadAllText(StatePath));
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

    /// <summary>
    /// Persists the Copilot HTTP rate-limit deadline and consecutive failures in state.json.
    /// </summary>
    /// <param name="deadlineUtc">The absolute UTC deadline.</param>
    /// <param name="consecutiveFailures">The count of consecutive failures.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task SaveCopilotHttpDeadlineAsync(
        DateTimeOffset deadlineUtc,
        int consecutiveFailures,
        CancellationToken cancellationToken = default)
    {

        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {

            var state = ReadStateObjectForWrite();
            var copilotHttp = state[COPILOT_HTTP_PROPERTY_NAME] as JsonObject ?? new JsonObject();

            copilotHttp[DEADLINE_UTC_PROPERTY_NAME] = deadlineUtc.ToString("O", CultureInfo.InvariantCulture);
            copilotHttp[CONSECUTIVE_FAILURES_PROPERTY_NAME] = consecutiveFailures;
            state[COPILOT_HTTP_PROPERTY_NAME] = copilotHttp;

            await WriteJsonAtomicallyAsync(StatePath, state, cancellationToken).ConfigureAwait(false);
        }
        finally
        {

            _writeLock.Release();
        }
    }

    /// <summary>
    /// Clears the Copilot HTTP deadline from state.json and resets the streak.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task ClearCopilotHttpDeadlineAsync(CancellationToken cancellationToken = default)
    {

        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {

            if (!File.Exists(StatePath))
                return;

            var state = ReadStateObjectForWrite();
            var changed = state.Remove(COPILOT_HTTP_PROPERTY_NAME);

            if (state[BACKOFF_UNTIL_PROPERTY_NAME] is JsonObject backoffUntil && backoffUntil.Remove("copilot"))
                changed = true;

            if (changed)
                await WriteJsonAtomicallyAsync(StatePath, state, cancellationToken).ConfigureAwait(false);
        }
        finally
        {

            _writeLock.Release();
        }
    }

    private static (DateTimeOffset? deadline, int failures) ParseCopilotHttpElement(JsonObject state)
    {

        if (state[COPILOT_HTTP_PROPERTY_NAME] is not JsonObject copilotHttp)
            return (null, 0);

        DateTimeOffset? deadline = null;
        var failures = 0;

        if (copilotHttp[DEADLINE_UTC_PROPERTY_NAME] is JsonValue deadlineVal
            && deadlineVal.TryGetValue<string>(out var strDeadline)
            && DateTimeOffset.TryParse(
                strDeadline,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out var parsedDeadline))
        {
            deadline = parsedDeadline;
        }

        if (copilotHttp[CONSECUTIVE_FAILURES_PROPERTY_NAME] is JsonValue failVal
            && failVal.TryGetValue<int>(out var parsedFailures))
        {
            failures = Math.Max(0, parsedFailures);
        }

        return (deadline, failures);
    }

    private static DateTimeOffset? ReadLegacyCopilotDeadline(JsonObject state)
    {

        if (state[BACKOFF_UNTIL_PROPERTY_NAME] is not JsonObject backoffUntil)
            return null;

        if (backoffUntil["copilot"] is JsonValue jsonVal
            && jsonVal.TryGetValue<string>(out var stringVal)
            && DateTimeOffset.TryParse(
                stringVal,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out var legacyDeadline))
        {
            return legacyDeadline;
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
}
