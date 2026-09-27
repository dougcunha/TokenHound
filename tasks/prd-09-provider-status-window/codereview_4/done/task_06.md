# Stable execution context

Load in this exact order:

1. `tasks/prd-09-provider-status-window/codereview_4/codereview.md`
2. This file

Use the already loaded report version; recover relevant contracts and code afterward. This order does not guarantee a host cache hit.

---

# T06 — Dark scrollbar for the status and Settings windows

## Outcome

When content overflows, the provider status window and the Settings window show a slim dark scrollbar that matches the dialog palette, instead of the default light WPF scrollbar.

## Dependencies and boundaries

- Depends on: — (T01–T05 are in `done/`)
- Unblocks: re-review `codereview_5` (in another session), then HIL 3
- In scope: a shared keyed `ScrollBar` style in `DialogResources.xaml` (thin rounded thumb on a transparent track, vertical and horizontal); an implicit `ScrollBar` style based on it in the `Window.Resources` of `ProviderStatusWindow.xaml` and `SettingsWindow.xaml`.
- Out of scope: the HUD (`NotchWindow`), tooltips, About, other controls, layout, data, behaviour, code-behind, view models, Core, and Infrastructure. The style is not made implicit at application level, so no other window changes.

## Traceability

| Source | Section | Finding covered |
| --- | --- | --- |
| codereview_4/CR-02 | `codereview.md#findings` | Default light scrollbar on the dark status window body |
| workflow DEC-12 | `workflow.md#DEC-12` | Correction authorized; scope extended to the Settings window |
| PRD NFR-04 | `prd.md#non-functional-requirements` | Dark panel following the reference |

## Requirements

- The scrollbar is about 8 px wide, has no arrow buttons, and has a transparent track. The thumb is rounded and uses `SurfaceBorderBrush` (#3F3F46), `ButtonPressedBackgroundBrush` (#52525B) on hover, and `TextMutedBrush` (#71717A) while dragging.
- Dragging the thumb, clicking the track (page up/down), and mouse-wheel scrolling keep working.
- A horizontal scrollbar, should one appear (e.g. inside a `TextBox`), uses the same look.

## Context to recover on demand

- TechSpec: `techspec.md#technical-decisions` DEC-11.
- Rules and skills: `AGENTS.md` HUD validation (Windows MCP `App` launch, `Screenshot`); `dotnet-efficient-validation`.
- Code:
  - `src/TokenHound.App/UI/Styles/DialogResources.xaml`: palette brushes; merged at application level in `App.xaml`.
  - `src/TokenHound.App/UI/Windows/ProviderStatusWindow.xaml:26-103`: window resources; `ScrollViewer` at `:111`.
  - `src/TokenHound.App/UI/Windows/SettingsWindow.xaml:26-221`: window resources; tab `ScrollViewer`s at `:256` and `:288`.

## Work

- [x] T06.1 Add `DialogScrollBarStyle` (with vertical and horizontal templates, `Track` + `Thumb`, page buttons as transparent `RepeatButton`s) to `DialogResources.xaml`.
- [x] T06.2 Add `<Style TargetType="ScrollBar" BasedOn="{StaticResource DialogScrollBarStyle}" />` to the resources of `ProviderStatusWindow.xaml` and `SettingsWindow.xaml`.
- [x] T06.3 Build; run the full Infrastructure and Core test projects; `git diff --check`.
- [x] T06.4 On screen via Windows MCP: open the status window and Settings, make content overflow, and check the scrollbar's look, dragging, and wheel scrolling.

## Acceptance criteria

- `TokenHound.App` builds with 0 warnings. Infrastructure.Tests (828) and Core.Tests (92) still pass.
- A screenshot of each window with overflowing content shows the slim dark scrollbar, and scrolling works by drag and by wheel.
- No `.cs` file changes; other windows keep their current look.

## Verification

- Unit: none. The change is XAML resources only.
- Integration: none.
- E2E: omitted by .NET desktop policy.
- Manual: MA-08. Open the status window (narrowed so the rows overflow) and Settings (shortened so a tab overflows). Expected: a slim dark scrollbar, drag and wheel scrolling work. Run by the coordinator via Windows MCP; the user accepts at HIL 3.
- Environment dependency: the desktop session with Windows MCP. Running the repository build needs the user's go-ahead to close the installed instance, if it locks or conflicts.
- Commands:
  - `rtk dotnet build src/TokenHound.App/TokenHound.App.csproj --no-restore --nologo --verbosity:minimal --no-incremental`
  - `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests --no-build --no-restore -- --minimum-expected-tests 1`
  - `rtk dotnet test --project tests/TokenHound.Core.Tests --no-build --no-restore -- --minimum-expected-tests 1`
  - `git diff --check -- src`
- Expected evidence: exit codes 0, test counts, and a screenshot description per window.

## Affected files

- Modify: `src/TokenHound.App/UI/Styles/DialogResources.xaml`, `src/TokenHound.App/UI/Windows/ProviderStatusWindow.xaml`, `src/TokenHound.App/UI/Windows/SettingsWindow.xaml`
- Create: —

## Observability and recovery

- Operational signal: none.
- Recovery: revert the three files.

## Handoff

> Updated by `sdd-execute-corrections` during implementation.

- Produced result: `DialogResources.xaml` gains `DialogScrollBarStyle` plus two helper styles (`DialogScrollBarPageButtonStyle`, `DialogScrollBarThumbStyle`). The scrollbar is 8 px wide with 1 px padding, has no arrow buttons, a transparent track, and a thumb with corner radius 3: `SurfaceBorderBrush` #3F3F46 at rest, `ButtonPressedBackgroundBrush` #52525B on hover, and `TextMutedBrush` #71717A while dragging. A horizontal-orientation trigger swaps in an 8 px high template with the page-left/right commands. The style is keyed, so only windows that opt in change: `ProviderStatusWindow.xaml` and `SettingsWindow.xaml` each add `<Style TargetType="ScrollBar" BasedOn="{StaticResource DialogScrollBarStyle}" />` to their `Window.Resources`.
- Changed files: modified `src/TokenHound.App/UI/Styles/DialogResources.xaml` (+98, 375 lines), `src/TokenHound.App/UI/Windows/ProviderStatusWindow.xaml` (+2, 201 lines), and `src/TokenHound.App/UI/Windows/SettingsWindow.xaml` (+2). No `.cs`, test, Core, or Infrastructure file changed; all three files keep their existing line endings.
- Checks:
  - `rtk dotnet build src/TokenHound.App/TokenHound.App.csproj --no-restore --nologo --verbosity:minimal --no-incremental`: 0 errors, 0 warnings (XAML compiles).
  - `rtk dotnet build tests/TokenHound.Infrastructure.Tests/...csproj`: 0 warnings. Full `tests/TokenHound.Infrastructure.Tests`: 828 passed, exit 0. `tests/TokenHound.Core.Tests`: 92 passed, exit 0.
  - `git diff --check -- src`: clean. The quality profile's blocking commands have no `.cs` file to run on.
  - MA-08 (Windows MCP, primary display 3440×1440 at 100%). The user authorized closing the installed instance (PID 4060). The Debug build was launched, and both windows were opened from the HUD right-click menu:
    - Provider status window: the rows overflow at the default size, and the slim dark scrollbar shows on the right. Five wheel notches scrolled the content and moved the thumb down. Dragging the thumb up returned to the top. The screenshot samples the hovered thumb at about #54545D (downscaled blend of #52525B) and the track at #18181B (body).
    - Settings, Providers tab: the provider list overflows, and the same scrollbar shows. The wheel scrolled to `OpenCode`. The thumb at rest samples #3F3F46 and the track #18181B.
    - Also seen on screen: the `Claude Code` row showed `Claude OAuth API rate limit exceeded (HTTP 429)` in the alert colour. This is the T04 blocked-status visual (an open HIL 3 item).
    - The repository instance was then closed and the installed one relaunched (PID 22344).
- Validated state: git base `53da181` + T01–T05 working tree + this correction; Debug; net10.0-windows; Windows 11; 2026-09-27.
- Open items: the Settings `Cadence & Rate Limits` tab and a horizontal scrollbar were not exercised on screen; they use the same style. This session issued `codereview_4` and made this correction, so `codereview_5` must run in another session.
