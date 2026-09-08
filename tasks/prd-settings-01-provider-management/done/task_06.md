# Stable execution context

Load in this exact order:

1. `tasks/prd-settings-01-provider-management/prd.md`
2. `tasks/prd-settings-01-provider-management/techspec.md`
3. This file

Use the current PRD and TechSpec versions already loaded; recover only missing or changed sources. This order does not guarantee a host cache hit.

---

# T06 — Give the Settings dialog a real body: provider rows, badge pills, and keyboard access

## Outcome

`SettingsWindow` stops being an empty shell. It shows a header, a rounded card listing one row per provider with glyph, name, status pill, and toggle, and a Close footer — all in the Windows 11 dark palette, fully keyboard navigable, and dismissible with Esc. `DialogService` supplies the view model and disposes it on close while keeping its single-instance modeless behavior.

## Dependencies and boundaries

- Depends on: T05
- Unblocks: T07
- In scope: window markup and sizing, badge pill and toggle styles, automation properties, and the `DialogService` view-model factory.
- Out of scope: startup wiring (T07) — until then the dialog is reachable only through the existing HUD context menu with whatever view model `DialogService` is handed. HUD geometry and a scrolling provider list are out of scope per the PRD and OPEN-02.

## Traceability

| Source | Section | Obligation covered |
| --- | --- | --- |
| FR-01, FR-02 | `prd.md#functional-requirements` | Each row shows name, glyph, and an interactive toggle |
| FR-05 | `prd.md#functional-requirements` | A styled pill badge per state with the PRD's color treatments |
| FR-08 | `prd.md#functional-requirements` | Modeless, single-instance; re-invoking activates rather than duplicating; closing does not exit the app |
| FR-09 | `prd.md#functional-requirements` | Esc and Close dismiss immediately; no Apply or Save button exists |
| OBJ-05 | `prd.md#outcomes-and-metrics` | Windows 11 Fluent dark mode, keyboard navigation, single-instance modeless lifecycle |
| NFR-01 | `prd.md#non-functional-requirements` | Dark theme brushes, semantic pill colors, clean scaling at 100/150/200% |
| NFR-02 | `prd.md#non-functional-requirements` | Tab order, Space/Enter toggle, Esc dismiss, `AutomationProperties.Name` and role on every interactive element |
| US-06 | `prd.md#stories-and-journeys` | Keyboard-only management of settings |
| CMP-12, CMP-13, CMP-14 | `techspec.md#components-and-flow` | Window markup, dialog resources, dialog service factory |
| OPEN-02 | `techspec.md#risks-and-open-items` | Height raised for five rows; scrolling deferred |

## Context to recover on demand

- Applicable skills: `dotnet-efficient-validation`, `antislop:antislop-human` (contrast and focus states), `no-workarounds`
- Existing code: `src/TokenHound.App/UI/Windows/SettingsWindow.xaml` — the shell already has the dark background, `DialogButtonStyle` Close with `IsCancel="True"`, and the `Border` placeholder to replace; `SettingsWindow.xaml.cs` already handles Esc and calls `WindowPlacement.EnableDarkMode`
- Existing code: `src/TokenHound.App/UI/Styles/DialogResources.xaml` — the palette and `DialogFocusVisualStyle` to reuse for the toggle's focus visual
- Existing code: `src/TokenHound.App/UI/Windows/AboutWindow.xaml` — the sibling dialog whose styling this must match
- Existing code: `src/TokenHound.App/UI/Windows/DialogService.cs` — `ShowSettings` currently does `new SettingsWindow()` with no DataContext; preserve the minimize-restore-activate branch and `OnSettingsWindowClosed`
- Existing code: `src/TokenHound.App/Assets/Logos/ProviderGlyphs.xaml` — the `Glyph.*` resource keys the rows bind through
- Contract or integration: `prd.md#user-experience` — exact pill background and foreground hex values per state

## Work

- [x] T06.1 Add badge pill brushes and a pill style to `DialogResources.xaml` using the PRD hex values, plus a dark-mode toggle/checkbox style with `DialogFocusVisualStyle`.
- [x] T06.2 Replace the empty `Border` in `SettingsWindow.xaml` with an `ItemsControl` over `Providers`, each row laying out glyph, name, badge pill, and right-aligned toggle.
- [x] T06.3 Add the subtitle "Manage monitored AI coding assistants and provider telemetry." under the existing header.
- [x] T06.4 Raise `Height` and `MinHeight` to fit five rows plus header and footer, keeping `ResizeMode="NoResize"` (OPEN-02).
- [x] T06.5 Set `AutomationProperties.Name` on each toggle to the provider display name and `HelpText` to the badge label; give the badge text its own automation name.
- [x] T06.6 Change `DialogService.ShowSettings` to accept a `Func<SettingsViewModel>` factory, assign the result as `DataContext`, and dispose it in `OnSettingsWindowClosed` and `CloseSettings`.
- [x] T06.7 Move initial focus off Close to the first provider row so Tab order starts at the top of the list.

## Acceptance criteria

- All rows render with glyph, name, pill, and toggle; no binding error appears in the debug output.
- Every pill uses the PRD's background and foreground pair for its state and always shows its label as text.
- Tab visits each row in order and then Close; the focus visual is visible at every stop; Space toggles the focused row; Esc and Close both dismiss.
- Re-invoking Settings while open activates the existing window instead of creating a second one; closing Settings leaves the application running.
- The view model is disposed exactly once per window close, including when `CloseAll` runs during shutdown.
- The window renders in dark mode with no light-mode flash, and nothing clips at 100%, 150%, or 200% scaling.

## Verification

- Unit: none — this task is markup and window lifecycle. The logic beneath it is already covered by TC-12 (T03) and TC-13/TC-14 (T05).
- Integration: none automatable at this layer under the desktop policy.
- E2E: omitted by .NET desktop policy. WPF UI automation of this dialog is excluded, which is precisely why the manual script below is the acceptance evidence rather than a convenience.
- Manual: MAN-01 (keyboard: Tab order, Space toggle, Esc dismiss — NFR-02, FR-09, US-06) and MAN-02 (dark mode and 100/150/200% scaling screenshots — NFR-01). Owner: Douglas Cunha. Launch via Windows MCP `App` with `mode="launch_executable"` per `AGENTS.md`; `Start-Process` renders to an isolated desktop and will not appear on screen. Capture with Windows MCP `Screenshot`, `display: [2]`.
- Commands:
  - `rtk dotnet build src/TokenHound.App/TokenHound.App.csproj --no-restore --nologo --verbosity:minimal`
  - Regression: `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1`
- Environment dependency: a real Windows 11 desktop session with Windows MCP available for MAN-01 and MAN-02. Authorization exists — the owner runs these locally. Until both are executed, NFR-01, NFR-02, and OBJ-05 acceptance stays pending.
- Expected evidence: the App project builds — it is the only project that compiles the XAML, so a bad binding path or missing resource key surfaces nowhere else. Plus three screenshots (100/150/200%) and a MAN-01 pass note.

## Affected files

- Modify: `src/TokenHound.App/UI/Windows/SettingsWindow.xaml`
- Modify: `src/TokenHound.App/UI/Windows/SettingsWindow.xaml.cs`
- Modify: `src/TokenHound.App/UI/Styles/DialogResources.xaml`
- Modify: `src/TokenHound.App/UI/Windows/DialogService.cs`

## Observability and recovery

- Operational signal: WPF binding failures appear in debug output; a missing `StaticResource` key fails the build rather than degrading silently.
- Recovery: reverting restores the empty-shell dialog; the HUD and engine are unaffected.

## Handoff

- Produced result: `SettingsWindow` now renders a real body. A header plus the subtitle "Manage monitored AI coding assistants and provider telemetry." sits above a rounded provider card whose `ItemsControl` binds `SettingsViewModel.Providers`; each row lays out the vector glyph (with the short text badge as fallback when `GlyphKey` is null), the display name, a status pill, and a right-aligned monitoring toggle. `DialogResources.xaml` gained the 14 badge brushes named by `ProviderBadgeResolver`, a pill Border/TextBlock style pair, and a dark-mode `CheckBox` template wired to `DialogFocusVisualStyle`. `DialogService.ShowSettings` now accepts a `Func<SettingsViewModel>` factory, binds the produced instance as `DataContext`, and disposes it exactly once per close.
- Changed files:
  - Modified `src/TokenHound.App/UI/Styles/DialogResources.xaml` - 14 `Badge{State}{Background,Foreground}Brush` resources with the PRD hex pairs (`Unsupported` reuses the `Stale` slate pair per OPEN-01), a toggle palette, `ProviderBadgePillStyle`, `ProviderBadgeTextStyle`, and `ProviderToggleStyle`.
  - Modified `src/TokenHound.App/UI/Windows/SettingsWindow.xaml` - provider row `DataTemplate`, card `ItemsControl`, subtitle, `Width` 440 -> 460 and `Height`/`MinHeight` 280/220 -> 440/440 with `ResizeMode="NoResize"` retained, and removal of `FocusManager.FocusedElement` pointing at Close.
  - Modified `src/TokenHound.App/UI/Windows/SettingsWindow.xaml.cs` - `Loaded` handler moving initial focus to the first generated toggle, falling back to Close when the list is empty.
  - Modified `src/TokenHound.App/UI/Windows/DialogService.cs` - optional `Func<SettingsViewModel>?` factory parameter, `_settingsViewModel` ownership, `CreateSettingsWindow`, and `DisposeSettingsViewModel`.
  - Created `src/TokenHound.App/UI/Converters/ResourceKeyConverter.cs` - resolves the resource-key strings the view models expose (`GlyphKey`, `BadgeBackgroundKey`, `BadgeForegroundKey`) through `Application.Current.TryFindResource`, keeping the view models free of WPF types (DEC-04, DEC-06).
- Checks:
  - `rtk dotnet build src/TokenHound.App/TokenHound.App.csproj --no-restore --nologo --verbosity:minimal` -> `ok dotnet build: 4 projects, 0 errors, 0 warnings`, exit 0. This is the only project that compiles the XAML, so every `StaticResource` key and binding path in the new markup is proven to resolve at compile time.
  - `rtk dotnet build TokenHound.slnx --no-restore --nologo --verbosity:minimal` -> `ok dotnet build: 6 projects, 0 errors, 0 warnings`, exit 0.
  - `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1` -> `407 tests passed`, exit 0.
  - `rtk dotnet test --project tests/TokenHound.Core.Tests/TokenHound.Core.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1` -> `45 tests passed`, exit 0.
  - Brush-key contract diffed mechanically: the 14 `x:Key` names in `DialogResources.xaml` are byte-identical to the 14 constants in `ProviderBadgeResolver.cs` (`diff` empty).
  - Contrast computed with the WCAG 2.x formula for every pill foreground over its alpha-composited background on `SurfaceCardBackgroundBrush` (#121214): OK 8.39, Needs Auth 9.57, Rate Limited 13.58, Stale 8.80, Access Denied 5.79, Checking 10.82, Disabled 3.11. Toggle chrome: unchecked border #71717A vs card 3.87, checked fill #3B82F6 vs card 5.09, check glyph #FFFFFF on the checked fill 4.06, focus ring #60A5FA vs card 7.36.
  - BOM verified absent on all five touched files (`head -c 3` returns `3c5769` / `3c5265` / `757369`, never `efbbbf`); all five were BOM-less at `HEAD` and remain so.
- Validated state: worktree `settings` at `9699f06` plus the uncommitted T01-T05 changes, which were left untouched. Configuration `Debug`, `net10.0-windows` for `TokenHound.App` and `net10.0` for the two test projects, SDK pinned by `global.json`. Projects validated: `TokenHound.App` (build), `TokenHound.Core`, `TokenHound.Infrastructure`, `TokenHound.Core.Tests`, `TokenHound.Infrastructure.Tests`. Nothing outside the authorized write scope was modified; `App.xaml.cs` is untouched and still compiles because the factory was added as an optional second parameter.
- Open items:
  - **MAN-01 pending** (keyboard: Tab order, Space toggle, Esc dismiss - NFR-02, FR-09, US-06). Not executed; needs a real desktop session. NFR-02 acceptance stays pending.
  - **MAN-02 pending** (dark mode plus 100/150/200% scaling screenshots - NFR-01). Not executed. NFR-01 and OBJ-05 acceptance stay pending.
  - **Disabled pill contrast**: the PRD's `#757575` on the composited `#1AFFFFFF` pill measures **3.11:1**, which clears the 3:1 non-text and large-text bar but misses the 4.5:1 AA bar for normal text - the only one of the seven pairs that does. The PRD hex values were used verbatim as specified rather than silently adjusted. Raising the foreground to roughly `#9A9A9A` would reach 4.5:1 while keeping the muted read. Owner decision, affects NFR-01 and FR-05.
  - **`ShowSettings` signature is transitional**: the factory is the *second*, optional parameter (`ShowSettings(Window? owner = null, Func<SettingsViewModel>? viewModelFactory = null)`) so that the existing `App.xaml.cs:168` call site keeps compiling while startup wiring remains T07's scope. Until T07 supplies the factory the dialog opens with a null `DataContext` and renders an empty card. T07 should pass the factory and may reorder the parameters once it owns that call site.
  - **OPEN-02 recorded, not confirmed**: `Height`/`MinHeight` set to 440 (client-area budget: 25 header + 32 subtitle + 244 card for five 46 DIP rows + 44 footer + 40 margins = 385 of roughly 407 available). `Width` raised 440 -> 460 to match the PRD's ~460 DIP figure and keep the "Access Denied" pill unclipped. No `ScrollViewer`; a sixth provider would overflow. Confirmation depends on MAN-02.

### ADR candidates

**T06-ADR-01 - Resolve view-model resource keys in the view with a single `IValueConverter`**

- Context: DEC-04 and DEC-06 require `ProviderBadgeResolver`, `ProviderToggleViewModel`, and `SettingsViewModel` to stay free of WPF types so they keep linking into the `net10.0` `TokenHound.Infrastructure.Tests` project. As a consequence they expose `GlyphKey`, `BadgeBackgroundKey`, and `BadgeForegroundKey` as resource *key strings*, which XAML cannot bind to a `Brush` or `Geometry` property directly. The repository contained no converter and no `UI/Converters` folder before this task, so the shape of the indirection was undecided.
- Decision: add one `ResourceKeyConverter : IValueConverter` under `src/TokenHound.App/UI/Converters/` that returns `Application.Current?.TryFindResource(key)`, and `DependencyProperty.UnsetValue` for a null, blank, or unknown key. It serves both geometry keys and brush keys, so a single converter covers every key-typed property the feature produces.
- Alternatives: (a) a `DataTrigger` per badge state in the row template - 7 states x 2 properties = 14 setters of duplicated markup, and the resolver's key names would stop being the contract, defeating the T03 -> T06 handoff; (b) exposing `Brush` properties from the view models - breaks DEC-06 and the test-project link; (c) attached properties performing the lookup - more machinery than a converter for the same effect.
- Consequences: converters become an accepted App-layer pattern and a new folder joins the project layout. A mistyped key degrades silently to the property's default rather than failing the build, which is why the key names are diffed against `ProviderBadgeResolver` as part of this task's evidence. `ConvertBack` throws `NotSupportedException`; every current binding is one-way by nature.
- Evidence: `src/TokenHound.App/UI/Converters/ResourceKeyConverter.cs`; consumed at `src/TokenHound.App/UI/Windows/SettingsWindow.xaml` for `Path.Data`, `Border.Background`, and `TextBlock.Foreground`.
- TechSpec relationship: complements DEC-04 and DEC-06; CMP-12 and CMP-13 named the markup and the resources but left the key-to-resource indirection unspecified. Worth folding into the TechSpec if a second dialog adopts the same view-model shape.
