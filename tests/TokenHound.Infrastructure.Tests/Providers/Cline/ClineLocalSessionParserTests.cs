using AwesomeAssertions;
using System;
using TokenHound.Infrastructure.Providers.Cline;
using Xunit;

namespace TokenHound.Infrastructure.Tests.Providers.Cline;

/// <summary>
/// Verifies Cline session message parsing: token bucket aggregation, window filtering, and free limit detection.
/// </summary>
public sealed class ClineLocalSessionParserTests
{
    private static readonly DateTimeOffset MESSAGE_ONE_TS = DateTimeOffset.FromUnixTimeMilliseconds(1789583300000);
    private static readonly DateTimeOffset MESSAGE_TWO_TS = DateTimeOffset.FromUnixTimeMilliseconds(1789583400000);
    private static readonly DateTimeOffset WINDOW_START = DateTimeOffset.FromUnixTimeMilliseconds(1789583200000);

    private const string SAMPLE_MESSAGES = """
        {
          "version": 1,
          "sessionId": "sess-1",
          "messages": [
            { "id": "m1", "role": "user", "ts": 1789583320000, "content": [{ "type": "text", "text": "hi" }] },
            {
              "id": "m2", "role": "assistant", "ts": 1789583300000,
              "content": [],
              "metrics": { "inputTokens": 100, "outputTokens": 20, "cacheReadTokens": 5, "cacheWriteTokens": 0 }
            },
            {
              "id": "m3", "role": "assistant", "ts": 1789583400000,
              "content": [],
              "metrics": { "inputTokens": 300, "outputTokens": 40, "cacheReadTokens": 10, "cacheWriteTokens": 2 }
            }
          ]
        }
        """;

    /// <summary>Verifies that in-window metrics aggregate into per-bucket totals.</summary>
    [Fact]
    public void ParseMessages_WithInWindowMetrics_AggregatesTotals()
    {

        var sample = ClineLocalSessionParser.ParseMessages(SAMPLE_MESSAGES, WINDOW_START);

        sample.Usage.Should().NotBeNull();
        sample.Usage!.InputTokens.Should().Be(400);
        sample.Usage.OutputTokens.Should().Be(60);
        sample.Usage.CacheReadTokens.Should().Be(15);
        sample.Usage.CacheWriteTokens.Should().Be(2);
        sample.Usage.ModelCalls.Should().Be(2);
        sample.Usage.TotalTokens.Should().Be(477);
        sample.Usage.LastActivityUtc.Should().Be(MESSAGE_TWO_TS);
        sample.FreeLimit.Should().BeNull();
    }

    /// <summary>Verifies that metrics recorded before the window start are ignored.</summary>
    [Fact]
    public void ParseMessages_WithOutOfWindowMetrics_IgnoresThem()
    {

        var windowStart = DateTimeOffset.FromUnixTimeMilliseconds(1789583350000);

        var sample = ClineLocalSessionParser.ParseMessages(SAMPLE_MESSAGES, windowStart);

        sample.Usage.Should().NotBeNull();
        sample.Usage!.InputTokens.Should().Be(300);
        sample.Usage.ModelCalls.Should().Be(1);
    }

    /// <summary>Verifies that the gateway free limit message is detected with its retry delay.</summary>
    [Fact]
    public void ParseMessages_WithFreeLimitMessage_DetectsHitAndDelay()
    {

        const string json = """
            {
              "version": 1,
              "sessionId": "sess-2",
              "messages": [
                {
                  "id": "m1", "role": "assistant", "ts": 1789583400000,
                  "content": [{ "type": "text", "text": "You have reached your free limit reached on model deepseek. Please try again in 5 hours." }]
                }
              ]
            }
            """;

        var sample = ClineLocalSessionParser.ParseMessages(json, WINDOW_START);

        sample.Usage.Should().BeNull();
        sample.FreeLimit.Should().NotBeNull();
        sample.FreeLimit!.DetectedAtUtc.Should().Be(MESSAGE_TWO_TS);
        sample.FreeLimit.RetryAfter.Should().Be(TimeSpan.FromHours(5));
        sample.FreeLimit.ResetTimeUtc.Should().Be(MESSAGE_TWO_TS.AddHours(5));
        sample.FreeLimit.IsActive(MESSAGE_TWO_TS.AddHours(4)).Should().BeTrue();
        sample.FreeLimit.IsActive(MESSAGE_TWO_TS.AddHours(6)).Should().BeFalse();
    }

    /// <summary>Verifies that invalid or shapeless payloads produce an empty sample.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("{not json")]
    [InlineData("""{"version":1}""")]
    public void ParseMessages_WithUnusablePayload_ReturnsEmpty(string? json)
    {

        var sample = ClineLocalSessionParser.ParseMessages(json, WINDOW_START);

        sample.Should().Be(ClineLocalUsageSample.Empty);
    }
}