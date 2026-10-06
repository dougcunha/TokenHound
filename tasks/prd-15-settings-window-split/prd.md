# PRD — Refactoring Settings window and dialog resources

## Context and motivation

`src/TokenHound.App/UI/Windows/SettingsWindow.xaml` has 1051 lines: window resources, four tabs, seven cards, and the footer in one file. `src/TokenHound.App/UI/Styles/DialogResources.xaml` has 561 lines: palette, dialog controls, combo box, HUD menus, and scroll bars in one dictionary. Both crossed the "touched file above 500 lines" escalation trigger in the reviews of `prd-14-hud-multimonitor-docking` (`codereview_02`, `codereview_03`), and each new Settings section or HUD menu style keeps growing them. The human decided to plan the split right after PRD-14 (PRD-14 DEC-07; this feature's DEC-01).

## Scope

- Target: `SettingsWindow.xaml` (tab content and window resources) and `DialogResources.xaml` (resource dictionary layout).
- Allowed structural change: move Settings cards and tab content into user controls under `src/TokenHound.App/UI/Controls/Settings/`; move Settings-only resources into a Settings resource dictionary; split `DialogResources.xaml` into themed dictionaries that it merges, so `App.xaml` and every key stay reachable at application level.
- Out of scope: new settings, text, layout, or visual changes; view model or code-behind behavior changes (beyond what moving markup requires); unifying the hover triggers of the four item templates; the Cadence tab content and the footer, which code-behind addresses by name.

## Behaviors to preserve

| ID | Observable behavior | Source and evidence | Verification |
| --- | --- | --- | --- |
| R-01 | Settings shows the tabs General, Providers, Cadence & Rate Limits, Updates in that order with the same headers and styles; General collapses when Startup, HUD size, and HUD placement are all unavailable | `SettingsWindow.xaml:256-923`, General tab trigger `:264-277` | Manual screenshot of each tab vs baseline |
| R-02 | General shows the Startup, HUD size, and HUD placement cards in that order with 12 px between cards and 4 px after the last; each card collapses when its view model is null | `SettingsWindow.xaml:284-566` | Manual screenshot; markup review |
| R-03 | Every binding, command, and automation name of the Startup (5), HUD size (9), HUD placement (4), and Updates (7) sections stays the same | `SettingsWindow.xaml` sections; 52 automation names in total | Automation-name diff (QA-04); view model tests; manual apply in each card |
| R-04 | Opening Settings and selecting the Cadence tab focus and select the active interval box; Enter applies on the Cadence tab; Tab order goes through intervals 1-3 then footer buttons 4-7 | `SettingsWindow.xaml.cs:45-63,90-115`; `TabIndex` at `SettingsWindow.xaml:660,713,783` and footer | Manual keyboard script |
| R-05 | Changing a HUD placement combo box does not hide the footer status text (tab-selection filter ignores bubbled `SelectionChanged`) | `SettingsWindow.xaml.cs:105` | Manual script |
| R-06 | Esc discards and closes; Close and Cancel act as cancel buttons | `SettingsWindow.xaml.cs:36-42`; `IsCancel` at `SettingsWindow.xaml:993,1012` | Manual script |
| R-07 | Providers tab lists provider rows with glyphs, badges, and toggles; when Startup and HUD size are unavailable the window opens on Providers | `ProviderRowTemplate` `SettingsWindow.xaml:156-228`; `SettingsWindow.xaml.cs:129-130`; `ResourceKeyConverter` | Manual screenshot; existing view model tests |
| R-08 | The Updates tab stays visible with its fixed text even when the Updates view model is null | `SettingsWindow.xaml:816-922` (no null collapse) | Markup review |
| R-09 | Dialog buttons, combo boxes, scroll bars, focus visuals, badges, and toggles render the same in About, Update, Provider Status, and Settings | `DialogResources.xaml:65-205,278-373,467-559`; consumers in the four windows | Manual screenshots vs baseline |
| R-10 | The HUD context menu, Position submenu, check items, and the tray menu render the same; the tray finds `HudContextMenuStyle` and `HudMenuItemStyle` by name | `DialogResources.xaml:208-275,376-461`; `TaskbarIconAdapter.cs:58-59` | Manual screenshots of HUD and tray menus |
| R-11 | Provider Status finds `SurfaceBackgroundColor` and `TextPrimaryColor` by name; badge brushes resolve by string key | `ProviderStatusWindow.xaml.cs:14-15,31-32`; `ProviderBadgeResolver.cs:22-36`; `ResourceKeyConverter.cs:23` | Key-set diff (QA-03); manual Provider Status and Providers tab |
| R-12 | The implicit scroll bar style based on `DialogScrollBarStyle` applies in Settings and Provider Status | `SettingsWindow.xaml:29`; `ProviderStatusWindow.xaml:33` | Manual screenshot with a visible scroll bar |

## Constraints

- WPF on .NET 10 (`net10.0-windows`); the test project is `net10.0` and cannot load XAML, so visual and binding behavior is checked manually.
- XAML binding paths are not compiler-checked: every moved binding keeps its exact path relative to the same data context.
- No `DynamicResource` is introduced, and `App.xaml` keeps merging `UI/Styles/DialogResources.xaml`.
- HUD invariants unchanged (non-activating window, `SWP_NOZORDER` without `SWP_SHOWWINDOW`); this refactoring does not touch window interop.

## Acceptance criteria

- [ ] All `R-NN` items were checked after refactoring.
- [ ] No new behavior entered the scope silently.
- [ ] Tests and checks proportional to risk passed.
- [ ] Rollback or reversal was proven when applicable.
- [ ] `SettingsWindow.xaml` and every dictionary that replaces `DialogResources.xaml` content are at most 500 lines.

## Assumptions and open items

- Assumption: baseline screenshots taken before any change are the reference for "renders the same" (TechSpec step 1).
- Assumption: the Cadence tab content and the footer stay in `SettingsWindow.xaml`, because code-behind uses `ActiveIntervalTextBox`, `StatusMessageTextBlock`, `ApplyButton`, `CadenceTab`, and `ProvidersTab` by name and footer buttons bind to `ElementName=CadenceTab`.
- Open item: none blocking.
