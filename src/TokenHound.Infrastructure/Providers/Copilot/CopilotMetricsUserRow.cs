using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TokenHound.Infrastructure.Providers.Copilot;

/// <summary>
/// Represents a single user row in a streamed Copilot daily metrics NDJSON report.
/// </summary>
public sealed record CopilotMetricsUserRow
{
    /// <summary>
    /// Gets the reporting day.
    /// </summary>
    [JsonPropertyName("day")]
    public DateOnly? Day { get; init; }

    /// <summary>
    /// Gets the GitHub user identifier.
    /// </summary>
    [JsonPropertyName("user_id")]
    public long? UserId { get; init; }

    /// <summary>
    /// Gets the GitHub user login name.
    /// </summary>
    [JsonPropertyName("user_login")]
    public string? UserLogin { get; init; }

    /// <summary>
    /// Gets the organization identifier or login.
    /// </summary>
    [JsonPropertyName("organization_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? OrganizationId { get; init; }

    /// <summary>
    /// Gets the enterprise identifier or slug.
    /// </summary>
    [JsonPropertyName("enterprise_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? EnterpriseId { get; init; }

    /// <summary>
    /// Gets the decimal quantity of AI credits consumed.
    /// </summary>
    [JsonPropertyName("ai_credits_used")]
    public decimal? AiCreditsUsed { get; init; }

    private sealed class FlexibleStringConverter : JsonConverter<string>
    {
        public override string? Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {

            return reader.TokenType switch
            {
                JsonTokenType.String => reader.GetString(),
                JsonTokenType.Number => reader.TryGetInt64(out var longVal)
                    ? longVal.ToString(CultureInfo.InvariantCulture)
                    : reader.GetDecimal().ToString(CultureInfo.InvariantCulture),
                JsonTokenType.Null => null,
                _ => null
            };
        }

        public override void Write(
            Utf8JsonWriter writer,
            string value,
            JsonSerializerOptions options)
        {

            writer.WriteStringValue(value);
        }
    }
}
