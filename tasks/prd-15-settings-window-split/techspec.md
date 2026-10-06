# TechSpec — Refactoring Settings window and dialog resources

> Status: draft, held until HIL 2 (refactoring path of `sdd-orchestrate-flow`).

## Sources and traceability

- PRD: `prd.md` (R-01..R-12)
- Current code: `src/TokenHound.App/UI/Windows/SettingsWindow.xaml` (1051 lines) and `SettingsWindow.xaml.cs` (184 lines); `src/TokenHound.App/UI/Styles/DialogResources.xaml` (561 lines); `src/TokenHound.App/App.xaml:5-10`; consumers `AboutWindow.xaml`, `UpdateWindow.xaml`, `ProviderStatusWindow.xaml(.cs)`, `NotchWindow.xaml`, `UI/Tray/TaskbarIconAdapter.cs:58-59`, `UI/Converters/ResourceKeyConverter.cs:23`, `ViewModels/ProviderBadgeResolver.cs:22-36`, `UI/Windows/DialogService.cs:105,232-241`
- Current tests: `tests/TokenHound.Infrastructure.Tests/ViewModels/` (`Settings*`, `Cadence*`, `HudSize*`, `HudPlacement*`, `Startup*`, `UpdateSettingsViewModelTests.cs`); no test loads XAML (the test project is `net10.0`)
- Applicable instructions, rules, and skills: `CLAUDE.md` (C# style, Windows MCP HUD validation), `sdd-plan-refactoring` references `wpf.md` and `providers-and-hud.md`, `dotnet-efficient-validation`

## Technical decisions

| ID | Requirements | Decision | Evidence and reason | Alternatives and trade-offs |
| --- | --- | --- | --- | --- |
| DEC-01 | R-02, R-03, R-08 | Extract each General card (Startup, HUD size, HUD placement), the Updates tab content, and the Providers tab content into a `UserControl` under `src/TokenHound.App/UI/Controls/Settings/`. Each control holds the card markup verbatim, including its own `Border DataContext="{Binding X}"` and null-collapse trigger, and inherits the `SettingsViewModel` data context from the window | Bindings stay relative to the same data context, so no path changes; the Updates card keeps "no null collapse" (R-08) because its markup moves unchanged | `DataTemplate` per view model: names unreachable and null content renders differently. Setting `DataContext` on the control: forces path and trigger rewrites |
| DEC-02 | R-01, R-04, R-05, R-06 | Keep in the window: the header, `TabControl` and its four `TabItem`s (names, headers, styles, General collapse trigger), the General `ScrollViewer`/`StackPanel` hosting the three cards, the whole Cadence tab content, and the footer | Code-behind uses `CadenceTab`, `ProvidersTab`, `SettingsTabControl`, `ActiveIntervalTextBox`, `StatusMessageTextBlock`, `ApplyButton` (`SettingsWindow.xaml.cs:48-130`); footer buttons bind `ElementName=CadenceTab`; Enter, focus, and `TabIndex` 1-7 stay in one focus scope | Extracting Cadence: breaks named fields and the Enter/focus logic, or needs new code-behind plumbing |
| DEC-03 | R-01, R-03, R-07 | Move the window-local keys `SettingsTabControlStyle`, `SettingsTabItemStyle`, `LabelItemTemplate`, `SettingsTextBoxStyle`, `ProviderRowTemplate`, and `ResourceKeyConverter` into `src/TokenHound.App/UI/Styles/SettingsResources.xaml`. The window merges it in `Window.Resources` and keeps its implicit `ScrollBar` style; each control that uses one of these keys (HUD placement card, Updates, Providers) merges it in its own `UserControl.Resources` | A separately compiled `UserControl` resolves `StaticResource` from its own resources and the application, not from the hosting window, so window-local keys would fail at load | Moving these keys to `App.xaml`: widens their scope to every window; `DynamicResource`: forbidden by the PRD constraint |
| DEC-04 | R-09, R-10, R-11, R-12 | Split `DialogResources.xaml` into `DialogFoundation.xaml` (palette colors and brushes, badge and toggle palette, `DialogFocusVisualStyle`), `DialogControls.xaml` (button, badge pill and text, provider toggle), `DialogComboBox.xaml` (item and combo box), `DialogScrollBar.xaml` (page button, thumb, scroll bar), and `HudMenus.xaml` (context menu, menu item, submenu, check item). `DialogResources.xaml` becomes an aggregator that only merges them in that order; `App.xaml` is unchanged | Every key stays reachable from `Application.Resources`, which `TaskbarIconAdapter`, `ProviderStatusWindow.xaml.cs`, and `ResourceKeyConverter` require; definition order inside each part keeps the current order | Pointing `App.xaml` at each part: touches the composition root and loses the single entry point |
| DEC-05 | R-09, R-10 | Each style part merges `DialogFoundation.xaml` itself (and `HudMenus.xaml` keeps `HudMenuItemStyle` before its `BasedOn` children) instead of relying on sibling dictionaries | Whether `StaticResource` in one merged dictionary resolves keys of a sibling while `Application.Resources` is still loading is not guaranteed; an explicit merge makes each part self-contained. Duplicated brush instances have identical values | Single palette at app level only: depends on load-order behavior that the explorer could not confirm |
| DEC-07 | R-01, R-02, R-07..R-12 | The visual baseline (MA-1) is a build of `git_base` from a temporary `git worktree`, launched and captured in the same session as the head build (MA-2), one build at a time. Step 1 persists only text baselines (QA-03 key set, QA-04 automation names, line counts) under `baseline/` | Windows MCP `Screenshot` returns an image without saving a file, so screenshots do not survive a session; the app holds a single-instance mutex (`ApplicationInstanceMutex.cs:30`) and both builds share settings in `%LOCALAPPDATA%\TokenHound`, so they run sequentially with the same placement | Stored PNGs: not persistable with the current tools, and screen coordinates drift with display layout. Comparing against memory of earlier sessions: not evidence |
| DEC-06 | Scope | Keep the four item templates with the repeated hover trigger as they are | They differ in background binding, `SnapsToDevicePixels`, and extra triggers (`DialogResources.xaml:252-272,283-298,379-421,430-458`); unifying them is a visual risk with no structural gain needed for the 500-line target | Shared base template: possible later, outside this refactoring |

## Affected components

| ID | Component | Current state | Allowed change | Risk and dependencies |
| --- | --- | --- | --- | --- |
| CMP-01 | `UI/Windows/SettingsWindow.xaml` | Resources, 4 tabs, 7 cards, footer (1051 lines) | Replace card and tab-content markup with control elements; merge `SettingsResources.xaml` | Binding paths, tab and focus order (DEC-02) |
| CMP-02 | `UI/Windows/SettingsWindow.xaml.cs` | Named-element logic | No change expected | Breaks only if a referenced name moves (DEC-02) |
| CMP-03 | `UI/Styles/SettingsResources.xaml` (new) | — | Holds the six Settings keys verbatim | Load-time resolution of app keys from a window-level dictionary |
| CMP-04 | `UI/Controls/Settings/StartupSettingsCard.xaml(.cs)` (new) | — | Startup card verbatim | R-02, R-03 |
| CMP-05 | `UI/Controls/Settings/HudSizeSettingsCard.xaml(.cs)` (new) | — | HUD size card verbatim | R-02, R-03 |
| CMP-06 | `UI/Controls/Settings/HudPlacementSettingsCard.xaml(.cs)` (new) | — | HUD placement card verbatim; merges `SettingsResources.xaml` | Bubbled `SelectionChanged` still reaches the window filter (R-05) |
| CMP-07 | `UI/Controls/Settings/UpdatesSettingsPanel.xaml(.cs)` (new) | — | Updates tab content verbatim; merges `SettingsResources.xaml` | R-08 |
| CMP-08 | `UI/Controls/Settings/ProvidersSettingsPanel.xaml(.cs)` (new) | — | Providers tab content verbatim; merges `SettingsResources.xaml` | R-07 |
| CMP-09 | `UI/Styles/DialogResources.xaml` | Monolithic dictionary (561 lines) | Becomes an aggregator of CMP-10..14 | R-09..R-12 |
| CMP-10..14 | `UI/Styles/DialogFoundation.xaml`, `DialogControls.xaml`, `DialogComboBox.xaml`, `DialogScrollBar.xaml`, `HudMenus.xaml` (new) | — | Keys moved verbatim in current order; style parts merge Foundation | Key loss or reorder (QA-03) |

User control code-behind follows the repository C# rules: file-scoped namespace `TokenHound.App.UI.Controls.Settings`, `public sealed partial class`, XML summary, constructor calling `InitializeComponent()`, and a `d:DataContext` of `SettingsViewModel` for the designer.

## Safety net

- Profile: `TokenHound.App` WPF (`net10.0-windows`); `TokenHound.Infrastructure.Tests` (`net10.0`, xUnit + AwesomeAssertions on Microsoft.Testing.Platform). Commands from `CLAUDE.md`: `rtk dotnet build src/TokenHound.App/TokenHound.App.csproj --no-restore`, `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-restore`, `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1`.
- E2E: omitted by .NET desktop policy.
- Command prerequisites and exclusions: stop the dev HUD before building; stop the installed TokenHound (`D:\Apps\TokenHound`) during manual checks and restart it after.
- Manual acceptance (owner: agent with Windows MCP, confirmed by the human at the visual check): MA-1 baseline from a `git_base` worktree build and MA-2 from the head build, captured back to back in the same session (DEC-07), each capturing Settings (all four tabs, General scrolled to show every card), About, Update (if reachable), Provider Status, the HUD context menu with the Position submenu, and the tray menu; MA-3 keyboard script (open Settings, Cadence tab focuses and selects the active interval, Tab order 1-7, Enter applies, Esc closes); MA-4 change HUD position in Settings and confirm the footer status is not hidden and the HUD moves; run it last and restore the original placement, since both builds share the settings file.

| ID | Requirement | Level | Scenario | Expected result | Command or script |
| --- | --- | --- | --- | --- | --- |
| TC-01 | R-03, R-07 | unit | Existing Settings view model suites | All pass, count unchanged (1043) | test command above |
| TC-02 | R-03 | static | Automation names across `SettingsWindow.xaml`, `SettingsResources.xaml`, and `UI/Controls/Settings/*.xaml` | Same multiset as baseline (41 Name + 11 HelpText = 52) | QA-04 |
| TC-03 | R-09..R-11 | static | `x:Key` set across `UI/Styles/*.xaml` and `SettingsWindow.xaml` | Same set as baseline, no key lost or renamed | QA-03 |
| TC-04 | All | integration | App build | 0 warnings, 0 errors (XAML compile) | build command above |
| TC-05 | R-01, R-02, R-07..R-10, R-12 | manual | MA-1 (`git_base` worktree build) vs MA-2 (head build), same session | No visible difference | `git worktree add` + `dotnet restore` + build; Windows MCP launch + Screenshot |
| TC-06 | R-04, R-06 | manual | MA-3 keyboard script | Same focus, order, Enter, and Esc behavior | Windows MCP |
| TC-07 | R-05 | manual | MA-4 | Status text stays; HUD moves to the chosen position | Windows MCP |

## Dependency sequencing

| Step | Depends on | Change | Evidence to advance | Reversal |
| --- | --- | --- | --- | --- |
| 1 | — | Save the QA-03/QA-04 baseline lists and line counts under `baseline/` in the feature folder; probe whether a screen capture can be saved to a file (secondary evidence only, DEC-07) | Files saved; probe result in handoff | — |
| 2 | 1 | Split `DialogResources.xaml` into CMP-10..14 with the aggregator (DEC-04, DEC-05) | TC-03, TC-04, smoke of HUD menu and Settings | Restore the single file |
| 3 | 1 | Move Settings-local keys to `SettingsResources.xaml` and merge it in the window (DEC-03) | TC-03, TC-04, Settings opens | Move keys back |
| 4 | 3 | Extract the General cards (CMP-04..06) | TC-02, TC-04, General tab screenshot | Inline the markup again |
| 5 | 3 | Extract Updates and Providers content (CMP-07, CMP-08) | TC-02, TC-04, tab screenshots | Inline the markup again |
| 6 | 2, 4, 5 | Full validation: TC-01..TC-07 and quality profile; MA-1 vs MA-2 from a temporary `git_base` worktree (DEC-07) | All pass | Revert per step; `git worktree remove` the baseline worktree |

Steps are reversible and independent of persisted data; no settings file or view model changes.

## Compatibility and rollout

- Preserved contracts: R-01..R-12; application-level resource keys used by name (`HudContextMenuStyle`, `HudMenuItemStyle`, `SurfaceBackgroundColor`, `TextPrimaryColor`, 14 badge brushes).
- Migration or coexistence: none; user settings untouched.
- Observability: XAML parse errors surface at startup or when Settings opens (dispatcher unhandled exception log); missing resources fail at load.
- Rollback: revert the commit(s) of this feature.

## Quality profile

| ID | Rule | Class | Verification command | Baseline | Target |
| --- | --- | --- | --- | --- | --- |
| QA-01 | `SettingsWindow.xaml` at most 500 lines | blocking | `wc -l src/TokenHound.App/UI/Windows/SettingsWindow.xaml` | 1051 | ≤ 500 (estimated ~465) |
| QA-02 | Every dictionary under `UI/Styles/` and every new Settings control at most 500 lines (aim ≤ 250) | blocking | `wc -l src/TokenHound.App/UI/Styles/*.xaml src/TokenHound.App/UI/Controls/Settings/*.xaml` | `DialogResources.xaml` 561 | max ≤ 250 |
| QA-03 | Same `x:Key` set across `UI/Styles/*.xaml` + `SettingsWindow.xaml` + Settings controls | blocking | `rg -o --no-filename 'x:Key="[^"]+"' <files> \| sort -u` diffed against step-1 list | baseline list | identical |
| QA-04 | Same `AutomationProperties.Name` and `.HelpText` multiset across Settings XAML (PRD R-03 counts both as its 52 "automation names") | blocking | `rg -o --no-filename 'AutomationProperties.(Name\|HelpText)="[^"]+"' <files> \| sort` diffed against step-1 list | 52 (41 Name + 11 HelpText) | identical |
| QA-05 | No `DynamicResource` | blocking | `rg -c DynamicResource src/TokenHound.App` | 0 | 0 |
| QA-06 | Build warnings | blocking | build command | 0 | 0 |
| QA-07 | `App.xaml` unchanged | blocking | `git diff --quiet <base> -- src/TokenHound.App/App.xaml` | unchanged | unchanged |

- Target measures today: `SettingsWindow.xaml` 1051 lines, `DialogResources.xaml` 561 lines, 52 automation names, 0 `DynamicResource`.
- Expected measures at the end: `SettingsWindow.xaml` ~465 lines; `DialogResources.xaml` ~15 lines (aggregator); largest new dictionary ~150 lines (`HudMenus.xaml`); five new Settings controls of ~35-120 lines each.

## Risks and open items

- Risk: `StaticResource` in `SettingsResources.xaml` (merged at window level) to application keys such as `DialogFocusVisualStyle`. Probability low (application resources are fully loaded when Settings opens and `StaticResource` falls back to them), impact high (Settings fails to open). Mitigation: step 3 opens Settings before any later step.
- Risk: duplicated brush instances from merging `DialogFoundation.xaml` into each part. Probability certain, impact negligible (identical values, small memory). Accepted for DEC-05.
- Risk: a user control starts a new keyboard focus scope and changes Tab order. Probability low (Cadence stays in the window), impact medium. Mitigation: TC-06.
- Risk: binding errors are silent at runtime. Mitigation: verbatim moves with unchanged data context (DEC-01), TC-02, and MA-2 checks that every field shows values.
- Risk: the baseline worktree build differs from the base because of a dirty or missing restore. Probability low, impact medium (false visual differences). Mitigation: fresh `git worktree add` at `git_base` and an explicit `dotnet restore` before its build.
- Open item: none blocking.
