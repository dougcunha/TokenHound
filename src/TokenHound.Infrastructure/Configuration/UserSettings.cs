using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TokenHound.Infrastructure.Configuration;

/// <summary>
/// Root model for user-configurable settings persisted in LocalAppData.
/// </summary>
public sealed record UserSettings
{
    /// <summary>
    /// Gets the HUD window placement settings.
    /// </summary>
    public HudPositionSettings? Hud { get; init; }

    /// <summary>
    /// Gets the provider enablement settings.
    /// </summary>
    [JsonConverter(typeof(ProviderSettingsJsonConverter))]
    public ProviderSettings? Providers { get; init; }

    /// <summary>
    /// Gets the periodic refresh schedule settings.
    /// </summary>
    public RefreshSettings? Refresh { get; init; }

    /// <summary>
    /// Gets the HTTP 429 rate limit resilience settings.
    /// </summary>
    public RateLimitSettings? RateLimit { get; init; }

    /// <summary>
    /// Gets extension data for unmapped JSON properties, preserving unknown sections during serialization.
    /// </summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? ExtensionData { get; init; }

    /// <summary>
    /// Custom converter mapping the nested providers dictionary to and from standard JSON.
    /// </summary>
    internal sealed class ProviderSettingsJsonConverter : JsonConverter<ProviderSettings>
    {
        private const string ALIAS_PROVIDER_KEY = "antigravity";
        private const string CANONICAL_PROVIDER_KEY = "gemini";
        private const string ENABLED_PROPERTY_NAME = "Enabled";

        /// <inheritdoc />
        public override ProviderSettings? Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {

            if (reader.TokenType == JsonTokenType.Null)
                return null;

            if (reader.TokenType != JsonTokenType.StartObject)
                return new ProviderSettings();

            using var document = JsonDocument.ParseValue(ref reader);

            var states = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

            foreach (var property in document.RootElement.EnumerateObject())
            {

                var rawKey = property.Name.Trim();

                if (rawKey.Length == 0)
                    continue;

                var isAlias = string.Equals(rawKey, ALIAS_PROVIDER_KEY, StringComparison.OrdinalIgnoreCase);
                var providerId = isAlias ? CANONICAL_PROVIDER_KEY : rawKey;

                if (isAlias && states.ContainsKey(CANONICAL_PROVIDER_KEY))
                    continue;

                states[providerId] = ReadEnabled(property.Value);
            }

            return new ProviderSettings { EnabledStates = states };
        }

        /// <inheritdoc />
        public override void Write(
            Utf8JsonWriter writer,
            ProviderSettings value,
            JsonSerializerOptions options)
        {

            writer.WriteStartObject();

            var writtenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (value.EnabledStates.TryGetValue(CANONICAL_PROVIDER_KEY, out var canonicalEnabled))
            {

                writtenKeys.Add(CANONICAL_PROVIDER_KEY);
                writtenKeys.Add(ALIAS_PROVIDER_KEY);

                writer.WritePropertyName(CANONICAL_PROVIDER_KEY);
                writer.WriteStartObject();
                writer.WriteBoolean(ENABLED_PROPERTY_NAME, canonicalEnabled);
                writer.WriteEndObject();
            }

            foreach (var (key, isEnabled) in value.EnabledStates)
            {

                var trimmed = key.Trim();

                if (trimmed.Length == 0)
                    continue;

                var isAlias = string.Equals(trimmed, ALIAS_PROVIDER_KEY, StringComparison.OrdinalIgnoreCase);
                var providerId = isAlias ? CANONICAL_PROVIDER_KEY : trimmed;

                if (!writtenKeys.Add(providerId))
                    continue;

                writer.WritePropertyName(providerId);
                writer.WriteStartObject();
                writer.WriteBoolean(ENABLED_PROPERTY_NAME, isEnabled);
                writer.WriteEndObject();
            }

            writer.WriteEndObject();
        }

        private static bool ReadEnabled(JsonElement element)
        {

            if (element.ValueKind != JsonValueKind.Object)
                return true;

            if (!element.TryGetProperty(ENABLED_PROPERTY_NAME, out var enabledProp))
                return true;

            if (enabledProp.ValueKind == JsonValueKind.True)
                return true;

            if (enabledProp.ValueKind == JsonValueKind.False)
                return false;

            return true;
        }
    }
}
