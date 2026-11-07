using AwesomeAssertions;
using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.App.HudFeed;
using TokenHound.App.ViewModels;
using TokenHound.Infrastructure.HudFeed;
using TokenHound.Infrastructure.Tests.ViewModels;

namespace TokenHound.Infrastructure.Tests.HudFeed;

/// <summary>Verifies the write triggers, burst merging, and shutdown document of <see cref="HudFeedPublisher"/>.</summary>
public sealed class HudFeedPublisherTests
{
    private static readonly DateTimeOffset FIXED_NOW = new(2026, 11, 7, 20, 0, 0, TimeSpan.Zero);

    /// <summary>Verifies that a publish writes the rings that the HUD shows.</summary>
    [Fact]
    public async Task PublishNowAsync_WhenStarted_WritesCurrentRings()
    {

        var sink = new RecordingSink();
        var rings = new ObservableCollection<ProviderRingViewModel> { new("codex"), new("claude") };
        await using var publisher = CreatePublisher(rings, sink, new ManualTimeProvider(FIXED_NOW));
        publisher.Start();

        await publisher.PublishNowAsync();

        sink.Documents.Should().ContainSingle();
        sink.Documents.Single().Providers.Select(static p => p.ProviderId).Should().Equal("codex", "claude");
        sink.Documents.Single().WrittenAtUtc.Should().Be(FIXED_NOW);
    }

    /// <summary>Verifies that a burst of ring changes produces one write.</summary>
    [Fact]
    public async Task Start_WhenRingsChangeInBurst_WritesOnce()
    {

        var sink = new RecordingSink();
        var ring = new ProviderRingViewModel("codex");
        var rings = new ObservableCollection<ProviderRingViewModel> { ring };
        await using var publisher = new HudFeedPublisher(
            rings,
            sink.WriteAsync,
            static action => action(),
            "1",
            TimeProvider.System,
            TimeSpan.FromMilliseconds(100),
            TimeSpan.FromHours(1)
        );

        publisher.Start();

        for (var index = 0; index < 5; index++)
            ring.UpdateActivity(index % 2 == 0, $"change {index}");

        rings.Add(new ProviderRingViewModel("claude"));
        await sink.WaitForCountAsync(1);
        await Task.Delay(300, TestContext.Current.CancellationToken);

        sink.Documents.Should().ContainSingle();
        sink.Documents.Single().Providers.Should().HaveCount(2);
    }

    /// <summary>Verifies that dispose writes a final stopped document and ignores later ring changes.</summary>
    [Fact]
    public async Task DisposeAsync_AfterWrite_WritesStoppedDocumentAndUnsubscribes()
    {

        var sink = new RecordingSink();
        var ring = new ProviderRingViewModel("codex");
        var clock = new ManualTimeProvider(FIXED_NOW);
        var publisher = CreatePublisher([ring], sink, clock);
        publisher.Start();
        await publisher.PublishNowAsync();

        await publisher.DisposeAsync();
        ring.UpdateActivity(true, "after dispose");
        clock.Advance(TimeSpan.FromMinutes(1));

        sink.Documents.Last().IsRunning.Should().BeFalse();
        sink.Documents.Last().Providers.Select(static p => p.ProviderId).Should().Equal("codex");
        sink.Documents.Count(static d => !d.IsRunning).Should().Be(1);
        sink.Documents.Should().HaveCount(2);
    }

    /// <summary>Verifies that dispose before any write writes nothing.</summary>
    [Fact]
    public async Task DisposeAsync_WhenNothingWritten_WritesNothing()
    {

        var sink = new RecordingSink();
        var publisher = CreatePublisher([], sink, new ManualTimeProvider(FIXED_NOW));

        await publisher.DisposeAsync();

        sink.Documents.Should().BeEmpty();
    }

    /// <summary>Verifies that a failing sink never throws into the caller.</summary>
    [Fact]
    public async Task PublishNowAsync_WhenSinkFails_DoesNotThrow()
    {

        await using var publisher = new HudFeedPublisher(
            [],
            static (_, _) => throw new UnauthorizedAccessException("denied"),
            static action => action(),
            "1",
            new ManualTimeProvider(FIXED_NOW)
        );

        var act = publisher.PublishNowAsync;

        await act.Should().NotThrowAsync();
    }

    private static HudFeedPublisher CreatePublisher(
        ObservableCollection<ProviderRingViewModel> rings,
        RecordingSink sink,
        TimeProvider clock)
        => new(
            rings,
            sink.WriteAsync,
            static action => action(),
            "1",
            clock
        );

    private sealed class RecordingSink
    {
        private readonly ConcurrentQueue<HudFeedDocument> _documents = new();

        public HudFeedDocument[] Documents
            => [.. _documents];

        public Task WriteAsync(HudFeedDocument document, CancellationToken cancellationToken)
        {

            _documents.Enqueue(document);

            return Task.CompletedTask;
        }

        public async Task WaitForCountAsync(int count)
        {

            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            while (_documents.Count < count)
                await Task.Delay(10, timeout.Token);
        }
    }
}
