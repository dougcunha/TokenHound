using System;

namespace TokenHound.Infrastructure.Engine;

/// <summary>
/// Carries a monitoring enablement change for a registered provider.
/// </summary>
public sealed class ProviderEnablementChangedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProviderEnablementChangedEventArgs"/> class.
    /// </summary>
    /// <param name="providerId">The provider identifier.</param>
    /// <param name="isEnabled">The new monitoring state of the provider.</param>
    public ProviderEnablementChangedEventArgs(
        string providerId,
        bool isEnabled)
    {

        ArgumentException.ThrowIfNullOrWhiteSpace(providerId);
        ProviderId = providerId;
        IsEnabled = isEnabled;
    }

    /// <summary>
    /// Gets the provider identifier whose monitoring state changed.
    /// </summary>
    public string ProviderId { get; }

    /// <summary>
    /// Gets a value indicating whether the provider is now monitored.
    /// </summary>
    public bool IsEnabled { get; }
}
