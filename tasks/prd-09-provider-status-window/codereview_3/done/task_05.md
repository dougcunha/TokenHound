# Stable execution context

Load in this exact order:

1. `tasks/prd-09-provider-status-window/codereview_3/codereview.md`
2. This file

Use the already loaded report version; recover relevant contracts and code afterward. This order does not guarantee a host cache hit.

---

# T05 — Status window title bar and group header polish

## Outcome

The provider status window's title bar has the same colour as the window body. Each provider group header stands out through weight, an accent bar, and a separator, not a larger font. The account count shows as a small rounded badge.

## Dependencies and boundaries

- Depends on: — (T01–T04 are in `done/`)
- Unblocks: re-review `codereview_4` (in another session), then HIL 3
- In scope: DWM caption, text, and border colours for the status window; the group header template in `ProviderStatusWindow.xaml` (accent bar, bold name, count badge, separator).
- Out of scope: the account rows, quota columns, colours of bars and status texts, window size, and behaviour. Settings and About keep their current title bars. No Core, Infrastructure, or view-model change.

## Traceability

| Source | Section | Finding covered |
| --- | --- | --- |
| workflow DEC-10 | `workflow.md#DEC-10` | HIL 3 change request: title bar colour, group title prominence, count presentation |
| PRD NFR-04 | `prd.md#non-functional-requirements` | Dark panel following the reference; readable at 100% and 150% |
| TechSpec DEC-11 | `techspec.md#technical-decisions` | Standard activating WPF window using `DialogResources.xaml` brushes |

## Requirements

- On Windows 11, the native caption is painted `SurfaceBackgroundColor` (#18181B), with `TextPrimaryColor` (#F4F4F5) caption text and a border in the body colour. Move, resize, snap, and the native close button keep working.
- On systems without those DWM attributes (Windows 10), the immersive dark title bar is applied as it is for Settings and About. A failed DWM call is ignored and never throws.
- The group header keeps the 16 px family name, now `Bold`, preceded by a 3 px rounded blue accent bar. The count is a rounded badge (grey background, 11 px semibold secondary text) centred vertically next to the name. A 1 px `SurfaceBorderBrush` line under the header separates it from the rows.

## Context to recover on demand

- TechSpec: `techspec.md#technical-decisions` DEC-11.
- Rules and skills: `AGENTS.md` C# rules and HUD validation (Windows MCP `App` launch, `Screenshot` display [2]); `docs/design/` (dialog palette only; no HUD geometry change); `dotnet-efficient-validation`.
- Code:
  - `src/TokenHound.App/Interop/WindowPlacement.cs:EnableDarkMode`: existing DWM helper.
  - `src/TokenHound.App/UI/Windows/SettingsWindow.xaml.cs:OnSourceInitialized`: the pattern that applies it.
  - `src/TokenHound.App/UI/Windows/ProviderStatusWindow.xaml:104-115`: the group header.
  - `src/TokenHound.App/UI/Styles/DialogResources.xaml`: palette colours.

## Work

- [x] T05.1 Add a DWM caption-colour helper next to `EnableDarkMode` (caption, text, and border attributes 35, 36, 34, as COLORREF).
- [x] T05.2 Apply dark mode and the body-coloured caption in `ProviderStatusWindow` on `SourceInitialized`, reading the palette colours from resources.
- [x] T05.3 Restyle the group header: accent bar, bold name, count badge, separator.
- [x] T05.4 Build; run the full Infrastructure and Core test projects; run QA-01..QA-03 and QA-07 over the touched `.cs` files; check the window on screen via Windows MCP.

## Acceptance criteria

- `TokenHound.App` builds with 0 warnings. Infrastructure.Tests (828) and Core.Tests (92) still pass.
- A screenshot of the window shows the title bar in the body colour, and group headers with the accent bar, bold name, count badge, and separator, at the same 16 px size.
- Touched files stay ≤ 300 lines with methods ≤ 30 lines. No new blocking QA hit, and no new 4+ parameter method.

## Verification

- Unit: none. The change is XAML and Win32 interop; no view-model behaviour changes.
- Integration: none.
- E2E: omitted by .NET desktop policy.
- Manual: MA-07. Open the status window from the tray. Expected: the caption matches the body and the headers are as described. Run by the coordinator via Windows MCP; the user accepts at HIL 3.
- Environment dependency: the desktop session with Windows MCP. Running the repository build next to the installed instance needs the user's go-ahead if the installed instance must be closed.
- Commands:
  - `rtk dotnet build src/TokenHound.App/TokenHound.App.csproj --no-restore --nologo --verbosity:minimal`
  - `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-restore --nologo --verbosity:minimal`
  - `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests --no-build --no-restore -- --minimum-expected-tests 1`
  - `rtk dotnet test --project tests/TokenHound.Core.Tests --no-build --no-restore -- --minimum-expected-tests 1`
- Expected evidence: exit codes 0, test counts, QA output, and a screenshot description.

## Affected files

- Modify: `src/TokenHound.App/Interop/WindowPlacement.cs`, `src/TokenHound.App/UI/Windows/ProviderStatusWindow.xaml`, `src/TokenHound.App/UI/Windows/ProviderStatusWindow.xaml.cs`
- Create: —

## Observability and recovery

- Operational signal: none. The DWM result is ignored, as it is in `EnableDarkMode`.
- Recovery: revert the three files.

## Handoff

> Updated by `sdd-execute-corrections` during implementation.

- Produced result: `WindowPlacement.SetCaptionColors(hwnd, background, foreground)` sets the DWM caption (35) and border (34) attributes to the background colour and the text attribute (36) to the foreground, as COLORREF through a private `SetColorAttribute`. Like `EnableDarkMode`, it ignores the DWM result, so systems without these attributes keep their title bar. `ProviderStatusWindow` now handles `SourceInitialized`: it applies `EnableDarkMode` (the Windows 10 fallback), then `SetCaptionColors` with `SurfaceBackgroundColor` and `TextPrimaryColor` read from the resources. The group header is now a `Border` with a 1 px `SurfaceBorderBrush` bottom line and 8 px padding. Inside it: a 3 px rounded `GroupAccentBrush` (#60A5FA) bar, the 16 px `Bold` family name, and the count in a `CountBadgeStyle` badge (#27272A, corner radius 9, 11 px semibold `TextSecondaryBrush`), all centred vertically.
- Changed files: modified `src/TokenHound.App/Interop/WindowPlacement.cs` (+34, 204 lines), `src/TokenHound.App/UI/Windows/ProviderStatusWindow.xaml.cs` (37 lines), and `src/TokenHound.App/UI/Windows/ProviderStatusWindow.xaml` (199 lines). No test, Core, or Infrastructure file changed; all files are LF.
- Checks:
  - `rtk dotnet build src/TokenHound.App/TokenHound.App.csproj --no-restore --nologo --verbosity:minimal`: 0 errors, 0 warnings (XAML compiles).
  - `rtk dotnet build tests/TokenHound.Infrastructure.Tests/...csproj`: 0 warnings. Full `tests/TokenHound.Infrastructure.Tests`: 828 passed, exit 0. `tests/TokenHound.Core.Tests`: 92 passed, exit 0.
  - QA-01..QA-04 and QA-07 over the two touched `.cs` files: no hits. `git diff --check -- src`: clean. Every new method is ≤ 30 lines, and the 4-argument `DwmSetWindowAttribute` call is split one argument per line.
  - MA-07 (Windows MCP, primary display 3440×1440 at 100%): the user authorized closing the installed instance. The repository build was launched and the window opened from the HUD right-click `Provider Status`. The screenshot pixel at the caption and at the body both read RGB 24,24,27 (#18181B). The caption text is light and the native minimize/maximize/close buttons are present. Headers show the blue accent bar, the bold name at the same size, the rounded count badge (`1`, `2`, `1`, `1`), and the separator line. The repository instance was then closed and the installed one relaunched (PID 4060).
- Validated state: git base `53da181` + T01–T04 working tree + this correction; Debug; net10.0-windows; Windows 11; 2026-09-27.
- Open items: the Windows 10 fallback (dark title bar only) is not verified; no Windows 10 machine was available. Observation outside this task's scope: the vertical scrollbar uses the default light WPF style, which stands out on the dark body. It is not part of DEC-10 and is left for the user to decide at HIL 3. This session issued `codereview_3` and made this correction, so `codereview_4` must run in another session.
