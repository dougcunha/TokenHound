# Stable execution context

Load in this exact order:

1. `tasks/prd-17-hud-backdrop-effects/prd.md`
2. `tasks/prd-17-hud-backdrop-effects/techspec.md`
3. This file

Use the current PRD and TechSpec versions already loaded; recover only missing or changed sources. This order does not guarantee a host cache hit.

---

# T02 — Ship the Acrylic capsule with toggle and fallback

## Outcome

With the effect enabled and available, the HUD capsule shows Acrylic inside its contour in every docking mode, with PRD 16 input and focus behavior unchanged. A Settings toggle (default on) and Windows availability changes switch live to the current solid fill.

## Dependencies and boundaries

- Depends on: T01 verdict, and the exception HIL decision when T01 raised one.
- Unblocks: visual check, independent review, HIL 3.
- In scope: CMP-01..08 for the selected candidate, unit tests, manual matrix, README/ROADMAP/ARCHITECTURE sync, `graft build`.
- Out of scope: Mica; popup, tooltip, and Settings backdrops; animations; light theme.

## Traceability

| Source | Section | Obligation covered |
| --- | --- | --- |
| FR-01..07 | `prd.md#functional-requirements` | Material, clip, click-through, toggle, fallback, live change, sync |
| NFR-01..05 | `prd.md#non-functional-requirements` | Focus, contrast, idle cost, fail closed, boundaries |
| DEC-01, DEC-04..08, CMP-01..08, TC-01..03, TC-05..08 | `techspec.md` | Implementation and verification |

## Context to recover on demand

- Applicable skills and rules: `dotnet-efficient-validation`, `repository-cli-efficiency`, AGENTS.md C# style and HUD invariants.
- Existing code: `HudContourController.cs` (frame sync and shadow companion), `NotchWindow.xaml:29` (fill), `HudSizeStore.cs` and `HudSizeSettingsCard.xaml` (setting pattern), `UserSettingsFile.cs:182` (merge).
- T01 handoff and the `validation.md` verdict.

## Work

- [x] T02.1 Add `HudBackdropSettings` and `HudBackdropStore`, carry the section through `MergeWithDefaults`, and add TC-02 tests.
- [x] T02.2 Add the pure `HudBackdropPolicy` and TC-01 tests.
- [x] T02.3 Prove the hand-written WinRT ABI interop (DEC-08) and a polygon region from a contour with inverse joins in the scratch spike, without the projection; if either fails, stop at an exception HIL with evidence (workflow DEC-05). Then add `HudBackdropInterop` (CMP-02) and the raw-HWND `HudBackdropWindow` (CMP-03) with the contour region, failing closed to solid; add TC-03 style tests.
- [x] T02.4 Extend `HudContourController` (or a sibling extracted per DEC-05) to create, sync, order, show/hide, and dispose the companion; switch the capsule fill in the same pass; wire availability messages.
- [x] T02.5 Add the Settings General card and view model; apply immediately.
- [x] T02.6 Build, run the full Infrastructure suite, execute TC-05..08, and record results in `validation.md`.
- [x] T02.7 Sync README, ROADMAP, and ARCHITECTURE; run scoped quality checks and `graft build`; write the handoff for the visual check.

## Acceptance criteria

- TC-01..03 pass in the full suite with a nonzero executed count.
- TC-05..08 are recorded with screenshots and measurements; any missing essential case stays pending.
- An interop failure or unsupported system shows the current solid capsule with one warning log.

## Verification

- Unit: TC-01, TC-02, TC-03.
- Integration: full Infrastructure MTP run.
- E2E: omitted by .NET desktop policy.
- Manual: TC-05..08 by the coordinator through Windows MCP; human visual check before review.
- Commands: TechSpec build route, then `rtk dotnet run --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj -c Release --no-build --no-restore -- --minimum-expected-tests 1`.
- Environment dependency: user desktop released; three mixed-DPI monitors.
- Expected evidence: MTP summary, manual tables, screenshots, contrast ratios, idle measurements.

## Affected files

- Create and modify: TechSpec "Relevant files".

## Observability and recovery

- Operational signal: structured log per material/solid transition; warning on interop failure.
- Recovery: turn the setting off, or revert the commit; older builds ignore the `HudBackdrop` section.

## Handoff

> Updated by `sdd-execute-task` during implementation.

- Produced result: Acrylic HUD capsule (candidate A, hand-written WinRT ABI, contour polygon region) with the Settings > General "Translucent background" toggle (default on, applies immediately) and a live solid fallback for the setting, transparency effects, energy saver, high contrast, and pre-22000 builds. The tint is 0xED (93%, workflow DEC-07).
- Changed files: new `HudBackdropSettings.cs`, `HudBackdropStore.cs` (Infrastructure); `HudBackdropMode.cs`, `HudBackdropInputs.cs`, `HudBackdropPolicy.cs`, `HudBackdropPreference.cs`, `HudBackdropSettingsViewModel.cs`, `HudBackdropInterop.cs`, `HudBackdropComposition.cs`, `HudBackdropWindow.cs`, `HudBackdropAvailability.cs`, `HudBackdropController.cs`, `HudBackdropSettingsCard.xaml(.cs)` (App); modified `UserSettings.cs`, `UserSettingsFile.cs`, `HudContourController.cs`, `HudContourDecorator.cs`, `ProviderRing.xaml`, `SettingsViewModel.cs`, `SettingsWindow.xaml`, `App.xaml.cs`, test `.csproj` links; 4 new test files; README, ROADMAP, ARCHITECTURE.
- Deviations within the contract: (1) the companion sync lives in sibling `HudBackdropController` (DEC-05 extraction) and `HudContourController` only calls it; (2) root-cause fix in `HudContourDecorator`: `Background` was registered with `AddOwner` without metadata, so it lacked `AffectsRender` and runtime fill swaps never repainted; (3) `ProviderRing` backing disc `#18181B` → `Transparent` (invisible on the solid fill, needed so the material shows inside the rings; hit testing kept); (4) energy saver is read from `GUID_ENERGY_SAVER_STATUS` / `GUID_POWER_SAVING_STATUS` notifications because `GetSystemPowerStatus` stays 0 for energy saver on AC power.
- Checks: `rtk dotnet build` App and Infrastructure tests (Release, 0 warnings); `rtk dotnet run --project tests/TokenHound.Infrastructure.Tests/... -- --minimum-expected-tests 1` → 1101 passed, 0 failed (19 new HudBackdrop tests: TC-01, TC-02, TC-03, FR-04 VM). Quality profile QA-01..03 clean; QA-04/05 notes below. Manual TC-05..08 in `validation.md#t02`.
- Validated state: Git base `2bc32ef` plus the working-tree diff above; Windows 10.0.26200; transparency temporarily on (DEC-06).
- Quality reservations: `HudBackdropInterop.cs` 228 lines and `HudBackdropComposition.cs` 232 lines (below 300); `App.xaml.cs` 462 lines (pre-existing 459 above 300, +3 lines); latent: `HudContourDecorator.BorderBrush` has the same `AddOwner`-without-metadata defect as `Background` but is never changed at runtime, left as is; `HudContourController.cs` 227 lines.
- Open items: TC-05 cases not run by the coordinator (Top left, Top right, Right edge, sizes 50/150 %, the 150 % primary monitor, which was running another automation session) are folded into the visual check script; idle CPU baseline ~13 % of one core exists with and without the material (pre-existing, not caused by PRD 17); README still states ~30-45 MB idle RAM while the working set measured ~182 MB (pre-existing).

### ADR candidates

- T02-ADR-01 — Hand-written WinRT ABI for Windows.UI.Composition. Context: the projection adds ≈25.4 MB to a 7.1 MB single-file app. Decision: call the needed `ICompositor`/`IVisual`/`ICompositionTarget` slots through `Marshal.GetDelegateForFunctionPointer`, with slots and IIDs taken from `Windows.UI.winmd`. Alternatives: versioned TFM; Win2D. Consequences: no size cost; a fixed slot table to keep correct; every failure falls back to the solid fill. Evidence: `validation.md#t023-interop-and-region-proof`; workflow DEC-05.
