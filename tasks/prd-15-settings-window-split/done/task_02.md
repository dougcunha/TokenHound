# Stable execution context

Load in this exact order:

1. `tasks/prd-15-settings-window-split/prd.md`
2. `tasks/prd-15-settings-window-split/techspec.md`
3. This file

Use the current PRD and TechSpec versions already loaded; recover only missing or changed sources. This order does not guarantee a host cache hit.

---

# T02 — Split dialog resources

## Outcome

`DialogResources.xaml` only merges `DialogFoundation.xaml`, `DialogControls.xaml`, `DialogComboBox.xaml`, `DialogScrollBar.xaml`, and `HudMenus.xaml`, in that order. Every key from the baseline is still reachable at application level, and dialogs, HUD menus, and the tray menu render as before.

## Dependencies and boundaries

- Depends on: T01
- Unblocks: T06
- In scope: CMP-09..CMP-14; verbatim moves that keep the current definition order inside each part; each style part merges `DialogFoundation.xaml` (DEC-05).
- Out of scope: `App.xaml` (QA-07); unifying the four item templates (DEC-06); `SettingsWindow.xaml`; any value, trigger, or key change.

## Traceability

| Source | Section | Obligation covered |
| --- | --- | --- |
| R-09, R-10, R-11, R-12 | `prd.md#behaviors-to-preserve` | Same rendering; keys found by name |
| DEC-04, DEC-05, DEC-06 | `techspec.md#technical-decisions` | Part layout, self-contained merges, templates untouched |
| CMP-09..CMP-14 | `techspec.md#affected-components` | Aggregator and five parts |
| TC-03, TC-04 | `techspec.md#safety-net` | Key set, build |

## Context to recover on demand

- Applicable skills and rules: `dotnet-efficient-validation`, `repository-cli-efficiency`
- Existing code: `UI/Styles/DialogResources.xaml` sections (current lines): palette and brushes 4-62, focus visual 64-77 → Foundation; button 79-127, badge pill/text 129-147, toggle 149-205 → Controls; HUD context menu 207-238, `HudMenuItemStyle` 240-275, submenu 375-424, check item 426-461 → HudMenus; combo box item/combo 277-373 → ComboBox; scroll bar 463-559 → ScrollBar.
- Name lookups that must keep working: `UI/Tray/TaskbarIconAdapter.cs:58-59`, `UI/Windows/ProviderStatusWindow.xaml.cs:14-15`, `ViewModels/ProviderBadgeResolver.cs:22-36`.

## Work

- [x] T02.1 Create `DialogFoundation.xaml` with lines 4-77 verbatim (comments included).
- [x] T02.2 Create the four style parts, each starting with `ResourceDictionary.MergedDictionaries` → `DialogFoundation.xaml`; `HudMenus.xaml` keeps `HudMenuItemStyle` before `HudSubmenuItemStyle` and `HudCheckMenuItemStyle`. If a part references a key from another style part (check `BasedOn` and `StaticResource` targets, such as `DialogFocusVisualStyle` or `DialogScrollBarStyle`), merge that part too and record it in the handoff.
- [x] T02.3 Replace `DialogResources.xaml` with an aggregator that merges the five parts in DEC-04 order, using the same URI form `App.xaml` uses.
- [x] T02.4 Diff the keys against `baseline/keys.txt` using the commands in `baseline/README.md` over the new file set.

## Acceptance criteria

- The key set is identical to the baseline (TC-03); `App.xaml` is unchanged (QA-07); no `DynamicResource` (QA-05).
- Every new dictionary is at most 250 lines (QA-02).
- The dev app starts, the HUD context menu with the Position submenu and the tray menu open styled, and Provider Status opens with badges.

## Verification

- Unit: none (XAML is not loaded by tests).
- Integration: `rtk dotnet build src/TokenHound.App/TokenHound.App.csproj --no-restore` with 0 warnings and 0 errors (TC-04).
- E2E: omitted by .NET desktop policy.
- Manual: smoke via Windows MCP `App` `launch_executable` on `src/TokenHound.App/bin/Debug/net10.0-windows/TokenHound.App.exe`, then `Screenshot` the HUD menu, tray menu, and Provider Status; the full comparison waits for T06.
- Commands: build above; key diff from `baseline/README.md`; `git diff --quiet 90d6748 -- src/TokenHound.App/App.xaml`.
- Environment dependency: stop the installed TokenHound and the dev HUD before building; restart the installed app after.
- Expected evidence: empty key diff, clean build, smoke screenshots described in the handoff.

## Affected files

- Modify: `src/TokenHound.App/UI/Styles/DialogResources.xaml`
- Create: `src/TokenHound.App/UI/Styles/DialogFoundation.xaml`, `DialogControls.xaml`, `DialogComboBox.xaml`, `DialogScrollBar.xaml`, `HudMenus.xaml`

## Observability and recovery

- Operational signal: a missing key fails at startup or when a window opens (dispatcher unhandled exception log).
- Recovery: restore `DialogResources.xaml` from `90d6748` and delete the five parts.

## Handoff

- Produced result: `DialogResources.xaml` is a 12-line aggregator merging `DialogFoundation.xaml` (79 lines; base lines 4-77), `DialogControls.xaml` (136; 79-205), `DialogComboBox.xaml` (106; 277-373), `DialogScrollBar.xaml` (106; 463-559), and `HudMenus.xaml` (166; 207-275 then 375-461) in DEC-04 order, with sibling-relative `Source` like `App.xaml` uses. Each style part merges only `DialogFoundation.xaml`: a scan of `StaticResource` references found no part that needs another style part, and `HudMenuItemStyle` stays before its two `BasedOn` children. Split done by a line-range script, so no text was retyped.
- Changed files: `src/TokenHound.App/UI/Styles/DialogResources.xaml` (modified); `DialogFoundation.xaml`, `DialogControls.xaml`, `DialogComboBox.xaml`, `DialogScrollBar.xaml`, `HudMenus.xaml` (new).
- Checks: the multiset of non-blank lines in the five parts (minus merge lines and headers) equals the base dictionary body; QA-03 `x:Key` set identical to `baseline/keys.txt`; QA-02 max 166 lines; QA-05 no `DynamicResource`; QA-07 `App.xaml` unchanged; `rtk dotnet build src/TokenHound.App/TokenHound.App.csproj --no-restore` → 0 errors, 0 warnings. Smoke with Windows MCP on the dev exe: HUD capsule renders; HUD context menu, Position submenu with check mark and hover render with the dark style; Provider Status opens with the dark surface and text (keys found by name). Tray menu: `TaskbarIconAdapter.cs:58-59` uses `Application.Current.TryFindResource`, the same application chain through which `NotchWindow.xaml:38-90` resolved both styles in the styled HUD menu above; the tray menu itself was not opened (the TokenHound tray icon is in the hidden area of an auto-hidden, centered taskbar), so its visual check stays in T06 MA-2.
- Validated state: HEAD `90d6748` plus T02 files; installed TokenHound stopped for the checks and kept stopped while the next tasks run in this session (restart at the session end or after T06).
- Open items: tray menu visual → T06 MA-2. Unit tests not run (no test loads XAML; TC-01 runs in T04..T06).

### ADR candidates

None - direct TechSpec implementation or local decision.
