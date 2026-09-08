using System;

namespace TokenHound.Infrastructure.Tests.Providers.Copilot;

/// <summary>
/// Controllable time provider for rate-limit and backoff testing.
/// </summary>
internal sealed class MutableTimeProvider(DateTimeOffset initialUtcNow) : TimeProvider
{
    private DateTimeOffset _current = initialUtcNow;

    public override DateTimeOffset GetUtcNow()
        => _current;

    public void Advance(TimeSpan duration)
        => _current += duration;
}
