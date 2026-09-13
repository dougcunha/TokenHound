# Stable execution context

Load in this exact order:

1. `tasks/prd-arch-20260912-05-settings-stores/prd.md`
2. `tasks/prd-arch-20260912-05-settings-stores/techspec.md`
3. This file

Use the current PRD and TechSpec versions already loaded; recover only missing or changed sources. This order does not guarantee a host cache hit.

---

# T02 — Migrate RateLimit and Provider stores

## Outcome

`RateLimitSettingsStore` and `ProviderSettingsStore` (including its extra JSON converter) delegate to `SectionStore<T>` with unchanged public behavior.

## Dependencies and boundaries

- Depends on: T01
- Unblocks: —
- In scope: migrate the two remaining stores; parameterize the provider store's converter.
- Out of scope: `UserSettingsFile` I/O semantics; settings UI.

## Traceability

| Source | Section | Obligation covered |
| --- | --- | --- |
| R-02, R-03, R-05, R-06, R-07 | `prd.md#behaviors-to-preserve` | RateLimit/Provider behavior preserved |
| DEC-02, DEC-04 | `techspec.md#technical-decisions` | Facade + converter injection |
| QA-01..QA-03 | `techspec.md#quality-profile` | Shared block counts reach target |

## Context to recover on demand

- Applicable skills: `dotnet-efficient-validation`, `repository-cli-efficiency`.
- Existing code: `RateLimitSettingsStore.cs:12-176`, `ProviderSettingsStore.cs`, `SectionStore<T>` from T01.
- Contract or integration: TechSpec `#technical-decisions`.

## Work

- [x] T02.1 Make `RateLimitSettingsStore` a thin facade over `SectionStore<RateLimitSettings>`.
- [x] T02.2 Make `ProviderSettingsStore` a thin facade over `SectionStore<ProviderSettings>`, injecting its extra converter.
- [x] T02.3 Remove duplicated options/ctor/path-resolution blocks from both stores.

## Acceptance criteria

- [x] `RateLimitSettingsStore` and `ProviderSettingsStore` public APIs compile unchanged (`(string? filePath, string? baseDirectory = null)` surface retained).
- [x] Provider settings round-trip including the converter's behavior, and `ProviderSettingsStore.FromJson` preserves case-insensitive root-section matching.
- [x] `rtk rg -n "SettingsPathResolver\.ResolveOverride" src/TokenHound.Infrastructure/Configuration` returns 1 hit.
- [x] `rtk rg -n "private static UserSettingsFile CreateSettingsFile" src/TokenHound.Infrastructure/Configuration` returns 1 hit.

## Verification

- Unit: `RateLimitSettingsStoreTests`, `ProviderSettingsStoreTests` include converter and missing-section cases.
- Integration: `UserSettingsFileTests`, `UserSettingsFileConcurrencyTests`.
- E2E: omitted by .NET desktop policy.
- Manual: none.
- Commands: focused MTP classes `*RateLimitSettingsStoreTests*`, `*ProviderSettingsStoreTests*`, `*UserSettingsFileTests*` with `--minimum-expected-tests 1`.
- Environment dependency: none.
- Expected evidence: pass counts plus target `rtk rg` counts.

## Affected files

- Modify: `src/TokenHound.Infrastructure/Configuration/RateLimitSettingsStore.cs`
- Modify: `src/TokenHound.Infrastructure/Configuration/ProviderSettingsStore.cs`

## Observability and recovery

- Operational signal: unchanged settings JSON on disk.
- Recovery: `git revert`.

## Handoff

> Updated by `sdd-execute-task` during implementation (rework after `codereview_1/codereview.md`).

- Produced result: `RateLimitSettingsStore` and `ProviderSettingsStore` are now thin facades over the T01 `SectionStore<T>`. Each facade holds a private `SectionStore<T>` instance built by a static `CreateStore(UserSettingsFile)` helper that passes the section name (`"RateLimit"` / `"Providers"`) once as the first constructor argument; no section-name constant remains in either facade and no static parser receives a name. The public constructor surface is exactly `()`, `(UserSettingsFile)`, and `(string? filePath, string? baseDirectory = null)` (default parameter present); a null `settingsFile` still throws `ArgumentNullException`. RateLimit delegates `Load`/`LoadAsync`/`Save`/`SaveAsync` and applies `Clamp` inside the read delegate (`settings => Clamp(settings.RateLimit)`) and the write delegate (`settings with { RateLimit = Clamp(value) }`); its static `FromJson` delegates to a static `PARSER` instance inside the existing whitespace guard and `try/catch` → `new`. Provider delegates `Load`/`LoadAsync`/`Save`/`SaveAsync`; `Save`/`SaveAsync` merge through `MergeStates` (case-insensitive, existing/unknown keys preserved, new keys override) inside a private static `MergeProviders`. Provider `FromJson` uses the whole-document path: `JsonSerializer.Deserialize<UserSettings>(json, JSON_OPTIONS)?.Providers ?? new ProviderSettings()` inside the whitespace guard and `try/catch` → `new ProviderSettings()`, where `JSON_OPTIONS` clones `SectionStore<ProviderSettings>.SharedOptions` and adds `UserSettings.ProviderSettingsJsonConverter` (DEC-04), with no second `AllowTrailingCommas = true` literal.
- Changed files:
  - `src/TokenHound.Infrastructure/Configuration/RateLimitSettingsStore.cs` (123 lines) — removed `DEFAULT_CONFIG_FILE`, `RATE_LIMIT_SECTION_NAME`, `DOCUMENT_OPTIONS`, `JSON_OPTIONS`, and `CreateSettingsFile`; now a facade.
  - `src/TokenHound.Infrastructure/Configuration/ProviderSettingsStore.cs` (149 lines) — removed `DEFAULT_CONFIG_FILE`, the duplicated options literal, and `CreateSettingsFile`; now a facade. `MergeStates` retained verbatim.
  - `src/TokenHound.Infrastructure/Configuration/SectionStore.cs` (151 lines) — added `internal static JsonSerializerOptions SharedOptions => JSON_OPTIONS;` so the provider facade can clone the shared options with its converter (the only authorized SectionStore change).
  - `tests/TokenHound.Infrastructure.Tests/Configuration/ProviderSettingsStoreTests.cs` — added three CR-01 regression cases: a `[Theory]` with exact-case `{"Providers":...}` and lower-case `{"providers":...}` both disabling `claude`, plus a `[Fact]` asserting an absent root section returns enabled. Existing tests were not renamed or weakened.
  - `tasks/prd-arch-20260912-05-settings-stores/task_02.md` — this handoff only.
  - No other tests, `UserSettingsFile`, `SettingsPathResolver`, `HudPositionStore`, `RefreshSettingsStore`, `tasks.md`, or pre-existing `Providers/` worktree changes were touched.
- Review corrections:
  - CR-01 (Medium): `ProviderSettingsStore.FromJson` restores the former case-insensitive root-section contract by deserializing the whole document into `UserSettings` with the provider converter options, then returning `settings?.Providers ?? new ProviderSettings()`. Regression coverage added at `ProviderSettingsStoreTests.cs` (`FromJson_WhenRootSectionCaseVaries_DisablesProvider`, `FromJson_WhenRootSectionIsAbsent_ReportsEnabled`).
  - CR-02: exact constructor surface restored to `()`, `(UserSettingsFile)`, `(string? filePath, string? baseDirectory = null)`; the one-argument `(string? filePath)` overload is gone; null `settingsFile` still throws `ArgumentNullException`.
  - CR-03: `SectionStore<T>` owns the section name; both facades pass `"RateLimit"` / `"Providers"` once inside `CreateStore`, following the T01 `CreateStore(UserSettingsFile)` + static `PARSER` pattern for RateLimit. Provider's static `FromJson` intentionally uses the whole-document path (CR-01) and therefore does not call `SectionStore.FromJson`.
  - CR-04: every four-argument `new SectionStore<T>(...)` invocation places the type name + opening parenthesis on one line, one argument per line, and closes with `);` on its own line; the path-override constructors chain through `SectionStore<T>.ResolveSettingsFile(filePath, baseDirectory)`, so no `baseDirectory))` nesting exists.
- Checks:
  - `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-restore --nologo --verbosity:minimal` → exit 0; 3 projects, 0 errors, 0 warnings.
  - `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*RateLimitSettingsStoreTests*"` → exit 0; 17 executed, 17 passed, 0 failed.
  - `... --filter-class "*ProviderSettingsStoreTests*"` → exit 0; 22 executed, 22 passed, 0 failed (19 pre-existing + 3 CR-01 regression cases).
  - `... --filter-class "*UserSettingsFileTests*"` → exit 0; 9 executed, 9 passed, 0 failed.
  - `... --filter-class "*UserSettingsFileConcurrencyTests*"` → exit 0; 3 executed, 3 passed, 0 failed.
  - `... --filter-class "*HudPositionStoreTests*"` → exit 0; 10 executed, 10 passed, 0 failed (T01 unaffected).
  - `... --filter-class "*RefreshSettingsStoreTests*"` → exit 0; 10 executed, 10 passed, 0 failed (T01 unaffected).
  - Supplementary full-project run: `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1` → exit 0; 678 passed, 0 failed (675 pre-existing + 3 CR-01 regression cases).
  - Quality profile: `rtk rg -n "AllowTrailingCommas = true" src/TokenHound.Infrastructure/Configuration` → **2** (`SectionStore.cs:19`, `UserSettingsFile.cs:26`); `rtk rg -n "SettingsPathResolver\.ResolveOverride" src/TokenHound.Infrastructure/Configuration` → **1** (`SectionStore.cs:143`); `rtk rg -n "private static UserSettingsFile CreateSettingsFile" src/TokenHound.Infrastructure/Configuration` → **1** (`SectionStore.cs:140`). Target 2/1/1 reached.
  - `rtk git diff --check` over the three authorized source/test files → exit 0; `rtk rg -n "[ \t]+$"` on `SectionStore.cs` → exit 1 (no matches).
- Validated state: code/diff (only the three authorized source files plus the CR-01 tests and this handoff), configuration (net10.0 MTP native via `dotnet test --project`), projects (Infrastructure.Tests), environment (Windows, PowerShell 7, `rtk` prefix, `$LASTEXITCODE` preserved). E2E omitted per the .NET desktop policy. No pre-existing failures observed; none were fixed. Quality baseline: T01 recorded 5/3/3; residual hits are now only the shared `SectionStore` (one per rule) and the pre-existing `UserSettingsFile.cs:26` (prior debt), meeting the 2/1/1 target.
- Open items: None blocking. The T02 work/acceptance checkboxes are intentionally unchanged because write authorization covers only the `## Handoff` and `### ADR candidates` sections. Optional follow-up (outside T02): the facades duplicate the two read/write lambdas inline; this matches the T01 pattern and no QA hit is attributable to it.

### ADR candidates

None - direct TechSpec implementation or local decision. Exposing `SectionStore<T>.SharedOptions` and cloning it with the provider converter implements DEC-04; the provider whole-document `FromJson` restores the pre-refactor serializer contract (CR-01). No new contract, boundary, or quality-attribute trade-off beyond the TechSpec.
