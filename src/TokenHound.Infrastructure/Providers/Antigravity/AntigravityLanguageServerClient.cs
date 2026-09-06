using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace TokenHound.Infrastructure.Providers.Antigravity;

/// <summary>
/// Client for querying quota metrics directly from the local Google Antigravity Language Server.
/// </summary>
public sealed class AntigravityLanguageServerClient : IDisposable
{
    private const string CSRF_HEADER_NAME = "x-codeium-csrf-token";
    private const string REQUEST_PAYLOAD = "{\"forceRefresh\": true}";

    private readonly HttpClient _httpClient;
    private readonly bool _disposeClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="AntigravityLanguageServerClient"/> class.
    /// </summary>
    /// <param name="httpClient">Optional pre-configured HttpClient instance.</param>
    public AntigravityLanguageServerClient(HttpClient? httpClient = null)
    {

        if (httpClient is not null)
        {
            _httpClient = httpClient;
            _disposeClient = false;
        }
        else
        {
            var handler = new SocketsHttpHandler
            {
                SslOptions = new SslClientAuthenticationOptions
                {
                    RemoteCertificateValidationCallback = static (sender, cert, chain, errors) => true
                },
                ConnectTimeout = TimeSpan.FromSeconds(2)
            };

            _httpClient = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(5)
            };

            _disposeClient = true;
        }
    }

    /// <summary>
    /// Converts a remaining quota fraction into a consumed quota fraction.
    /// </summary>
    /// <param name="remainingFraction">Remaining fraction reported by the server.</param>
    /// <returns>Inverted used fraction between 0.0 and 1.0, or null if unmeasured.</returns>
    public static double? CalculateUsedFraction(double? remainingFraction)
    {

        if (!remainingFraction.HasValue)
        {
            return null;
        }

        return Math.Clamp(1.0 - remainingFraction.Value, 0.0, 1.0);
    }

    /// <summary>
    /// Queries the Language Server quota summary attempting candidate ports.
    /// </summary>
    /// <param name="endpoint">The discovered Language Server endpoint.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The quota summary response, or null if all ports fail.</returns>
    public async ValueTask<AntigravityQuotaSummaryResponse?> RetrieveUserQuotaSummaryAsync(
        AntigravityEndpoint endpoint,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(endpoint);

        foreach (var port in endpoint.CandidatePorts)
        {
            var response = await QueryPortAsync(
                port,
                endpoint.CsrfToken,
                cancellationToken
            ).ConfigureAwait(false);

            if (response is not null)
            {
                return response;
            }
        }

        return null;
    }

    /// <summary>
    /// Queries a specific local Language Server port.
    /// </summary>
    /// <param name="port">Listening TCP port.</param>
    /// <param name="csrfToken">CSRF token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Parsed response, or null on failure.</returns>
    public async ValueTask<AntigravityQuotaSummaryResponse?> QueryPortAsync(
        int port,
        string csrfToken,
        CancellationToken cancellationToken = default)
    {
        var url = $"https://127.0.0.1:{port}/exa.language_server_pb.LanguageServerService/RetrieveUserQuotaSummary";

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add(CSRF_HEADER_NAME, csrfToken);
        request.Content = new StringContent(REQUEST_PAYLOAD, Encoding.UTF8, "application/json");

        try
        {
            using var response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken
            ).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            var envelope = JsonSerializer.Deserialize<AntigravityQuotaEnvelope>(content);

            return envelope?.Response;
        }
        catch
        {
            return null;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {

        if (_disposeClient)
        {
            _httpClient.Dispose();
        }
    }
}
