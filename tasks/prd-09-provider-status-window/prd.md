# PRD — Provider Status Window

## Problem and context

TokenHound shows usage through the HUD notch. Each ring carries one provider or Claude profile, and details appear only in a hover tooltip for one ring at a time. A user with several providers, or several Claude profiles (`claude`, `claude-<slug>`, delivered by PRD 07), cannot compare every account's quota windows side by side. Answering "which account still has room, and when does the exhausted one come back?" means hovering each ring in turn.

The user asked for a dedicated window listing the status of every configured provider. It follows the layout of a reference screenshot: a dark panel, grouped by provider with an account count, and one row per account. Each row has a column per quota window, showing a label, a percentage, a progress bar, and the reset timing. Exhausted accounts are dimmed and show when they come back. Unlike the reference, the columns are not fixed. Each provider shows the quota windows it already exposes today (DEC-01, workflow.md).

## Outcomes and metrics

| ID | Expected outcome | Metric or evidence |
| --- | --- | --- |
| OBJ-01 | Every configured provider's status is visible at once | With N enabled providers/profiles, the window shows N account rows without hovering the HUD |
| OBJ-02 | Quota comparison across accounts of the same provider | Claude profiles appear under one `Claude` group with a count equal to the number of enabled Claude profiles |
| OBJ-03 | Exhausted accounts are recognizable with their comeback time | An account with any quota window at 100% used shows a "back in" countdown and is visually dimmed |
| OBJ-04 | Status stays current without reopening | A new snapshot or an enablement change is reflected in the open window on the next UI dispatch |

## Stories and journeys

| ID | User | Need | Benefit | Flow or edge |
| --- | --- | --- | --- | --- |
| US-01 | Developer with several Claude profiles | See every profile's quota windows side by side | Pick the account with room left without hovering each ring | Opens the window from the tray menu; the `Claude` group lists `Claude Code`, `Claude Code (work)`, … with their 5-hour, 7-day, and model-scoped windows |
| US-02 | Developer using several providers | See every provider's status in one place | Know which tool is usable now | Codex, Copilot, Cursor, Cline, and others each show their own quota columns, which differ per provider |
| US-03 | Developer whose account hit a limit | Know when it becomes usable again | Plan work around the reset | An exhausted row is dimmed and shows "back in 2d 23h" |
| US-04 | Developer with a provider in an error state | Understand why a provider shows no quota | Fix the cause (e.g. log in) | A `NeedsAuth` provider row shows the existing guidance text, e.g. "Execute 'claude login' in terminal" |
| US-05 | Developer working with the HUD | Open the window from where they already are | No detour through the tray | Right-click on the notch, choose the status window entry |

## Functional requirements

| ID | Requirement | Acceptance criterion |
| --- | --- | --- |
| FR-01 | Open from the tray menu | The tray context menu has an entry that opens the status window; invoking it with the window already open brings the existing window to the front instead of opening a second one |
| FR-02 | Open from the HUD menu | The notch right-click actions menu has an entry that opens the same single status window |
| FR-03 | Configured providers only | The window lists every registered provider/profile that is enabled in Settings, and no disabled one; a provider enabled or disabled while the window is open appears or disappears without reopening |
| FR-04 | Grouping by provider | Rows are grouped by provider family; each group header shows the family name and the number of account rows in it (e.g. `Claude 5`); every Claude profile (`claude`, `claude-<slug>`) belongs to the `Claude` group; other providers form a group of one account |
| FR-05 | Account identity | Each row's left column shows the account's existing display name (as in the HUD, e.g. `Claude Code (work)`) and, when present, the provider status message the HUD already resolves |
| FR-06 | Dynamic quota columns | Each row shows one column per usage row the HUD produces for that provider's snapshot today, in the same order and with the same labels; the column count varies per provider and per snapshot |
| FR-07 | Column content | Each column shows its label, the used percentage (same semantics and rounding as the HUD's "% Used"), a progress bar filled to the used fraction, and the reset line; a column with no used fraction shows the provider's existing quantity text (e.g. `~12 requests`, `Unmeasured`) and no bar |
| FR-08 | Reset line | When a reset time exists, the line shows a relative countdown and the absolute local date and time (e.g. `in 14 hours · 09/27, 13:00`); when no reset time exists, it shows `No reset pending` |
| FR-09 | Bar colour | The bar colour follows the ring colour states of `docs/design/2026-08-28-usage-notch-design.md` (green below 50%, yellow 50–79%, orange from 80%) |
| FR-10 | Exhausted account | When any quota window of a row is at 100% used, the row shows `back in <countdown>` to that window's earliest reset in an alert colour and the row is dimmed; the exhausted window's reset line uses the alert colour |
| FR-11 | Live update | While open, the window updates a row when a new snapshot for that provider arrives and refreshes countdowns at least once per minute, without user action |
| FR-12 | Empty and pending states | With no enabled provider, the window shows an explanatory empty state; an enabled provider without a snapshot yet shows its row with a waiting indication instead of columns |

## Non-functional requirements

| ID | Attribute | Limit or criterion |
| --- | --- | --- |
| NFR-01 | Architecture | No change to `TokenHound.Core` models; no new NuGet dependency; the window consumes the snapshots and enablement events that already exist |
| NFR-02 | Honesty of data | No percentage, bar, or limit is shown where the snapshot has no used fraction (AGENTS.md: never invent limits or denominators) |
| NFR-03 | Consistency | Labels, percentages, and status messages match the HUD tooltip for the same snapshot |
| NFR-04 | Visual style | Dark panel following the reference: monospace account name, grey secondary text, thin rounded progress bars, row separators, dimmed exhausted rows; readable at 100% and 150% display scaling |
| NFR-05 | Lifecycle | Single instance; closing the window releases its subscriptions; application shutdown closes it with the other dialogs |
| NFR-06 | Scale | Content scrolls vertically when the rows exceed the window height; at least 10 account rows remain usable |
| NFR-07 | Code rules | AGENTS.md C# rules (sealed classes, ≤ 300 lines per file, ≤ 30 lines per method); `App.xaml.cs` must not grow further past its current 448 lines |

## User experience

- Entry: tray menu entry and notch right-click entry, both labelled for the status window (final wording in the TechSpec).
- Layout: vertically scrolling list of groups; group header with name and grey count; rows with the account column on the left and quota columns to the right, wrapping to a new line when the window is too narrow.
- Feedback: status messages (`NeedsAuth`, `RateLimited`, `Stale`, …) appear under the account name in grey, or in the alert colour when the account is blocked; exhausted rows are dimmed.
- Behaviour: a normal modeless window that can be moved, resized, and activated like Settings. The HUD's no-activation invariants do not apply to it.
- Accessibility: text contrast readable on the dark background; percentages are text, not colour alone.

## Constraints and dependencies

- Depends on the multi-profile Claude registration (PRD 07) and the Claude model-scoped windows (PRD 08) already on `main`.
- Reuses the usage rows the HUD builds from `Snapshot` (`ProviderUsageRowFactory`) as the source of columns.
- Follows AGENTS.md, `docs/design/2026-08-28-usage-notch-design.md` ring colours, and the MTP test commands.

## Out of scope

- Plan tier (e.g. `Max`), account email, or masked identifiers in the account column (DEC-02).
- The reference's per-row arrow action or any action that switches, logs in, or modifies an account.
- Showing the remaining percentage instead of used (DEC-02).
- A global hotkey to open the window.
- Persisting window position, size, or open state across restarts.
- History, charts, or cost tracking.

## Assumptions and sources

- Assumption: "configured provider" means a provider registered at startup and enabled in Settings. Source: enablement gating in `UsageStore` (`ProviderEnablementChanged`). Impact if wrong: disabled providers would also need rows.
- Assumption: "exhausted" means a quota window with used fraction ≥ 1.0. Impact if wrong: the "back in" and dimming rules would need another trigger (e.g. an active block).
- Assumption: the absolute reset time uses the user's local time zone and short month/day format of the current culture, as in the reference `09/27, 13:00`.
- Source: reference screenshot supplied by the user at HIL 0 (dark Claude panel with five accounts).
- Source: `docs/design/2026-08-28-usage-notch-design.md` §Ring colour states.
- Source: decisions DEC-01 and DEC-02 in `workflow.md`.

## PRD acceptance gate

- [x] Every requirement has an ID and an observable criterion.
- [x] Metrics, boundaries, and out-of-scope items are explicit.
- [x] Internal rules came from the user or an identified project source.
- [x] Implementation details remain in the TechSpec.
