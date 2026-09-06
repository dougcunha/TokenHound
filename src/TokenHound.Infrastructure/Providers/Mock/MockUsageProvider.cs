using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Core.Contracts;
using TokenHound.Core.Models;

namespace TokenHound.Infrastructure.Providers.Mock;

/// <summary>
/// Provides a deterministic, configurable offline implementation of <see cref="IUsageProvider"/>
/// for development, UI state testing, and automated test fixtures without external network I/O.
/// </summary>
public sealed class MockUsageProvider : IUsageProvider
{
    private const string DEFAULT_PROVIDER_ID = "mock";

    private MockScenario? _currentScenario;
    private Snapshot _currentSnapshot = null!;

    /// <summary>
    /// Initializes a new instance of the <see cref="MockUsageProvider"/> class with the default provider identifier.
    /// </summary>
    public MockUsageProvider()
        : this(DEFAULT_PROVIDER_ID)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MockUsageProvider"/> class with a specified provider identifier.
    /// </summary>
    /// <param name="providerId">The unique identifier for this provider.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="providerId"/> is null, empty, or whitespace.</exception>
    public MockUsageProvider(string providerId)
    {

        ArgumentException.ThrowIfNullOrWhiteSpace(providerId);

        ProviderId = providerId;
        SetScenario(MockScenario.Normal);
    }

    /// <inheritdoc />
    public string ProviderId { get; }

    /// <summary>
    /// Gets the currently active scenario preset, or <see langword="null"/> if a custom snapshot was set.
    /// </summary>
    public MockScenario? CurrentScenario
        => _currentScenario;

    /// <inheritdoc />
    public ValueTask<Snapshot> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {

        if (cancellationToken.IsCancellationRequested)
            return ValueTask.FromCanceled<Snapshot>(cancellationToken);

        return ValueTask.FromResult(_currentSnapshot);
    }

    /// <summary>
    /// Sets a custom snapshot to be returned by subsequent calls to <see cref="GetSnapshotAsync"/>.
    /// </summary>
    /// <param name="snapshot">The custom snapshot to return.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="snapshot"/> is null.</exception>
    public void SetCustomSnapshot(Snapshot snapshot)
    {

        ArgumentNullException.ThrowIfNull(snapshot);

        _currentScenario = null;
        _currentSnapshot = snapshot;
    }

    /// <summary>
    /// Switches the provider to a predefined operational scenario.
    /// </summary>
    /// <param name="scenario">The operational scenario to activate.</param>
    public void SetScenario(MockScenario scenario)
    {

        _currentScenario = scenario;
        _currentSnapshot = CreateSnapshot(scenario);
    }

    private Snapshot CreateSnapshot(MockScenario scenario)
        => scenario switch
        {
            MockScenario.Normal => CreateNormalSnapshot(),
            MockScenario.Warning => CreateWarningSnapshot(),
            MockScenario.RateLimited => CreateRateLimitedSnapshot(),
            MockScenario.NeedsAuth => CreateNeedsAuthSnapshot(),
            MockScenario.Stale => CreateStaleSnapshot(),
            MockScenario.Unidirectional => CreateUnidirectionalSnapshot(),
            _ => throw new ArgumentOutOfRangeException(nameof(scenario), scenario, null)
        };

    private Snapshot CreateNormalSnapshot()
    {

        var now = DateTimeOffset.UtcNow;

        return new Snapshot
        {
            ProviderId = ProviderId,
            Status = ProviderStatus.Ok,
            Fidelity = Fidelity.Official,
            FetchedAtUtc = now,
            LimitWindows =
            [
                new LimitWindow
                {
                    Name = "Session",
                    UsedFraction = 0.20,
                    RemainingUnits = 80,
                    TotalUnits = 100,
                    Period = TimeSpan.FromHours(5),
                    ResetTimeUtc = now.AddHours(4)
                },
                new LimitWindow
                {
                    Name = "Weekly",
                    UsedFraction = 0.15,
                    RemainingUnits = 85,
                    TotalUnits = 100,
                    Period = TimeSpan.FromDays(7),
                    ResetTimeUtc = now.AddDays(6)
                }
            ],
            ActiveBlock = null,
            ErrorDescription = null
        };
    }

    private Snapshot CreateWarningSnapshot()
    {

        var now = DateTimeOffset.UtcNow;

        return new Snapshot
        {
            ProviderId = ProviderId,
            Status = ProviderStatus.Ok,
            Fidelity = Fidelity.Official,
            FetchedAtUtc = now,
            LimitWindows =
            [
                new LimitWindow
                {
                    Name = "Session",
                    UsedFraction = 0.20,
                    RemainingUnits = 80,
                    TotalUnits = 100,
                    Period = TimeSpan.FromHours(5),
                    ResetTimeUtc = now.AddHours(4)
                },
                new LimitWindow
                {
                    Name = "Weekly",
                    UsedFraction = 0.85,
                    RemainingUnits = 15,
                    TotalUnits = 100,
                    Period = TimeSpan.FromDays(7),
                    ResetTimeUtc = now.AddDays(1)
                }
            ],
            ActiveBlock = null,
            ErrorDescription = null
        };
    }

    private Snapshot CreateRateLimitedSnapshot()
    {

        var now = DateTimeOffset.UtcNow;

        return new Snapshot
        {
            ProviderId = ProviderId,
            Status = ProviderStatus.RateLimited,
            Fidelity = Fidelity.Official,
            FetchedAtUtc = now,
            LimitWindows = [],
            ActiveBlock = new UsageBlock
            {
                Reason = "HTTP 429",
                IsBlocked = true,
                ResetTimeUtc = now.AddMinutes(10),
                RetryAfterSeconds = 600
            },
            ErrorDescription = null
        };
    }

    private Snapshot CreateNeedsAuthSnapshot()
    {

        var now = DateTimeOffset.UtcNow;

        return new Snapshot
        {
            ProviderId = ProviderId,
            Status = ProviderStatus.NeedsAuth,
            Fidelity = Fidelity.Official,
            FetchedAtUtc = now,
            LimitWindows = [],
            ActiveBlock = null,
            ErrorDescription = "Execute login in terminal"
        };
    }

    private Snapshot CreateStaleSnapshot()
    {

        var now = DateTimeOffset.UtcNow;

        return new Snapshot
        {
            ProviderId = ProviderId,
            Status = ProviderStatus.Stale,
            Fidelity = Fidelity.Official,
            FetchedAtUtc = now.AddMinutes(-20),
            LimitWindows =
            [
                new LimitWindow
                {
                    Name = "Session",
                    UsedFraction = 0.20,
                    RemainingUnits = 80,
                    TotalUnits = 100,
                    Period = TimeSpan.FromHours(5),
                    ResetTimeUtc = now.AddHours(4)
                },
                new LimitWindow
                {
                    Name = "Weekly",
                    UsedFraction = 0.15,
                    RemainingUnits = 85,
                    TotalUnits = 100,
                    Period = TimeSpan.FromDays(7),
                    ResetTimeUtc = now.AddDays(6)
                }
            ],
            ActiveBlock = null,
            ErrorDescription = null
        };
    }

    private Snapshot CreateUnidirectionalSnapshot()
    {

        var now = DateTimeOffset.UtcNow;

        return new Snapshot
        {
            ProviderId = ProviderId,
            Status = ProviderStatus.Ok,
            Fidelity = Fidelity.Official,
            FetchedAtUtc = now,
            LimitWindows =
            [
                new LimitWindow
                {
                    Name = "Unidirectional",
                    RemainingUnits = 450,
                    TotalUnits = null,
                    UsedFraction = null
                }
            ],
            ActiveBlock = null,
            ErrorDescription = null
        };
    }
}
