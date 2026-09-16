using System;
using System.Text.Json;

namespace TokenHound.Infrastructure.Providers.Cline;

/// <summary>
/// Parses the Cline CLI/desktop provider store into borrowed credentials.
/// </summary>
/// <remarks>
/// The store maps provider identifiers to a <c>settings</c> object holding an <c>auth</c> block with
/// <c>accessToken</c>, <c>expiresAt</c>, and <c>accountId</c>. Cline Pass shares the same credential
/// shape under its own key, so both keys are accepted in a fixed precedence order.
/// </remarks>
public static class ClineProvidersJson
{
    /// <summary>Non-sensitive source prefix for credentials discovered from providers.json.</summary>
    public const string SOURCE_PREFIX = "providers.json";

    private const string ACCESS_TOKEN_PROPERTY = "accessToken";
    private const string ACCOUNT_ID_PROPERTY = "accountId";
    private const string API_KEY_PROPERTY = "apiKey";
    private const string AUTH_PROPERTY = "auth";
    private const string EXPIRES_AT_PROPERTY = "expiresAt";
    private const string PROVIDERS_PROPERTY = "providers";
    private const string SETTINGS_PROPERTY = "settings";

    private static readonly string[] PROVIDER_KEYS = ["cline", "cline-pass"];

    /// <summary>
    /// Parses Cline <c>providers.json</c> content and extracts the first usable credential.
    /// </summary>
    /// <param name="jsonContent">The raw JSON string to parse.</param>
    /// <returns>The extracted <see cref="ClineAuthDto"/>, or <see langword="null"/> if not found or invalid.</returns>
    public static ClineAuthDto? Parse(string? jsonContent)
    {

        if (string.IsNullOrWhiteSpace(jsonContent))
            return null;

        try
        {

            using var document = JsonDocument.Parse(jsonContent);

            if (document.RootElement.ValueKind != JsonValueKind.Object ||
                !document.RootElement.TryGetProperty(PROVIDERS_PROPERTY, out var providers) ||
                providers.ValueKind != JsonValueKind.Object)
                return null;

            foreach (var providerKey in PROVIDER_KEYS)
            {

                if (!providers.TryGetProperty(providerKey, out var providerEntry))
                    continue;

                var credential = ExtractCredential(providerEntry, providerKey);

                if (credential is not null)
                    return credential;
            }

            return null;
        }
        catch (JsonException)
        {

            return null;
        }
    }

    private static ClineAuthDto? ExtractCredential(JsonElement providerEntry, string providerKey)
    {

        if (providerEntry.ValueKind != JsonValueKind.Object ||
            !providerEntry.TryGetProperty(SETTINGS_PROPERTY, out var settings) ||
            settings.ValueKind != JsonValueKind.Object)
            return null;

        var accessToken = ResolveAccessToken(settings);

        if (string.IsNullOrWhiteSpace(accessToken))
            return null;

        settings.TryGetProperty(AUTH_PROPERTY, out var auth);

        return new ClineAuthDto
        {
            AccessToken = accessToken.Trim(),
            ExpiresAtUtc = ReadExpiry(auth),
            AccountId = ReadString(auth, ACCOUNT_ID_PROPERTY),
            Source = $"{SOURCE_PREFIX}:{providerKey}"
        };
    }

    private static string? ResolveAccessToken(JsonElement settings)
    {

        if (!settings.TryGetProperty(AUTH_PROPERTY, out var auth) || auth.ValueKind != JsonValueKind.Object)
            return ReadString(settings, API_KEY_PROPERTY);

        return ReadString(auth, ACCESS_TOKEN_PROPERTY)
            ?? ReadString(auth, API_KEY_PROPERTY);
    }

    private static string? ReadString(JsonElement parent, string propertyName)
    {

        if (parent.ValueKind != JsonValueKind.Object ||
            !parent.TryGetProperty(propertyName, out var value) ||
            value.ValueKind != JsonValueKind.String)
            return null;

        return value.GetString();
    }

    private static DateTimeOffset? ReadExpiry(JsonElement auth)
    {

        if (auth.ValueKind != JsonValueKind.Object ||
            !auth.TryGetProperty(EXPIRES_AT_PROPERTY, out var expiry))
            return null;

        if (expiry.ValueKind == JsonValueKind.Number && expiry.TryGetInt64(out var epochMilliseconds))
            return DateTimeOffset.FromUnixTimeMilliseconds(epochMilliseconds);

        if (expiry.ValueKind == JsonValueKind.String && DateTimeOffset.TryParse(expiry.GetString(), out var parsed))
            return parsed.ToUniversalTime();

        return null;
    }
}
