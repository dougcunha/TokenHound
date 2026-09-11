using System;
using System.Linq;
using TokenHound.Core.Models;

namespace TokenHound.App.ViewModels;

public sealed partial class ProviderRingViewModel
{
    private static string? ResolveStatusMessage(Snapshot snapshot)
    {

        if (snapshot.Status == ProviderStatus.Ok && snapshot.Fidelity == Fidelity.Derived)
        {
            var requestWindow = snapshot.LimitWindows.FirstOrDefault(static w =>
                w.RemainingUnits.HasValue && w.TotalUnits == null);

            if (requestWindow?.RemainingUnits is { } count)
            {
                return $"~{count} requests today · no limit published";
            }
        }

        if (snapshot.ActiveBlock is { } activeBlock)
            return activeBlock.Reason;

        return snapshot.Status switch
        {
            ProviderStatus.NeedsAuth => !string.IsNullOrWhiteSpace(snapshot.ErrorDescription) ? snapshot.ErrorDescription : "Execute 'claude login' in terminal",
            ProviderStatus.RateLimited => snapshot.ActiveBlock?.Reason ?? snapshot.ErrorDescription ?? "Rate limit reached",
            ProviderStatus.AccessDenied => snapshot.ErrorDescription ?? "Access denied",
            ProviderStatus.Stale => snapshot.ErrorDescription ?? "Telemetry is stale",
            ProviderStatus.Unsupported => snapshot.ErrorDescription ?? "No usable quota available",
            ProviderStatus.NotRunning => !string.IsNullOrWhiteSpace(snapshot.ErrorDescription) ? snapshot.ErrorDescription : "Provider is not running",
            _ => null
        };
    }
}
