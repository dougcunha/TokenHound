using System;
using System.Globalization;
using System.Text.Json;

namespace TokenHound.Infrastructure.Providers.Claude;

public sealed partial class ClaudeProfileDiscovery
{
    private const string OAUTH_PROPERTY_NAME = "claudeAiOauth";
    private const string ACCESS_TOKEN_PROPERTY_NAME = "accessToken";
    private const string TOKEN_PROPERTY_NAME = "token";
    private const string EXPIRES_AT_PROPERTY_NAME = "expiresAt";
    private const string EXPIRES_AT_SNAKE_PROPERTY_NAME = "expires_at";

    /// <summary>
    /// Parses a JSON string containing Claude Code credentials.
    /// </summary>
    /// <param name="jsonContent">The raw JSON string to parse.</param>
    /// <returns>The extracted <see cref="ClaudeCredentialDto"/>, or <see langword="null"/> if parsing fails or no token is found.</returns>
    public static ClaudeCredentialDto? ParseCredentialJson(string? jsonContent)
    {

        if (string.IsNullOrWhiteSpace(jsonContent))
            return null;

        try
        {

            using var document = JsonDocument.Parse(jsonContent);
            var root = document.RootElement;

            if (root.ValueKind != JsonValueKind.Object)
                return null;

            var token = ExtractToken(root);

            if (string.IsNullOrWhiteSpace(token))
                return null;

            return new ClaudeCredentialDto { AccessToken = token, ExpiresAt = ExtractExpiresAt(root) };
        }
        catch (JsonException)
        {

            return null;
        }
    }

    private static string? ExtractToken(JsonElement root)
    {

        if (root.TryGetProperty(OAUTH_PROPERTY_NAME, out var oauthElement) &&
            oauthElement.ValueKind == JsonValueKind.Object)
        {

            var oauthToken = TryGetTokenString(oauthElement);

            if (!string.IsNullOrWhiteSpace(oauthToken))
                return oauthToken;
        }

        return TryGetTokenString(root);
    }

    private static string? TryGetTokenString(JsonElement element)
    {

        if (element.TryGetProperty(ACCESS_TOKEN_PROPERTY_NAME, out var prop) &&
            prop.ValueKind == JsonValueKind.String)
            return prop.GetString();

        if (element.TryGetProperty(TOKEN_PROPERTY_NAME, out prop) &&
            prop.ValueKind == JsonValueKind.String)
            return prop.GetString();

        return null;
    }

    private static DateTimeOffset? ExtractExpiresAt(JsonElement root)
    {

        if (root.TryGetProperty(OAUTH_PROPERTY_NAME, out var oauthElement) &&
            oauthElement.ValueKind == JsonValueKind.Object)
        {

            var expiry = TryGetExpiry(oauthElement);

            if (expiry.HasValue)
                return expiry;
        }

        return TryGetExpiry(root);
    }

    private static DateTimeOffset? TryGetExpiry(JsonElement element)
    {

        if (element.TryGetProperty(EXPIRES_AT_PROPERTY_NAME, out var prop) ||
            element.TryGetProperty(EXPIRES_AT_SNAKE_PROPERTY_NAME, out prop))
            return ParseExpiryElement(prop);

        return null;
    }

    private static DateTimeOffset? ParseExpiryElement(JsonElement element)
    {

        if (element.ValueKind == JsonValueKind.Number && element.TryGetInt64(out var epochVal))
            return ClaudeEpochTimestampParser.Parse(epochVal);

        if (element.ValueKind == JsonValueKind.String)
        {

            var str = element.GetString();

            if (long.TryParse(str, CultureInfo.InvariantCulture, out var parsedEpoch))
                return ClaudeEpochTimestampParser.Parse(parsedEpoch);

            if (DateTimeOffset.TryParse(str, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsedDate))
                return parsedDate;
        }

        return null;
    }
}
