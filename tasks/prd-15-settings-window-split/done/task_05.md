# Stable execution context

Load in this exact order:

1. `tasks/prd-15-settings-window-split/prd.md`
2. `tasks/prd-15-settings-window-split/techspec.md`
3. This file

Use the current PRD and TechSpec versions already loaded; recover only missing or changed sources. This order does not guarantee a host cache hit.

---

# T05 — Extract Updates and Providers content

## Outcome

The Updates and Providers `TabItem`s host `UpdatesSettingsPanel` and `ProvidersSettingsPanel`, which hold the tab content verbatim. The Updates tab stays visible with its fixed text when its view model is null, provider rows render the same, and `SettingsWindow.xaml` is at most 500 lines.

## Dependencies and boundaries

- Depends on: T04 (file collision on `SettingsWindow.xaml`)
- Unblocks: T06
- In scope: CMP-07, CMP-08; the `TabItem`s (`ProvidersTab` name, header, style) stay in the window (DEC-02).
- Out of scope: Cadence tab content, footer, code-behind, view models.

## Traceability

| Source | Section | Obligation covered |
| --- | --- | --- |
| R-03, R-07, R-08 | `prd.md#behaviors-to-preserve` | Updates bindings and names; provider rows and start tab; no null collapse |
| DEC-01, DEC-02, DEC-03 | `techspec.md#technical-decisions` | Verbatim controls; tabs stay; both panels merge `SettingsResources.xaml` |
| CMP-07, CMP-08 | `techspec.md#affected-components` | Two new controls |
| QA-01, TC-02, TC-04 | `techspec.md#quality-profile`, `#safety-net` | ≤ 500 lines, names, build |

## Context to recover on demand

- Applicable skills and rules: `dotnet-efficient-validation`; `CLAUDE.md` C# rules; user control shape as in T04.
- Existing code at the base: Providers tab `SettingsWindow.xaml:571-603` (uses `ProviderRowTemplate`); Updates tab `:815-923` (uses `SettingsTextBoxStyle`). Line numbers shift after T03 and T04, so locate them by the `<!-- Tab 2 -->` and `<!-- Tab 4 -->` comments.
- `SettingsWindow.xaml.cs:129-130` selects `ProvidersTab` when General is unavailable; the name stays on the `TabItem`.

## Work

- [x] T05.1 Create `ProvidersSettingsPanel` and `UpdatesSettingsPanel` under `UI/Controls/Settings/` with the code-behind shape from T04; both merge `SettingsResources.xaml`.
- [x] T05.2 Move each tab's content (the `TabItem` child) verbatim and replace it with the control element.
- [x] T05.3 Check `wc -l` on `SettingsWindow.xaml` and every file under `UI/Controls/Settings/`.

## Acceptance criteria

- `SettingsWindow.xaml` ≤ 500 lines (QA-01); new controls ≤ 250 lines (QA-02).
- The automation-name multiset and key set equal the baselines (TC-02, TC-03).
- Providers lists rows with glyphs, badges, and toggles; Updates shows its fields and fixed text.

## Verification

- Unit: test command from T04 → 1043 passed (TC-01).
- Integration: `rtk dotnet build src/TokenHound.App/TokenHound.App.csproj --no-restore` with 0 warnings (TC-04).
- E2E: omitted by .NET desktop policy.
- Manual: Windows MCP launch of the dev exe, open Settings (L-01), and `Screenshot` the Providers and Updates tabs.
- Commands: as above; diffs from `baseline/README.md`; `wc -l`.
- Environment dependency: stop the installed TokenHound and the dev HUD before building; restart the installed app after.
- Expected evidence: line counts, empty diffs, clean build, test count, screenshots described in the handoff.

## Affected files

- Modify: `src/TokenHound.App/UI/Windows/SettingsWindow.xaml`
- Create: `src/TokenHound.App/UI/Controls/Settings/ProvidersSettingsPanel.xaml(.cs)`, `UpdatesSettingsPanel.xaml(.cs)`

## Observability and recovery

- Operational signal: an empty Providers list or blank Updates fields at runtime.
- Recovery: inline the two panels again from `90d6748` and delete the controls.

## Handoff

- Produced result: `ProvidersSettingsPanel` (40 lines) and `UpdatesSettingsPanel` (117) under `UI/Controls/Settings/`, each holding its tab's `ScrollViewer` verbatim (base `SettingsWindow.xaml:576-601` and `:819-921`) and merging `../../Styles/SettingsResources.xaml` (`ProviderRowTemplate`, `SettingsTextBoxStyle`). The `TabItem`s keep their names, headers, styles, and automation names in the window (DEC-02). `ProviderItems` (`x:Name` on the `ItemsControl`) moved into the panel's namescope; no code references it. `SettingsWindow.xaml` 577 → 450 lines (QA-01).
- Changed files: `src/TokenHound.App/UI/Windows/SettingsWindow.xaml` (modified); `src/TokenHound.App/UI/Controls/Settings/ProvidersSettingsPanel.xaml(.cs)`, `UpdatesSettingsPanel.xaml(.cs)` (new).
- Checks: whitespace-insensitive diff of both panel bodies vs the base `ScrollViewer` blocks is empty; QA-01 450 ≤ 500; QA-02 largest control 123 lines, largest dictionary `SettingsResources.xaml` 207; QA-03 key set identical; QA-04 automation multiset identical; QA-05 none; QA-07 unchanged. `rtk dotnet build` app and test project → 0 errors, 0 warnings; tests → 1043 passed (TC-01). Windows MCP: the Providers tab lists rows with glyphs, colored badges, and toggles as in T03; the Updates tab shows the checkbox, the 24 h interval in the styled text box, and Apply. Note: the first diff attempt ran from PowerShell's `bash -c`, where `rg` is not on PATH, and produced an empty-input diff; rerun in the Bash tool, identical.
- Validated state: HEAD `90d6748` plus T02..T05 files; installed TokenHound still stopped.
- Open items: R-08 (Updates visible with a null view model) is satisfied by the verbatim move (no null-collapse trigger exists in the moved markup); not exercised at runtime because the view model is never null in this build.

### ADR candidates

None - direct TechSpec implementation or local decision.
