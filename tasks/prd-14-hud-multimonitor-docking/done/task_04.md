# Stable execution context

Load in this exact order:

1. `tasks/prd-14-hud-multimonitor-docking/prd.md`
2. `tasks/prd-14-hud-multimonitor-docking/techspec.md`
3. This file

Use the current PRD and TechSpec versions already loaded; recover only missing or changed sources. This order does not guarantee a host cache hit.

---

# T04 — Display catalog and pixel window move

## Outcome

`DisplayCatalog.GetDisplays()` returns every connected display with device path, EDID key, friendly name, number, primary flag, pixel work area, and resolution, falling back gracefully when the display configuration query fails; `WindowPlacement` gains a pixel bounds reader and a non-activating pixel move.

## Dependencies and boundaries

- Depends on: T02
- Unblocks: T05, T07
- In scope: P/Invoke declarations, the catalog, two `WindowPlacement` helpers, and one `App` startup call that logs the enumerated displays at debug level.
- Out of scope: using the catalog for placement (T05) or Settings (T07).

## Traceability

| Source | Section | Obligation covered |
| --- | --- | --- |
| FR-06, FR-09 | `prd.md#functional-requirements` | Display list data; stable identity |
| NFR-01, NFR-02, NFR-06 | `prd.md#non-functional-requirements` | No activation; pixel space; platform |
| DEC-03, DEC-05, DEC-06 | `techspec.md#technical-decisions` | Pixel move; identity; enumeration and fallback |
| CMP-08, CMP-09 | `techspec.md#components-and-flow` | Catalog; window helpers |
| Win32 APIs and flags | `techspec.md#integrations-and-interfaces` | Exact calls and flags |

## Context to recover on demand

- Applicable skills and rules: `AGENTS.md` (C# style, ≤ 300 lines per file, ≤ 30 per method), `dotnet-efficient-validation`, TechSpec quality profile QA-01..QA-07.
- Existing code: `src/TokenHound.App/Interop/WindowPlacement.cs` — P/Invoke style (`DllImport`, private structs, `MONITORINFO`); `src/TokenHound.App/Interop/WindowStyles.cs` — `SWP_*` constants and the non-activating rule.
- External: Microsoft Learn — `QueryDisplayConfig`, `DisplayConfigGetDeviceInfo`, `DISPLAYCONFIG_TARGET_DEVICE_NAME`, `DISPLAYCONFIG_SOURCE_DEVICE_NAME`, `MONITORINFOEXW`, `EnumDisplaySettingsW`.

## Work

- [x] T04.1 Create `DisplayCatalog.Native.cs` (partial; P/Invoke and structs with `CharSet.Unicode` and explicit sizes) and `DisplayCatalog.cs` (`GetDisplays()` joins monitors to display-config targets by GDI source name; `Number` from the trailing digits of `\\.\DISPLAYn`; never throws; logs failures once per call without empty catches).
- [x] T04.2 Add `WindowPlacement.GetWindowPixelBounds(IntPtr)` and `WindowPlacement.MoveWindowTo(IntPtr, int x, int y)` (`SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE`).
- [x] T04.3 In `App.xaml.cs` startup, call `DisplayCatalog.GetDisplays()` once so each display is logged at debug level.
- [x] T04.4 Build the App, run the full test suite, stop any running instance, launch through Windows MCP, and read the log.

## Acceptance criteria

- After launch, the log lists each connected display with number, name, a non-empty device path, and the primary flagged.
- When `QueryDisplayConfig` fails, displays still come back with GDI names and `Display N` names.
- No `SWP_SHOWWINDOW`, no activation call (QA-04).

## Verification

- Unit: none (interop); the pure consumers are tested in T02.
- Integration: launch the app and read the startup display log.
- E2E: omitted by .NET desktop policy.
- Manual: launch through Windows MCP `App` and check the log file.
- Commands: `rtk dotnet build src/TokenHound.App/TokenHound.App.csproj --no-restore`; `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-restore`; `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1`.
- Environment dependency: Windows MCP desktop access; stop the running HUD before building (MSB3027).
- Expected evidence: App build 0 warnings; full test count unchanged or higher; log excerpt with the displays; QA-01, QA-02, QA-04 greps empty on the diff.

## Affected files

- Create: `src/TokenHound.App/Interop/DisplayCatalog.cs`, `src/TokenHound.App/Interop/DisplayCatalog.Native.cs`
- Modify: `src/TokenHound.App/Interop/WindowPlacement.cs`, `src/TokenHound.App/App.xaml.cs`

## Observability and recovery

- Operational signal: `Log.Debug` per display (number, name, device path, primary); `Log.Warning` on query failure.
- Recovery: additive; only a startup log until T05.

## Handoff

> Updated by `sdd-execute-task` during implementation.

- Produced result: `DisplayCatalog` (instance class, partials `.Native.cs`, `.Targets.cs`): `GetDisplays()` joins `EnumDisplayMonitors`/`GetMonitorInfoW` to `QueryDisplayConfig` source→target names, reads the resolution with `EnumDisplaySettingsW`, falls back to GDI names and `Display N`, never throws, and logs the list (plus process DPI awareness) only when it changes. `WindowPlacement.GetWindowPixelBounds` and `MoveWindowTo` (`SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE`). `App` holds one `_displayCatalog` and calls it at startup.
- Changed files: `src/TokenHound.App/Interop/DisplayCatalog.cs`, `DisplayCatalog.Native.cs`, `DisplayCatalog.Targets.cs` (new); `src/TokenHound.App/Interop/WindowPlacement.cs`; `src/TokenHound.App/App.xaml.cs` (field, using, startup call).
- Checks: App build 0 warnings; test build 0 warnings; Infrastructure.Tests 1035 passed. Launched the dev build through Windows MCP (installed instance PID 22236 from `D:\Apps\TokenHound` stopped first; user `settings.json` backed up to the session scratchpad). Startup log: `Display list changed: 3 display(s), DPI awareness SystemAware`; Display 1 (no friendly name → "Display 1", `\?\DISPLAY#CMN1509…`), Display 2 `CX156A` primary, Display 3 `LG ULTRAWIDE` 3440x1440, each with a non-empty device path.
- Validated state: working tree at `c4b55b9` plus T01..T04.
- Finding: the process is **SystemAware** (no DPI manifest), so monitor rectangles of non-150% displays are virtualized by the system scale (Display 3 work area reported as 5160x2088 for 3440x1392). Window rectangles and `SetWindowPos` use the same virtualized space, so DEC-03 holds; Display labels use the real resolution from `EnumDisplaySettingsW`.
- Quality profile: QA-01..QA-04 clean. QA-05: P/Invoke signatures (`DisplayCatalog.Native.cs:17, 21`) are covered by the TechSpec justification; `MonitorEntry` positional record (`DisplayCatalog.cs`) is a declaration; the one 4-argument call was split. QA-06: `App.xaml.cs` 471 lines (baseline 468, +3 lines of wiring); others ≤ 268.
- Open items: none.

### ADR candidates

None - direct TechSpec implementation or local decision.
