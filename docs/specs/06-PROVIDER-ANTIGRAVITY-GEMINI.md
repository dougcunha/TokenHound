# Provider Technical Specification: Google Antigravity / Gemini

This specification defines the integration with the **Google Antigravity / Gemini** environment on **Windows 11**, detailing discovery of the local Codeium-based Language Server, interaction with the Google Cloud Code backend, and fallback aggregation of AI turns from local transcripts.

---

## 1. Overview and Data Fidelity

- **Provider ID**: `gemini`.
- **Display Name**: `Antigravity`.
- **Fidelity**: `.official` (when connected to Language Server or Cloud Code) or `.derived` (when running local transcript turn counting).
- **Headline Metric**: Primary Model Weekly Quota (`gemini-weekly`) or daily prompt count (`requests`).

---

## 2. Three-Tier Waterfall Architecture

```
+---------------------------------------------------------------------------------+
| Layer 1 (Preferred / Official Live): Local Language Server                      |
| - Discover 'language_server.exe' process and ephemeral TCP port via TCP table   |
| - Local HTTPS POST with CSRF token and forceRefresh: true                       |
+---------------------------------------------------------------------------------+
                                      | (Fails or Antigravity closed)
                                      v
+---------------------------------------------------------------------------------+
| Layer 2 (Official Remote): Google Cloud Code Backend                            |
| - OAuth token read from Windows Credential Manager ('gemini:antigravity')       |
| - POST https://cloudcode-pa.googleapis.com/v1internal:retrieveUserQuotaSummary  |
| - Note: Personal/free accounts return HTTP 403 #3501 (no license)               |
+---------------------------------------------------------------------------------+
                                      | (Unlicensed account or no network)
                                      v
+---------------------------------------------------------------------------------+
| Layer 3 (Local Derived Fallback): Transcript Aggregation                        |
| - Scan %USERPROFILE%\.gemini\antigravity\brain\                                 |
| - Aggregate steps where source == "MODEL" during the current local day          |
+---------------------------------------------------------------------------------+
```

---

## 3. Layer 1: Discovery & Local Language Server Communication

Antigravity hosts an embedded Codeium-derived Language Server holding authorized Google client identity to query real-time user quotas.

### A. Process Discovery and CSRF Token Extraction on Windows 11
The server launches with command-line arguments:
`language_server.exe ... --https_server_port 0 --csrf_token <UUID_TOKEN> ...`

In C# on Windows 11, the token is extracted by querying WMI:

```csharp
using System;
using System.Diagnostics;
using System.Management;
using System.Text.RegularExpressions;

public static class AntigravityProcessDiscovery
{
    public record LanguageServerEndpoint(int Pid, string CsrfToken);

    public static LanguageServerEndpoint? FindRunningServer()
    {
        var processes = Process.GetProcessesByName("language_server");
        foreach (var proc in processes)
        {
            try
            {
                using var searcher = new ManagementObjectSearcher(
                    $"SELECT CommandLine FROM Win32_Process WHERE ProcessId = {proc.Id}"
                );
                foreach (ManagementObject obj in searcher.Get())
                {
                    string? cmdLine = obj["CommandLine"]?.ToString();
                    if (cmdLine != null && cmdLine.Contains("--csrf_token"))
                    {
                        var match = Regex.Match(cmdLine, @"--csrf_token\s+([^\s]+)");
                        if (match.Success)
                        {
                            return new LanguageServerEndpoint(proc.Id, match.Groups[1].Value);
                        }
                    }
                }
            }
            catch
            {
                // Ignore permission failures on system processes
            }
        }
        return null;
    }
}
```

### B. Ephemeral TCP Port Discovery
Because the server launches with `--https_server_port 0`, the Windows kernel dynamically allocates the port. The server binds two local ports, of which one answers quota queries.

Listening TCP ports belonging to the PID are resolved via `iphlpapi.dll` (`GetExtendedTcpTable`):

```csharp
// Query local TCP ports in LISTEN state belonging to server.Pid
var candidatePorts = WindowsNetworkHelper.GetListeningTcpPortsForPid(server.Pid);
```

### C. Local RPC Call

- **Method**: `POST`
- **URL**: `https://127.0.0.1:<port>/exa.language_server_pb.LanguageServerService/RetrieveUserQuotaSummary`
- **Mandatory Headers**:
  ```http
  Content-Type: application/json
  x-codeium-csrf-token: <csrfToken>
  ```
  *(Note: The exact header is `x-codeium-csrf-token`. Other casing or names are rejected with "missing CSRF token").*
- **Payload**:
  ```json
  {"forceRefresh": true}
  ```
  *(Note: `forceRefresh: true` is strictly required. Without it, the server serves stale cached data from `QuotaSummaryCache` that would only update if the user opened Antigravity's internal panel).*
- **SSL Certificate Bypass**: The server uses a local self-signed certificate. Configure `HttpClient.ServerCertificateCustomValidationCallback` to trust connections to `127.0.0.1`.

### D. Response Structure & Fraction Inversion

```json
{
  "response": {
    "groups": [
      {
        "displayName": "Gemini 2.5 Pro",
        "buckets": [
          {
            "bucketId": "gemini-2.5-pro-weekly",
            "displayName": "Weekly Limit Remaining",
            "remainingFraction": 0.85,
            "resetTime": "2026-09-10T12:00:00.000000Z"
          }
        ]
      }
    }
  ]
}
```

> **INVERSION RULE**: The Language Server API returns what **remains** (`remainingFraction = 0.85`). TokenHound visual indicators display what has been **consumed**. C# must invert the fraction:
>
> $$\text{usedFraction} = 1.0 - \text{remainingFraction}$$
>
> Example: $1.0 - 0.85 = 0.15$ ($15\%$ Used).

---

## 4. Layer 2: Remote Call to Google Cloud Code Backend

If Antigravity is not actively running, but the user has an active licensed corporate workspace, quotas can be fetched remotely using the stored OAuth token.

### Token Retrieval on Windows 11
1. **Primary Target**: Windows Credential Manager target `"gemini:antigravity"`.
2. **Secondary Target**: File `%USERPROFILE%\.gemini\oauth_creds.json` (property `access_token`).

### Plan Validation
- **URL**: `POST https://cloudcode-pa.googleapis.com/v1internal:loadCodeAssist`
- **Headers**: `Authorization: Bearer <token>`, `Content-Type: application/json`
- **Body**: `{"metadata": {"pluginType": "GEMINI"}}` *(Note: Must use `GEMINI`, not `ANTIGRAVITY`).*

### Quota Request
- **URL**: `POST https://cloudcode-pa.googleapis.com/v1internal:retrieveUserQuotaSummary`
- **Headers**: `Authorization: Bearer <token>`, `Content-Type: application/json`
- **Body**: `{}` *(Empty JSON object. Sending extra fields causes "Unknown name" validation errors).*
- **Handling Unlicensed Accounts**:
  - Personal Google accounts receive `HTTP 403` with message `#3501: You do not have a valid license of this product`.
  - This is **not an authentication failure**. Catch the 403 and degrade smoothly to Layer 3.

---

## 5. Layer 3: Local Transcript Fallback

For personal accounts where Google does not publish percentage quotas, TokenHound computes the actual volume of prompts processed by the model during the current calendar day.

### Transcript Directories on Windows
- `%USERPROFILE%\.gemini\antigravity\brain\` or `%APPDATA%\..\.gemini\antigravity-cli\brain\`
- Subfolders: `<conversation-id>\.system_generated\logs\transcript.jsonl`

### Aggregation Rules in C#
1. Enumerate and read lines from `transcript.jsonl`.
2. Deserialize each JSON line:
   ```json
   {
     "step_index": 12,
     "source": "MODEL",
     "type": "PLANNER_RESPONSE",
     "created_at": "2026-09-06T14:25:30.123Z"
   }
   ```
3. Strictly filter lines where:
   $$\text{step.source} == \text{"MODEL"}$$
   *(Note: User prompts and system checkpoints share the file. Ignoring them prevents inflating prompt counts).*
4. Compare `created_at` (UTC) with the current local calendar date (`DateTime.UtcNow.ToLocalTime().Date`).
5. **Presentation**: Display as an integer count (`~34 requests today · no limit published`). The ring fraction remains null (`usedFraction = null`), rendering the track without a percentage arc.
6. Data fidelity is labeled `.derived` (showing the `~` prefix).

---

## 6. Live Agent Activity Monitoring on Windows 11

Because transcript steps are written continuously as the agent progresses:
1. The monitor polls `LastWriteTimeUtc` across `transcript.jsonl` files every 2 seconds.
2. If any transcript file was updated within the last **45 seconds** (`staleAfter = 45`), the agent is marked as **Busy**.
3. A generous 45-second window accounts for the extended inference time complex reasoning models spend thinking before executing subsequent tool calls.
