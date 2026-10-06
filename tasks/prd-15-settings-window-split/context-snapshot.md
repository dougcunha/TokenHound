# Context snapshot — prd-15-settings-window-split

> Hints for the next session, not an authority: artifacts, manifests, handoffs, reports, and code win on conflict. Protocol: `.agents/skills/sdd-snapshot/SKILL.md`.

## Header

- status: closed
- generated: 2026-10-06
- stage: acceptance
- stage_source: codereview_01/codereview.md
- covers_through: codereview_01 (APPROVED WITH RESERVATIONS)
- authored_code: yes
- git_head: 90d6748 (uncommitted feature changes in src/TokenHound.App/UI)
- worktree: T02..T05 changes under src/TokenHound.App/UI/{Styles,Windows,Controls/Settings}; feature folder; pre-existing .agents/, .context-brake/, .opencode/, context-brake.config.json
- next_step: — (feature completed, DEC-06)
- other_eligible: —
- superseded_by: —

## Load map

| Tier | Load when |
| --- | --- |
| `now` | Session start: header, next step brief, open threads waiting on the user |
| `on-select` | Chosen unit matches a trigger: task or finding ID, affected file, or traceability ID |
| `on-edit` | About to edit or create a path matching a trigger |
| `on-run` | About to run a matching command, or it just failed |
| `on-demand` | A gist is not enough: follow its `src:` pointer, that section only |

Review sessions load only the header, next step brief, `Open threads`, and `on-run` entries.

Entry shape: `- [ID] (when: tier: trigger; trigger) gist — src: path#section; until: condition`

## Next step brief

- Why next: T01..T06 done, visual check approved (DEC-04), delegated review `codereview_01` APPROVED WITH RESERVATIONS; one Low reservation OI-01.
- Read first: `codereview_01/codereview.md#Summary`, the OI-01 row, `workflow.md#Events` (last entry).
- Known change points: OI-01 only touches `src/TokenHound.App/UI/Windows/SettingsWindow.xaml:6`.
- Applicable entries: O-03
- Watch out: correcting OI-01 needs a new delegated re-review (new reviewer, next suffix); finalizing goes straight to HIL 3.

## Decisions

- [D-01] (when: on-select: DEC-02; review) Cadence tab content and footer stay in the window: code-behind uses named elements and `ElementName=CadenceTab` — src: techspec.md#Technical decisions; until: feature closed

## Learnings

- [L-01] (when: on-run: launch; screenshot) Dev HUD opens Settings via right-click on the capsule at (-960,-1412) then "Settings" at (-912,-1283) on Display 1; tabs at y -1104 (General -1180, Providers -1077, Cadence -920, Updates -768); Settings region [-1250,-1275,-670,-535] — src: —; until: display layout changes
- [L-02] (when: on-run: python; heredoc) Long heredocs fail in the Bash tool; write scripts to the scratchpad with Write first — src: —; until: feature closed
- [L-04] (when: on-run: screenshot) Windows MCP `Screenshot` returns an image only; the app is single-instance and shares settings in `%LOCALAPPDATA%\TokenHound`, so base and head builds run sequentially — src: techspec.md#Technical decisions DEC-07; until: feature closed
- [L-05] (when: on-edit: techspec.md; tasks.md) `sed` replacements with `|` inside Markdown table cells mangle rows; edit table rows with Python by line index — src: —; until: feature closed
- [L-06] (when: on-run: keyboard; MA-3) Settings does not take the foreground under Windows MCP clicks, so `Shortcut` keys go to another window; keyboard checks need the human — src: task_06.md#Handoff; until: feature closed
- [L-07] (when: on-run: baseline diff) Run `baseline/README.md` commands in the Bash tool: PowerShell `bash -c` lacks `rg` and yields empty-input diffs — src: tasks.md#Problems and solutions; until: feature closed

## Code map

- [M-01] (when: on-edit: src/TokenHound.App/UI/Windows/SettingsWindow*) Code-behind uses CadenceTab, ProvidersTab, SettingsTabControl, ActiveIntervalTextBox, StatusMessageTextBlock, ApplyButton (`SettingsWindow.xaml.cs:48-130`); tab filter ignores bubbled SelectionChanged (`:105`) — src: prd.md#Behaviors to preserve; until: files change
- [M-02] (when: on-edit: src/TokenHound.App/UI/Styles/*) Keys looked up by name at app level: HudContextMenuStyle, HudMenuItemStyle (`TaskbarIconAdapter.cs:58-59`), SurfaceBackgroundColor, TextPrimaryColor (`ProviderStatusWindow.xaml.cs:14-15`), 14 badge brushes (`ProviderBadgeResolver.cs:22-36`) — src: prd.md#R-10, R-11; until: files change

## Open threads

- None. Feature closed at DEC-06; OI-01 accepted at DEC-05.
