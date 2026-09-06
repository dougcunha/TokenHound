using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Core.Contracts;

namespace TokenHound.Infrastructure.Security;

/// <summary>
/// Provides read-only credential retrieval from the Windows Credential Manager
/// using native <c>advapi32.dll</c> interop.
/// </summary>
public sealed class WindowsCredentialManager : ICredentialStore
{
    private const string ADVAPI32 = "advapi32.dll";
    private const int CRED_TYPE_GENERIC = 1;
    private const int ERROR_NOT_FOUND = 1168;
    private const int ERROR_NO_SUCH_LOGON_SESSION = 1312;
    private const string GO_KEYRING_PREFIX = "go-keyring-base64:";

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowsCredentialManager"/> class.
    /// </summary>
    public WindowsCredentialManager()
    {
    }

    /// <summary>
    /// Reads a stored credential for the specified target name from Windows Credential Manager.
    /// </summary>
    /// <param name="target">The target credential name or resource identifier.</param>
    /// <returns>The credential secret text, or <see langword="null"/> if not found or unsupported OS.</returns>
    public static string? ReadCredential(string target)
    {

        ArgumentException.ThrowIfNullOrWhiteSpace(target);

        if (!OperatingSystem.IsWindows())
            return null;

        return ReadCredentialCore(target);
    }

    /// <inheritdoc />
    public ValueTask<string?> ReadCredentialAsync(
        string target,
        CancellationToken cancellationToken = default)
    {

        ArgumentException.ThrowIfNullOrWhiteSpace(target);

        if (cancellationToken.IsCancellationRequested)
            return ValueTask.FromCanceled<string?>(cancellationToken);

        if (!OperatingSystem.IsWindows())
            return ValueTask.FromResult<string?>(null);

        return ValueTask.FromResult(ReadCredentialCore(target));
    }

    [DllImport(
        ADVAPI32,
        EntryPoint = "CredReadW",
        CharSet = CharSet.Unicode,
        SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CredReadW(
        string target,
        int type,
        int reservedFlag,
        out IntPtr credentialPtr
    );

    [DllImport(ADVAPI32, EntryPoint = "CredFree")]
    private static extern void CredFree(IntPtr buffer);

    private static string? ReadCredentialCore(string target)
    {

        if (!TryReadCredentialPointer(target, out var credPtr))
            return null;

        try
        {

            return ExtractCredentialSecret(credPtr);
        }
        finally
        {

            CredFree(credPtr);
        }
    }

    private static bool TryReadCredentialPointer(
        string target,
        out IntPtr credentialPtr)
    {

        if (CredReadW(
            target,
            CRED_TYPE_GENERIC,
            0,
            out credentialPtr
        ))
            return true;

        var errorCode = Marshal.GetLastPInvokeError();

        if (errorCode is ERROR_NOT_FOUND or ERROR_NO_SUCH_LOGON_SESSION)
            return false;

        throw new Win32Exception(
            errorCode,
            $"Failed to read Windows credential for target '{target}'."
        );
    }

    private static string? ExtractCredentialSecret(IntPtr credPtr)
    {

        var cred = Marshal.PtrToStructure<NativeCredential>(credPtr);

        if (cred.CredentialBlob == IntPtr.Zero || cred.CredentialBlobSize <= 0)
            return null;

        var blob = new byte[cred.CredentialBlobSize];

        Marshal.Copy(
            cred.CredentialBlob,
            blob,
            0,
            cred.CredentialBlobSize
        );

        return DecodeCredentialBlob(blob);
    }

    private static string DecodeCredentialBlob(byte[] blob)
    {

        try
        {

            var utf8 = new UTF8Encoding(
                encoderShouldEmitUTF8Identifier: false,
                throwOnInvalidBytes: true
            );
            var utf8Text = utf8.GetString(blob);

            if (!HasEmbeddedNulls(utf8Text))
                return UnpackGoKeyringString(utf8Text);
        }
        catch (DecoderFallbackException)
        {

            // Payload is not valid UTF-8; fallback to Unicode (UTF-16LE).
        }

        var unicodeText = Encoding.Unicode.GetString(blob);

        return UnpackGoKeyringString(unicodeText);
    }

    private static bool HasEmbeddedNulls(string text)
    {

        var trimmed = text.TrimEnd('\0');

        return trimmed.Contains('\0');
    }

    private static string UnpackGoKeyringString(string input)
    {

        var trimmed = input.TrimEnd('\0');

        if (!trimmed.StartsWith(GO_KEYRING_PREFIX, StringComparison.OrdinalIgnoreCase))
            return trimmed;

        try
        {

            var base64Part = trimmed[GO_KEYRING_PREFIX.Length..].TrimEnd('\0');
            var decodedBytes = Convert.FromBase64String(base64Part);

            return Encoding.UTF8.GetString(decodedBytes).TrimEnd('\0');
        }
        catch (FormatException)
        {

            return trimmed;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeCredential
    {
        public int Flags;
        public int Type;
        public IntPtr TargetName;
        public IntPtr Comment;
        public FILETIME LastWritten;
        public int CredentialBlobSize;
        public IntPtr CredentialBlob;
        public int Persist;
        public int AttributeCount;
        public IntPtr Attributes;
        public IntPtr TargetAlias;
        public IntPtr UserName;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct FILETIME
    {
        public uint DateTimeLow;
        public uint DateTimeHigh;
    }
}
