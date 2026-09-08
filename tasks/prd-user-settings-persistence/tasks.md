# Implementation plan — User Settings Persistence in LocalAppData

## Stable sources

- PRD: `tasks/prd-user-settings-persistence/prd.md`
- TechSpec: `tasks/prd-user-settings-persistence/techspec.md`

## Dependency graph

| ID | Delivery | Depends on | Unblocks |
| --- | --- | --- | --- |
| T01 | Strongly typed models (`UserSettings`, `RateLimitSettings`) and atomic `UserSettingsFile` engine | — | T02, T03 |
| T02 | Adapt `HudPositionStore`, `ProviderSettingsStore`, and `RefreshSettingsStore` to `UserSettingsFile` | T01 | — |
| T03 | Implement `RateLimitSettingsStore` backed by `UserSettingsFile` | T01 | — |

## Traceability matrix

| Source ID | Source section | Obligation | Tasks | Evidence or test |
| --- | --- | --- | --- | --- |
| FR-01, FR-02 | `prd.md#functional-requirements` | Storage path resolution in LocalAppData and read fallback to defaults | T01 | `UserSettingsFileTests` (TC-01) |
| FR-03, NFR-03 | `prd.md#functional-requirements` | Atomic write via `.tmp` file and `File.Move` replacement | T01 | `UserSettingsFileTests` (TC-02) |
| FR-04 | `prd.md#functional-requirements` | Automatic directory creation when `%LOCALAPPDATA%\TokenHound\` is absent | T01 | `UserSettingsFileTests` (TC-03) |
| FR-05 | `prd.md#functional-requirements` | First-run migration from customized `appsettings.json` | T01 | `UserSettingsFileTests` (TC-04) |
| FR-06, NFR-04 | `prd.md#functional-requirements` | Unified section update API with serialized file gating | T01 | `UserSettingsFileTests` (TC-05) |
| FR-07 | `prd.md#functional-requirements` | Refactor `HudPositionStore`, `ProviderSettingsStore`, and `RefreshSettingsStore` | T02 | `HudPositionStoreTests`, `ProviderSettingsStoreTests`, `RefreshSettingsStoreTests` |
| FR-07 | `prd.md#functional-requirements` | Implement `RateLimitSettingsStore` | T03 | `RateLimitSettingsStoreTests` |
| NFR-01, NFR-02 | `prd.md#non-functional-requirements` | Core purity and System.Text.Json usage | T01, T02, T03 | Build validation, Core zero-dependency verification |

## Tasks

- [T01 — Core atomic persistence engine and models](done/task_01.md): Create `UserSettings`, `RateLimitSettings`, and `UserSettingsFile` with atomic `.tmp` swap, read-only defaults fallback, and migration.
- [T02 — Store adapters for HUD, Providers, and Refresh](done/task_02.md): Refactor `HudPositionStore`, `ProviderSettingsStore`, and `RefreshSettingsStore` to delegate to `UserSettingsFile`.
- [T03 — RateLimitSettingsStore implementation](done/task_03.md): Implement `RateLimitSettingsStore` on top of `UserSettingsFile` with 60-second floor clamping.

## Coverage gate

- Coverage: Pass. Every FR, NFR, and component in the PRD and TechSpec maps to a task.
- Traceability: Pass. PRD requirements, TechSpec decisions/components, and test cases are verified.
- Dependencies: Pass. Acyclic graph; T01 unblocks T02 and T03.
- Atomicity: Pass. Each task has independent test verification.
- Executability: Pass. All tasks have focused MTP commands.
- Validation profile: Pass. Net10.0, MTP with xUnit v3, E2E omitted by .NET desktop policy.
- Idempotency: Pass. Temporary directories used in unit tests; re-running tests is idempotent.

## Assumptions and open items

- Assumption: `Environment.SpecialFolder.LocalApplicationData` is available on all supported Windows environments.
- Open item: None.
- Required environment: .NET SDK 10.0.400 and existing MTP test runner.

## State

- [x] T01 — completed
- [x] T02 — completed
- [x] T03 — completed

## Problems and solutions

- T01: Implemented UserSettings and RateLimitSettings models with UserSettingsFile engine providing atomic .tmp swap, defaults fallback from appsettings.json, automatic first-run migration, and thread serialization via SettingsFileGate. Verified with 9 tests in UserSettingsFileTests.
- T02: Refactored HudPositionStore, ProviderSettingsStore, and RefreshSettingsStore to delegate to UserSettingsFile while maintaining complete backward compatibility for public APIs and adding Save/SaveAsync to RefreshSettingsStore. Verified with 38 unit tests across all 3 stores.
- T03: Implemented RateLimitSettingsStore backed by UserSettingsFile with hard clamping to 60s minimum retry floor and sibling section preservation. Verified with 17 unit tests in RateLimitSettingsStoreTests.
