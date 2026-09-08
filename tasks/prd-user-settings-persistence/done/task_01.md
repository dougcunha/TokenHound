# Stable execution context

Load in this exact order:

1. `tasks/prd-user-settings-persistence/prd.md`
2. `tasks/prd-user-settings-persistence/techspec.md`
3. This file

---

# T01 — Core atomic persistence engine and models

## Outcome

`UserSettings` and `RateLimitSettings` models exist, and `UserSettingsFile` safely resolves `%LOCALAPPDATA%\TokenHound\settings.json`, falls back to read-only defaults in `appsettings.json`, automatically migrates customized settings on first launch, and writes changes atomically via `.tmp` file and `File.Move` under `SettingsFileGate`.

## Dependencies and boundaries

- Depends on: —
- Unblocks: T02, T03
- In scope: `UserSettings`, `RateLimitSettings`, `UserSettingsFile`, and comprehensive unit tests in `UserSettingsFileTests`.
- Out of scope: Modifying existing store public APIs, UI bindings, and SQLite.

## Traceability

| Source | Section | Obligation covered |
| --- | --- | --- |
| FR-01, FR-02 | `prd.md#functional-requirements` | LocalAppData path resolution and read fallback to defaults |
| FR-03, NFR-03 | `prd.md#functional-requirements` | Atomic write via `.tmp` file and `File.Move` replacement |
| FR-04 | `prd.md#functional-requirements` | Automatic directory creation when directory is absent |
| FR-05 | `prd.md#functional-requirements` | First-run migration from customized `appsettings.json` |
| FR-06, NFR-04 | `prd.md#functional-requirements` | Unified section update API with serialized file gating |
| NFR-01, NFR-02 | `prd.md#non-functional-requirements` | Core purity and System.Text.Json usage |
| TC-01 to TC-05 | `techspec.md#test-approach` | Unit tests for resolution, atomic write, fallback, migration, and updates |

## Context to recover on demand

- Applicable skills: `no-workarounds`, `dotnet-efficient-validation`, `repository-cli-efficiency`.
- Existing code: `src/TokenHound.Infrastructure/Configuration/SettingsFileGate.cs` — file serialization gate.
- Existing code: `src/TokenHound.Infrastructure/Configuration/HudPositionSettings.cs`, `ProviderSettings.cs`, `RefreshSettings.cs`.

## Work

- [x] T01.1 Create `UserSettings` record with `HudPositionSettings? Hud`, `ProviderSettings? Providers`, `RefreshSettings? Refresh`, and `RateLimitSettings? RateLimit`.
- [x] T01.2 Create `RateLimitSettings` record with `MinimumRetryFloorSeconds` (default 60s, clamped to `MINIMUM_FLOOR_SECONDS = 60`).
- [x] T01.3 Create `UserSettingsFile` with:
  - Default path: `%LOCALAPPDATA%\TokenHound\settings.json` (overridable in constructor for tests).
  - Defaults path: `appsettings.json` in `AppContext.BaseDirectory` (overridable in constructor).
  - Atomic write: write to `.tmp`, flush, `File.Move(tmp, target, overwrite: true)`.
  - Read with fallback: load user settings if present; if absent or sections are missing, deserialize from defaults file; if absent there, use empty/default models.
  - Automatic migration on first run: if user file is missing and defaults file has custom coordinates or provider states, migrate them into the user file.
  - Sectional update helpers: `Update(Func<UserSettings, UserSettings>)` and `UpdateAsync(...)` under `SettingsFileGate`.
- [x] T01.4 Create `UserSettingsFileTests` in `TokenHound.Infrastructure.Tests` covering defaults fallback, atomic replacement, directory creation, migration, and concurrent sectional updates.

## Acceptance criteria

- `UserSettingsFile.Load()` loads user configuration if present, or falls back to defaults without throwing.
- `UserSettingsFile.Save()` writes atomically: mid-write kill or error never corrupts target file.
- Missing `%LOCALAPPDATA%\TokenHound\` directory is created automatically on write.
- First-run migration successfully copies custom values from `appsettings.json`.
- Concurrent calls to `UpdateAsync` do not cause race conditions or lost updates.

## Verification

- Unit: `UserSettingsFileTests` passes all test cases using isolated temp files/directories.
- Integration: Verified through file system operations on temp paths.
- E2E: Omitted by .NET desktop policy.
- Commands: `rtk dotnet run --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -c Release -- --minimum-expected-tests 1 --filter-class "*UserSettingsFileTests*"`
- Expected evidence: All tests in `UserSettingsFileTests` pass with exit code 0.

## Affected files

- Create: `src/TokenHound.Infrastructure/Configuration/UserSettings.cs`
- Create: `src/TokenHound.Infrastructure/Configuration/RateLimitSettings.cs`
- Create: `src/TokenHound.Infrastructure/Configuration/UserSettingsFile.cs`
- Create: `tests/TokenHound.Infrastructure.Tests/Configuration/UserSettingsFileTests.cs`

## Observability and recovery

- Operational signal: Warning log on write or migration failure; recovery falls back to in-memory defaults.
- Recovery: Original configuration file remains intact on write failure.

## Handoff

> Updated by `sdd-execute-task` during implementation.

- Produced result: Strongly typed models `UserSettings` and `RateLimitSettings` created, and `UserSettingsFile` atomic persistence engine implemented with `.tmp` swap, defaults fallback, automatic first-run migration, and thread-safe sectional updates under `SettingsFileGate`. Verified by 9 new unit tests.
- Changed files:
  - `src/TokenHound.Infrastructure/Configuration/UserSettings.cs`
  - `src/TokenHound.Infrastructure/Configuration/RateLimitSettings.cs`
  - `src/TokenHound.Infrastructure/Configuration/UserSettingsFile.cs`
  - `tests/TokenHound.Infrastructure.Tests/Configuration/UserSettingsFileTests.cs`
  - `tasks/prd-user-settings-persistence/task_01.md`
- Checks:
  - `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj -c Release --no-restore --nologo --verbosity:minimal` (0 errors, 0 warnings)
  - `rtk dotnet run --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -c Release -- --minimum-expected-tests 1 --filter-class "*UserSettingsFileTests*"` (9 passed, 0 failed, exit code 0)
  - `rtk dotnet run --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -c Release -- --minimum-expected-tests 1 --filter-class "*HudPositionStoreTests*"` (7 passed, 0 failed, exit code 0)
  - `rtk dotnet run --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -c Release -- --minimum-expected-tests 1 --filter-class "*ProviderSettingsStoreTests*"` (16 passed, 0 failed, exit code 0)
- Validated state: Net10.0 Release build, zero warnings, 32 passed tests in `TokenHound.Infrastructure.Tests`.
- Open items: None. T01 is complete and unblocks T02 and T03.

### ADR candidates

None.
