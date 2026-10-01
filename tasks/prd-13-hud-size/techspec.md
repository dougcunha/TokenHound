# TechSpec — HUD size setting

## Sources and traceability

- PRD: `tasks/prd-13-hud-size/prd.md`
- Applicable instructions, rules, and skills: `CLAUDE.md` (HUD invariants, C# style), `dotnet-efficient-validation`, `repository-cli-efficiency`
- Specs and design: `docs/design/2026-08-28-usage-notch-design.md` (HUD and tooltip geometry; macOS-oriented, so only the card/tooltip structure applies). No provider, rate-limit, or credential spec is involved.
- Evidence in existing code: `NotchWindow.xaml:15-22` (`SizeToContent`, `MinWidth=180`, `MinHeight=48`), `NotchWindow.xaml:25` (root `Grid`), `NotchWindow.xaml:98-122` (`StatusPopup`), `NotchWindow.xaml.cs:130-133,238-251` (`OnSizeChanged` → `ApplyPlacement`), `ProviderRing.xaml:23-37` (tooltip hosts `TooltipCard`), `TooltipCard.xaml:36-45` (card root `Border`), `UserSettings.cs:11-37` (settings sections), `RefreshSettingsStore.cs` (store pattern), `StartupSettingsViewModel.cs` (Apply pattern), `SettingsWindow.xaml:255-339` (General tab), `SettingsWindow.xaml.cs:129-130` (selects Providers when `Startup` is null).

## Solution summary

The HUD size is a percentage (50–150, step 5, default 100) persisted in its own `HudSize` section of the user settings JSON. A shared observable `HudScale` holds the current factor. The HUD capsule root, the status popup, and the tooltip card each apply a `ScaleTransform` through `LayoutTransform` bound to that factor, so layout is recomputed at the scaled size (vector text and borders stay crisp) and `SizeToContent` shrinks or grows the window. A new `HudSizeSettingsViewModel` in the General tab edits the percentage with a slider and three presets, and applies it with the same Apply pattern as the other tabs: save, then set `HudScale.Percent`, which resizes the live HUD immediately.

The existing `OnSizeChanged` → `ApplyPlacement` path already re-centers an unmoved HUD and clamps a dragged one, so no placement code changes except scaling the window `MinWidth`/`MinHeight`.

## Technical decisions

| ID | PRD obligations | Decision | Reason and evidence | Alternatives and trade-offs |
| --- | --- | --- | --- | --- |
| DEC-01 | FR-01, FR-07 | New section `HudSize` (`HudSizeSettings` record with `int? Percent`) and `HudSizeStore` in `TokenHound.Infrastructure/Configuration`, registered as `UserSettings.HudSize` | `HudPositionStore.Save` writes a whole `HudPositionSettings { Left, Top }` (`NotchWindow.xaml.cs:256`); adding the percentage to it would be overwritten on every drag | Reuse the `Hud` section: rejected, the drag save would reset the size |
| DEC-02 | FR-01 | `HudSizeSettings` owns the bounds: `MINIMUM_PERCENT = 50`, `MAXIMUM_PERCENT = 150`, `STEP_PERCENT = 5`, `DEFAULT_PERCENT = 100`. `ResolvedPercent` clamps and rounds to the step; null or invalid reads as 100. `Factor` is `ResolvedPercent / 100.0` | Same shape as `RefreshSettings.Resolve` (`RefreshSettings.cs:40-47`); testable without WPF | Clamp in the ViewModel only: duplicates the rule between load and UI |
| DEC-03 | FR-03, FR-04, FR-05 | `HudScale` (App, `Presentation/HudScale.cs`): `INotifyPropertyChanged`, `Percent` (int), `Factor` (double), static `Current` instance used by XAML bindings. No WPF types, so tests can link it | ToolTip and Popup content live in separate visual trees and cannot inherit a transform from the HUD, but a `{Binding Source=...}` reaches a static instance from any tree | Attached property on the window: does not reach `ToolTip`; app resource dictionary: not observable without extra plumbing |
| DEC-04 | FR-04 | Scale with `LayoutTransform` (`ScaleTransform` bound to `HudScale.Current.Factor`) on the root `Grid` of `NotchWindow` | `LayoutTransform` re-measures at the scaled size, so `SizeToContent` and `ActualWidth` are correct and text is re-rendered as vector (NFR-03) | `RenderTransform`: the window size would not change; `Viewbox`: would need fixed sizing |
| DEC-05 | FR-05 | Apply the same `LayoutTransform` to the root `Border` of `TooltipCard` and to the `StatusPopup` content `Border` | Both host their own tree (`ProviderRing.xaml:23`, `NotchWindow.xaml:98`) | Leave them at 100%: fails FR-05 |
| DEC-06 | FR-04, FR-06 | `NotchWindow` subscribes to `HudScale.PropertyChanged` and sets `MinWidth = 180 * Factor` and `MinHeight = 48 * Factor`. `ApplyPlacement` is unchanged (it already runs on `SizeChanged`) | The fixed minimums (`NotchWindow.xaml:17-18`) would keep the capsule wider than its content at 50–75% | Remove the minimums: changes the current look at 100% |
| DEC-07 | FR-02, FR-03 | `HudSizeSettingsViewModel` (App/ViewModels), same shape as `StartupSettingsViewModel`: `Percent` (settable), `Label`, `IsDirty`, `CanApply`, `ApplyCommand`, `ApplyError`, `IsApplied`, `SetPreset(int)`. Presets: Small 75, Default 100, Large 125. Apply saves through `HudSizeStore`, then sets `HudScale.Percent`; on save failure it keeps the persisted baseline and sets `ApplyError` | Mirrors the General-tab pattern the user already has | Merge into `SettingsViewModel`: grows a file already at 179 lines |
| DEC-08 | FR-02, FR-03 | `SettingsViewModel` gets `public HudSizeSettingsViewModel? HudSize { get; init; }`; `App.CreateSettingsViewModel` fills it | Same optional-section pattern as `Updates` and `Startup` (`SettingsViewModel.cs:62-65`) | — |
| DEC-09 | FR-02 | The General tab becomes always visible: the "Startup" section hides when `Startup` is null, and `SettingsWindow.xaml.cs:129-130` no longer jumps to Providers. A "HUD size" section is added above it | Today the whole tab collapses when `Startup` is null (`SettingsWindow.xaml:258-264`) | A new tab: adds a tab for one control |
| DEC-10 | FR-03 | At startup `App` loads `HudSizeStore` and sets `HudScale.Current.Percent` before `_notchWindow.Show()` | The first layout must already use the stored factor, or the HUD flashes at 100% | Apply after show: visible flicker |
| DEC-11 | NFR-02 | No change to `WndProc`, `WindowStyles.EnableNonActivating`, or window flags | `NotchWindow.xaml.cs:95-117` | — |

## Components and flow

| ID | Component | New or modified | Responsibility | Dependencies |
| --- | --- | --- | --- | --- |
| CMP-01 | `Infrastructure/Configuration/HudSizeSettings.cs` | New | Persisted percentage, bounds, resolve and clamp | DEC-02 |
| CMP-02 | `Infrastructure/Configuration/HudSizeStore.cs` | New | Load and save the `HudSize` section via `SectionStore` | CMP-01 |
| CMP-03 | `Infrastructure/Configuration/UserSettings.cs` | Modified | Adds the `HudSize` section property | CMP-01 |
| CMP-04 | `App/Presentation/HudScale.cs` | New | Shared observable scale factor | DEC-03 |
| CMP-05 | `App/ViewModels/HudSizeSettingsViewModel.cs` | New | Slider and presets state, Apply | CMP-02, CMP-04 |
| CMP-06 | `App/ViewModels/SettingsViewModel.cs` | Modified | Exposes `HudSize` | CMP-05 |
| CMP-07 | `App/UI/Windows/NotchWindow.xaml` and `.xaml.cs` | Modified | Root `LayoutTransform`, popup transform, scaled minimums | CMP-04 |
| CMP-08 | `App/UI/Controls/TooltipCard.xaml` | Modified | Card `LayoutTransform` | CMP-04 |
| CMP-09 | `App/UI/Windows/SettingsWindow.xaml` and `.xaml.cs` | Modified | HUD size section, always-visible General tab | CMP-05 |
| CMP-10 | `App/App.xaml.cs` | Modified | Load the stored size before show, wire the ViewModel | CMP-02, CMP-04, CMP-05 |

Flow: startup → `HudSizeStore.Load()` → `HudScale.Current.Percent` → the HUD lays out at that factor. Settings → slider or preset → `HudSizeSettingsViewModel.Percent` → Apply → `HudSizeStore.Save` → `HudScale.Percent` → bindings re-layout the capsule, popup, and tooltip card → `SizeChanged` → `ApplyPlacement` re-centers or clamps.

## Contracts and data

New section in the user settings JSON. An absent section reads as default, and unknown sections are preserved by the existing extension-data mechanism.

```json
{ "HudSize": { "Percent": 75 } }
```

| Field | Type | Required | Validation |
| --- | --- | --- | --- |
| `Percent` | integer | no | Clamped to 50–150 and rounded to a multiple of 5 on read; invalid or missing → 100. Stored as written by the UI (already valid) |

No schema version change; older builds ignore the section.

## Integrations and interfaces

HUD only. No provider API, credential store, or database is touched.

## Errors, security, and recovery

- Errors and edges: a failed save keeps the previous size and shows `ApplyError` (so the UI stays consistent with the persisted state); a malformed `HudSize` section reads as 100 (existing `SectionStore` fallback).
- Credentials and sensitive data: none.
- Concurrency and idempotency: UI-thread only; applying the same value is a no-op (`CanApply` is false).
- Rollback or reversal: choose 100% and Apply, or delete the `HudSize` section.

## Sequencing

| Step | Depends on | Verifiable result |
| --- | --- | --- |
| T01 settings, store, `HudScale`, ViewModel, and tests | — | TC-01..TC-07 pass |
| T02 HUD, popup, tooltip scaling, Settings UI, and wiring | T01 | Build with 0 warnings; MA-1..MA-4 |

## Test approach

- Profile: C#/.NET, WPF desktop app (`TokenHound.App`, `UseWPF`); xUnit on Microsoft.Testing.Platform. App ViewModels and policies are tested from `tests/TokenHound.Infrastructure.Tests` through linked `<Compile Include>` entries (the new ViewModel and `HudScale` must be linked there). Commands per `CLAUDE.md`: `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests --no-build --no-restore -- --minimum-expected-tests 1`, with `--filter-class` for focused runs. Build the affected projects first.
- E2E: omitted by .NET desktop policy.
- Command prerequisites and exclusions: none beyond a prior build. Tests use a temp settings file, never the real LocalAppData file.
- Manual acceptance (owner: the human; launch through the Windows MCP `App` tool and verify on the primary monitor):
  - MA-1: Settings → General → HUD size. Pick Small, Apply: the HUD shrinks at once. Hover a ring and open the status popup: both are scaled. Repeat at 50% and 150%: no clipped or misaligned element, and the text stays readable.
  - MA-2: Unmoved HUD: change the size and it stays centered on the top edge. Drag it aside and change the size: it keeps its position and stays on screen.
  - MA-3: Restart the app: the size is kept. Choose Default and Apply: the HUD matches the current look.
  - MA-4: Click a ring: focus is not stolen from the foreground app. In a portable copy with no Startup section, the General tab still opens and shows HUD size.

| ID | Obligations | Level | Scenario | Expected result | Command or project |
| --- | --- | --- | --- | --- | --- |
| TC-01 | FR-01, DEC-02 | unit | `ResolvedPercent` for null, below 50, above 150, off-step (e.g. 77), valid | 100, 50, 150, 75, unchanged | `Infrastructure.Tests` `HudSizeSettingsTests` |
| TC-02 | FR-07, DEC-01 | unit | Save then load through a temp `UserSettingsFile`; absent section; other sections preserved | Round-trips; default when absent; others intact | `HudSizeStoreTests` |
| TC-03 | FR-07, DEC-01 | unit | Saving the HUD position after the size does not change `HudSize` | `Percent` unchanged | `HudSizeStoreTests` |
| TC-04 | FR-02, FR-03 | unit | ViewModel: load baseline, `SetPreset` (75/100/125), slider change updates `Label` | Percent and label match; `CanApply` follows the diff | `HudSizeSettingsViewModelTests` |
| TC-05 | FR-03 | unit | Apply saves and sets `HudScale.Percent`; no-op when unchanged | Store holds the value; scale updated; `IsApplied` true | `HudSizeSettingsViewModelTests` |
| TC-06 | FR-03 | unit | Failed save keeps the baseline and sets `ApplyError` | `HudScale` unchanged; error set | `HudSizeSettingsViewModelTests` |
| TC-07 | FR-04, DEC-03 | unit | `HudScale.Percent` change raises `PropertyChanged` for `Percent` and `Factor`; `Factor = Percent / 100` | Events and value | `HudScaleTests` |
| TC-08 | FR-06 | unit | `NotchPlacement.Clamp` and `CenterOnTopEdge` with a smaller width keep the box inside the bounds | Inside the bounds | existing `NotchPlacementTests` (add cases) |
| TC-09 | FR-04, FR-05, FR-06, NFR-02, NFR-03, NFR-04 | manual | MA-1..MA-4 | As scripted | Visual check and HIL 3 |

## Quality profile

| ID | Rule | Class | Verification command | Prior justification |
| --- | --- | --- | --- | --- |
| QA-01 | No `async void`, `.Result`, or `.Wait()` | blocking | `rtk rg -n --type cs 'async void\|\.Result\b\|\.Wait\(\)' <diff files>` | — |
| QA-02 | No empty `catch`, no `#pragma warning disable` | blocking | `rtk rg -n --type cs 'catch\s*\{\s*\}\|#pragma warning disable' <diff files>` | — |
| QA-03 | `TokenHound.Core` unchanged and free of UI/OS dependencies | blocking | `rtk git diff --stat -- src/TokenHound.Core` shows no change | — |
| QA-04 | Repository C# style (`CLAUDE.md`): one class per file, files ≤ 300 lines, methods ≤ 30 lines, XML docs on public members, `UPPER_CASE` constants | reservation | `rtk rg -c '^' <new and modified .cs files>` | — |
| QA-05 | Calls with 4+ arguments split across lines | reservation | `rtk rg -n --type cs '\w+\((?:[^),]+,){3,}[^)]*\)' <diff files>` | — |

- Verification scope: files in the task diff.
- Escalation trigger: 8+ reservation hits, a touched file above 500 lines, or duplication in 3+ places. `SettingsWindow.xaml` is already above 500 lines (821).

### Terrain baseline

| File | Lines | Public members | Constructor deps | Cases | Pre-existing hits | Destination |
| --- | --- | --- | --- | --- | --- | --- |
| `src/TokenHound.App/UI/Windows/NotchWindow.xaml.cs` | 294 | 2 | 0 | 0 | none (QA-01, QA-02) | recorded |
| `src/TokenHound.App/UI/Windows/SettingsWindow.xaml` | 821 | n/a (XAML) | n/a | 0 | file above 500 lines | recorded: the feature adds one section and moves one trigger (2 places), below the contact threshold |
| `src/TokenHound.App/UI/Windows/SettingsWindow.xaml.cs` | 184 | few | 0 | 0 | none | recorded |
| `src/TokenHound.App/ViewModels/SettingsViewModel.cs` | 179 | few | 5 | 0 | none | recorded |
| `src/TokenHound.App/App.xaml.cs` | 465 | many | 0 | 0 | none; under 500, and the feature adds about 6 lines | recorded |

- Preparatory refactoring: not recommended.

## Observability and rollout

- Signals: `Log.Debug` of the applied percentage; `Log.Warning` when the save fails (same style as `PersistPosition`).
- Migration and compatibility: none; the section is additive.
- Rollout and rollback: ships with the next build; rollback by choosing 100% or removing the section.

## Risks and open items

- Risk: a very small factor makes text unreadable (low probability, medium impact). Mitigation: the bounds start at 50 and are judged at MA-1; the bound is a single constant if it must rise to 60.
- Risk: `LayoutTransform` plus `AllowsTransparency` and the drop-shadow effect may soften edges at fractional scales (low probability, low impact). Mitigation: steps of 5% and `SnapsToDevicePixels` already on the capsule; judged at MA-1.
- Open item: none blocking.

## Relevant files

- Modify: `src/TokenHound.Infrastructure/Configuration/UserSettings.cs`, `src/TokenHound.App/ViewModels/SettingsViewModel.cs`, `src/TokenHound.App/UI/Windows/NotchWindow.xaml`, `src/TokenHound.App/UI/Windows/NotchWindow.xaml.cs`, `src/TokenHound.App/UI/Controls/TooltipCard.xaml`, `src/TokenHound.App/UI/Windows/SettingsWindow.xaml`, `src/TokenHound.App/UI/Windows/SettingsWindow.xaml.cs`, `src/TokenHound.App/App.xaml.cs`, `tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj`, `tests/TokenHound.Infrastructure.Tests/Placement/NotchPlacementTests.cs`
- Create: `src/TokenHound.Infrastructure/Configuration/HudSizeSettings.cs`, `src/TokenHound.Infrastructure/Configuration/HudSizeStore.cs`, `src/TokenHound.App/Presentation/HudScale.cs`, `src/TokenHound.App/ViewModels/HudSizeSettingsViewModel.cs`, and the tests `HudSizeSettingsTests.cs`, `HudSizeStoreTests.cs`, `HudScaleTests.cs`, `HudSizeSettingsViewModelTests.cs`
