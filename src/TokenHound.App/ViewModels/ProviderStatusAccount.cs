using System.Collections.Generic;

namespace TokenHound.App.ViewModels;

/// <summary>
/// Represents one account or profile row of the provider status window.
/// </summary>
public sealed record ProviderStatusAccount
{
    /// <summary>Gets the provider identifier of the account.</summary>
    public required string ProviderId { get; init; }

    /// <summary>Gets the account display name, identical to the HUD provider name.</summary>
    public required string DisplayName { get; init; }

    /// <summary>Gets the HUD status message, or null when the provider reports nothing noteworthy.</summary>
    public string? StatusMessage { get; init; }

    /// <summary>Gets a value indicating whether a status message is present.</summary>
    public bool HasStatusMessage
        => !string.IsNullOrWhiteSpace(StatusMessage);

    /// <summary>Gets a value indicating whether the provider is blocking requests, which paints the status message in the alert colour.</summary>
    public bool IsBlocked { get; init; }

    /// <summary>Gets a value indicating whether any quota window of the account is exhausted.</summary>
    public bool IsExhausted { get; init; }

    /// <summary>Gets the comeback text of an exhausted account, or null when not exhausted.</summary>
    public string? BackInText { get; init; }

    /// <summary>Gets a value indicating whether comeback text is present.</summary>
    public bool HasBackIn
        => !string.IsNullOrWhiteSpace(BackInText);

    /// <summary>Gets a value indicating whether the provider has not produced its first snapshot yet.</summary>
    public bool IsPending { get; init; }

    /// <summary>Gets the quota and credit columns, one per HUD usage row.</summary>
    public IReadOnlyList<ProviderStatusColumn> Columns { get; init; } = [];
}
