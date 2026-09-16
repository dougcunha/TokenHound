using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Infrastructure.Providers;

namespace TokenHound.Infrastructure.Providers.Cline;

/// <summary>
/// Maps Cline account API failures onto the provider exception taxonomy.
/// </summary>
public sealed partial class ClineAccountClient
{
    private async Task<HttpRequestException> CreateExceptionForResponseAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {

        var detail = await TryReadErrorAsync(response, cancellationToken).ConfigureAwait(false);
        var statusCode = response.StatusCode;
        var message = detail ?? $"Cline account request failed with HTTP {(int)statusCode} ({statusCode}).";

        if (statusCode == HttpStatusCode.TooManyRequests)
            return new ClineRateLimitException(message, HttpRetryAfterParser.ExtractSeconds(response, _timeProvider));

        if (statusCode == HttpStatusCode.Unauthorized)
            return new ClineAuthException(message);

        if (statusCode == HttpStatusCode.Forbidden)
            return new ClineEntitlementException(message);

        return new HttpRequestException(message, null, statusCode);
    }

    private static async Task<string?> TryReadErrorAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {

        try
        {

            var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(content))
                return null;

            var envelope = JsonSerializer.Deserialize<ClineEnvelope<JsonElement>>(content, JSON_OPTIONS);

            return string.IsNullOrWhiteSpace(envelope?.Error) ? null : envelope.Error;
        }
        catch (Exception)
        {

            // Defensive: the error body is optional context, never a failure of its own.
            return null;
        }
    }
}