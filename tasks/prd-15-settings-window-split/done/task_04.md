# Stable execution context

Load in this exact order:

1. `tasks/prd-15-settings-window-split/prd.md`
2. `tasks/prd-15-settings-window-split/techspec.md`
3. This file

Use the current PRD and TechSpec versions already loaded; recover only missing or changed sources. This order does not guarantee a host cache hit.

---

# T04 — Extract General cards

## Outcome

The General tab hosts `StartupSettingsCard`, `HudSizeSettingsCard`, and `HudPlacementSettingsCard` in the same order and spacing. Each card holds its markup verbatim with its own `Border DataContext="{Binding X}"` and null-collapse trigger, and every binding, command, and automation name works as before.

## Dependencies and boundaries

- Depends on: T03
- Unblocks: T05 (file collision on `SettingsWindow.xaml`)
- In scope: CMP-04..CMP-06; the General `ScrollViewer`/`StackPanel` and `GeneralTab` stay in the window (DEC-02).
- Out of scope: Cadence tab, footer, `TabItem` elements, `SettingsWindow.xaml.cs` (CMP-02), view models.

## Traceability

| Source | Section | Obligation covered |
| --- | --- | --- |
| R-02, R-03, R-05 | `prd.md#behaviors-to-preserve` | Card order/spacing/collapse; bindings and names; bubbled `SelectionChanged` |
| DEC-01, DEC-03 | `techspec.md#technical-decisions` | Verbatim user controls; only the placement card merges `SettingsResources.xaml` |
| CMP-04..CMP-06 | `techspec.md#affected-components` | Three new controls |
| TC-02, TC-04 | `techspec.md#safety-net` | Automation names, build |

## Context to recover on demand

- Applicable skills and rules: `dotnet-efficient-validation`; `CLAUDE.md` C# rules; user control shape under CMP table note in `techspec.md#affected-components`.
- Existing code: `UI/Windows/SettingsWindow.xaml:284-566` (three cards); `UI/Controls/TooltipCard.xaml(.cs)` as a local user control example.
- Window-local keys used at the base: only the placement card uses one (`LabelItemTemplate` ×2). Startup and HUD size use none, so they do not merge `SettingsResources.xaml`.
- Snapshot: M-01 (named elements in code-behind), L-01.

## Work

- [x] T04.1 Create the three `UserControl`s under `UI/Controls/Settings/` with code-behind: namespace `TokenHound.App.UI.Controls.Settings`, `public sealed partial class`, XML summary, `InitializeComponent()`, and `d:DataContext` of `SettingsViewModel`.
- [x] T04.2 Move each card's markup verbatim; keep margins (12 px between cards, 4 px after the last) on the element that carries them today. If a margin sits on the card root, keep it there.
- [x] T04.3 `HudPlacementSettingsCard` merges `SettingsResources.xaml` in `UserControl.Resources`.
- [x] T04.4 Replace the cards in the window with the three control elements and add the `xmlns` for the new namespace.

## Acceptance criteria

- The automation-name multiset over `SettingsWindow.xaml` + `UI/Controls/Settings/*.xaml` equals `baseline/automation-names.txt` (TC-02).
- Each new control is at most 250 lines (QA-02); no `DynamicResource`.
- Each card shows values, applies a change, and collapses when its view model is null (as at the base). Changing a placement combo box leaves the footer status visible (R-05).

## Verification

- Unit: `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-restore`, then `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1` → 1043 passed (TC-01).
- Integration: `rtk dotnet build src/TokenHound.App/TokenHound.App.csproj --no-restore` with 0 warnings (TC-04).
- E2E: omitted by .NET desktop policy.
- Manual: Windows MCP launch of the dev exe, open Settings (L-01), `Screenshot` General scrolled through all three cards, and change one placement combo box.
- Commands: as above; automation-name diff from `baseline/README.md`.
- Environment dependency: stop the installed TokenHound and the dev HUD before building; restart the installed app after.
- Expected evidence: empty name diff, clean build, test count, General screenshots described in the handoff.

## Affected files

- Modify: `src/TokenHound.App/UI/Windows/SettingsWindow.xaml`
- Create: `src/TokenHound.App/UI/Controls/Settings/StartupSettingsCard.xaml(.cs)`, `HudSizeSettingsCard.xaml(.cs)`, `HudPlacementSettingsCard.xaml(.cs)`

## Observability and recovery

- Operational signal: binding errors are silent; a field showing empty or default text is the signal.
- Recovery: inline the three cards again from `90d6748` and delete the controls.

## Handoff

- Produced result: `StartupSettingsCard` (85 lines), `HudSizeSettingsCard` (123), and `HudPlacementSettingsCard` (109) under `UI/Controls/Settings/`, each holding its card `Border` verbatim (own `DataContext` binding, null-collapse style, and the 12/12/4 px bottom margins on the root `Border`), dedented to the control root. Only `HudPlacementSettingsCard` merges `../../Styles/SettingsResources.xaml` (for `LabelItemTemplate`). Code-behind: file-scoped namespace `TokenHound.App.UI.Controls.Settings`, `public sealed partial class`, XML summaries, constructor calling `InitializeComponent()`; `d:DataContext` is `SettingsViewModel`. The window declares `xmlns:settings` and its General `StackPanel` hosts the three elements (window 856 → 577 lines). The cards had no `x:Name`, `ElementName`, `RelativeSource`, or event handlers, so no namescope or code-behind impact; `SelectionChanged` still bubbles to the window.
- Changed files: `src/TokenHound.App/UI/Windows/SettingsWindow.xaml` (modified); `src/TokenHound.App/UI/Controls/Settings/StartupSettingsCard.xaml(.cs)`, `HudSizeSettingsCard.xaml(.cs)`, `HudPlacementSettingsCard.xaml(.cs)` (new). Extraction done by a line-range script reused in T05.
- Checks: whitespace-insensitive diff of the three controls' bodies vs base `SettingsWindow.xaml:284-566` is empty; QA-03 key set identical; QA-04 automation multiset identical; QA-05 none; QA-02 max 123 lines. `rtk dotnet build` app and test project → 0 errors, 0 warnings; `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/... --no-build --no-restore -- --minimum-expected-tests 1` → 1043 passed (TC-01). Windows MCP: General shows Startup, HUD size, HUD placement with the same values and spacing as in T03; the Position combo drop-down is styled, choosing "Top right" moved the HUD immediately, and choosing "Top center" restored it (placement setting back to the original).
- Validated state: HEAD `90d6748` plus T02..T04 files; installed TokenHound still stopped.
- Open items: R-05 with a visible footer status is checked in T06 MA-4 (no status text was showing during this smoke).

### ADR candidates

None - direct TechSpec implementation or local decision.
