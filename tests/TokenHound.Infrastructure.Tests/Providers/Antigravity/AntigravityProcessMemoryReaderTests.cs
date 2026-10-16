using System;
using System.Text;
using TokenHound.Infrastructure.Providers.Antigravity;
using Xunit;

namespace TokenHound.Infrastructure.Tests.Providers.Antigravity;

/// <summary>
/// Unit tests for <see cref="AntigravityProcessMemoryReader"/>.
/// </summary>
public sealed class AntigravityProcessMemoryReaderTests
{
    private const string EXPECTED_TOKEN = "430f9893-5762-4aa3-a4d4-3c15ef5c193f";

    [Fact]
    public void ScanRegion_WhenTokenIsInLaterChunkBeyondFirstWindow_ReturnsToken()
    {

        var memory = new byte[4096];
        WriteEntry(memory, 2600, $"ANTIGRAVITY_CSRF_TOKEN={EXPECTED_TOKEN}");
        var buffer = new byte[512];

        var actual = AntigravityProcessMemoryReader.ScanRegion(
            memory,
            memory.Length,
            buffer,
            CopyFromRegion
        );

        Assert.Equal(EXPECTED_TOKEN, actual);
    }

    [Fact]
    public void ScanRegion_WhenTokenStraddlesChunkBoundary_ReturnsToken()
    {

        var memory = new byte[1024];
        var buffer = new byte[256];
        var boundaryOffset = buffer.Length - 12;
        WriteEntry(memory, boundaryOffset, $"ANTIGRAVITY_CSRF_TOKEN={EXPECTED_TOKEN}");

        var actual = AntigravityProcessMemoryReader.ScanRegion(
            memory,
            memory.Length,
            buffer,
            CopyFromRegion
        );

        Assert.Equal(EXPECTED_TOKEN, actual);
    }

    [Fact]
    public void ScanRegion_WhenInvalidTokenPrecedesValidToken_SkipsInvalidAndReturnsValid()
    {

        var memory = new byte[1024];
        WriteEntry(memory, 32, "ANTIGRAVITY_CSRF_TOKEN=not-a-valid-guid-string-000000000000");
        WriteEntry(memory, 600, $"ANTIGRAVITY_CSRF_TOKEN={EXPECTED_TOKEN}");
        var buffer = new byte[256];

        var actual = AntigravityProcessMemoryReader.ScanRegion(
            memory,
            memory.Length,
            buffer,
            CopyFromRegion
        );

        Assert.Equal(EXPECTED_TOKEN, actual);
    }

    [Fact]
    public void ScanRegion_WhenEarlierChunkReadFails_ContinuesScanningRemainingChunks()
    {

        var memory = new byte[1024];
        WriteEntry(memory, 600, $"ANTIGRAVITY_CSRF_TOKEN={EXPECTED_TOKEN}");
        var buffer = new byte[256];

        var actual = AntigravityProcessMemoryReader.ScanRegion(
            memory,
            memory.Length,
            buffer,
            static (source, offset, dest, count) => offset == 0
                ? 0
                : CopyFromRegion(
                    source,
                    offset,
                    dest,
                    count
                )
        );

        Assert.Equal(EXPECTED_TOKEN, actual);
    }

    [Fact]
    public void ScanRegion_WhenNoValidTokenPresent_ReturnsNull()
    {

        var memory = new byte[1024];
        WriteEntry(memory, 128, "ANTIGRAVITY_CSRF_TOKEN=invalid-token");
        var buffer = new byte[256];

        var actual = AntigravityProcessMemoryReader.ScanRegion(
            memory,
            memory.Length,
            buffer,
            CopyFromRegion
        );

        Assert.Null(actual);
    }

    private static void WriteEntry(byte[] destination, int offset, string text)
    {

        var bytes = Encoding.ASCII.GetBytes(text);
        Buffer.BlockCopy(
            bytes,
            0,
            destination,
            offset,
            bytes.Length
        );
    }

    private static int CopyFromRegion(
        byte[] source,
        long offset,
        byte[] destination,
        int count)
    {

        var available = (int)Math.Min(source.Length - offset, count);

        if (available <= 0)
            return 0;

        Buffer.BlockCopy(
            source,
            (int)offset,
            destination,
            0,
            available
        );

        return available;
    }
}
