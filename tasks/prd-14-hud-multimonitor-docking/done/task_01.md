# Stable execution context

Load in this exact order:

1. `tasks/prd-14-hud-multimonitor-docking/prd.md`
2. `tasks/prd-14-hud-multimonitor-docking/techspec.md`
3. This file

Use the current PRD and TechSpec versions already loaded; recover only missing or changed sources. This order does not guarantee a host cache hit.

---

# T01 — Persisted placement mode and display preference

## Outcome

The `Hud` settings section stores `Mode` and `Display` next to `Left`/`Top`, and `HudPositionSettings.ResolveMode()` returns the placement mode with the migration and fallback rules, so older files keep their position as Free and broken values fall back to Top Center with a warning.

## Dependencies and boundaries

- Depends on: —
- Unblocks: T02, T03
- In scope: `HudDockMode`, `HudDisplayPreference`, `HudPositionSettings.Mode`/`Display`/`ResolveMode()`, store round-trip and resolution tests.
- Out of scope: any App code, UI, placement math, persistence writes on load.

## Traceability

| Source | Section | Obligation covered |
| --- | --- | --- |
| FR-01, FR-14, FR-15, FR-16 | `prd.md#functional-requirements` | Six modes; default; migration; fallback |
| NFR-04 | `prd.md#non-functional-requirements` | Compatibility |
| DEC-01, DEC-02 | `techspec.md#technical-decisions` | String `Mode` + resolver; resolution table |
| CMP-01..CMP-03 | `techspec.md#components-and-flow` | New enum, record, extended settings |
| TC-01, TC-02 | `techspec.md#test-approach` | Resolution and round-trip tests |

## Context to recover on demand

- Applicable skills and rules: `AGENTS.md` (C# style, ≤ 300 lines per file, ≤ 30 per method), `dotnet-efficient-validation`, TechSpec quality profile QA-01..QA-07.
- Existing code: `src/TokenHound.Infrastructure/Configuration/HudPositionSettings.cs` — record to extend; `HudPositionStore.cs` — `SectionStore<HudPositionSettings>` on section `Hud`; `UserSettingsFile.cs:MergeWithDefaults` — copies `Hud` whole.
- Existing tests: `tests/TokenHound.Infrastructure.Tests/Configuration/HudPositionStoreTests.cs` — temp-file pattern to reuse.
- Contract: `techspec.md#contracts-and-data`.

## Work

- [x] T01.1 Create `HudDockMode` (`TopLeft, TopCenter, TopRight, LeftEdge, RightEdge, Free`) and `HudDisplayPreference` (`DevicePath`, `EdidKey`, `Name`, all `string?`) in `TokenHound.Infrastructure.Configuration`, with XML docs.
- [x] T01.2 Add `Mode` (`string?`) and `Display` (`HudDisplayPreference?`) to `HudPositionSettings`; add `ResolveMode()` returning `(HudDockMode Mode, string? Warning)` per DEC-02, parsing with `Enum.TryParse(..., ignoreCase: true)` and rejecting numeric strings.
- [x] T01.3 Add `HudPositionSettingsTests` (TC-01) and extend `HudPositionStoreTests` (TC-02: round-trip of `Mode` + `Display`; unknown `Mode` keeps `Left`/`Top`; saving `Hud` keeps `HudSize`).

## Acceptance criteria

- `{ "Left": 10, "Top": 0 }` resolves to Free; `{}` to TopCenter; `"rightedge"` to RightEdge; `"Diagonal"` and `"3"` to TopCenter with a non-null warning; `Free` without coordinates to TopCenter with a warning.
- A stored file with an unknown `Mode` still yields its `Left`/`Top` after `Load()`.
- No write happens during `Load()`.

## Verification

- Unit: TC-01, TC-02.
- Integration: store against a temp settings file (existing pattern).
- E2E: omitted by .NET desktop policy.
- Manual: none.
- Commands: `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-restore`; `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1` with `--filter-class "*HudPosition*"` while iterating, then unfiltered.
- Environment dependency: none.
- Expected evidence: build 0 warnings; filtered and full test counts.

## Affected files

- Create: `src/TokenHound.Infrastructure/Configuration/HudDockMode.cs`, `src/TokenHound.Infrastructure/Configuration/HudDisplayPreference.cs`, `tests/TokenHound.Infrastructure.Tests/Configuration/HudPositionSettingsTests.cs`
- Modify: `src/TokenHound.Infrastructure/Configuration/HudPositionSettings.cs`, `tests/TokenHound.Infrastructure.Tests/Configuration/HudPositionStoreTests.cs`

## Observability and recovery

- Operational signal: the warning string is logged by the caller (T05), not here.
- Recovery: additive fields; older builds ignore them.

## Handoff

> Updated by `sdd-execute-task` during implementation.

- Produced result: `HudDockMode` and `HudDisplayPreference` added; `HudPositionSettings` gained `Mode` (string) and `Display` plus `ResolveMode()` implementing DEC-02 (null mode → Free with coordinates or TopCenter; unknown, numeric, comma list, or empty → TopCenter + warning; Free without coordinates → TopCenter + warning). Matching uses `Enum.GetValues` names with `OrdinalIgnoreCase`, so numeric and flag-combination strings are rejected.
- Changed files: `src/TokenHound.Infrastructure/Configuration/HudDockMode.cs` (new), `HudDisplayPreference.cs` (new), `HudPositionSettings.cs`; `tests/TokenHound.Infrastructure.Tests/Configuration/HudPositionSettingsTests.cs` (new, TC-01), `HudPositionStoreTests.cs` (+4 tests, TC-02: round-trip of `Mode` + `Display`, unknown mode keeps coordinates, legacy file not rewritten on load, `HudSize` preserved).
- Checks: `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-restore` → 0 errors, 0 warnings; `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/... --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*HudPosition*"` → 27 passed; unfiltered → 999 passed, exit 0.
- Validated state: working tree at `c4b55b9` plus the T01 diff; Debug configuration; net10.0 test project.
- Quality profile: QA-01..QA-04, QA-07 clean. QA-05 hit at `HudPositionStoreTests.cs:241` is a false positive (commas inside a JSON string literal, two real arguments). QA-06: no file above 300 lines (largest `HudPositionStoreTests.cs`, 280).
- Open items: none. The `ResolveMode` warning is logged by the caller (T05).

### ADR candidates

None - direct TechSpec implementation or local decision.
