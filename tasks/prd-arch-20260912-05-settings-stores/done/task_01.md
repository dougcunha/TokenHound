# Stable execution context

Load in this exact order:

1. `tasks/prd-arch-20260912-05-settings-stores/prd.md`
2. `tasks/prd-arch-20260912-05-settings-stores/techspec.md`
3. This file

Use the current PRD and TechSpec versions already loaded; recover only missing or changed sources. This order does not guarantee a host cache hit.

---

# T01 — Add `SectionStore<T>` and migrate Hud/Refresh

## Outcome

A single internal `SectionStore<T>` owns the shared options, path resolution, and load/save plumbing; `HudPositionStore` and `RefreshSettingsStore` delegate to it with unchanged public behavior.

## Dependencies and boundaries

- Depends on: —
- Unblocks: T02
- In scope: create `SectionStore<T>`; migrate `HudPositionStore` and `RefreshSettingsStore` as thin facades.
- Out of scope: `ProviderSettingsStore` converter and `RateLimitSettingsStore` (T02); `UserSettingsFile` I/O semantics.

## Traceability

| Source | Section | Obligation covered |
| --- | --- | --- |
| R-01, R-04, R-05, R-06, R-07 | `prd.md#behaviors-to-preserve` | Hud/Refresh behavior and paths preserved |
| DEC-01, DEC-02, DEC-03 | `techspec.md#technical-decisions` | Section store + facades |
| QA-01..QA-03 | `techspec.md#quality-profile` | Shared block counts reduce |

## Context to recover on demand

- Applicable skills: `dotnet-efficient-validation`, `repository-cli-efficiency`.
- Existing code: `RefreshSettingsStore.cs:12-148`, `HudPositionStore.cs`, `SettingsPathResolver.ResolveOverride`, `UserSettingsFile.Update`.
- Contract or integration: TechSpec `#technical-decisions`.

## Work

- [x] T01.1 Add internal `SectionStore<T>` that owns the section name and shared serializer options, exposing `FromJson`/`Load`/`LoadAsync`/`Save`/`SaveAsync`/`FilePath`.
- [x] T01.2 Make `HudPositionStore` a thin facade over `SectionStore<HudPositionSettings>`, preserving the exact public constructor surface and XML docs.
- [x] T01.3 Make `RefreshSettingsStore` a thin facade over `SectionStore<RefreshSettings>`.
- [x] T01.4 Remove duplicated options/ctor/path-resolution blocks from the two stores.

## Acceptance criteria

- [x] `HudPositionStore` and `RefreshSettingsStore` public APIs compile unchanged (`(string? filePath, string? baseDirectory = null)` surface retained).
- [x] Missing-section `FromJson` returns an empty instance as before.
- [x] On-disk section names and JSON shape unchanged.
- [x] Other sections survive Save/SaveAsync.

## Verification

- Unit: `HudPositionStoreTests`, `RefreshSettingsStoreTests` include missing-section, round-trip, and override-path cases.
- Integration: `UserSettingsFileTests` (section preservation).
- E2E: omitted by .NET desktop policy.
- Manual: none.
- Commands: focused MTP classes `*HudPositionStoreTests*`, `*RefreshSettingsStoreTests*`, `*UserSettingsFileTests*` with `--minimum-expected-tests 1`.
- Environment dependency: none.
- Expected evidence: pass counts plus reduced duplicated-block counts.

## Affected files

- Create: `src/TokenHound.Infrastructure/Configuration/SectionStore.cs` (name at implementation's discretion)
- Modify: `src/TokenHound.Infrastructure/Configuration/HudPositionStore.cs`
- Modify: `src/TokenHound.Infrastructure/Configuration/RefreshSettingsStore.cs`

## Observability and recovery

- Operational signal: unchanged `FilePath` and settings JSON.
- Recovery: `git revert`.

## Handoff

> Updated by `sdd-execute-task` during implementation (rework after `codereview_1/codereview.md`).

- Produced result: Added internal `SectionStore<T>` that owns the section name (`_sectionName`, supplied once via the constructor), the single shared `JsonSerializerOptions` (trailing commas + comments tolerated, accessed only through that one options instance), path resolution via `SettingsPathResolver.ResolveOverride` (default `appsettings.json`), the `CreateSettingsFile` helper, and the instance operations `FromJson(string)`/`Load`/`LoadAsync`/`Save`/`SaveAsync`/`FilePath`. `HudPositionStore` and `RefreshSettingsStore` are thin facades: no section-name constant remains in either; each holds a `private static readonly SectionStore<T>` parse instance plus an instance `SectionStore<T>` built by a single `CreateStore(UserSettingsFile)` helper, and delegates its static `FromJson` to the parse instance.
- Changed files:
  - Created `src/TokenHound.Infrastructure/Configuration/SectionStore.cs` (145 lines).
  - Modified `src/TokenHound.Infrastructure/Configuration/HudPositionStore.cs` (100 lines).
  - Modified `src/TokenHound.Infrastructure/Configuration/RefreshSettingsStore.cs` (100 lines).
  - No other files touched; `RateLimitSettingsStore`/`ProviderSettingsStore`/`UserSettingsFile`/`SettingsPathResolver`/tests untouched; pre-existing `Providers/` worktree changes untouched.
- Review corrections:
  - CR-03 / DEC-01 — `SectionStore<T>` now stores `_sectionName` from its constructor argument and its instance `FromJson(string json)` uses `_sectionName`; no facade keeps a section-name `const` and no static parser receives the name. The facades only pass the section name (`"Hud"` / `"Refresh"`) once, as a constructor argument inside `CreateStore`.
  - CR-02 — exact public surface restored: each facade exposes only `()`, `(UserSettingsFile)`, and `(string? filePath, string? baseDirectory = null)` (default parameter present) at `HudPositionStore.cs:20,29,42` and `RefreshSettingsStore.cs:20,29,42`; the added one-argument `(string? filePath)` overload is gone; null `settingsFile` still throws `ArgumentNullException` at `HudPositionStore.cs:33` / `RefreshSettingsStore.cs:33`.
  - CR-04 — the four-argument `new SectionStore<T>(...)` invocations in `CreateStore` place the type name + opening parenthesis on one line, one argument per line, and close with `);` on its own line; no `baseDirectory))` nesting exists (`SectionStore<...>` is obtained via `ResolveSettingsFile(filePath, baseDirectory)` before chaining to the `(UserSettingsFile)` constructor).
- Preserved contracts: constructors resolve paths via `SettingsPathResolver.ResolveOverride` with default `appsettings.json`; `FilePath` == underlying `UserSettingsFile.UserSettingsPath`; static `FromJson` returns `new` on whitespace/absent section, parses the raw section with the shared options (root extracted as `JsonElement` through the same single `JsonSerializerOptions`), and malformed top-level JSON still throws (no catch added for Hud/Refresh); `Load`/`LoadAsync` return `Section ?? new`; `Save`/`SaveAsync` throw on null and persist via `UserSettingsFile.Update`/`UpdateAsync` with `current with { Section = value }`, preserving sibling sections.
- Checks:
  - `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-restore --nologo --verbosity:minimal` → exit 0; 3 projects, 0 errors, 0 warnings (untouched standalone RateLimit/Provider compile).
  - `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*HudPositionStoreTests*"` → exit 0; total 10, passed 10, failed 0, skipped 0.
  - `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*RefreshSettingsStoreTests*"` → exit 0; total 10, passed 10, failed 0, skipped 0.
  - `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*UserSettingsFileTests*"` → exit 0; total 9, passed 9, failed 0, skipped 0.
  - Supplemental full-project run: `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1` → exit 0; total 675, passed 675, failed 0, skipped 0.
  - Quality profile `rtk rg -n "AllowTrailingCommas = true" src/TokenHound.Infrastructure/Configuration` → **5** hits: `SectionStore.cs:19` (the only T01 shared occurrence), `UserSettingsFile.cs:26` (pre-existing, prior debt), `RateLimitSettingsStore.cs:19`, `RateLimitSettingsStore.cs:25`, `ProviderSettingsStore.cs:19` (T02 scope).
  - Quality profile `rtk rg -n "SettingsPathResolver\.ResolveOverride" src/TokenHound.Infrastructure/Configuration` → **3** hits: `SectionStore.cs:137` (exactly one from SectionStore), `RateLimitSettingsStore.cs:168`, `ProviderSettingsStore.cs:173` (T02 scope).
  - Quality profile `rtk rg -n "private static UserSettingsFile CreateSettingsFile" src/TokenHound.Infrastructure/Configuration` → **3** hits: `SectionStore.cs:134` (only SectionStore in T01), `RateLimitSettingsStore.cs:165`, `ProviderSettingsStore.cs:170` (T02 scope).
- Validated state: code/diff (only the three authorized files), configuration (net10.0, MTP native via `dotnet test --project`, `--minimum-expected-tests 1` enforced), projects (Infrastructure.Tests), environment (Windows, PowerShell 7, `rtk` prefix, `$LASTEXITCODE` preserved). E2E omitted per the .NET desktop policy. No test files were modified and no pre-existing failures were observed.
- Open items / block: The TechSpec end-state QA targets (2/1/1) require migrating `RateLimitSettingsStore` and `ProviderSettingsStore` (T02), which T01's write limit forbids. Totals after T01 are therefore 5/3/3: `SectionStore` contributes exactly one hit per pattern and the two migrated facades contribute zero, so every residual is the untouched T02-scope files (`RateLimitSettingsStore`, `ProviderSettingsStore`) plus the pre-existing `UserSettingsFile.cs:26`. No QA hit is attributable to an unmigrated T01 surface. No block requires a decision; recommend executing T02 to reach 2/1/1.

### ADR candidates

- None — direct TechSpec implementation. Section-name ownership is the existing DEC-01 contract (now implemented as specified), and the only local choice (accessor delegates `Func<UserSettings, T?>` / `Func<UserSettings, T, UserSettings>` plus a static parse instance per facade) is an implementation detail of that contract with no alternative contract, boundary, or quality-attribute trade-off beyond the TechSpec.
