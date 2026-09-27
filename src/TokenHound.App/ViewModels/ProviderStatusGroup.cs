using System.Collections.Generic;

namespace TokenHound.App.ViewModels;

/// <summary>
/// Represents a provider family and its account rows in the provider status window.
/// </summary>
public sealed record ProviderStatusGroup
{
    /// <summary>Gets the provider family name shown in the group header.</summary>
    public required string FamilyName { get; init; }

    /// <summary>Gets the account rows of the family, default profile first.</summary>
    public required IReadOnlyList<ProviderStatusAccount> Accounts { get; init; }

    /// <summary>Gets the number of account rows in the group.</summary>
    public int Count
        => Accounts.Count;
}
