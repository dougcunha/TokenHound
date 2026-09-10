using System;

namespace TokenHound.Infrastructure.Providers.Claude;

internal static class ClaudeEpochTimestampParser
{
    private const long MILLISECOND_EPOCH_THRESHOLD = 10_000_000_000L;

    internal static DateTimeOffset? Parse(long epochValue)
    {

        try
        {
            return epochValue > MILLISECOND_EPOCH_THRESHOLD
                ? DateTimeOffset.FromUnixTimeMilliseconds(epochValue)
                : DateTimeOffset.FromUnixTimeSeconds(epochValue);
        }
        catch (ArgumentOutOfRangeException)
        {
            return null;
        }
    }
}
