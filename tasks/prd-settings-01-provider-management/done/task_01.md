# Stable execution context

Load in this exact order:

1. `tasks/prd-settings-01-provider-management/prd.md`
2. `tasks/prd-settings-01-provider-management/techspec.md`
3. This file

Use the current PRD and TechSpec versions already loaded; recover only missing or changed sources. This order does not guarantee a host cache hit.

---

# T01 — Persist provider enablement in `appsettings.json` without losing sibling sections

## Outcome

A `ProviderSettingsStore` reads and writes a `"Providers"` section of `appsettings.json`, leaving `Hud`, `Refresh`, and `Log` intact. Every absent file, section, key, or malformed value resolves to *enabled*. Concurrent writes from the HUD position store and this store no longer overwrite each other.

## Dependencies and boundaries

- Depends on: —
- Unblocks: T05, T07
- In scope: settings record, section store, shared write gate, and the `HudPositionStore.Save` change that takes the gate.
- Out of scope: engine gating (T02), any view model, any UI. Nothing reads this store yet — T07 wires it into startup.

## Traceability

| Source | Section | Obligation covered |
| --- | --- | --- |
| FR-06 | `prd.md#functional-requirements` | Persist enabled/disabled under `"Providers"`, preserving `Hud`, `Refresh`, `Log` and valid JSON |
| FR-07 | `prd.md#functional-requirements` | Missing file, section, or key defaults to enabled |
| OBJ-04 | `prd.md#outcomes-and-metrics` | Toggles survive restart |
| NFR-05 | `prd.md#non-functional-requirements` | Configuration models and persistence live in `TokenHound.Infrastructure`; Core untouched |
| A-01 | `prd.md#explicit-assumptions` | `{"Providers": {"<id>": {"Enabled": bool}}}` shape |
| DEC-01 | `techspec.md#technical-decisions` | `JsonNode` DOM section store mirroring `HudPositionStore` |
| DEC-02 | `techspec.md#technical-decisions` | Canonical key is the runtime `ProviderId`; `"antigravity"` is a read alias for `"gemini"` |
| DEC-07 | `techspec.md#technical-decisions` | Shared per-path gate serializing writers of `appsettings.json` |
| CMP-01 – CMP-04 | `techspec.md#components-and-flow` | `ProviderSettings`, `ProviderSettingsStore`, `SettingsFileGate`, `HudPositionStore` change |

## Context to recover on demand

- Applicable skills: `dotnet-efficient-validation` (before any build or test), `repository-cli-efficiency`, `no-workarounds`
- Existing code: `src/TokenHound.Infrastructure/Configuration/HudPositionStore.cs` — copy its `ReadRoot`, `ResolveFilePath`, `JSON_OPTIONS`/`DOCUMENT_OPTIONS`, and swallow-and-default posture; it is the pattern the PRD names
- Existing code: `src/TokenHound.Infrastructure/Configuration/RefreshSettings.cs` — `record` with `init` and resolved fallbacks
- Existing code: `tests/TokenHound.Infrastructure.Tests/Configuration/HudPositionStoreTests.cs` — temp-directory fixture and `IDisposable` cleanup to reuse
- Contract or integration: `techspec.md#contracts-and-data` — full field table, alias rules, and the "both keys present ⇒ `gemini` wins" case

## Work

- [x] T01.1 Create `ProviderSettings` record with an `IReadOnlyDictionary<string, bool>` and `IsEnabled(providerId)` defaulting to `true`, using `StringComparer.OrdinalIgnoreCase`.
- [x] T01.2 Create `SettingsFileGate` holding a `SemaphoreSlim` per resolved absolute path.
- [x] T01.3 Create `ProviderSettingsStore` with `Load()` and `SaveAsync(ProviderSettings, CancellationToken)`, writing only the `"Providers"` node through a `JsonNode` DOM and preserving unknown provider keys.
- [x] T01.4 Normalize the `"antigravity"` alias to `"gemini"` on read; emit only `"gemini"` on write; `"gemini"` wins when both keys are present.
- [x] T01.5 Take the gate in `ProviderSettingsStore.SaveAsync` and in `HudPositionStore.Save`.
- [x] T01.6 Add `ProviderSettingsStoreTests` covering TC-01 – TC-04.

## Acceptance criteria

- Saving a `Providers` map into a file already holding `Hud`, `Refresh`, and `Log` leaves all three structurally intact and the file valid, indented JSON.
- No file, no `"Providers"` section, an absent key, a non-boolean `Enabled`, and a corrupt file each report *enabled* and throw nothing.
- A file written with `"antigravity"` loads as `gemini`; the next save emits `"gemini"` and no `"antigravity"`.
- Provider keys present in the file but unknown to this build survive a save.
- A `SaveAsync` failure returns `false` and logs with structured Serilog arguments; it never throws to the caller.
- `TokenHound.Core` gains no reference and no new member.

## Verification

- Unit: TC-01 section preservation and round trip; TC-02 the three default paths; TC-03 alias read and canonical rewrite; TC-04 corrupt file and non-boolean value.
- Integration: none — this task has no cross-process or engine boundary. File I/O runs against a real temp directory rather than a double, so the JSON DOM semantics are genuinely exercised.
- E2E: omitted by .NET desktop policy.
- Manual: none.
- Commands:
  - `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-restore --nologo --verbosity:minimal`
  - `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*ProviderSettingsStoreTests*"`
  - Full project before handoff: `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1`
- Environment dependency: none. No network, no credentials, no provider. Tests write to a fresh `Path.GetTempPath()` directory.
- Expected evidence: non-zero executed test count with exit code 0; `HudPositionStoreTests` still green after the T01.5 change.

## Affected files

- Create: `src/TokenHound.Infrastructure/Configuration/ProviderSettings.cs`
- Create: `src/TokenHound.Infrastructure/Configuration/ProviderSettingsStore.cs`
- Create: `src/TokenHound.Infrastructure/Configuration/SettingsFileGate.cs`
- Create: `tests/TokenHound.Infrastructure.Tests/Configuration/ProviderSettingsStoreTests.cs`
- Modify: `src/TokenHound.Infrastructure/Configuration/HudPositionStore.cs`

## Observability and recovery

- Operational signal: structured Serilog warning on `SaveAsync` failure, carrying the resolved file path.
- Recovery: deleting the `"Providers"` section restores all-enabled behavior. The change is additive; no migration and no schema version.

## Handoff

- Produced result: `ProviderSettingsStore` reads and writes only the `"Providers"` object of `appsettings.json` through a `JsonNode` DOM, mirroring `HudPositionStore`. `ProviderSettings` is a `record` whose `EnabledStates` map is normalized to `StringComparer.OrdinalIgnoreCase` on `init`, with `IsEnabled(providerId)` defaulting to `true`. Absent file, absent section, absent key, non-boolean/`null`/non-object `Enabled`, and corrupt JSON all resolve to *enabled* and throw nothing. `"antigravity"` is read as `gemini` (canonical wins when both keys are present, in either document order) and the alias key is dropped **as part of writing the canonical `gemini` entry** — a save that carries no `gemini` id leaves a stored `"antigravity"` preference untouched. Save merges into the existing `"Providers"` node, so provider keys unknown to this build survive. `SaveAsync` returns `Task<bool>`, logs `Log.Warning(ex, "Failed to persist provider enablement to {SettingsFilePath}", _filePath)` on failure, and never throws. `SettingsFileGate` is an `internal static` per-path `SemaphoreSlim` registry (`Acquire`/`AcquireAsync`, keyed by `Path.GetFullPath`, `OrdinalIgnoreCase`) taken by both `ProviderSettingsStore.SaveAsync` and `HudPositionStore.Save`, so the two writers of `appsettings.json` no longer lose each other's section. `TokenHound.Core` was not touched.
- Changed files:
  - Created `src/TokenHound.Infrastructure/Configuration/ProviderSettings.cs` (58 lines)
  - Created `src/TokenHound.Infrastructure/Configuration/ProviderSettingsStore.cs` (252 lines)
  - Created `src/TokenHound.Infrastructure/Configuration/SettingsFileGate.cs` (93 lines)
  - Created `tests/TokenHound.Infrastructure.Tests/Configuration/ProviderSettingsStoreTests.cs` (221 lines)
  - Modified `src/TokenHound.Infrastructure/Configuration/HudPositionStore.cs` (154 lines) — `Save` now opens with `using var gate = SettingsFileGate.Acquire(_filePath);` before its read-modify-write
- Review correction applied (round 2): `ApplyStates` previously called `section.Remove(ALIAS_PROVIDER_KEY)` unconditionally before its write loop, so a save whose map carried no `gemini` id deleted a stored `"antigravity": { "Enabled": false }` and silently reverted that provider to *enabled* on the next `Load`. Root cause: the alias removal was decoupled from the canonical rewrite that justifies it (DEC-02 authorizes dropping the alias as part of writing `gemini`, not as an unconditional delete). Fixed at the source by moving the removal into `WriteEnabled`, guarded by `string.Equals(providerId, CANONICAL_PROVIDER_KEY, StringComparison.OrdinalIgnoreCase)`, so the alias is dropped only in the same operation that emits the canonical value. `Load` normalization, canonical-wins-in-both-orders, and the existing rewrite test are unchanged and still green. Pinned by the new regression test `SaveAsync_WhenCanonicalKeyIsNotSupplied_KeepsStoredAliasPreference`.
- Checks (re-run after the review correction):
  - `rtk dotnet restore tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --nologo --verbosity:minimal` -> 3 projects, 0 errors, 0 warnings; exit 0. Required because this worktree had no `project.assets.json`; the first `--no-restore` build failed with `NETSDK1004`. Not repeated in round 2 — no package reference changed.
  - `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-restore --nologo --verbosity:minimal` -> 3 projects, 0 errors, 0 warnings; exit 0.
  - `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*ProviderSettingsStoreTests*"` -> **16 tests passed**; exit 0.
  - `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1` (full-project regression) -> **373 tests passed**; exit 0.
  - `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*HudPositionStoreTests*"` -> **7 tests passed**; exit 0 (round 1) — confirms T01.5 caused no regression; still covered by the 373-test round-2 run.
  - Coverage map: TC-01 `SaveAsync_PreservesSiblingSectionsAndRoundTrips` (Hud/Refresh/Log intact + round trip); TC-02 `Load_WhenFileMissing_ReportsEnabled`, `Load_WhenProvidersSectionMissing_ReportsEnabled`, `Load_WhenProviderKeyAbsent_ReportsEnabled`; TC-03 `Load_WhenAliasKeyStored_ResolvesAsCanonicalAndRewritesIt` plus `Load_WhenBothAliasAndCanonicalKeysExist_CanonicalWins` (both key orders); TC-04 `Load_WhenFileIsCorrupt_ReportsEnabled` and `Load_WhenEnabledIsNotBoolean_ReportsEnabled` (5 malformed shapes). Extra: `SaveAsync_PreservesProviderKeysUnknownToThisBuild`, `SaveAsync_WhenCanonicalKeyIsNotSupplied_KeepsStoredAliasPreference`, `Load_MatchesProviderKeysCaseInsensitively`.
- Validated state: worktree `settings` at `9699f06` plus this change; .NET SDK 10.0.400 as pinned by `global.json`; native MTP route via `dotnet test --project`; `Debug` configuration, `net10.0`. Projects compiled: `TokenHound.Core`, `TokenHound.Infrastructure`, `TokenHound.Infrastructure.Tests`. `TokenHound.App` was not built — T01 touches no App file and adds no public signature it consumes. No commit, no push, no move to `done/`, `tasks.md` untouched.
- Open items:
  - Nothing reads `ProviderSettingsStore` yet; T07 wires it into `App.OnStartup` and T05 supplies the map from the view model. `SettingsFileGate` is `internal`, so it is not directly unit tested; its effect is covered indirectly by `HudPositionStoreTests` staying green and by `ProviderSettingsStoreTests` exercising `SaveAsync` through the async gate. Reviewed and accepted.
  - `SaveAsync` over a *corrupt* `appsettings.json` returns `false` and logs rather than overwriting the file, because `ReadRootAsync` reuses `HudPositionStore`'s parse-then-throw posture. Preserving an unparseable user file was judged safer than clobbering it; `Load` still degrades to all-enabled. Reviewed and accepted; no acceptance criterion is left open by it.
  - E2E omitted by the .NET desktop policy recorded in the TechSpec; no manual script belongs to T01.

### ADR candidates

None - direct TechSpec implementation or local decision. DEC-01, DEC-02, and DEC-07 already carry the durable decisions (DOM section store, `gemini` canonical key with `antigravity` read alias, shared per-path write gate) with their alternatives and trade-offs; this task introduced no decision beyond them.
