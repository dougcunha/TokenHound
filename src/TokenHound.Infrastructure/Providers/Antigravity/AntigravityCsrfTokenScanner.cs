using System;
using System.Buffers.Binary;

namespace TokenHound.Infrastructure.Providers.Antigravity;

/// <summary>
/// Extracts Antigravity CSRF tokens from process memory buffers.
/// </summary>
internal static class AntigravityCsrfTokenScanner
{
    public const int GUID_STRING_LENGTH = 36;
    public const int CHUNK_OVERLAP = 64;
    public const int SCRATCH_BUFFER_SIZE = 64;

    private const int GO_STRING_PAIR_SIZE = 32;
    private const int MIN_HOST_LENGTH = 11;
    private const int MAX_HOST_LENGTH = 22;
    private const long MIN_USER_ADDRESS = 0x10000L;
    private const long MAX_USER_ADDRESS = 0x7FFFFFFF0000L;

    private static readonly byte[] TARGET_PREFIX = "ANTIGRAVITY_CSRF_TOKEN="u8.ToArray();

    /// <summary>
    /// Scans an 8-byte aligned memory span for a Go string header pair representing
    /// a localhost endpoint followed by a 36-character UUID CSRF token.
    /// </summary>
    public static string? FindGoStructToken<TState>(
        ReadOnlySpan<byte> span,
        TState state,
        byte[] scratch,
        Func<TState, long, byte[], int, int> readAtAddress)
    {

        for (var offset = 0; offset <= span.Length - GO_STRING_PAIR_SIZE; offset += 8)
        {
            var tokenLen = BinaryPrimitives.ReadInt64LittleEndian(span.Slice(offset + 24, 8));

            if (tokenLen != GUID_STRING_LENGTH)
                continue;

            var hostLen = BinaryPrimitives.ReadInt64LittleEndian(span.Slice(offset + 8, 8));

            if (hostLen is < MIN_HOST_LENGTH or > MAX_HOST_LENGTH)
                continue;

            var hostPtr = BinaryPrimitives.ReadInt64LittleEndian(span.Slice(offset, 8));
            var tokenPtr = BinaryPrimitives.ReadInt64LittleEndian(span.Slice(offset + 16, 8));
            var token = TryResolveGoStructPair(
                state,
                hostPtr,
                (int)hostLen,
                tokenPtr,
                scratch,
                readAtAddress
            );

            if (!string.IsNullOrEmpty(token))
                return token;
        }

        return null;
    }

    /// <summary>
    /// Scans a memory span for an environment variable prefix followed by a valid UUID token.
    /// </summary>
    public static string? FindPrefixToken(ReadOnlySpan<byte> span)
    {
        var offset = 0;

        while (offset < span.Length)
        {
            var index = span[offset..].IndexOf(TARGET_PREFIX);

            if (index < 0)
                break;

            var matchPos = offset + index;
            var tokenStart = matchPos + TARGET_PREFIX.Length;

            if (tokenStart + GUID_STRING_LENGTH <= span.Length)
            {
                var token = ExtractValidToken(span.Slice(tokenStart, GUID_STRING_LENGTH));

                if (!string.IsNullOrEmpty(token))
                    return token;
            }

            offset = matchPos + TARGET_PREFIX.Length;
        }

        return null;
    }

    private static string? TryResolveGoStructPair<TState>(
        TState state,
        long hostPtr,
        int hostLen,
        long tokenPtr,
        byte[] scratch,
        Func<TState, long, byte[], int, int> readAtAddress)
    {

        if (!IsValidUserPointer(hostPtr) || !IsValidUserPointer(tokenPtr))
            return null;

        var hostRead = readAtAddress(
            state,
            hostPtr,
            scratch,
            hostLen
        );

        if (hostRead != hostLen || !IsLocalhostEndpoint(scratch.AsSpan(0, hostLen)))
            return null;

        var tokenRead = readAtAddress(
            state,
            tokenPtr,
            scratch,
            GUID_STRING_LENGTH
        );

        if (tokenRead != GUID_STRING_LENGTH)
            return null;

        return ExtractValidToken(scratch.AsSpan(0, GUID_STRING_LENGTH));
    }

    private static bool IsValidUserPointer(long address)
        => address is >= MIN_USER_ADDRESS and < MAX_USER_ADDRESS;

    private static bool IsLocalhostEndpoint(ReadOnlySpan<byte> hostSpan)
    {
        var portOffset = GetPortOffset(hostSpan);

        if (portOffset <= 0 || portOffset >= hostSpan.Length)
            return false;

        for (var i = portOffset; i < hostSpan.Length; i++)
        {

            if (hostSpan[i] is < (byte)'0' or > (byte)'9')
                return false;
        }

        return true;
    }

    private static int GetPortOffset(ReadOnlySpan<byte> hostSpan)
    {

        if (hostSpan.StartsWith("localhost:"u8) || hostSpan.StartsWith("127.0.0.1:"u8))
            return 10;

        if (hostSpan.StartsWith("http://localhost:"u8))
            return 17;

        return -1;
    }

    private static string? ExtractValidToken(ReadOnlySpan<byte> tokenBytes)
    {
        Span<char> charBuffer = stackalloc char[GUID_STRING_LENGTH];

        for (var i = 0; i < GUID_STRING_LENGTH; i++)
            charBuffer[i] = (char)tokenBytes[i];

        if (Guid.TryParse(charBuffer, out var guid))
            return guid.ToString();

        return null;
    }
}
