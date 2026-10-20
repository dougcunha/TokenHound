using System;
using System.Buffers.Binary;
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
    private const string STALE_TOKEN = "11111111-2222-3333-4444-555555555555";
    private const long SIMULATED_BASE_ADDRESS = 0x3DB0000000L;

    [Fact]
    public void ScanRegion_WhenGoStructPairContainsLocalhostAndGuid_ReturnsToken()
    {
        var memory = new byte[1024];
        WriteEntry(memory, 128, "localhost:63348");
        WriteEntry(memory, 256, EXPECTED_TOKEN);
        WriteGoStringPair(
            memory,
            512,
            SIMULATED_BASE_ADDRESS + 128,
            15,
            SIMULATED_BASE_ADDRESS + 256,
            36
        );
        var buffer = new byte[256];

        var actual = AntigravityProcessMemoryReader.ScanRegion(
            memory,
            memory.Length,
            buffer,
            CopyFromRegion,
            CopyFromVirtualAddress
        );

        Assert.Equal(EXPECTED_TOKEN, actual);
    }

    [Fact]
    public void ScanRegion_WhenStalePrefixTokenPrecedesGoStructPair_PrefersGoStructToken()
    {
        var memory = new byte[1024];
        WriteEntry(memory, 32, $"ANTIGRAVITY_CSRF_TOKEN={STALE_TOKEN}");
        WriteEntry(memory, 160, "localhost:63348");
        WriteEntry(memory, 200, EXPECTED_TOKEN);
        WriteGoStringPair(
            memory,
            640,
            SIMULATED_BASE_ADDRESS + 160,
            15,
            SIMULATED_BASE_ADDRESS + 200,
            36
        );
        var buffer = new byte[256];

        var actual = AntigravityProcessMemoryReader.ScanRegion(
            memory,
            memory.Length,
            buffer,
            CopyFromRegion,
            CopyFromVirtualAddress
        );

        Assert.Equal(EXPECTED_TOKEN, actual);
    }

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

    private static void WriteGoStringPair(
        byte[] destination,
        int offset,
        long hostPtr,
        long hostLen,
        long tokenPtr,
        long tokenLen)
    {
        BinaryPrimitives.WriteInt64LittleEndian(destination.AsSpan(offset, 8), hostPtr);
        BinaryPrimitives.WriteInt64LittleEndian(destination.AsSpan(offset + 8, 8), hostLen);
        BinaryPrimitives.WriteInt64LittleEndian(destination.AsSpan(offset + 16, 8), tokenPtr);
        BinaryPrimitives.WriteInt64LittleEndian(destination.AsSpan(offset + 24, 8), tokenLen);
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

        if (offset < 0 || offset >= source.Length)
            return 0;

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

    private static int CopyFromVirtualAddress(
        byte[] source,
        long virtualAddress,
        byte[] destination,
        int count)
        => CopyFromRegion(
            source,
            virtualAddress - SIMULATED_BASE_ADDRESS,
            destination,
            count
        );
}
