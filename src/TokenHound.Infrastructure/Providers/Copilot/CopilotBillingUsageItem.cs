using System.Text.Json.Serialization;

namespace TokenHound.Infrastructure.Providers.Copilot;

/// <summary>
/// Represents one usage item reported by the GitHub billing AI credit usage endpoint.
/// </summary>
public sealed record CopilotBillingUsageItem
{
    /// <summary>
    /// Gets the product name.
    /// </summary>
    [JsonPropertyName("product")]
    public string? Product { get; init; }

    /// <summary>
    /// Gets the SKU name.
    /// </summary>
    [JsonPropertyName("sku")]
    public string? Sku { get; init; }

    /// <summary>
    /// Gets the model identifier.
    /// </summary>
    [JsonPropertyName("model")]
    public string? Model { get; init; }

    /// <summary>
    /// Gets the unit type.
    /// </summary>
    [JsonPropertyName("unitType")]
    public string? UnitType { get; init; }

    /// <summary>
    /// Gets the gross quantity consumed.
    /// </summary>
    [JsonPropertyName("grossQuantity")]
    public decimal? GrossQuantity { get; init; }

    /// <summary>
    /// Gets the discount quantity applied.
    /// </summary>
    [JsonPropertyName("discountQuantity")]
    public decimal? DiscountQuantity { get; init; }

    /// <summary>
    /// Gets the net quantity consumed.
    /// </summary>
    [JsonPropertyName("netQuantity")]
    public decimal? NetQuantity { get; init; }

    /// <summary>
    /// Gets the gross monetary amount.
    /// </summary>
    [JsonPropertyName("grossAmount")]
    public decimal? GrossAmount { get; init; }

    /// <summary>
    /// Gets the discount monetary amount.
    /// </summary>
    [JsonPropertyName("discountAmount")]
    public decimal? DiscountAmount { get; init; }

    /// <summary>
    /// Gets the net monetary amount.
    /// </summary>
    [JsonPropertyName("netAmount")]
    public decimal? NetAmount { get; init; }

    /// <summary>
    /// Gets the price per unit.
    /// </summary>
    [JsonPropertyName("pricePerUnit")]
    public decimal? PricePerUnit { get; init; }
}
