# PRD — Refactoring settings stores onto one section store

## Context and motivation

The 2026-09-12 analysis (AA-09) found four settings stores (`HudPositionStore`, `ProviderSettingsStore`, `RateLimitSettingsStore`, `RefreshSettingsStore`) that repeat the same triple-constructor pattern, the same `JsonDocumentOptions`/`JsonSerializerOptions` blocks, the same `FilePath` passthrough, and the same `CreateSettingsFile` override resolution. Only the section name and payload type differ. A change to path resolution or JSON options must currently be made in four files.

## Scope

- Target: the four `Configuration/*SettingsStore` classes and their shared plumbing.
- Allowed structural change: introduce one generic internal section store; keep the four public classes as thin typed facades with identical signatures. JSON section names, payload types, and file behavior are unchanged.
- Out of scope: `UserSettingsFile` file I/O semantics; `SettingsPathResolver` policy (may be reused, not redesigned); the settings UI.

## Behaviors to preserve

| ID | Observable behavior | Source and evidence | Verification |
| --- | --- | --- | --- |
| R-01 | `HudPositionStore` Load/LoadAsync/Save/SaveAsync and `FilePath` behave as today. | `HudPositionStoreTests.cs` | store tests pass |
| R-02 | `ProviderSettingsStore` behavior incl. its extra JSON converter is unchanged. | `ProviderSettingsStoreTests.cs`; `ProviderSettingsStore.cs:13-62` | store tests pass |
| R-03 | `RateLimitSettingsStore` behavior is unchanged. | `RateLimitSettingsStoreTests.cs`; `RateLimitSettingsStore.cs:12-67` | store tests pass |
| R-04 | `RefreshSettingsStore` behavior is unchanged. | `RefreshSettingsStoreTests.cs`; `RefreshSettingsStore.cs:12-148` | store tests pass |
| R-05 | Static `FromJson` returns an empty instance when the section is absent; missing/whitespace input is handled identically. | `RefreshSettingsStore.cs:74-87` | store tests |
| R-06 | `Save`/`SaveAsync` preserve every other section via `UserSettingsFile.Update`. | `RefreshSettingsStore.cs:114-136` | `UserSettingsFileTests`, concurrency tests |
| R-07 | Constructor overloads `()`, `(UserSettingsFile)`, `(filePath, baseDirectory)` resolve the same paths. | `SettingsPathResolver.ResolveOverride` usage in each store | store tests |

## Constraints

- `UserSettingsFile` remains the single write coordinator; no new independent writers.
- Public API of the four stores is unchanged (callers compile without edits).
- Section names and JSON shape must not change on disk.

## Acceptance criteria

- [x] All `R-NN` items were checked after refactoring. Evidence: `codereview_2/codereview.md` covers R-01 through R-07 after the four prior findings were corrected.
- [x] No new behavior entered the scope silently. Evidence: `codereview_2/codereview.md` bounds the implementation by `1deb228..633dd21` and records no new blocking hit.
- [x] Settings + concurrency tests pass with `--minimum-expected-tests 1`. Evidence: the current Infrastructure test run passed 678 tests with zero failures.
- [x] Duplicated constructor/options/path-resolution blocks collapse to one. Evidence: `codereview_2/codereview.md` records QA-01/QA-02/QA-03 counts of 2/1/1 against baselines 8/4/4.

## Assumptions and open items

- Assumption: the four public facades remain the call-site contract; introducing `SectionStore<T>` is internal.
