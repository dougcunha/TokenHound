# Provider Technical Specification: Claude Code (Anthropic)

This specification defines the technical integration with **Claude Code** on **Windows 11**, covering authentication, querying the official limits endpoint, resolving reset window rollover traps, and monitoring live agent activity.

---

## 1. Overview and Data Fidelity

- **Provider ID**: `claude` (for default profile) or `claude-<slug>` (for isolated profiles).
- **Display Name**: `Claude` or `Claude (<slug>)`.
- **Fidelity**: `.official` (data sourced directly from Anthropic's official usage endpoint).
- **Headline Metric**: 5-Hour Session Window (`session`).

---

## 2. Credential Location & Extraction on Windows 11

On Windows 11, Claude Code stores its OAuth credentials in JSON files protected by user-level NTFS ACL permissions.

### Profiles & Directories

1. **Default Profile**:
   - Path: `%USERPROFILE%\.claude\.credentials.json`
2. **Alternative / Isolated Profiles** (e.g. workspace configs created via `CLAUDE_CONFIG_DIR=~/.claude-work`):
   - Directory Convention: `%USERPROFILE%\.claude-<slug>`
   - Credentials File: `%USERPROFILE%\.claude-<slug>\.credentials.json`

### Profile Discovery Algorithm in C#

```csharp
public static List<ClaudeProfile> DiscoverProfiles()
{
    var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    var defaultDir = Path.Combine(home, ".claude");
    var profiles = new List<ClaudeProfile> { new ClaudeProfile("claude", "Claude", defaultDir, null) };

    var candidateDirs = Directory.GetDirectories(home, ".claude-*");
    foreach (var dir in candidateDirs.OrderBy(d => d))
    {
        var dirName = Path.GetFileName(dir);
        var slug = dirName.Substring(".claude-".Length);
        if (string.IsNullOrWhiteSpace(slug)) 
            continue;

        // Validate that the directory is actively used (ignores abandoned empty folders)
        if (IsActiveProfileDirectory(dir))
        {
            profiles.Add(new ClaudeProfile($"claude-{slug}", $"Claude ({slug})", dir, slug));
        }
    }

    return profiles;
}

private static bool IsActiveProfileDirectory(string dir)
{
    string[] markers = ["sessions", "projects", "settings.json", "history.jsonl", ".credentials.json"];
    return markers.Any(m => File.Exists(Path.Combine(dir, m)) || Directory.Exists(Path.Combine(dir, m)));
}
```

### Structure of `.credentials.json`

```json
{
  "claudeAiOauth": {
    "accessToken": "sk-ant-oat01-xxxxxxxxxxxxxxxxxxxx",
    "expiresAt": 1757121000000,
    "refreshToken": "sk-ant-ort01-xxxxxxxxxxxxxxxxxxxx",
    "refreshTokenExpiresAt": 1757725800000,
    "scopes": ["user:read", "usage:read"],
    "rateLimitTier": "default",
    "subscriptionType": "pro"
  }
}
```

### Token Handling Rules
- `expiresAt`: Unix epoch timestamp in **milliseconds** UTC.
- **Validity Check**: `DateTimeOffset.FromUnixTimeMilliseconds(expiresAt) <= DateTimeOffset.UtcNow`.
- **No-Refresh Invariant**: The monitoring client **must never** attempt to use `refreshToken` to rotate or issue new tokens. Token issuance belongs exclusively to Claude Code. If the token is expired, report `CredentialExpired` (retaining the previous reading in `Stale` state) until the developer runs Claude Code in their terminal.

---

## 3. Telemetry Endpoint & Query Protocol

### HTTP Request

- **Method**: `GET`
- **URL**: `https://api.anthropic.com/api/oauth/usage`
- **Headers**:
  ```http
  Authorization: Bearer <accessToken>
  anthropic-beta: oauth-2025-04-20
  Accept: application/json
  ```
- **Recommended Timeout**: 15 seconds.

### JSON Response Structure

```json
{
  "limits": [
    {
      "kind": "session",
      "percent": 34.5,
      "resets_at": "2026-09-06T15:00:00.000Z"
    },
    {
      "kind": "weekly_all",
      "percent": 12.0,
      "resets_at": "2026-09-12T00:00:00.000Z"
    },
    {
      "kind": "weekly_opus",
      "percent": 5.0,
      "resets_at": "2026-09-12T00:00:00.000Z"
    }
  ],
  "five_hour": {
    "utilization": 34.5,
    "resets_at": "2026-09-06T15:00:00.000Z"
  },
  "seven_day": {
    "utilization": 12.0,
    "resets_at": "2026-09-12T00:00:00.000Z"
  }
}
```

### 5-Hour Window Rollover Trap

> **CRITICAL BEHAVIOR**: Anthropic's API drops the `"session"` entry from the `limits` array at the exact instant the window expires (`resets_at <= now`). If the parser relies solely on `limits`, the moment the session reaches 0%, the session window vanishes, and the weekly window (`weekly_all`) gets erroneously promoted to the headline metric, silently changing its meaning.
>
> **MANDATORY FIX**: The parser must process `limits` and **defensively merge the top-level `five_hour` and `seven_day` objects** if missing from the array:

```csharp
public static List<LimitWindow> ParseLimitWindows(UsageResponse response)
{
    var windows = new List<LimitWindow>();

    if (response.Limits != null)
    {
        foreach (var l in response.Limits)
        {
            if (l.ResetsAt.HasValue)
            {
                windows.Add(new LimitWindow(
                    Id: l.Kind,
                    Label: FormatKindLabel(l.Kind),
                    UsedFraction: l.Percent / 100.0,
                    ResetsAt: l.ResetsAt.Value
                ));
            }
        }
    }

    // Defensive merge preserving window existence across rollover resets
    void MergeIfMissing(WindowDetail? detail, string id, string label)
    {
        if (detail?.ResetsAt != null && !windows.Any(w => w.Id == id))
        {
            windows.Add(new LimitWindow(
                Id: id,
                Label: label,
                UsedFraction: detail.Utilization / 100.0,
                ResetsAt: detail.ResetsAt.Value
            ));
        }
    }

    MergeIfMissing(response.FiveHour, "session", "Current session");
    MergeIfMissing(response.SevenDay, "weekly_all", "All models");

    return windows.OrderBy(GetDisplayRank).ToList();
}
```

---

## 4. Session Monitoring and Live Agent Activity

Claude Code writes a JSON descriptor for each active session in the sessions directory.

### Location
`%USERPROFILE%\.claude\sessions\<pid>.json` (or `%USERPROFILE%\.claude-<slug>\sessions\<pid>.json`).

### Session File Schema (`<pid>.json`)

```json
{
  "pid": 14220,
  "cwd": "D:\\MyProjects\\Ideas\\TokenHound",
  "name": "TokenHound",
  "status": "busy",
  "tempo": "active",
  "waitingFor": null,
  "entrypoint": "claude-vscode",
  "startedAt": 1788640000000,
  "statusUpdatedAt": 1788641200000
}
```

### State Mapping
- `tempo == "blocked"` or `status == "waiting"` $\rightarrow$ **Waiting** (Awaiting user feedback or approval - Amber pulsing ring).
- `tempo == "active"` or `status == "busy"` $\rightarrow$ **Busy** (Working / Generating code - Rotating white arc).
- Any other value $\rightarrow$ **Idle**.

### Ghost Process Detection on Windows 11 (Liveness Guard)
If the terminal or Claude Code process is terminated abruptly (e.g., `taskkill` or crash), the JSON file remains on disk reporting `"busy"`. C# validates process liveness via `System.Diagnostics.Process`:

```csharp
public static bool IsProcessAlive(int pid, DateTime? startedAtUtc)
{
    try
    {
        var process = Process.GetProcessById(pid);
        if (process.HasExited) 
            return false;

        // Guard against Windows PID recycling:
        if (startedAtUtc.HasValue)
        {
            var actualStart = process.StartTime.ToUniversalTime();
            var diff = Math.Abs((actualStart - startedAtUtc.Value).TotalMinutes);
            return diff < 5.0; // 5-minute tolerance between process creation and JSON write
        }

        return true;
    }
    catch (ArgumentException)
    {
        // PID not found on Windows
        return false;
    }
    catch (Exception)
    {
        // Access denied (system process reusing the PID)
        return false;
    }
}
```

### FileSystemWatcher Strategy
A `FileSystemWatcher` observes the `sessions\` folder with a 120ms debounce to consolidate burst write events into a single scan, backed by a lightweight 5-second poll to sweep dead processes.
