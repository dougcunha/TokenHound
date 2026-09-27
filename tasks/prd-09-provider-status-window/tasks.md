# Implementation plan — Provider Status Window

## Stable sources

- PRD: `tasks/prd-09-provider-status-window/prd.md`
- TechSpec: `tasks/prd-09-provider-status-window/techspec.md`

> Common sources come before the task and mutable state; read order does not guarantee a cache hit.

## Dependency graph

| ID | Delivery | Depends on | Unblocks |
| --- | --- | --- | --- |
| T01 | WPF-free status projection: groups, accounts, dynamic columns, formatting, and a live view model, proven by unit tests | — | T02 |
| T02 | Status window opened from the tray and the HUD menu, single instance, bound to the T01 view model, closed on shutdown; manual acceptance | T01 | — |

## Traceability matrix

| Source ID | Source section | Obligation | Tasks | Evidence or test |
| --- | --- | --- | --- | --- |
| FR-01 | `prd.md#functional-requirements` | Tray entry opens single window | T02 | TC-10, MA-01 |
| FR-02 | `prd.md#functional-requirements` | HUD menu entry opens same window | T02 | TC-10, MA-02 |
| FR-03 | `prd.md#functional-requirements` | Enabled providers only, live enable/disable | T01, T02 | TC-01, TC-02, MA-04 |
| FR-04 | `prd.md#functional-requirements` | Group by family with count | T01, T02 | TC-04, MA-03 |
| FR-05 | `prd.md#functional-requirements` | Display name + status message | T01, T02 | TC-05, MA-03 |
| FR-06 | `prd.md#functional-requirements` | Dynamic columns = HUD rows | T01, T02 | TC-05, MA-03 |
| FR-07 | `prd.md#functional-requirements` | Label, used %, bar, reset line; quantity text without fraction | T01, T02 | TC-06, MA-03 |
| FR-08 | `prd.md#functional-requirements` | Relative + absolute reset / `No reset pending` | T01 | TC-07, TC-09 |
| FR-09 | `prd.md#functional-requirements` | Colour by ring states | T01, T02 | TC-06, MA-03 |
| FR-10 | `prd.md#functional-requirements` | Exhausted: back in + dimmed | T01, T02 | TC-08, MA-05 |
| FR-11 | `prd.md#functional-requirements` | Live update + minute refresh | T01 | TC-02, TC-03 |
| FR-12 | `prd.md#functional-requirements` | Empty and pending states | T01, T02 | TC-01, MA-03 |
| NFR-01 | `prd.md#non-functional-requirements` | No Core change, no new dependency | T01, T02 | diff scope |
| NFR-02 | `prd.md#non-functional-requirements` | No invented percentage | T01 | TC-06 |
| NFR-03 | `prd.md#non-functional-requirements` | Consistency with HUD | T01 | TC-05 |
| NFR-04 | `prd.md#non-functional-requirements` | Dark style, 100%/150% scaling | T02 | MA-06 |
| NFR-05 | `prd.md#non-functional-requirements` | Single instance, release, shutdown close | T01, T02 | TC-03, MA-01 |
| NFR-06 | `prd.md#non-functional-requirements` | Scroll, ≥ 10 rows | T02 | MA-06 |
| NFR-07 | `prd.md#non-functional-requirements` | AGENTS.md rules; `App.xaml.cs` growth | T01, T02 | QA-06, DEC-03 |
| DEC-01..DEC-09 | `techspec.md#technical-decisions` | Projection, formatting, timer, catalog, status reuse | T01 | TC-01..TC-09 |
| DEC-03, DEC-10, DEC-11 | `techspec.md#technical-decisions` | Wiring, menu keys, window style | T02 | TC-10, MA-01..MA-06 |
| TC-01..TC-09 | `techspec.md#test-approach` | Unit scenarios | T01 | `ProviderStatus*Tests`, `ProviderUsageRowFactoryTests` |
| TC-10 | `techspec.md#test-approach` | Entry point tests | T02 | `TrayMenuModelTests`, `TrayIconViewModelTests`, `HudActionsViewModelTests` |
| TC-11 | `techspec.md#test-approach` | Manual MA-01..MA-06 | T02 | Windows MCP screenshots |

## Tasks

- [T01 — Status projection and live view model](done/task_01.md): WPF-free types that turn the store's enabled providers into grouped accounts with the HUD's columns, formatted and kept live.
- [T02 — Status window and entry points](done/task_02.md): the dark status window opened from the tray and the HUD menu as a single instance, wired at the composition root and accepted manually.

## Coverage gate

- Coverage: pass — every FR, NFR, DEC, and TC maps to T01 or T02.
- Traceability: pass — IDs preserved from PRD and TechSpec.
- Dependencies: pass — T01 → T02, acyclic; no file collisions between tasks.
- Atomicity: pass — T01 is a testable slice (projection + VM); T02 is the visible slice (window + entry points).
- Executability: pass — commands from `AGENTS.md` and TechSpec §Test approach.
- Validation profile: pass — E2E omitted by .NET desktop policy; unit tests on `tests/TokenHound.Infrastructure.Tests`; manual acceptance through Windows MCP.
- Idempotency: pass — no persisted state; rerunning tests and builds is safe.

## Assumptions and open items

- Assumption: the PRD assumptions on "configured provider", "exhausted", and culture date format hold (accepted with DEC-03 in `workflow.md`).
- Open item: TechSpec DEC-03 deviates from the literal of NFR-07 by one line in `App.xaml.cs`; decision owner: user at HIL 2; affects T02.
- Required environment: MA-01..MA-06 need the desktop session with Windows MCP and at least two Claude profiles plus one other enabled provider; MA-05 may use a real exhausted account or be marked pending if none exists at acceptance time.

## State

- [x] T01 — done (`done/task_01.md`); TC-05/TC-09 gap found in `codereview_1/CR-01`, completed by `codereview_1/done/task_03.md`
- [x] T02 — done (`done/task_02.md`); MA-05 and the 150% part of MA-06 pending for HIL 3
- [x] T04 (correction) — done (`codereview_2/done/task_04.md`); blocked-account alert colour from `codereview_2/CR-01`, authorized by workflow DEC-07
- [x] T05 (correction) — done (`codereview_3/done/task_05.md`); title bar in the body colour and group header polish, requested at HIL 3 and authorized by workflow DEC-10
- [x] T06 (correction) — done (`codereview_4/done/task_06.md`); dark dialog scrollbar for the status and Settings windows from `codereview_4/CR-02`, authorized by workflow DEC-12

## Problems and solutions

- T01: Python text-mode writes on Windows produced CRLF in edited files, while committed files are LF. Fixed by normalizing to LF; later scripted writes must use binary or `newline=''`.
- T01: the TechSpec QA-05 regex `Dispatcher` also matches the `_uiDispatcher` delegate name. This is a false positive; WPF independence is proven by the `net10.0` test build.
- T02: at the default 1100 px width, Claude's third quota column wrapped to a second line (MA-03 first run). Cells were narrowed from 240/28 to 220/24; the re-run fits three columns per row.
- T02: `DialogService` would pass 300 lines with a third dialog pair. The status window lifecycle moved to `ProviderStatusDialog` (TechSpec risk mitigation); `DialogService` is at 274 lines.
- T01/T02 jev J3 (shadow): the diffs were condensed to fit the gate, which lowered gate confidence. Recorded in `jev-log.jsonl` with `note`.
- T01 (review): `codereview_1/CR-01` found that TC-05 and TC-09 lacked the Copilot and Cline scenarios, although T01 had recorded them as passing. Correction `codereview_1/done/task_03.md` added the tests (Infrastructure.Tests 821 passed); no production code changed.
- Round 2 (review): `codereview_2/CR-01` (PRD UX alert colour for blocked accounts, not carried into the TechSpec) was authorized at the reservations HIL (workflow DEC-07). Correction `codereview_2/done/task_04.md` adds `ProviderStatusAccount.IsBlocked`, following the HUD tooltip's error rule plus `ActiveBlock.IsBlocked`. Infrastructure.Tests 828 passed.
- HIL 3 (not accepted): the user asked for visual changes (title bar, group title prominence, count). Workflow DEC-10 authorized them, and correction `codereview_3/done/task_05.md` applies the native caption in the body colour (DWM, Windows 11) and an accent-bar header with a count badge. Seen on screen (MA-07); the Windows 10 fallback is not verified.
- Round 4 (review): `codereview_4/CR-02` (default light scrollbar on the dark status window) was authorized at the reservations HIL, and the user extended it to Settings (workflow DEC-12). Correction `codereview_4/done/task_06.md` adds a keyed `DialogScrollBarStyle` to `DialogResources.xaml`, and both windows opt in. XAML only; seen on screen (MA-08).
