namespace TokenHound.Core.Models;

/// <summary>
/// Represents one already parsed Copilot billing usage dimension.
/// </summary>
public sealed record CopilotCreditUsageItem
{
    /// <summary>
    /// Gets the product identifier reported by the billing source.
    /// </summary>
    public string? Product { get; init; }

    /// <summary>
    /// Gets the SKU identifier reported by the billing source.
    /// </summary>
    public string? Sku { get; init; }

    /// <summary>
    /// Gets the model identifier reported by the billing source.
    /// </summary>
    public string? Model { get; init; }

    /// <summary>
    /// Gets the unit identifier reported by the billing source.
    /// </summary>
    public string? UnitType { get; init; }

    /// <summary>
    /// Gets the gross quantity, when reported.
    /// </summary>
    public decimal? GrossQuantity { get; init; }

    /// <summary>
    /// Gets the discounted or included quantity, when reported.
    /// </summary>
    public decimal? DiscountQuantity { get; init; }

    /// <summary>
    /// Gets the net or additional quantity, when reported.
    /// </summary>
    public decimal? NetQuantity { get; init; }
}
