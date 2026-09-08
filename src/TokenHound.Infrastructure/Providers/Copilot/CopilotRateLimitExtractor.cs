using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;

namespace TokenHound.Infrastructure.Providers.Copilot;

/// <summary>
/// Extracts rate-limit retry penalties and identifies 429 status conditions from responses and exceptions.
/// </summary>
internal static class CopilotRateLimitExtractor
{
    internal static bool TryExtractRateLimit(
        Exception ex,
        out int? retryAfterSeconds)
    {

        if (ex is CopilotApiException apiEx && apiEx.StatusCode == HttpStatusCode.TooManyRequests)
        {

            retryAfterSeconds = apiEx.RetryAfterSeconds;

            return true;
        }

        if (ex is HttpRequestException httpEx && httpEx.StatusCode == HttpStatusCode.TooManyRequests)
        {

            retryAfterSeconds = null;

            return true;
        }

        if (ex.InnerException is not null)
            return TryExtractRateLimit(ex.InnerException, out retryAfterSeconds);

        retryAfterSeconds = null;

        return false;
    }

    internal static int? ExtractRetryAfterSeconds(
        HttpResponseMessage response,
        TimeProvider timeProvider)
    {

        if (response.Headers.RetryAfter?.Delta is { } delta)
            return (int)Math.Ceiling(delta.TotalSeconds);

        if (response.Headers.RetryAfter?.Date is { } date)
            return (int)Math.Ceiling((date - timeProvider.GetUtcNow()).TotalSeconds);

        if (response.Headers.TryGetValues("Retry-After", out var values)
            && int.TryParse(values.FirstOrDefault(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var sec))
            return sec;

        return null;
    }
}
