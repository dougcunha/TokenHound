using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Core.Models;
using TokenHound.Infrastructure.Providers.Copilot;
using Xunit;

namespace TokenHound.Infrastructure.Tests.Providers.Copilot;

/// <summary>
/// Verifies NDJSON report streaming, validation, deduplication, and partition merging.
/// </summary>
public sealed class CopilotMetricsReportParserTests
{
    private static readonly DateOnly TEST_DAY = new(2026, 9, 6);

    [Fact]
    public void Parse_WithValidRows_SumsCreditsAndCountsUsers()
    {
        const string ndjson = """
        {"day":"2026-09-06","user_id":1,"user_login":"alice","organization_id":"ColibriAgile","ai_credits_used":10.5}
        {"day":"2026-09-06","user_id":2,"user_login":"bob","organization_id":"ColibriAgile","ai_credits_used":5.25}
        """;

        using var reader = new StringReader(ndjson);
        var result = CopilotMetricsReportParser.Parse(reader, TEST_DAY, "alice", "ColibriAgile");

        Assert.False(result.HasInvalidRows);
        Assert.False(result.IsDayInvalid);
        Assert.Equal(15.75m, result.TotalCreditsUsed);
        Assert.Equal(2, result.UserCount);
        Assert.NotNull(result.PrincipalRow);
        Assert.Equal("alice", result.PrincipalRow.UserLogin);
    }

    [Fact]
    public void Parse_WithIdenticalDuplicateRows_DeduplicatesAndPreservesQuantities()
    {
        const string ndjson = """
        {"day":"2026-09-06","user_id":1,"user_login":"alice","organization_id":"ColibriAgile","ai_credits_used":10.5}
        {"day":"2026-09-06","user_id":1,"user_login":"alice","organization_id":"ColibriAgile","ai_credits_used":10.5}
        """;

        using var reader = new StringReader(ndjson);
        var result = CopilotMetricsReportParser.Parse(reader, TEST_DAY);

        Assert.False(result.HasInvalidRows);
        Assert.False(result.IsDayInvalid);
        Assert.Equal(10.5m, result.TotalCreditsUsed);
        Assert.Equal(1, result.UserCount);
    }

    [Fact]
    public void Parse_WithConflictingDuplicateRows_RejectsAndInvalidatesDay()
    {
        const string ndjson = """
        {"day":"2026-09-06","user_id":1,"user_login":"alice","organization_id":"ColibriAgile","ai_credits_used":10.5}
        {"day":"2026-09-06","user_id":1,"user_login":"alice","organization_id":"ColibriAgile","ai_credits_used":20.0}
        """;

        using var reader = new StringReader(ndjson);
        var result = CopilotMetricsReportParser.Parse(reader, TEST_DAY);

        Assert.True(result.IsDayInvalid);
        Assert.True(result.HasInvalidRows);
        Assert.Equal(0m, result.TotalCreditsUsed);
    }

    [Fact]
    public void Parse_WithInvalidOrMalformedRows_FlagsInvalidAndPreservesValidRows()
    {
        const string ndjson = """
        {not-valid-json}
        {"day":"2026-09-05","user_id":1,"user_login":"alice","ai_credits_used":10.0}
        {"day":"2026-09-06","ai_credits_used":10.0}
        {"day":"2026-09-06","user_id":3,"user_login":"carol","ai_credits_used":-5.0}
        {"day":"2026-09-06","user_id":4,"user_login":"dave","ai_credits_used":12.0}
        """;

        using var reader = new StringReader(ndjson);
        var result = CopilotMetricsReportParser.Parse(reader, TEST_DAY);

        Assert.True(result.HasInvalidRows);
        Assert.False(result.IsDayInvalid);
        Assert.Equal(12.0m, result.TotalCreditsUsed);
        Assert.Equal(1, result.UserCount);
    }

    [Fact]
    public async Task ParseAsync_StreamsValidNdjsonFromStream()
    {
        const string ndjson = """
        {"day":"2026-09-06","user_id":1,"user_login":"alice","ai_credits_used":8.0}
        {"day":"2026-09-06","user_id":2,"user_login":"bob","ai_credits_used":12.0}
        """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(ndjson));
        var result = await CopilotMetricsReportParser.ParseAsync(
            stream,
            TEST_DAY,
            null,
            null,
            CopilotBillingScope.Organization,
            TestContext.Current.CancellationToken
        );

        Assert.False(result.HasInvalidRows);
        Assert.Equal(20.0m, result.TotalCreditsUsed);
        Assert.Equal(2, result.UserCount);
    }

    [Fact]
    public async Task ParseAsync_WhenCancelledAfterFirstLine_StopsReading()
    {

        using var stream = new CancellationAwareStream(
            "{\"day\":\"2026-09-06\",\"user_id\":1,\"user_login\":\"alice\",\"ai_credits_used\":8.0}\n"
        );
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(
            TestContext.Current.CancellationToken
        );
        var parseTask = CopilotMetricsReportParser.ParseAsync(
            stream,
            TEST_DAY,
            cancellationToken: cancellation.Token
        );

        await stream.SecondReadStarted.WaitAsync(TestContext.Current.CancellationToken);
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await parseTask);
    }

    [Fact]
    public void Merge_WithCrossPartitionIdenticalAndConflictingRows_BehavesCorrectly()
    {
        var part1 = CopilotMetricsReportParser.Parse(
            new StringReader("""
            {"day":"2026-09-06","user_id":1,"user_login":"alice","ai_credits_used":10.0}
            {"day":"2026-09-06","user_id":2,"user_login":"bob","ai_credits_used":20.0}
            """),
            TEST_DAY
        );

        var part2 = CopilotMetricsReportParser.Parse(
            new StringReader("""
            {"day":"2026-09-06","user_id":3,"user_login":"carol","ai_credits_used":30.0}
            {"day":"2026-09-06","user_id":1,"user_login":"alice","ai_credits_used":10.0}
            """),
            TEST_DAY
        );

        var merged = CopilotMetricsReportParser.Merge(TEST_DAY, [part1, part2]);

        Assert.False(merged.IsDayInvalid);
        Assert.False(merged.HasInvalidRows);
        Assert.Equal(60.0m, merged.TotalCreditsUsed);
        Assert.Equal(3, merged.UserCount);

        var part3 = CopilotMetricsReportParser.Parse(
            new StringReader("""
            {"day":"2026-09-06","user_id":1,"user_login":"alice","ai_credits_used":99.0}
            """),
            TEST_DAY
        );

        var conflictingMerge = CopilotMetricsReportParser.Merge(TEST_DAY, [part1, part3]);

        Assert.True(conflictingMerge.IsDayInvalid);
        Assert.True(conflictingMerge.HasInvalidRows);
        Assert.Equal(0m, conflictingMerge.TotalCreditsUsed);
    }

    [Fact]
    public void Parse_WithScopeMismatch_FlagsInvalidRow()
    {
        const string ndjson = """
        {"day":"2026-09-06","user_id":1,"user_login":"alice","organization_id":"WrongOrg","ai_credits_used":10.0}
        """;

        using var reader = new StringReader(ndjson);
        var result = CopilotMetricsReportParser.Parse(
            reader,
            TEST_DAY,
            null,
            "ExpectedOrg",
            CopilotBillingScope.Organization
        );

        Assert.True(result.HasInvalidRows);
        Assert.Equal(0m, result.TotalCreditsUsed);
        Assert.Equal(0, result.UserCount);
    }

    private sealed class CancellationAwareStream(string firstChunk) : Stream
    {
        private readonly byte[] _firstChunk = Encoding.UTF8.GetBytes(firstChunk);
        private readonly TaskCompletionSource _secondReadStarted = new(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        private int _readCount;

        internal Task SecondReadStarted
            => _secondReadStarted.Task;

        /// <inheritdoc />
        public override bool CanRead => true;

        /// <inheritdoc />
        public override bool CanSeek => false;

        /// <inheritdoc />
        public override bool CanWrite => false;

        /// <inheritdoc />
        public override long Length => throw new NotSupportedException();

        /// <inheritdoc />
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        /// <inheritdoc />
        public override void Flush()
            => throw new NotSupportedException();

        /// <inheritdoc />
        public override int Read(byte[] buffer, int offset, int count)
            => throw new NotSupportedException();

        /// <inheritdoc />
        public override ValueTask<int> ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken = default)
        {

            if (Interlocked.Increment(ref _readCount) == 1)
            {
                _firstChunk.AsSpan().CopyTo(buffer.Span);

                return ValueTask.FromResult(_firstChunk.Length);
            }

            _secondReadStarted.TrySetResult();

            return new ValueTask<int>(WaitForCancellationAsync(cancellationToken));
        }

        /// <inheritdoc />
        public override long Seek(long offset, SeekOrigin origin)
            => throw new NotSupportedException();

        /// <inheritdoc />
        public override void SetLength(long value)
            => throw new NotSupportedException();

        /// <inheritdoc />
        public override void Write(byte[] buffer, int offset, int count)
            => throw new NotSupportedException();

        private static async Task<int> WaitForCancellationAsync(CancellationToken cancellationToken)
        {

            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);

            return 0;
        }
    }
}
