# Stable execution context

Load in this exact order:

1. `tasks/prd-user-settings-persistence/prd.md`
2. `tasks/prd-user-settings-persistence/techspec.md`
3. This file

---

# T02 — Store adapters for HUD, Providers, and Refresh

## Outcome

`HudPositionStore`, `ProviderSettingsStore`, and `RefreshSettingsStore` are refactored to delegate persistence to `UserSettingsFile`. Existing public contracts (`Load`, `Save`, `SaveAsync`) remain compatible, and `RefreshSettingsStore` gains `Save`/`SaveAsync` capability.

## Dependencies and boundaries

- Depends on: T01
- Unblocks: —
- In scope: Adapting `HudPositionStore`, `ProviderSettingsStore`, `RefreshSettingsStore`, and updating their unit test suites.
- Out of scope: `RateLimitSettingsStore` (covered by T03), UI changes, or Core changes.

## Traceability

| Source | Section | Obligation covered |
| --- | --- | --- |
| FR-07 | `prd.md#functional-requirements` | Refactor store adapters to use unified persistence |
| TC-06, TC-07, TC-08 | `techspec.md#test-approach` | HUD, Providers, and Refresh save/load tests |
| NFR-01, NFR-02 | `prd.md#non-functional-requirements` | System.Text.Json usage and Core purity |

## Context to recover on demand

- Applicable skills: `no-workarounds`, `dotnet-efficient-validation`, `repository-cli-efficiency`.
- Existing code: `src/TokenHound.Infrastructure/Configuration/HudPositionStore.cs`
- Existing code: `src/TokenHound.Infrastructure/Configuration/ProviderSettingsStore.cs`
- Existing code: `src/TokenHound.Infrastructure/Configuration/RefreshSettingsStore.cs`

## Work

- [x] T02.1 Update `HudPositionStore` to use `UserSettingsFile` (allowing custom `UserSettingsFile` or paths in constructor) while maintaining `Load()` and `Save(HudPositionSettings)`.
- [x] T02.2 Update `ProviderSettingsStore` to use `UserSettingsFile` while maintaining `Load()` and `SaveAsync(ProviderSettings, CancellationToken)`.
- [x] T02.3 Update `RefreshSettingsStore` to use `UserSettingsFile`, maintaining `Load()` and adding `Save(RefreshSettings)` and `SaveAsync(RefreshSettings, CancellationToken)`.
- [x] T02.4 Update/create unit tests in `HudPositionStoreTests`, `ProviderSettingsStoreTests`, and `RefreshSettingsStoreTests` to verify save, load, and sibling-section preservation.

## Acceptance criteria

- `HudPositionStore.Save()` updates only the `Hud` section in `UserSettingsFile` without modifying `Providers` or `Refresh`.
- `ProviderSettingsStore.SaveAsync()` updates only the `Providers` section without modifying other sections.
- `RefreshSettingsStore.Save()` persists `ActiveIntervalSeconds` and `IdleIntervalSeconds`.
- All existing tests pass without regressions.

## Verification

- Unit: Run focused tests for all three stores.
- Commands:
  - `rtk dotnet run --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -c Release -- --minimum-expected-tests 1 --filter-class "*HudPositionStoreTests*"`
  - `rtk dotnet run --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -c Release -- --minimum-expected-tests 1 --filter-class "*ProviderSettingsStoreTests*"`
  - `rtk dotnet run --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -c Release -- --minimum-expected-tests 1 --filter-class "*RefreshSettingsStoreTests*"`
- Expected evidence: All tests execute and pass with exit code 0.

## Affected files

- Modify: `src/TokenHound.Infrastructure/Configuration/HudPositionStore.cs`
- Modify: `src/TokenHound.Infrastructure/Configuration/ProviderSettingsStore.cs`
- Modify: `src/TokenHound.Infrastructure/Configuration/RefreshSettingsStore.cs`
- Modify: `tests/TokenHound.Infrastructure.Tests/Configuration/HudPositionStoreTests.cs`
- Modify: `tests/TokenHound.Infrastructure.Tests/Configuration/ProviderSettingsStoreTests.cs`
- Create: `tests/TokenHound.Infrastructure.Tests/Configuration/RefreshSettingsStoreTests.cs`

## Observability and recovery

- Operational signal: Warning logs on save failure.
- Recovery: Preserves prior settings file on write failure.

## Handoff
 
 > Updated by `sdd-execute-task` during implementation.
 
 - Produced result: `HudPositionStore`, `ProviderSettingsStore`, and `RefreshSettingsStore` refactored as thin adapters over `UserSettingsFile`. All existing public methods and constructors preserved for backwards compatibility, with support for custom `UserSettingsFile` instances or file paths. `RefreshSettingsStore` updated with `Save(RefreshSettings)` and `SaveAsync(RefreshSettings, CancellationToken)`. Unit test suites updated with sibling-section preservation, synchronous/asynchronous persistence, and constructor delegation tests.
 - Changed files:
   - `src/TokenHound.Infrastructure/Configuration/HudPositionStore.cs`
   - `src/TokenHound.Infrastructure/Configuration/ProviderSettingsStore.cs`
   - `src/TokenHound.Infrastructure/Configuration/RefreshSettingsStore.cs`
   - `tests/TokenHound.Infrastructure.Tests/Configuration/HudPositionStoreTests.cs`
   - `tests/TokenHound.Infrastructure.Tests/Configuration/ProviderSettingsStoreTests.cs`
   - `tests/TokenHound.Infrastructure.Tests/Configuration/RefreshSettingsStoreTests.cs`
   - `tasks/prd-user-settings-persistence/task_02.md`
 - Checks:
   - `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj -c Release --no-restore --nologo --verbosity:minimal` (3 projects, 0 errors, 0 warnings)
   - `rtk dotnet run --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -c Release -- --minimum-expected-tests 1 --filter-class "*HudPositionStoreTests*"` (10 passed, 0 failed, exit code 0)
   - `rtk dotnet run --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -c Release -- --minimum-expected-tests 1 --filter-class "*ProviderSettingsStoreTests*"` (18 passed, 0 failed, exit code 0)
   - `rtk dotnet run --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -c Release -- --minimum-expected-tests 1 --filter-class "*RefreshSettingsStoreTests*"` (10 passed, 0 failed, exit code 0)
   - `rtk dotnet run --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -c Release -- --minimum-expected-tests 1` (533 passed, 0 failed, exit code 0)
   - `rtk dotnet build src/TokenHound.App/TokenHound.App.csproj -c Release --no-restore --nologo --verbosity:minimal` (4 projects, 0 errors, 0 warnings)
 - Validated state: Net10.0 Release build, zero warnings, 38 passed tests across the 3 store test suites, 533 total passed tests in `TokenHound.Infrastructure.Tests`.
 - Open items: None. Ready for T03 (`RateLimitSettingsStore`).
 
 ### ADR candidates
 
 None.
