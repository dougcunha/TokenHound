using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Core.Contracts;
using TokenHound.Infrastructure.Providers;
using TokenHound.Infrastructure.Security;

namespace TokenHound.Infrastructure.Providers.Antigravity;

/// <summary>
/// Client for querying quota metrics remotely from the Google Cloud Code backend (Layer 2).
/// </summary>
public sealed class AntigravityCloudCodeClient : IDisposable
{
    private const string CREDENTIAL_TARGET = "gemini:antigravity";
    private const string ENDPOINT_URL = "https://cloudcode-pa.googleapis.com/v1internal:retrieveUserQuotaSummary";
    private const string EMPTY_PAYLOAD = "{}";

    private readonly HttpClient _httpClient;
    private readonly ICredentialStore _credentialStore;
    private readonly string _credentialsFilePath;
    private readonly bool _disposeClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="AntigravityCloudCodeClient"/> class.
    /// </summary>
    /// <param name="httpClient">Optional pre-configured HttpClient instance.</param>
    /// <param name="credentialStore">Optional credential store instance.</param>
    /// <param name="credentialsFilePath">Optional file path to oauth_creds.json.</param>
    public AntigravityCloudCodeClient(
        HttpClient? httpClient = null,
        ICredentialStore? credentialStore = null,
        string? credentialsFilePath = null)
    {
        _credentialStore = credentialStore ?? new WindowsCredentialManager();

        if (httpClient is not null)
        {
            _httpClient = httpClient;
            _disposeClient = false;
        }
        else
        {
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(5)
            };
            _disposeClient = true;
        }

        if (credentialsFilePath is not null)
        {
            _credentialsFilePath = credentialsFilePath;
        }
        else
        {
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            _credentialsFilePath = Path.Combine(userProfile, ".gemini", "oauth_creds.json");
        }
    }

    /// <summary>
    /// Resolves an OAuth access token from Credential Manager or local oauth_creds.json.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Access token, or null if not found.</returns>
    public async ValueTask<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var storedSecret = await _credentialStore.ReadCredentialAsync(
            CREDENTIAL_TARGET,
            cancellationToken
        ).ConfigureAwait(false);

        if (!string.IsNullOrWhiteSpace(storedSecret))
        {
            var token = ExtractTokenFromSecret(storedSecret);

            if (!string.IsNullOrWhiteSpace(token))
            {
                return token;
            }
        }

        return await ReadTokenFromFileAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Determines whether an OAuth credential exists in Credential Manager or oauth_creds.json.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if a credential exists; otherwise, false.</returns>
    public async ValueTask<bool> HasCredentialAsync(CancellationToken cancellationToken = default)
    {
        var token = await GetAccessTokenAsync(cancellationToken).ConfigureAwait(false);

        return !string.IsNullOrWhiteSpace(token);
    }

    /// <summary>
    /// Queries the Google Cloud Code backend for user quota summary.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The quota summary response, or null when no borrowed credential exists.</returns>
    /// <exception cref="ProviderHttpException">Thrown when the endpoint returns a non-success status.</exception>
    public async ValueTask<AntigravityQuotaSummaryResponse?> RetrieveUserQuotaSummaryAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var accessToken = await GetAccessTokenAsync(cancellationToken).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(accessToken))
            return null;

        using var request = new HttpRequestMessage(HttpMethod.Post, ENDPOINT_URL);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Content = new StringContent(EMPTY_PAYLOAD, Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken
        ).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            var message = $"Google Cloud Code quota request failed with HTTP {(int)response.StatusCode} ({response.StatusCode}).";

            throw ProviderHttpException.FromResponse(response, message);
        }

        var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        var envelope = JsonSerializer.Deserialize<AntigravityQuotaEnvelope>(content);

        return envelope?.Response
            ?? throw new JsonException("Google Cloud Code quota response was empty.");
    }

    private static string? ExtractTokenFromSecret(string secret)
    {
        var trimmed = secret.Trim();

        if (!trimmed.StartsWith('{'))
        {
            return trimmed;
        }

        try
        {
            using var doc = JsonDocument.Parse(trimmed);
            var root = doc.RootElement;

            if (root.TryGetProperty("token", out var tokenObj) &&
                tokenObj.ValueKind == JsonValueKind.Object &&
                tokenObj.TryGetProperty("access_token", out var nestedToken) &&
                nestedToken.ValueKind == JsonValueKind.String)
            {
                return nestedToken.GetString();
            }

            if (root.TryGetProperty("access_token", out var rootToken) &&
                rootToken.ValueKind == JsonValueKind.String)
            {
                return rootToken.GetString();
            }
        }
        catch (JsonException)
        {
            // Ignore parse errors on malformed secrets
        }

        return null;
    }

    private async ValueTask<string?> ReadTokenFromFileAsync(CancellationToken cancellationToken)
    {

        if (!File.Exists(_credentialsFilePath))
        {
            return null;
        }

        try
        {
            using var stream = new FileStream(
                _credentialsFilePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete);

            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);

            if (doc.RootElement.TryGetProperty("access_token", out var prop) &&
                prop.ValueKind == JsonValueKind.String)
            {
                return prop.GetString();
            }
        }
        catch
        {
            return null;
        }

        return null;
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
