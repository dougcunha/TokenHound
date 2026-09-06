using AwesomeAssertions;
using NSubstitute;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Core.Contracts;
using TokenHound.Core.Models;

namespace TokenHound.Core.Tests.Contracts;

/// <summary>
/// Verifies mock substitution, type signatures, and async contract behavior for core interfaces.
/// </summary>
public sealed class ContractsSmokeTests
{
    /// <summary>
    /// Verifies that <see cref="IUsageProvider"/> can be substituted and returns expected snapshot.
    /// </summary>
    [Fact]
    public async Task IUsageProvider_CanBeSubstituted_AndReturnsSnapshotAsync()
    {
        var expectedSnapshot = new Snapshot
        {
            ProviderId = "test-provider",
            Status = ProviderStatus.Ok,
            Fidelity = Fidelity.Official,
            FetchedAtUtc = DateTimeOffset.UtcNow,
            LimitWindows = []
        };

        var provider = Substitute.For<IUsageProvider>();
        provider.ProviderId.Returns("test-provider");
        provider.GetSnapshotAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(expectedSnapshot));

        var providerId = provider.ProviderId;
        var snapshot = await provider.GetSnapshotAsync(CancellationToken.None);

        providerId.Should().Be("test-provider");
        snapshot.Should().Be(expectedSnapshot);
        await provider.Received(1).GetSnapshotAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that <see cref="IActivityMonitor"/> can be substituted and returns active session data.
    /// </summary>
    [Fact]
    public async Task IActivityMonitor_CanBeSubstituted_AndReturnsSessionAsync()
    {
        var expectedSession = new AgentSession
        {
            Pid = 4321,
            StartTimeUtc = DateTimeOffset.UtcNow.AddHours(-1),
            State = AgentSessionState.Busy,
            LastActivityUtc = DateTimeOffset.UtcNow
        };

        var monitor = Substitute.For<IActivityMonitor>();
        monitor.ProviderId.Returns("test-monitor");
        monitor.CheckLivenessAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<AgentSession?>(expectedSession));

        var providerId = monitor.ProviderId;
        var session = await monitor.CheckLivenessAsync(CancellationToken.None);

        providerId.Should().Be("test-monitor");
        session.Should().Be(expectedSession);
        await monitor.Received(1).CheckLivenessAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that <see cref="IActivityMonitor"/> returns null when no active session exists.
    /// </summary>
    [Fact]
    public async Task IActivityMonitor_CanBeSubstituted_AndReturnsNullWhenInactiveAsync()
    {
        var monitor = Substitute.For<IActivityMonitor>();
        monitor.CheckLivenessAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<AgentSession?>(null));

        var session = await monitor.CheckLivenessAsync(CancellationToken.None);

        session.Should().BeNull();
        await monitor.Received(1).CheckLivenessAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that <see cref="ICredentialStore"/> can be substituted and retrieves stored credentials.
    /// </summary>
    [Fact]
    public async Task ICredentialStore_CanBeSubstituted_AndReturnsCredentialAsync()
    {
        var store = Substitute.For<ICredentialStore>();
        store.ReadCredentialAsync("gemini:antigravity", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<string?>("secret-token-value"));

        var credential = await store.ReadCredentialAsync("gemini:antigravity", CancellationToken.None);

        credential.Should().Be("secret-token-value");
        await store.Received(1).ReadCredentialAsync("gemini:antigravity", Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that <see cref="ICredentialStore"/> returns null when a requested credential is missing.
    /// </summary>
    [Fact]
    public async Task ICredentialStore_CanBeSubstituted_AndReturnsNullWhenMissingAsync()
    {
        var store = Substitute.For<ICredentialStore>();
        store.ReadCredentialAsync("unknown:target", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<string?>(null));

        var credential = await store.ReadCredentialAsync("unknown:target", CancellationToken.None);

        credential.Should().BeNull();
        await store.Received(1).ReadCredentialAsync("unknown:target", Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that all domain contracts expose the expected type signatures and interface traits.
    /// </summary>
    [Fact]
    public void Contracts_ExposeExpectedTypeSignatures()
    {
        typeof(IUsageProvider).IsInterface.Should().BeTrue();
        typeof(IActivityMonitor).IsInterface.Should().BeTrue();
        typeof(ICredentialStore).IsInterface.Should().BeTrue();

        var usageProviderProp = typeof(IUsageProvider).GetProperty(nameof(IUsageProvider.ProviderId));
        usageProviderProp.Should().NotBeNull();
        usageProviderProp!.PropertyType.Should().Be(typeof(string));
        usageProviderProp.CanRead.Should().BeTrue();

        var usageMethod = typeof(IUsageProvider).GetMethod(nameof(IUsageProvider.GetSnapshotAsync));
        usageMethod.Should().NotBeNull();
        usageMethod!.ReturnType.Should().Be(typeof(ValueTask<Snapshot>));

        var monitorProp = typeof(IActivityMonitor).GetProperty(nameof(IActivityMonitor.ProviderId));
        monitorProp.Should().NotBeNull();
        monitorProp!.PropertyType.Should().Be(typeof(string));
        monitorProp.CanRead.Should().BeTrue();

        var monitorMethod = typeof(IActivityMonitor).GetMethod(nameof(IActivityMonitor.CheckLivenessAsync));
        monitorMethod.Should().NotBeNull();
        monitorMethod!.ReturnType.Should().Be(typeof(ValueTask<AgentSession>));

        var storeMethod = typeof(ICredentialStore).GetMethod(nameof(ICredentialStore.ReadCredentialAsync));
        storeMethod.Should().NotBeNull();
        storeMethod!.ReturnType.Should().Be(typeof(ValueTask<string>));
    }
}
