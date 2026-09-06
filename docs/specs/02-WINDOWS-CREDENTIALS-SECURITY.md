# Windows 11 Credential Storage and Security

This document specifies the security infrastructure, secret retrieval mechanisms, and concurrent file access strategies for the **Windows 11** operating system using **C# / .NET 10**.

---

## 1. Storage Mapping per Tool on Windows

Unlike macOS (where Keychain centralizes most secrets with OS confirmation prompts), developer tools on Windows 11 adopt a hybrid set of approaches:

```
+-------------------+---------------------------------------------+-------------------------------+
| Tool              | Windows 11 Storage Mechanism                | Location / Key                |
+-------------------+---------------------------------------------+-------------------------------+
| Claude Code       | JSON file with restricted NTFS ACL          | %USERPROFILE%\.claude\        |
|                   |                                             |   .credentials.json           |
+-------------------+---------------------------------------------+-------------------------------+
| Google            | Windows Credential Manager                  | Target: gemini:antigravity    |
| Antigravity       | (P/Invoke advapi32.dll -> CredReadW)        | User: antigravity             |
|                   | Fallback: Local JSON                        | %USERPROFILE%\.gemini\        |
|                   |                                             |   oauth_creds.json            |
+-------------------+---------------------------------------------+-------------------------------+
| Cursor            | SQLite in WAL mode                          | %APPDATA%\Cursor\User\        |
|                   | (cursorAuth/ keys in ItemTable)             |   globalStorage\state.vscdb   |
+-------------------+---------------------------------------------+-------------------------------+
| OpenAI Codex      | JSON file containing JWT claims             | %USERPROFILE%\.codex\         |
|                   |                                             |   auth.json                   |
+-------------------+---------------------------------------------+-------------------------------+
| Z.ai GLM          | Configuration JSON files in host            | %USERPROFILE%\.claude\        |
|                   | developer tools                             |   settings.json               |
|                   |                                             | %USERPROFILE%\.zcode\v2\      |
|                   |                                             |   config.json                 |
+-------------------+---------------------------------------------+-------------------------------+
| Perplexity        | Isolated Chromium WebView2 profile          | %LOCALAPPDATA%\TokenHound\    |
|                   | (Protected internally via DPAPI)            |   WebView2Profile             |
+-------------------+---------------------------------------------+-------------------------------+
```

---

## 2. Windows Credential Manager via P/Invoke (`advapi32.dll`)

The Go `keyring` package (utilized by Google Antigravity and several CLI tools) delegates secure Windows storage to the native **Windows Credential Manager** (`Credential Locker`).

### Target Identifier
- **TargetName**: `"gemini:antigravity"` (legacy format: `LegacyGeneric:target=gemini:antigravity`).
- **Credential Type**: `CRED_TYPE_GENERIC` ($1$).
- **UserName**: `"antigravity"`.

### C# (.NET) Implementation

Reading uses the Win32 APIs `CredReadW` and `CredFree` exported by `advapi32.dll`:

```csharp
using System;
using System.Runtime.InteropServices;
using System.Text;

public static class WindowsCredentialManager
{
    private const int CRED_TYPE_GENERIC = 1;

    [DllImport("advapi32.dll", EntryPoint = "CredReadW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CredReadW(string target, int type, int reservedFlag, out IntPtr credentialPtr);

    [DllImport("advapi32.dll", EntryPoint = "CredFree", SetLastError = true)]
    private static extern void CredFree(IntPtr buffer);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct FILETIME
    {
        public uint DateTimeLow;
        public uint DateTimeHigh;

        public DateTime ToDateTime()
        {
            long fileTime = ((long)DateTimeHigh << 32) | DateTimeLow;
            return DateTime.FromFileTimeUtc(fileTime);
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct CREDENTIALW
    {
        public int Flags;
        public int Type;
        public string TargetName;
        public string Comment;
        public FILETIME LastWritten;
        public int CredentialBlobSize;
        public IntPtr CredentialBlob;
        public int Persist;
        public int AttributeCount;
        public IntPtr Attributes;
        public string TargetAlias;
        public string UserName;
    }

    public record CredentialResult(string SecretText, DateTime LastModifiedUtc);

    public static CredentialResult? ReadGenericCredential(string targetName)
    {
        if (!CredReadW(targetName, CRED_TYPE_GENERIC, 0, out IntPtr credPtr))
            return null; // Not found (ERROR_NOT_FOUND = 1168)

        try
        {
            var cred = Marshal.PtrToStructure<CREDENTIALW>(credPtr);
            if (cred.CredentialBlob == IntPtr.Zero || cred.CredentialBlobSize <= 0)
                return null;

            byte[] blob = new byte[cred.CredentialBlobSize];
            Marshal.Copy(cred.CredentialBlob, blob, 0, cred.CredentialBlobSize);

            // Go keyring may store raw UTF-8 or base64 with a go-keyring-base64: prefix
            string rawString = Encoding.UTF8.GetString(blob);
            string cleanJson = UnpackGoKeyringString(rawString);

            return new CredentialResult(cleanJson, cred.LastWritten.ToDateTime());
        }
        finally
        {
            CredFree(credPtr);
        }
    }

    private static string UnpackGoKeyringString(string input)
    {
        const string prefix = "go-keyring-base64:";
        if (input.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            string b64 = input.Substring(prefix.Length);
            byte[] decoded = Convert.FromBase64String(b64);
            return Encoding.UTF8.GetString(decoded);
        }
        return input;
    }
}
```

### Cache Optimization with `LastWritten`
The `LastWritten` field inside `CREDENTIALW` provides the timestamp when Antigravity last rotated the credential. TokenHound checks this timestamp before parsing or re-evaluating tokens, avoiding unnecessary CPU overhead.

---

## 3. Native Encryption via Windows DPAPI (`Data Protection API`)

For secure local storage of credentials or tokens cached directly by TokenHound (e.g., user-provided API keys or cached tokens):

### DPAPI Principle
DPAPI (`CryptProtectData` and `CryptUnprotectData`) uses keys derived from the current user's Windows login credentials managed by the Local Security Authority Subsystem Service (LSASS). No symmetric key is ever stored in application source code or configuration files.

### C# (.NET) Implementation

Exposed natively via `System.Security.Cryptography.ProtectedData`:

```csharp
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

public static class DpapiStorage
{
    // Application-specific entropy to isolate against other apps running under the same user profile
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("TokenHound.Win11.Entropy.2026");

    public static void SaveEncrypted(string filePath, string plaintext)
    {
        byte[] plainBytes = Encoding.UTF8.GetBytes(plaintext);
        byte[] encryptedBytes = ProtectedData.Protect(
            plainBytes, 
            Entropy, 
            DataProtectionScope.CurrentUser
        );

        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        File.WriteAllBytes(filePath, encryptedBytes);
    }

    public static string? LoadDecrypted(string filePath)
    {
        if (!File.Exists(filePath)) 
            return null;

        try
        {
            byte[] encryptedBytes = File.ReadAllBytes(filePath);
            byte[] plainBytes = ProtectedData.Unprotect(
                encryptedBytes, 
                Entropy, 
                DataProtectionScope.CurrentUser
            );
            return Encoding.UTF8.GetString(plainBytes);
        }
        catch (CryptographicException)
        {
            // Occurs if the file belongs to another user account or was corrupted
            return null;
        }
    }
}
```

---

## 4. Concurrent SQLite Reading in Write-Ahead Log (WAL) Mode

Both **Cursor** (`state.vscdb`) and **OpenAI Codex** (`state_5.sqlite`, `codex-dev.db`) use SQLite in **WAL** mode.

### Windows File-Locking Constraints
Windows file locking is stricter than POSIX/macOS:
- When Cursor is open, it holds file handles for `state.vscdb`, `state.vscdb-wal`, and `state.vscdb-shm`.
- Opening the database with `immutable=1` causes SQLite to bypass the `-wal` sidecar. Recent writes (such as newly refreshed session tokens or active composer tasks) become invisible, and the agent falsely appears idle.
- Opening with strict `mode=ro` requires SQLite to open the shared memory file (`-shm`). However, **the `-shm` sidecar only exists while Cursor is open**. Once Cursor terminates and executes a WAL checkpoint, `-shm` is deleted. Opening in `mode=ro` at that exact instant throws: `SQLite Error: unable to open database file`.

### Safe Opening Algorithm in C# (`Microsoft.Data.Sqlite`)

The recommended pattern attempts read-only WAL mode first (`Mode=ReadOnly`), and gracefully falls back to `immutable=1` when `-shm` is missing:

```csharp
using System;
using System.IO;
using Microsoft.Data.Sqlite;

public static class SafeSqliteReader
{
    public static SqliteConnection OpenReadOnly(string dbPath)
    {
        if (!File.Exists(dbPath))
            throw new FileNotFoundException("SQLite database not found.", dbPath);

        // Attempt 1: Mode=ReadOnly (sees active WAL changes if editor is running)
        try
        {
            var csBuilder = new SqliteConnectionStringBuilder
            {
                DataSource = dbPath,
                Mode = SqliteOpenMode.ReadOnly,
                Pooling = false // Do not hold connections in pool to release file locks promptly
            };

            var connection = new SqliteConnection(csBuilder.ConnectionString);
            connection.Open();

            // Quick sanity check validating locks and -shm resolution
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT 1;";
            cmd.ExecuteScalar();

            return connection;
        }
        catch (SqliteException)
        {
            // Attempt 2: Fallback to immutable=1 (when the app closed and removed -shm)
            var csBuilderFallback = new SqliteConnectionStringBuilder
            {
                DataSource = $"file:{dbPath}?immutable=1",
                Mode = SqliteOpenMode.ReadOnly,
                Pooling = false
            };

            var fallbackConn = new SqliteConnection(csBuilderFallback.ConnectionString);
            fallbackConn.Open();
            return fallbackConn;
        }
    }
}
```

---

## 5. File Permissions and Access Control Lists (NTFS ACL)

For plain JSON files used by Claude Code (`.credentials.json`) and Codex (`auth.json`), security depends strictly on NTFS user permissions within the user's profile (`C:\Users\<User>`).

### Non-Blocking Shared Read Pattern in C#
External tools may write to or rotate these files concurrently at the moment TokenHound reads them. Never request exclusive locks (`FileShare.None`).

```csharp
public static string? ReadWithSharedAccess(string path)
{
    if (!File.Exists(path)) 
        return null;

    try
    {
        using var stream = new FileStream(
            path, 
            FileMode.Open, 
            FileAccess.Read, 
            FileShare.ReadWrite | FileShare.Delete
        );
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }
    catch (IOException)
    {
        // The file was being written or rotated at this exact moment
        return null;
    }
}
```

---

## 6. Process and Port Discovery on Windows 11

To communicate with the Antigravity Language Server (which runs on an ephemeral port via `--https_server_port 0`), TokenHound:
1. Enumerates running processes to find `language_server.exe`.
2. Extracts `--csrf_token <token>` from the process command line using WMI (`Win32_Process`).
3. Queries Windows native TCP tables (`GetExtendedTcpTable` via `iphlpapi.dll`) to resolve which local TCP listening port belongs to the process PID.
