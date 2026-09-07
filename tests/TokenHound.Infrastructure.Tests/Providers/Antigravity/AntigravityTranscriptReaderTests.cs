using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Infrastructure.Providers.Antigravity;
using Xunit;

namespace TokenHound.Infrastructure.Tests.Providers.Antigravity;

/// <summary>
/// Unit tests for <see cref="AntigravityTranscriptReader"/>.
/// </summary>
public sealed class AntigravityTranscriptReaderTests
{
    private sealed class FakeTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    [Fact]
    public async Task CountTodayModelRequestsAsync_CountsOnlyModelTurnsFromToday()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), $"antigravity_test_{Guid.NewGuid():N}");
        var brainDir = Path.Combine(tempDir, "conv1", ".system_generated", "logs");
        Directory.CreateDirectory(brainDir);

        var fixedNow = new DateTimeOffset(2026, 9, 6, 15, 0, 0, TimeSpan.Zero);
        var timeProvider = new FakeTimeProvider(fixedNow);

        var transcriptPath = Path.Combine(brainDir, "transcript.jsonl");

        var lines = new[]
        {
            """{"step_index": 1, "source": "MODEL", "type": "PLANNER_RESPONSE", "created_at": "2026-09-06T14:00:00.000Z"}""",
            """{"step_index": 2, "source": "USER", "type": "USER_INPUT", "created_at": "2026-09-06T14:05:00.000Z"}""",
            """{"step_index": 3, "source": "MODEL", "type": "PLANNER_RESPONSE", "created_at": "2026-09-06T14:10:00.000Z"}""",
            """{"step_index": 4, "source": "SYSTEM", "type": "SYSTEM_CHECKPOINT", "created_at": "2026-09-06T14:15:00.000Z"}""",
            """{"step_index": 5, "source": "MODEL", "type": "PLANNER_RESPONSE", "created_at": "2026-09-05T20:00:00.000Z"}""", // Yesterday
            """{invalid json}""",
            ""
        };

        await File.WriteAllLinesAsync(transcriptPath, lines, TestContext.Current.CancellationToken);

        try
        {
            var reader = new AntigravityTranscriptReader([tempDir], timeProvider);

            // Act
            var count = await reader.CountTodayModelRequestsAsync(TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(2, count);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task CountTodayModelRequestsAsync_WhenNoFiles_ReturnsZero()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), $"antigravity_empty_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var reader = new AntigravityTranscriptReader([tempDir]);

            // Act
            var count = await reader.CountTodayModelRequestsAsync(TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(0, count);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task CountTodayModelRequestsAsync_PropagatesCancellation()
    {
        // Arrange
        var reader = new AntigravityTranscriptReader(["C:\\nonexistent_dummy_path_123"]);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await reader.CountTodayModelRequestsAsync(cts.Token);
        });
    }
}
