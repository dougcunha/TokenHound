# Code review report — Settings window and dialog resources split (prd-15-settings-window-split)

## Summary

- Status: APPROVED WITH RESERVATIONS
- Execution: delegated reviewer
- Git scope: `90d6748f49437a2ff4e70ce15bcccbfa64e60793..working tree` (uncommitted, including untracked files). HEAD is still `90d6748`.
- Previous review: —

## Sources and scope

| Source | Path or reference | State |
| --- | --- | --- |
| PRD | `tasks/prd-15-settings-window-split/prd.md` | read; sha256 `4ce9322edb0f…` matches workflow DEC-02 and checkpoint `approved_sources` |
| TechSpec | `tasks/prd-15-settings-window-split/techspec.md` | read; sha256 `ea117aa17512…` matches workflow DEC-03 |
| Manifest | `tasks/prd-15-settings-window-split/tasks.md` | read; T01..T06 marked done; every link `done/task_0N.md` resolves; sha256 `4e9ec120b44a…` differs from the DEC-03 hash `3032a97b1005…` (see limitations) |
| Tasks and handoffs | `done/task_01.md` .. `done/task_06.md` | read; every work item checked, every handoff filled, no open items |
| Baselines | `baseline/keys.txt` (66), `baseline/automation-names.txt` (52), `baseline/line-counts.txt` (1051, 561), `baseline/README.md` | read; both lists regenerated from `git show 90d6748:…` in this review and identical |
| Workflow and checkpoint | `workflow.md` (DEC-01..DEC-04), `checkpoint.json` | read only |
| Snapshot | `context-snapshot.md` | loaded through the independent-stage filter (header, next step brief, open threads, on-run entries L-01/L-02/L-04/L-06/L-07); Decisions, Code map, and other Learnings skipped. Header validation: `git_head` 90d6748 = HEAD; worktree matches; `covers_through` "T05 (T06 partial)" is behind the manifest (T06 done) and workflow DEC-04, so the next step brief is stale; open thread O-02 (visual check) is closed by workflow DEC-04 |
| Feature folder | no extra files besides `baseline/`, `done/`, `checkpoint*.json`, `context-snapshot.md`, and the reserved `codereview_01/` | checked with a full recursive listing |
| Implementation | Modified: `src/TokenHound.App/UI/Styles/DialogResources.xaml`, `src/TokenHound.App/UI/Windows/SettingsWindow.xaml`. New: `UI/Styles/DialogFoundation.xaml`, `DialogControls.xaml`, `DialogComboBox.xaml`, `DialogScrollBar.xaml`, `HudMenus.xaml`, `SettingsResources.xaml`; `UI/Controls/Settings/{StartupSettingsCard,HudSizeSettingsCard,HudPlacementSettingsCard,ProvidersSettingsPanel,UpdatesSettingsPanel}.xaml(.cs)` | delimited |
| Excluded from scope | `.agents/settings.json` (+5 lines of ContextBrake hooks), untracked `.agents/hooks/`, `.agents/settings.local.json`, `.context-brake/`, `.opencode/`, `context-brake.config.json` | pre-existing per `workflow.md#Feature Context`; the `.agents/settings.json` diff was inspected and contains only hook registrations unrelated to the feature |

## Coverage matrix

| Source | Obligation | Implementation | Test | State | Evidence |
| --- | --- | --- | --- | --- | --- |
| R-01 | Four tabs in order with same headers and styles; General collapses when all three VMs null | `SettingsWindow.xaml:62-320` (TabControl, four `TabItem`s, General `MultiDataTrigger` kept in window) | TC-05 | conformant | Window body is an ordered subsequence of base with no lost line (reviewer multiset/order script); T06 MA-1 vs MA-2 pairs match for all four tabs |
| R-02 | General cards order, 12/12/4 px spacing, null collapse | `StartupSettingsCard.xaml:10-16` (Margin 0,0,0,12), `HudSizeSettingsCard.xaml` (0,0,0,12), `HudPlacementSettingsCard.xaml:14-20` (0,0,0,4); each keeps its `DataContext` binding and null `DataTrigger`; window `StackPanel` hosts them in base order (`SettingsWindow.xaml:89-92`) | TC-02, TC-05 | conformant | Verbatim move proven; T04 and T06 screenshots of General top and scrolled match base |
| R-03 | Bindings, commands, automation names of Startup, HUD size, HUD placement, Updates unchanged | five controls under `UI/Controls/Settings/` | TC-01, TC-02 | conformant | QA-04 diff identical (52 entries); line multiset of base `SettingsWindow.xaml` fully present in window + `SettingsResources.xaml` + controls (only wrapper lines added); data context is inherited from the window (no `DataContext` on any `UserControl` root); 1043 tests pass |
| R-04 | Cadence focus/select, Enter applies on Cadence, TabIndex 1-7 | Cadence tab and footer stay in window (`SettingsWindow.xaml:106-320`, `TabIndex` at `:161,214,284,336,391,410,430`); `SettingsWindow.xaml.cs` unchanged | TC-06 | conformant | `git diff --quiet 90d6748 -- …SettingsWindow.xaml.cs` clean; every code-behind name (`ActiveIntervalTextBox`, `ApplyButton`, `CadenceTab`, `ProvidersTab`, `SettingsTabControl`, `StatusMessageTextBlock`) is still declared in the window; MA-3 run by the human (workflow DEC-04, `task_06.md#Handoff`) |
| R-05 | Placement combo does not hide footer status | `HudPlacementSettingsCard.xaml` (no handler; `SelectionChanged` bubbles to `OnTabSelectionChanged` on `SettingsTabControl`) | TC-07 | conformant | Code-behind filter unchanged; MA-4 status part run by the human (DEC-04); HUD move and restore in T04 handoff |
| R-06 | Esc discards; Close and Cancel are cancel buttons | footer `SettingsWindow.xaml:389-411` (`IsCancel="True"` ×2) | TC-06 | conformant | Footer unchanged in window; MA-3 Esc by the human (DEC-04) |
| R-07 | Provider rows with glyphs, badges, toggles; opens on Providers when General unavailable | `ProvidersSettingsPanel.xaml` (merges `SettingsResources.xaml` for `ProviderRowTemplate`); `ProvidersTab` name kept at `SettingsWindow.xaml:98` | TC-01, TC-05 | conformant | `ProviderItems` `x:Name` moved into the panel namescope with zero code references (`rg ProviderItems src` → only the panel); MA-1 vs MA-2 Providers pair matches |
| R-08 | Updates tab visible with fixed text when VM null | `UpdatesSettingsPanel.xaml:19-25` (`Border DataContext="{Binding Updates}"`, no null-collapse trigger) | markup review | conformant | Only `x:Null` triggers are the `IntervalError`/`ApplyError` ones (`:65`, `:106`), same 2 as base lines 816-923 |
| R-09 | Dialog controls render the same in About, Update, Provider Status, Settings | `DialogControls.xaml`, `DialogComboBox.xaml`, `DialogScrollBar.xaml`, `DialogFoundation.xaml` | TC-03, TC-05 | conformant | Line multiset of base `DialogResources.xaml` equals the five parts minus wrappers; each part is an ordered subsequence of base; MA-1 vs MA-2 About, Provider Status, Settings match; Update window not captured (limitation) |
| R-10 | HUD context menu, Position submenu, check items, tray menu render the same; tray finds keys by name | `HudMenus.xaml` (`HudMenuItemStyle` before its `BasedOn` children); aggregator merges it at app level | TC-03, TC-05 | conformant | QA-03 identical; HUD menu pair matches (T06); tray menu approved by the human on head (DEC-04); `App.xaml` unchanged so `TryFindResource` reaches the same application chain |
| R-11 | `SurfaceBackgroundColor`, `TextPrimaryColor`, badge brushes resolve by name | `DialogFoundation.xaml` | TC-03 | conformant | QA-03 identical; Provider Status opened with dark surface and badges (T02, T06) |
| R-12 | Implicit `ScrollBar` style based on `DialogScrollBarStyle` in Settings and Provider Status | `SettingsWindow.xaml` `Window.Resources` keeps the implicit style; `DialogScrollBarStyle` in `DialogScrollBar.xaml` at app level | TC-05 | conformant | Moved `ScrollViewer`s sit inside `UserControl`s under the window, so implicit lookup still reaches `Window.Resources`; slim scroll bar seen in T03 and T06 captures |
| DEC-01 | Cards and Updates/Providers content as verbatim `UserControl`s inheriting `SettingsViewModel` | five controls | TC-02 | conformant | Reviewer multiset and order checks; code-behind shape per TechSpec CMP note |
| DEC-02 | Header, TabControl, TabItems, General host, Cadence, footer stay in window | `SettingsWindow.xaml` | TC-06 | conformant | Names and `ElementName=CadenceTab` bindings at `:344,400,420,439` still in window |
| DEC-03 | Six Settings keys in `SettingsResources.xaml`; window and the three consuming controls merge it | `SettingsResources.xaml:6-133`; merges in window, `HudPlacementSettingsCard`, `ProvidersSettingsPanel`, `UpdatesSettingsPanel` | TC-03, TC-04 | conformant | Reviewer reference scan: every `StaticResource` in each control resolves from its own merges plus app keys; Startup and HUD size use no Settings key |
| DEC-04 | Five parts behind an aggregator in fixed order; `App.xaml` unchanged | `DialogResources.xaml:4-10` | TC-03, TC-04 | conformant | Order Foundation, Controls, ComboBox, ScrollBar, HudMenus; QA-07 unchanged |
| DEC-05 | Each style part merges `DialogFoundation.xaml` | four style parts | TC-03 | conformant | Reviewer scan: no part references a key outside itself + Foundation |
| DEC-06 | Item templates unchanged | `HudMenus.xaml`, `DialogComboBox.xaml` | markup diff | conformant | Multiset equality and ordered-subsequence check over the dialog split |
| DEC-07 | Text baselines; visual base-vs-head in one session | `baseline/`; T06 worktree build | TC-05 | conformant | Baselines regenerated in review and identical; T06 handoff records same-session sequential capture and worktree removal |
| CMP-01..CMP-14 | Component changes as allowed | files listed in scope | TC-02..TC-04 | conformant | CMP-02 `SettingsWindow.xaml.cs` unchanged; no file outside CMP list changed under `src/` |
| TC-01 | Settings VM suites pass, 1043 | — | test command | conformant | 1043 passed in this review |
| TC-02 / QA-04 | Automation multiset identical | — | baseline diff | conformant | `QA-04 identical` (52) |
| TC-03 / QA-03 | Key set identical | — | baseline diff | conformant | `QA-03 identical` (66) |
| TC-04 / QA-06 | App build 0 warnings | — | build | conformant | `--no-incremental` app build: 0 errors, 0 warnings |
| TC-05 | MA-1 vs MA-2 no visible difference | — | manual | conformant (reused) | `task_06.md#Handoff`; code unchanged since (see validations) |
| TC-06 | MA-3 keyboard | — | manual | conformant (reused) | human run, workflow DEC-04 |
| TC-07 | MA-4 placement and status | — | manual | conformant (reused) | human run of status part (DEC-04); HUD move/restore in T04 |
| PRD acceptance | ≤ 500 lines; no silent new behavior; rollback when applicable | — | QA-01/02 | conformant | 450 lines max for window, 207 max for dictionaries; only wrapper lines added; reversal = restore two base files and delete new ones, no data or settings change |

## Compliance with rules and skills

| Rule or skill | State | Evidence |
| --- | --- | --- |
| CLAUDE.md: one class per file, sealed | OK | five `public sealed partial class` files under `UI/Controls/Settings/` |
| CLAUDE.md: XML docs on public members | OK | class and constructor `<summary>` in each `*.xaml.cs` |
| CLAUDE.md: file-scoped namespace, alphabetized usings | OK | `namespace TokenHound.App.UI.Controls.Settings;`, single `using System.Windows.Controls;` |
| CLAUDE.md: blank line inside multi-line blocks | OK | blank line after `{` in each constructor, e.g. `StartupSettingsCard.xaml.cs:15` |
| CLAUDE.md: file/class ≤ 300 lines, methods ≤ 30 | OK | code-behind files 18 lines each |
| CLAUDE.md HUD invariants (WM_MOUSEACTIVATE, SWP flags) | N/A | no interop file changed |
| PRD constraint: no `DynamicResource`, `App.xaml` keeps merging `DialogResources.xaml` | OK | QA-05, QA-07 |
| Line endings and encoding | OK | all changed files CRLF without BOM, same as `SettingsWindow.xaml.cs`; `git diff --check 90d6748 -- src/` clean |
| `dotnet-efficient-validation` | OK | MTP native route (`global.json` `test.runner`), `--minimum-expected-tests 1`, exit codes checked |
| `repository-cli-efficiency` | OK | scoped `rg`/`git diff --stat` first; full diff only for the window |

## Quality profile

| ID | Rule | Class | Command | Hits | State |
| --- | --- | --- | --- | --- | --- |
| QA-01 | `SettingsWindow.xaml` ≤ 500 lines | blocking | `wc -l src/TokenHound.App/UI/Windows/SettingsWindow.xaml` | 450 (baseline 1051) | OK |
| QA-02 | Dictionaries and Settings controls ≤ 500 (aim ≤ 250) | blocking | `wc -l src/TokenHound.App/UI/Styles/*.xaml src/TokenHound.App/UI/Controls/Settings/*.xaml` | max 207 (`SettingsResources.xaml`); controls max 123 | OK |
| QA-03 | Same `x:Key` set | blocking | `baseline/README.md` QA-03 command | 0 of 66 changed | OK |
| QA-04 | Same automation Name + HelpText multiset | blocking | `baseline/README.md` QA-04 command | 0 of 52 changed | OK |
| QA-05 | No `DynamicResource` | blocking | `rg -c DynamicResource src/TokenHound.App` | 0 | OK |
| QA-06 | Build warnings | blocking | `rtk dotnet build src/TokenHound.App/TokenHound.App.csproj --no-restore --no-incremental` | 0 | OK |
| QA-07 | `App.xaml` unchanged | blocking | `git diff --quiet 90d6748 -- src/TokenHound.App/App.xaml` | unchanged | OK |

- Terrain baseline: applied from TechSpec (Baseline column; `baseline/` lists regenerated from `git_base`).
- Hits discounted by baseline: 0.
- Reservations accumulated in the feature: 0 (all profile rules are blocking class).
- Suggested escalation: no trigger fired (no touched file above 500 lines; largest is 450).

## TechSpec adherence

| Decision or contract | State | Evidence |
| --- | --- | --- |
| DEC-01..DEC-07 | YES | see coverage matrix |
| CMP-01..CMP-14 | YES | only listed files changed under `src/`; CMP-02 untouched |
| User control code-behind shape (CMP note) | YES | namespace, `public sealed partial`, XML summary, `InitializeComponent()`, `d:DataContext` `SettingsViewModel` with `mc:Ignorable="d"` |
| Safety net TC-01..TC-07 | YES | TC-01..TC-04 rerun here; TC-05..TC-07 reused (below) |
| Dependency sequencing steps 1-6 | YES | handoffs T01..T06 in order; baseline worktree removed (T06) |
| Compatibility: no settings or VM changes | YES | no file under `ViewModels/` or settings code in the diff |

## Verified tasks

| Task | Location | State | Handoff and evidence |
| --- | --- | --- | --- |
| T01 | `done/task_01.md` | COMPLETE | baselines reproduced from `90d6748` in this review |
| T02 | `done/task_02.md` | COMPLETE | dialog split verified by multiset and order; tray menu deferred to T06 and closed by DEC-04 |
| T03 | `done/task_03.md` | COMPLETE | `SettingsResources.xaml` 207 lines, six keys in base order |
| T04 | `done/task_04.md` | COMPLETE | three cards; R-05 status part deferred to T06 and closed by DEC-04 |
| T05 | `done/task_05.md` | COMPLETE | two panels; window 450 lines |
| T06 | `done/task_06.md` | COMPLETE | integrated validation; human visual check DEC-04 |

## Executed validations

- Profile and exclusions: `TokenHound.App` WPF `net10.0-windows`; `TokenHound.Infrastructure.Tests` `net10.0` on native MTP (SDK 10.0.401, `global.json` `test.runner: Microsoft.Testing.Platform`). E2E omitted by .NET desktop policy.
- Validated state: HEAD `90d6748` plus the uncommitted and untracked feature files listed in scope; Debug configuration; Windows 11; installed TokenHound running from `D:\Apps\TokenHound` (does not lock the repository `bin/`).
- Reused evidence: TC-05 (MA-1 vs MA-2), TC-06 (MA-3), and TC-07 (MA-4) from `task_06.md#Handoff` and workflow DEC-04. Still valid: the last source change under `src/` is 10:51:04 (`SettingsWindow.xaml`, panels), before `task_06.md` (11:04:24) and the DEC-04 record in `workflow.md` (11:04:45); HEAD unchanged.
- Manual acceptance: not re-run by this reviewer (contract forbids launching the app or Windows MCP).

| Command | Result | Obligations covered |
| --- | --- | --- |
| `rtk dotnet build src/TokenHound.App/TokenHound.App.csproj --no-restore --no-incremental --nologo --verbosity:minimal` | passed, 0 errors, 0 warnings | TC-04, QA-06 |
| `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-restore --nologo --verbosity:minimal` | passed, 0 warnings | TC-01 prerequisite |
| `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1` | passed, 1043 tests, exit 0 | TC-01, R-03, R-07 |
| `baseline/README.md` QA-03, QA-04, QA-05, QA-07 commands (Bash) | identical / identical / none / unchanged | TC-02, TC-03, QA-03..QA-05, QA-07 |
| baseline regeneration from `git show 90d6748:…` | identical to stored lists | T01, DEC-07 |
| `wc -l` over window, styles, and controls | 450 / max 207 / max 123 | QA-01, QA-02 |
| reviewer script: trimmed non-blank line multiset base vs new, and ordered-subsequence check per file | no base line lost; only wrapper lines added; order preserved | R-02, R-03, R-09, DEC-01, DEC-04..DEC-06 |
| reviewer script: `StaticResource`/`BasedOn` targets per dictionary and control | all resolve from own merges + app keys | DEC-03, DEC-05, R-10, R-11 |
| `git diff --check 90d6748 -- src/` | clean | style |

## Findings

No `CR-NN` findings.

Optional improvement (not blocking):

| ID | Severity | Source | Evidence | Impact | Proven recommendation |
| --- | --- | --- | --- | --- | --- |
| OI-01 | Low | DEC-03 leftover | `src/TokenHound.App/UI/Windows/SettingsWindow.xaml:6` — `xmlns:converters` is still declared, but its only user (`converters:ResourceKeyConverter`) moved to `SettingsResources.xaml`; no `converters:` prefix remains in the window | none at runtime or build; dead namespace declaration | Remove the `xmlns:converters` line from `SettingsWindow.xaml` |

## Limitations and open items

- `tasks.md` sha256 `4e9ec120b44a…` differs from the DEC-03 approved hash `3032a97b1005…`. Observable deltas are the State checkboxes and the "Problems and solutions" entry (both execution-mutable sections); the file is untracked, so the plan sections cannot be proven byte-identical. Task list, dependency graph, and traceability IDs match the TechSpec and `done/`. No status impact.
- Update window not captured in MA-1/MA-2 (TechSpec allows "if reachable"; T06 records that it needs a network check). R-09 for that window rests on the static verbatim proof and the shared button style seen in About.
- Tray menu verified on the head build only, by the human (DEC-04), not base-vs-head; static coverage comes from the verbatim multiset/order proof, identical key set, and unchanged `App.xaml`.
- R-08 not exercised at runtime (VM never null in this build); the TechSpec verification is markup review, which passes.
- This reviewer did not launch the app or use Windows MCP (contract); TC-05..TC-07 are reused evidence from the same code state.
- Snapshot `context-snapshot.md` is behind the manifest (`covers_through` T05/T06 partial vs T06 done); for the flow to record, not for this reviewer to edit.

## Conclusion

Every PRD behavior, TechSpec decision, component, safety-net case, and quality profile rule is conformant, and all six tasks are complete with intact links. The reviewer independently proved the refactoring is a verbatim move (no base line lost, order preserved, only wrapper lines added), that every resource reference resolves under the new merge layout, and that build, tests (1043), key set, and automation names match the baseline. Manual acceptance is reused from the same code state with human approval. The only item is the optional removal of a dead `xmlns:converters` declaration, so the status is APPROVED WITH RESERVATIONS.
