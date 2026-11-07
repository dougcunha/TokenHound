# HUD feed (`hud.json`)

While TokenHound runs, it writes the content of the HUD to a JSON file. Other local apps read this file to show the same rings and hover cards. The first consumer is a Herdr app mod.

The feed is the second integration path. The [MCP server](MCP.md) gives raw provider metrics to agents. The feed gives the HUD projection: the same order, labels, texts, and activity state that the HUD shows.

## File

| Item | Value |
| --- | --- |
| Path | `%LOCALAPPDATA%\TokenHound\hud.json` |
| Encoding | UTF-8 JSON, indented |
| Write method | Atomic replace: the writer writes a temporary file and then replaces `hud.json`. A reader never sees a partial file. |
| Writer | `HudFeedPublisher` (`src/TokenHound.App/HudFeed/`) through `HudFeedWriter` (`src/TokenHound.Infrastructure/HudFeed/`) |

## When TokenHound writes the file

1. At start, 500 ms after TokenHound starts. If no provider has a reading yet, `providers` is empty; the next reading causes a new write.
2. 500 ms after a HUD change. A change is a new reading, a status change, an activity change (busy or idle), or a provider that the person turns on or off. A burst of changes in the 500 ms window gives one write.
3. Every `heartbeatSeconds` (30 s), also with no change. This write updates `writtenAtUtc` and the countdown texts.
4. At exit from the tray, one last write with `isRunning: false`. TokenHound keeps the last provider values in this write.

If a write fails, TokenHound logs one warning and tries again at the next trigger. The HUD continues to work.

## How a reader knows that TokenHound runs

Use these rules in this order:

1. If the file does not exist, TokenHound did not run on this account.
2. If `isRunning` is `false`, TokenHound stopped normally.
3. If `writtenAtUtc` is older than `3 × heartbeatSeconds`, TokenHound stopped without a final write (crash, kill, or sign-out). Show the data as not current.
4. Otherwise, the data is current.

Read `heartbeatSeconds` from the file. Do not use a fixed value in the reader.

## Document

```json
{
  "schemaVersion": 1,
  "writtenAtUtc": "2026-11-07T20:16:43.7473050+00:00",
  "isRunning": true,
  "appVersion": "0.1.15",
  "heartbeatSeconds": 30,
  "providers": [
    {
      "providerId": "claude",
      "displayName": "Claude Code",
      "familyName": "Claude",
      "badge": "C",
      "status": "ok",
      "statusMessage": null,
      "isBusy": true,
      "activeSessionText": "PID 18244 • Active",
      "sessionUsedFraction": 0.04,
      "sessionResetText": "Resets in 3h 53m",
      "weeklyUsedFraction": 0.31,
      "weeklyResetText": "Resets in 4d 2h",
      "rows": [
        {
          "key": "quota:0",
          "label": "Current session (5h)",
          "usedFraction": 0.04,
          "primaryText": "4% Used",
          "secondaryText": null,
          "scopeText": null,
          "resetText": "Resets in 3h 53m",
          "resetTimeUtc": "2026-11-08T00:10:00.1955060+00:00",
          "provenanceText": null,
          "errorText": null
        }
      ]
    }
  ]
}
```

TokenHound writes every field. A field without a value is `null`; TokenHound does not omit it.

### Document fields

| Field | Meaning |
| --- | --- |
| `schemaVersion` | The contract version. See [Versions](#versions). |
| `writtenAtUtc` | When TokenHound wrote the file. |
| `isRunning` | `false` only in the last write at exit. |
| `appVersion` | The TokenHound version that wrote the file. |
| `heartbeatSeconds` | The longest interval between 2 writes while TokenHound runs. |
| `providers[]` | One entry for each ring, in HUD order. |

### Provider fields

| Field | Meaning |
| --- | --- |
| `providerId` | Stable ID, such as `claude`, `claude-work`, `codex`, `copilot`, `cursor`, `gemini`, `opencode`, or `cline`. |
| `displayName` | The name in the HUD hover card. |
| `familyName` | The group of profiles, such as `Claude` for `claude` and `claude-work`. |
| `badge` | The short text badge of the ring. The HUD draws a vector mark for some providers; the feed has only the text badge. |
| `status` | `ok`, `stale`, `needsAuth`, `accessDenied`, `rateLimited`, `unsupported`, or `notRunning`. |
| `statusMessage` | The guidance text that the HUD shows for the status, or `null`. |
| `isBusy` | `true` when an agent session of the provider works now. The ring animates in this state. |
| `activeSessionText` | Session text, such as `PID 18244 • Active`, or `null` when no session exists. |
| `sessionUsedFraction` | The value of the ring (the session window), from 0 to 1, or `null`. |
| `sessionResetText` | The text under the session value. For a provider with only a request count, this is a count, such as `~120 requests`. |
| `weeklyUsedFraction` | The value of the weekly window, or `null`. |
| `weeklyResetText` | The reset text of the weekly window, or `null`. |
| `rows[]` | The rows of the hover card, in HUD order. |

### Row fields

| Field | Meaning |
| --- | --- |
| `key` | Row ID, unique in its provider: `quota:<index>`, `copilot:credits`, `cline:credits`, `cline:local`, or `cline:freelimit`. |
| `label` | Row title, such as `Current session (5h)` or `Premium interactions`. |
| `usedFraction` | Normally from 0 to 1, or `null`. Do not assume that the value is never more than 1; clamp it when you draw a bar. |
| `primaryText` | The main value text, such as `63% Used`, `~120 requests`, or `Unmeasured`. |
| `secondaryText` | Extra quantity text, such as `1,200 of 1,500 remaining`, or `null`. |
| `scopeText` | The account, organization, or model group, or `null`. |
| `resetText` | The countdown text at write time, or `null`. |
| `resetTimeUtc` | The reset time. Use it to count down between writes. |
| `provenanceText` | Source and freshness text, or `null`. |
| `errorText` | Error or unavailable text for the row, or `null`. |

## Rules for readers

- `usedFraction`, `sessionUsedFraction`, and `weeklyUsedFraction` are `null` when the provider reports no total. Do not calculate a percentage from other fields. TokenHound never invents a limit.
- The texts are in English and come from the HUD. A reader can show them as they are, or build its own text from the numeric fields.
- The texts are correct at `writtenAtUtc`. To show a live countdown, calculate it from `resetTimeUtc`.
- The file does not contain the HUD demonstration provider (`mock`). If the person has no provider with credentials, `providers` is empty.
- A provider that the person turns off in Settings is not in the file.
- Ignore fields that you do not know. Later versions can add fields.

## Versions

- An additive change (a new field, a new `status` value, a new row `key`) keeps `schemaVersion`.
- A breaking change (a removed or renamed field, or a field with a new meaning) increases `schemaVersion`.
- A reader that gets a `schemaVersion` higher than the version it knows shows a "TokenHound update needed" message, and does not read the providers.

## Privacy

The file contains usage data and operational texts only. It never contains access tokens, cookies, credential paths, or session content. Unlike the MCP responses, the file contains the texts of the HUD as they are. Thus `scopeText` can contain the organization or account name that the HUD shows, for example the owner of a Copilot billing plan. The file is in the local data folder of your Windows account. Other processes that run under your account can read it.

## Known limits

- The feed exists only on Windows, because TokenHound runs only on Windows. A reader on another machine (for example, a Herdr space on a remote host) cannot read it.
- There is no setting to turn off the feed. Add one if a person asks for it.
