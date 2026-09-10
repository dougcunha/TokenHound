using System;
using System.Globalization;
using System.Linq;
using System.Net.Http;

namespace TokenHound.Infrastructure.Providers;

internal static class HttpRetryAfterParser
{
    internal static int? ExtractSeconds(
        HttpResponseMessage response,
        TimeProvider timeProvider)
    {

        if (response.Headers.RetryAfter?.Delta is { } delta)
            return (int)Math.Ceiling(delta.TotalSeconds);

        if (response.Headers.RetryAfter?.Date is { } date)
            return (int)Math.Ceiling((date - timeProvider.GetUtcNow()).TotalSeconds);

        if (response.Headers.TryGetValues("Retry-After", out var values)
            && int.TryParse(
                values.FirstOrDefault(),
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var seconds))
        {
            return seconds;
        }

        return null;
    }
}
