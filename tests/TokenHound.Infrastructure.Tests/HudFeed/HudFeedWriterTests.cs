using AwesomeAssertions;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using TokenHound.Infrastructure.HudFeed;

namespace TokenHound.Infrastructure.Tests.HudFeed;

/// <summary>Verifies the file format and replacement behavior of <see cref="HudFeedWriter"/>.</summary>
public sealed class HudFeedWriterTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"hudfeed-{Guid.NewGuid():N}");

    /// <inheritdoc />
    public void Dispose()
    {

        if (Directory.Exists(_directory))
            Directory.Delete(_directory, true);
    }

    /// <summary>Verifies that the file uses the camelCase contract and writes null fields explicitly.</summary>
    [Fact]
    public async Task WriteAsync_WhenDocumentHasNulls_WritesContractKeys()
    {

        var writer = new HudFeedWriter(_directory);

        await writer.WriteAsync(CreateDocument(isRunning: true), TestContext.Current.CancellationToken);

        using var json = JsonDocument.Parse(await File.ReadAllTextAsync(writer.FilePath, TestContext.Current.CancellationToken));
        var root = json.RootElement;
        root.GetProperty("schemaVersion").GetInt32().Should().Be(1);
        root.GetProperty("isRunning").GetBoolean().Should().BeTrue();
        root.GetProperty("heartbeatSeconds").GetInt32().Should().Be(30);
        var provider = root.GetProperty("providers")[0];
        provider.GetProperty("providerId").GetString().Should().Be("claude");
        provider.GetProperty("weeklyUsedFraction").ValueKind.Should().Be(JsonValueKind.Null);
        provider.GetProperty("rows")[0].GetProperty("usedFraction").ValueKind.Should().Be(JsonValueKind.Null);
    }

    /// <summary>Verifies that a second write replaces the file and leaves no temporary file.</summary>
    [Fact]
    public async Task WriteAsync_WhenFileExists_ReplacesItAtomically()
    {

        var writer = new HudFeedWriter(_directory);

        await writer.WriteAsync(CreateDocument(isRunning: true), TestContext.Current.CancellationToken);
        await writer.WriteAsync(CreateDocument(isRunning: false), TestContext.Current.CancellationToken);

        using var json = JsonDocument.Parse(await File.ReadAllTextAsync(writer.FilePath, TestContext.Current.CancellationToken));
        json.RootElement.GetProperty("isRunning").GetBoolean().Should().BeFalse();
        Directory.GetFiles(_directory).Should().ContainSingle().Which.Should().EndWith(HudFeedWriter.FILE_NAME);
    }

    private static HudFeedDocument CreateDocument(bool isRunning)
        => new()
        {
            WrittenAtUtc = new DateTimeOffset(2026, 11, 7, 20, 0, 0, TimeSpan.Zero),
            IsRunning = isRunning,
            AppVersion = "1.2.3",
            HeartbeatSeconds = 30,
            Providers =
            [
                new HudFeedProvider
                {
                    ProviderId = "claude",
                    DisplayName = "Claude Code",
                    FamilyName = "Claude",
                    Badge = "C",
                    Status = "ok",
                    IsBusy = false,
                    SessionUsedFraction = 0.4,
                    Rows = [new HudFeedRow { Key = "quota:0", Label = "Pro", PrimaryText = "~120 requests" }]
                }
            ]
        };
}
