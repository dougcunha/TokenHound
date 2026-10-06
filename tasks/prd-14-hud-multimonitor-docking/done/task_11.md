# T11 — Startup card first in Settings General tab

## Outcome

The Settings General tab shows its cards in the order Startup, HUD size, HUD placement; every card keeps its bindings, visibility rules, and behavior.

## Dependencies and boundaries

- Depends on: T07
- Unblocks: delegated review `codereview_03`, then HIL 3
- In scope: move the `Startup` card `Border` (DataContext `Startup`) to the top of the General tab `StackPanel`; swap the bottom margins so the first and middle cards keep `0,0,0,12` and the last card keeps `0,0,0,4`.
- Out of scope: card content, view models, other tabs, and the suggested split of `SettingsWindow.xaml` (separate refactoring after PRD-14).

## Traceability

| Source | Section | Item covered |
| --- | --- | --- |
| workflow.md DEC-07 | `workflow.md#DEC-07` | HIL 3 request: start with Windows first in Settings |

## Requirements

- Order inside the General tab: Startup, HUD size, HUD placement.
- Spacing stays visually identical: 12 px between cards, 4 px after the last card.
- Each card still collapses when its view model is `null`.

## Context to recover on demand

- Code: `src/TokenHound.App/UI/Windows/SettingsWindow.xaml` General tab `ScrollViewer` > `StackPanel`.
- Rules and skills: `CLAUDE.md` HUD validation via Windows MCP; `dotnet-efficient-validation`.

## Work

- [x] T11.1 Move the Startup card to the top and adjust the margins.
- [x] T11.2 Build, run the full test suite, and check the Settings window visually.

## Acceptance criteria

- Settings General tab shows Startup first, then HUD size, then HUD placement.
- App builds with 0 warnings; full Infrastructure test suite passes.

## Verification

- Unit: full suite run as a regression guard (XAML is not linked into tests).
- Integration: App build (XAML compile).
- E2E: omitted by .NET desktop policy.
- Manual: open Settings from the dev HUD context menu and capture the General tab via Windows MCP (owner: agent; human confirms at HIL 3).
- Environment dependency: installed TokenHound stopped during the check and restarted after.
- Commands: `rtk dotnet build src/TokenHound.App/TokenHound.App.csproj --no-restore`; `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-restore`; `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1`
- Expected evidence: build output with 0 warnings and 0 errors; test summary with 0 failed; screenshot of the General tab.

## Affected files

- Modify: `src/TokenHound.App/UI/Windows/SettingsWindow.xaml`

## Observability and recovery

- Operational signal: none.
- Recovery: revert the card move.

## Handoff

- Produced result: The Settings General tab `StackPanel` now holds the Startup card first, then HUD size, then HUD placement. Startup takes the inter-card margin `0,0,0,12`; HUD placement, now last, takes `0,0,0,4`; the Startup `Border.Style` block was re-indented to match its siblings. Bindings, collapse-on-null triggers, and card content are unchanged; `SettingsWindow.xaml` stays at 1051 lines.
- Changed files: `src/TokenHound.App/UI/Windows/SettingsWindow.xaml`
- Checks: `rtk dotnet build src/TokenHound.App/TokenHound.App.csproj --no-restore` → 4 projects, 0 errors, 0 warnings, exit 0; `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-restore` → 0 errors, 0 warnings, exit 0; `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1` → 1043 passed, exit 0. Manual: dev HUD (PID 7276) launched via Windows MCP, Settings opened from the context menu; the General tab shows Startup, HUD size, HUD placement in that order (screenshot `t11-settings-general.png` in the session scratchpad).
- Validated state: Uncommitted worktree on base c4b55b9 after codereview_02 corrections plus T11; Debug, net10.0-windows, Windows 11, three displays.
- Open items: Human confirmation of the new order at HIL 3. Installed TokenHound was stopped for the check and restarted (PID 28772).
