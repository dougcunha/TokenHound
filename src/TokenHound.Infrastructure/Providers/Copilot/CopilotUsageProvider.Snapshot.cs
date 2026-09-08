using System;
using System.Net;
using TokenHound.Core.Models;

namespace TokenHound.Infrastructure.Providers.Copilot;

public sealed partial class CopilotUsageProvider
{
    private Snapshot MapResponse(CopilotQuotaResponse response)
    {

        var result = CopilotQuotaParser.Parse(response);

        return result.Status switch
        {
            CopilotQuotaParseStatus.Success => CreateSuccessSnapshot(result),
            CopilotQuotaParseStatus.NoFiniteQuota => CreateUnsupportedSnapshot(),
            _ => CreateStaleSnapshot(result.ErrorDescription ?? "Copilot quota schema drifted.")
        };
    }

    private Snapshot MapHttpError(
        CopilotApiException exception,
        CopilotCredential credential)
    {

        return exception.StatusCode switch
        {
            HttpStatusCode.Unauthorized => CreateNeedsAuthSnapshot(),
            HttpStatusCode.Forbidden => CopilotCredential.IsPatShaped(credential.AccessToken)
                ? CreateNeedsAuthSnapshot(NEEDS_AUTH_MESSAGE + OVERRIDE_WARNING)
                : CreateUnsupportedSnapshot(
                    "Copilot has no usable finite quota or entitlement." + OVERRIDE_WARNING),
            HttpStatusCode.TooManyRequests => CreateRateLimitedSnapshot(
                _timeProvider.GetUtcNow().AddSeconds(exception.RetryAfterSeconds ?? 60),
                exception.RetryAfterSeconds
            ),
            _ => CreateStaleSnapshot($"Copilot quota request returned HTTP {(int?)exception.StatusCode}.")
        };
    }

    private Snapshot CreateSuccessSnapshot(CopilotQuotaParseResult result)
    {

        var activeBlock = CreateQuotaBlock(result);

        return new Snapshot
        {
            ProviderId = PROVIDER_ID,
            Status = ProviderStatus.Ok,
            Fidelity = Fidelity.Official,
            FetchedAtUtc = _timeProvider.GetUtcNow(),
            LimitWindows = [result.LimitWindow!],
            ActiveBlock = activeBlock
        };
    }

    private static UsageBlock? CreateQuotaBlock(CopilotQuotaParseResult result)
    {

        if (result.Remaining is not <= 0 || result.ResetTimeUtc is null)
            return null;

        var overagePermitted = result.OveragePermitted == true;

        return new UsageBlock
        {
            IsBlocked = !overagePermitted,
            ResetTimeUtc = result.ResetTimeUtc,
            Reason = overagePermitted
                ? "Quota exhausted; metered overage continues."
                : "Quota exhausted until the reported reset."
        };
    }

    private Snapshot CreateRateLimitedSnapshot(DateTimeOffset deadlineUtc, int? retryAfterSeconds)
    {

        return new Snapshot
        {
            ProviderId = PROVIDER_ID,
            Status = ProviderStatus.Stale,
            Fidelity = Fidelity.Official,
            FetchedAtUtc = _timeProvider.GetUtcNow(),
            LimitWindows = [],
            ActiveBlock = new UsageBlock
            {
                IsBlocked = true,
                ResetTimeUtc = deadlineUtc,
                RetryAfterSeconds = retryAfterSeconds,
                Reason = "Copilot quota request was rate limited."
            },
            ErrorDescription = "Copilot quota is temporarily rate limited."
        };
    }

    private Snapshot CreateNeedsAuthSnapshot(string? description = null)
        => CreateSnapshot(ProviderStatus.NeedsAuth, description ?? NEEDS_AUTH_MESSAGE);

    private Snapshot CreateUnsupportedSnapshot(string? description = null)
        => CreateSnapshot(
            ProviderStatus.Unsupported,
            description ?? "Copilot has no usable finite quota or entitlement.");

    private Snapshot CreateStaleSnapshot(string description)
        => CreateSnapshot(ProviderStatus.Stale, description);

    private Snapshot CreateSnapshot(ProviderStatus status, string description)
        => new()
        {
            ProviderId = PROVIDER_ID,
            Status = status,
            Fidelity = Fidelity.Official,
            FetchedAtUtc = _timeProvider.GetUtcNow(),
            LimitWindows = [],
            ErrorDescription = description
        };
}
