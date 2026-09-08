# Stable execution context

Load in this exact order:

1. `tasks/prd-settings-02-cadence-and-retries/prd.md`
2. `tasks/prd-settings-02-cadence-and-retries/techspec.md`
3. This file

---

# T03 - Persist cadence and retry settings atomically

## Outcome

Valid Refresh and RateLimit values can be loaded and saved without altering unrelated configuration bytes, comments, or formatting, and a failed write leaves the prior configuration usable.

## Dependencies and boundaries

- Depends on: P-01 (approved replacement for TechSpec DEC-03)
- Unblocks: T04
- In scope: `RefreshSettingsStore.Save`, `RateLimitSettings`, `RateLimitSettingsStore`, the approved lossless section-writer, and isolated persistence tests.
- Out of scope: UI apply behavior, cadence timer mutation, rate-limit calculation, provider/credential configuration, and writing settings owned by other tools.

## Traceability

| Source | Section | Obligation covered |
| --- | --- | --- |
| OBJ-01, OBJ-02, OBJ-04 | `prd.md#outcomes-and-metrics` | Persist the configurable values without collateral configuration changes |
| FR-08 | `prd.md#functional-requirements` | Update only Refresh and RateLimit while preserving other content |
| NFR-02, NFR-04 | `prd.md#non-functional-requirements` | Safe external values and atomic, fault-tolerant writes |
| DEC-03, CMP-01 to CMP-03 | `techspec.md#technical-decisions`, `techspec.md#components-and-flow` | Persistence seams and models |
| TC-01, TC-02 | `techspec.md#test-approach` | Refresh and RateLimit save/reload evidence |

## Context to recover on demand

- Applicable skills: `no-workarounds`, `dotnet-efficient-validation`, `repository-cli-efficiency`.
- Existing code: `src/TokenHound.Infrastructure/Configuration/RefreshSettingsStore.cs` - read-only store to extend.
- Existing code: `src/TokenHound.Infrastructure/Configuration/HudPositionStore.cs` - concurrent-access gate may be reusable, but its JSON DOM/direct-write behavior is explicitly not a valid implementation for this task.
- Contract or integration: P-01 and `techspec.md#configuration-json-schema-in-appsettingsjson`.

## Work

- [x] T03.1 Do not start until P-01 names the lossless update and atomic-replacement contract. Record that decision in the TechSpec before implementation.
- [x] T03.2 Implement `RateLimitSettings` and load-time resolution that defaults missing settings to 60 seconds and clamps invalid values without touching Core or UI dependencies.
- [x] T03.3 Implement the approved shared persistence primitive and both stores so updating Refresh/RateLimit preserves unrelated sections, comments, and formatting, serializes concurrent writers, and handles absent/empty files and replacement failure safely.
- [x] T03.4 Add isolated temp-file tests for exact unrelated-content preservation, each section's save/reload, missing/invalid sections, concurrent-store behavior, and injected write/replacement failure.

## Acceptance criteria

- Saving Refresh or RateLimit updates only its declared section and preserves Hud, Providers, Log, unknown sections, comments, and formatting outside the changed section.
- A failed persistence operation returns a useful failure result to the application boundary and leaves the previous file intact and parseable; no catch-and-default result may report a failed write as successful.
- Missing RateLimit resolves to 60 seconds; values below the hard floor resolve safely and never write a lower effective value.

## Verification

- Unit: `RefreshSettingsStoreTests` and `RateLimitSettingsStoreTests` use isolated copies of realistic commented/whitespace-sensitive JSON and assert raw unrelated segments plus parsed values.
- Integration: Inject or simulate the approved file-replacement failure at the writer boundary and verify the original file remains the active source.
- E2E: Omitted by .NET desktop policy.
- Manual: MAN-01 confirms Apply persists values; owner: implementer.
- Commands: `rtk dotnet run --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -c Release -- --minimum-expected-tests 1 --filter-class "*RefreshSettingsStoreTests*"` and `rtk dotnet run --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -c Release -- --minimum-expected-tests 1 --filter-class "*RateLimitSettingsStoreTests*"`
- Environment dependency: P-01, .NET SDK 10.0.400, and a writable isolated temporary directory. No provider credentials are needed.
- Expected evidence: Both MTP selections execute at least one test and pass; failure-path test proves pre-write file recovery.

## Affected files

- Modify: `src/TokenHound.Infrastructure/Configuration/RefreshSettingsStore.cs`
- Create: `src/TokenHound.Infrastructure/Configuration/RateLimitSettings.cs`
- Create: `src/TokenHound.Infrastructure/Configuration/RateLimitSettingsStore.cs`
- Create: approved shared lossless persistence helper in `src/TokenHound.Infrastructure/Configuration/`
- Create: `tests/TokenHound.Infrastructure.Tests/Configuration/RefreshSettingsStoreTests.cs`
- Create: `tests/TokenHound.Infrastructure.Tests/Configuration/RateLimitSettingsStoreTests.cs`

## Observability and recovery

- Operational signal: The application boundary logs an actionable persistence failure once; stores do not silently claim success.
- Recovery: Retain the original configuration file on any write/replacement failure and allow the user to retry after the filesystem cause is resolved.

## Handoff

> Updated by `sdd-execute-task` during implementation.

- Produced result: Resolved P-01 via `UserSettingsFile` storing mutable configuration in `%LOCALAPPDATA%\TokenHound\settings.json` with atomic `.tmp` swap, defaults fallback from `appsettings.json`, and automatic first-run migration. Implemented `RateLimitSettings`, `RateLimitSettingsStore` (with 60s hard floor clamping), and added `Save`/`SaveAsync` to `RefreshSettingsStore`.
- Changed files:
  - `src/TokenHound.Infrastructure/Configuration/RefreshSettingsStore.cs`
  - `src/TokenHound.Infrastructure/Configuration/RateLimitSettings.cs`
  - `src/TokenHound.Infrastructure/Configuration/RateLimitSettingsStore.cs`
  - `src/TokenHound.Infrastructure/Configuration/UserSettings.cs`
  - `src/TokenHound.Infrastructure/Configuration/UserSettingsFile.cs`
  - `tests/TokenHound.Infrastructure.Tests/Configuration/RefreshSettingsStoreTests.cs`
  - `tests/TokenHound.Infrastructure.Tests/Configuration/RateLimitSettingsStoreTests.cs`
  - `tests/TokenHound.Infrastructure.Tests/Configuration/UserSettingsFileTests.cs`
- Checks:
  - `rtk dotnet run --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -c Release -- --minimum-expected-tests 1 --filter-class "*RefreshSettingsStoreTests*"` (10 passed, 0 failed, exit code 0)
  - `rtk dotnet run --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -c Release -- --minimum-expected-tests 1 --filter-class "*RateLimitSettingsStoreTests*"` (17 passed, 0 failed, exit code 0)
  - `rtk dotnet run --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -c Release -- --minimum-expected-tests 1 --filter-class "*UserSettingsFileTests*"` (9 passed, 0 failed, exit code 0)
- Validated state: Clean build and 550 tests passing in TokenHound.Infrastructure.Tests.
- Open items: Unblocks T04 (`CadenceSettingsViewModel`).

### ADR candidates

None - direct TechSpec implementation resolving P-01 via LocalAppData user settings persistence architecture.
