using AwesomeAssertions;
using Microsoft.Data.Sqlite;
using System;
using System.IO;
using System.Threading.Tasks;
using TokenHound.Core.Models;
using TokenHound.Infrastructure.Providers.Codex;

namespace TokenHound.Infrastructure.Tests.Providers;

/// <summary>Verifies Codex usage snapshots, source fallback, mapping, rate limits, and auth status.</summary>
public sealed class CodexUsageProviderTests
{
    private const string SUCCESS_RESPONSE = """
        {"jsonrpc":"2.0","id":2,"result":{"rateLimits":{"planType":"plus","rateLimitReachedType":null,"primary":{"usedPercent":35.0,"windowDurationMins":300,"resetsAt":1788659888},"secondary":{"usedPercent":12.0,"windowDurationMins":10080,"resetsAt":1789246688}}}}
        """;
    private const string RATE_LIMITED_RESPONSE = """
        {"jsonrpc":"2.0","id":2,"result":{"rateLimits":{"planType":"team","rateLimitReachedType":"rate_limit_reached","primary":{"usedPercent":80.0,"windowDurationMins":300,"resetsAt":1788659888}}}}
        """;

    /// <summary>Verifies the provider identifier.</summary>
    [Fact]
    public void ProviderId_ReturnsCodex()
    {

        var provider = new CodexUsageProvider();

        provider.ProviderId.Should().Be("codex");
    }

    /// <summary>Verifies app-server priority and complete limit-window mapping.</summary>
    [Fact]
    public async Task GetSnapshotAsync_WhenAppServerReturnsLimits_ReturnsOfficialWindows()
    {

        var provider = new CodexUsageProvider(appServerClient: CreateAppServerClient(SUCCESS_RESPONSE));

        var snapshot = await provider.GetSnapshotAsync(TestContext.Current.CancellationToken);

        snapshot.Status.Should().Be(ProviderStatus.Ok);
        snapshot.Fidelity.Should().Be(Fidelity.Official);
        snapshot.LimitWindows.Should().HaveCount(2);

        var primary = snapshot.LimitWindows[0];
        primary.Name.Should().Be("Primary (5h)");
        primary.UsedFraction.Should().Be(0.35);
        primary.TotalUnits.Should().Be(100);
        primary.RemainingUnits.Should().Be(65);
        primary.ResetTimeUtc.Should().Be(DateTimeOffset.FromUnixTimeSeconds(1788659888));
        primary.Period.Should().Be(TimeSpan.FromMinutes(300));

        var secondary = snapshot.LimitWindows[1];
        secondary.Name.Should().Be("Secondary (Weekly)");
        secondary.UsedFraction.Should().Be(0.12);
        secondary.TotalUnits.Should().Be(100);
        secondary.RemainingUnits.Should().Be(88);
        secondary.ResetTimeUtc.Should().Be(DateTimeOffset.FromUnixTimeSeconds(1789246688));
        secondary.Period.Should().Be(TimeSpan.FromMinutes(10080));
    }

    /// <summary>Verifies rollout logs are used when the app server has no executable.</summary>
    [Fact]
    public async Task GetSnapshotAsync_WhenAppServerReturnsNull_UsesDerivedRolloutLimits()
    {

        var tempDirectory = CreateTempDirectory();

        try
        {

            var rolloutPath = CreateRolloutDatabase(tempDirectory);
            await File.WriteAllTextAsync(
                rolloutPath,
                CreateRolloutEvent("team", 42),
                TestContext.Current.CancellationToken
            );

            var provider = new CodexUsageProvider(
                CreateAppServerClient(null),
                new CodexRolloutLogReader(tempDirectory),
                new CodexAuthDiscovery(tempDirectory)
            );
            var snapshot = await provider.GetSnapshotAsync(TestContext.Current.CancellationToken);

            snapshot.Status.Should().Be(ProviderStatus.Ok);
            snapshot.Fidelity.Should().Be(Fidelity.Derived);
            snapshot.LimitWindows[0].Name.Should().Be("Primary (5h)");
            snapshot.LimitWindows[0].UsedFraction.Should().Be(0.42);
            snapshot.LimitWindows[0].RemainingUnits.Should().Be(58);
        }
        finally
        {

            DeleteTempDirectory(tempDirectory);
        }
    }

    /// <summary>Verifies rate-limit metadata creates an active block and RateLimited status.</summary>
    [Fact]
    public async Task GetSnapshotAsync_WhenRateLimitReached_ReturnsRateLimitedWithBlock()
    {

        var provider = new CodexUsageProvider(appServerClient: CreateAppServerClient(RATE_LIMITED_RESPONSE));

        var snapshot = await provider.GetSnapshotAsync(TestContext.Current.CancellationToken);

        snapshot.Status.Should().Be(ProviderStatus.RateLimited);
        snapshot.ActiveBlock.Should().NotBeNull();
        snapshot.ActiveBlock!.Reason.Should().Be("rate_limit_reached");
        snapshot.ActiveBlock.IsBlocked.Should().BeTrue();
        snapshot.ActiveBlock.ResetTimeUtc.Should().Be(DateTimeOffset.FromUnixTimeSeconds(1788659888));
        snapshot.LimitWindows[0].RemainingUnits.Should().Be(20);
    }

    /// <summary>Verifies missing auth after both telemetry sources fail returns actionable NeedsAuth.</summary>
    [Fact]
    public async Task GetSnapshotAsync_WhenTelemetryAndAuthAreMissing_ReturnsNeedsAuth()
    {

        var tempDirectory = CreateTempDirectory();

        try
        {

            var provider = new CodexUsageProvider(
                CreateAppServerClient(null),
                new CodexRolloutLogReader(tempDirectory),
                new CodexAuthDiscovery(tempDirectory)
            );
            var snapshot = await provider.GetSnapshotAsync(TestContext.Current.CancellationToken);

            snapshot.Status.Should().Be(ProviderStatus.NeedsAuth);
            snapshot.Fidelity.Should().Be(Fidelity.Official);
            snapshot.LimitWindows.Should().BeEmpty();
            snapshot.ErrorDescription.Should().Be(CodexUsageProvider.NEEDS_AUTH_MESSAGE);
        }
        finally
        {

            DeleteTempDirectory(tempDirectory);
        }
    }

    private static CodexAppServerClient CreateAppServerClient(string? response)
    {

        var output = response is null
            ? string.Empty
            : "{\"jsonrpc\":\"2.0\",\"id\":1,\"result\":{}}\n" + response;
        var process = new FakeProcess(new StringReader(output));

        return new CodexAppServerClient(
            new FakeProcessFactory(process),
            () => response is null ? null : "codex.exe"
        );
    }

    private static string CreateRolloutDatabase(string baseDirectory)
    {

        var codexDirectory = Path.Combine(baseDirectory, ".codex");
        Directory.CreateDirectory(codexDirectory);
        var databasePath = Path.Combine(codexDirectory, "state_5.sqlite");
        var rolloutPath = Path.Combine(baseDirectory, "rollout-fallback.jsonl");

        using var connection = new SqliteConnection($"Data Source={databasePath};Pooling=False");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "CREATE TABLE threads (rollout_path TEXT, archived INTEGER, updated_at_ms INTEGER);";
        command.ExecuteNonQuery();
        command.CommandText = "INSERT INTO threads (rollout_path, archived, updated_at_ms) VALUES ($path, 0, 1);";
        command.Parameters.AddWithValue("$path", rolloutPath);
        command.ExecuteNonQuery();

        return rolloutPath;
    }

    private static string CreateRolloutEvent(string planType, int usedPercent)
        => $"{{\"type\":\"event_msg\",\"payload\":{{\"type\":\"token_count\",\"rate_limits\":{{\"plan_type\":\"{planType}\",\"primary\":{{\"used_percent\":{usedPercent},\"window_minutes\":300,\"resets_at\":1788659888}}}}}}}}";

    private static string CreateTempDirectory()
    {

        var path = Path.Combine(Path.GetTempPath(), $"codex_provider_{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);

        return path;
    }

    private static void DeleteTempDirectory(string path)
    {

        if (Directory.Exists(path))
            Directory.Delete(path, recursive: true);
    }

    private sealed class FakeProcessFactory : CodexAppServerClient.ICodexProcessFactory
    {
        private readonly CodexAppServerClient.ICodexProcess _process;

        public FakeProcessFactory(CodexAppServerClient.ICodexProcess process)
        {

            _process = process;
        }

        public CodexAppServerClient.ICodexProcess Start(string executablePath, string arguments)
            => _process;
    }

    private sealed class FakeProcess : CodexAppServerClient.ICodexProcess
    {
        public FakeProcess(TextReader standardOutput)
        {

            StandardOutput = standardOutput;
        }

        public TextWriter StandardInput { get; } = new StringWriter();

        public TextReader StandardOutput { get; }

        public bool HasExited { get; private set; }

        public void Kill()
        {

            HasExited = true;
        }

        public void Dispose()
        {
        }
    }
}
