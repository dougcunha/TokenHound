# Stable execution context

Load in this exact order:

1. `tasks/prd-15-settings-window-split/prd.md`
2. `tasks/prd-15-settings-window-split/techspec.md`
3. This file

Use the current PRD and TechSpec versions already loaded; recover only missing or changed sources. This order does not guarantee a host cache hit.

---

# T03 — Move Settings-only resources

## Outcome

`SettingsResources.xaml` holds `ResourceKeyConverter`, `SettingsTabControlStyle`, `SettingsTabItemStyle`, `LabelItemTemplate`, `SettingsTextBoxStyle`, and `ProviderRowTemplate` verbatim. `SettingsWindow.xaml` merges it in `Window.Resources` and keeps its implicit `ScrollBar` style, and Settings opens and looks the same.

## Dependencies and boundaries

- Depends on: T01
- Unblocks: T04
- In scope: CMP-01 (resources section only) and CMP-03.
- Out of scope: tab content, cards, footer; moving keys to `App.xaml`; `DynamicResource`.

## Traceability

| Source | Section | Obligation covered |
| --- | --- | --- |
| R-01, R-07, R-12 | `prd.md#behaviors-to-preserve` | Tabs, provider rows, implicit scroll bar |
| DEC-03 | `techspec.md#technical-decisions` | Settings dictionary merged at window level |
| CMP-01, CMP-03 | `techspec.md#affected-components` | Window resources and new dictionary |
| TC-03, TC-04 | `techspec.md#safety-net` | Key set, build |

## Context to recover on demand

- Applicable skills and rules: `dotnet-efficient-validation`
- Existing code: `UI/Windows/SettingsWindow.xaml:26-229` (`Window.Resources`; the implicit `ScrollBar` style is near line 28-29 and stays); xmlns prefixes `converters` and `viewModels` must be declared in the new dictionary.
- Snapshot: L-01 (how to open Settings in the dev HUD).

## Work

- [x] T03.1 Create `UI/Styles/SettingsResources.xaml` with the six keys in their current order and the xmlns prefixes they need.
- [x] T03.2 In `SettingsWindow.xaml`, replace them with a `ResourceDictionary.MergedDictionaries` entry for `SettingsResources.xaml`, keeping the implicit `ScrollBar` style as a window resource.
- [x] T03.3 Build, launch the dev app, and open Settings (DEC-03 risk mitigation).

## Acceptance criteria

- Settings opens without an exception and shows the four tabs with the same tab styles, provider rows, and text boxes. This is required before T04 starts.
- The key set over `UI/Styles/*.xaml` + `SettingsWindow.xaml` equals the baseline (TC-03).

## Verification

- Unit: none (XAML is not loaded by tests).
- Integration: `rtk dotnet build src/TokenHound.App/TokenHound.App.csproj --no-restore` with 0 warnings (TC-04).
- E2E: omitted by .NET desktop policy.
- Manual: Windows MCP `App` `launch_executable` on `src/TokenHound.App/bin/Debug/net10.0-windows/TokenHound.App.exe`, open Settings (L-01), and `Screenshot` each tab.
- Commands: build above; key diff from `baseline/README.md`.
- Environment dependency: stop the installed TokenHound and the dev HUD before building; restart the installed app after.
- Expected evidence: clean build, empty key diff, Settings open in a screenshot described in the handoff.

## Affected files

- Modify: `src/TokenHound.App/UI/Windows/SettingsWindow.xaml`
- Create: `src/TokenHound.App/UI/Styles/SettingsResources.xaml`

## Observability and recovery

- Operational signal: an unresolved `StaticResource` throws `XamlParseException` when Settings opens.
- Recovery: move the six keys back into `Window.Resources` and delete the new dictionary.

## Handoff

- Produced result: `UI/Styles/SettingsResources.xaml` (207 lines) holds `ResourceKeyConverter`, `SettingsTabControlStyle`, `SettingsTabItemStyle`, `LabelItemTemplate`, `SettingsTextBoxStyle`, and `ProviderRowTemplate` in base order, with the `converters` and `viewModels` prefixes; content moved by a script and dedented by 4 spaces to the dictionary root level. `SettingsWindow.xaml` `Window.Resources` now wraps a `ResourceDictionary` that merges `../Styles/SettingsResources.xaml` and keeps the implicit `ScrollBar` style (window 1051 → 856 lines). The moved keys reference only application keys (`DialogFocusVisualStyle`, badge and toggle styles, surface and text brushes), which resolve through the application fallback.
- Changed files: `src/TokenHound.App/UI/Windows/SettingsWindow.xaml` (modified); `src/TokenHound.App/UI/Styles/SettingsResources.xaml` (new).
- Checks: whitespace-insensitive diff of the moved block vs base lines 27 and 31-228 shows only a trailing blank line; QA-03 key set identical; QA-04 automation multiset identical (over `SettingsWindow.xaml` + `SettingsResources.xaml`); `rtk dotnet build src/TokenHound.App/TokenHound.App.csproj --no-restore` → 0 errors, 0 warnings. Windows MCP on the dev exe: Settings opens without an exception (DEC-03 risk retired); General shows the tab style, the Startup and HUD size cards, and the slim dialog scroll bar (R-12); Providers shows rows with glyphs, colored badges, and toggles (R-07, ResourceKeyConverter); Cadence shows styled text boxes and the footer.
- Validated state: HEAD `90d6748` plus T02 and T03 files; installed TokenHound still stopped.
- Open items: none. QA-01 (≤ 500 lines) is reached in T05.

### ADR candidates

None - direct TechSpec implementation or local decision.
