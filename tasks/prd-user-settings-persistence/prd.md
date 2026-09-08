# PRD — User Settings Persistence in LocalAppData

## Problem and context

In TokenHound, user preferences (HUD placement, provider enablement, polling intervals, and rate-limit retry floors) are currently modified directly inside `appsettings.json` in the application base directory (`AppContext.BaseDirectory`).

This architecture creates three critical operational problems on Windows:
1. **Permission Denied in Production**: When TokenHound is installed to `C:\Program Files\TokenHound`, standard users lack write privileges to the installation directory. In-place writes to `appsettings.json` fail with `UnauthorizedAccessException`.
2. **Corrupted File on Sudden Termination**: Direct `File.WriteAllText` writes are non-atomic. A process kill, OS crash, or power loss mid-write results in an empty (0-byte) or corrupted JSON file.
3. **Serialization & Comment Fragility**: `appsettings.json` mixes static deployment configuration (Serilog `Log` configuration with dozens of parameters) with mutable user state. Attempting to parse and re-serialize the shared DOM while preserving comments and formatting is fragile and has repeatedly blocked feature delivery.

Moving mutable user preferences to `%LOCALAPPDATA%\TokenHound\settings.json` with atomic replacement solves all three issues, aligns with standard Windows desktop architecture, and respects repository rules.

## Outcomes and metrics

| ID | Expected outcome | Metric or evidence |
| --- | --- | --- |
| OBJ-01 | Isolate all mutable user configuration into `%LOCALAPPDATA%\TokenHound\settings.json` | Running from a read-only directory persists settings successfully to user profile. |
| OBJ-02 | Guarantee atomic, corruption-proof file writes | Mid-write interruptions leave the prior valid settings file completely intact. |
| OBJ-03 | Maintain `appsettings.json` as read-only defaults & logging configuration | Application directory `appsettings.json` is never mutated at runtime. |
| OBJ-04 | Seamless migration for existing installations | On first run, existing customizations in `appsettings.json` seed `%LOCALAPPDATA%\TokenHound\settings.json`. |
| OBJ-05 | Eliminate feature bottlenecks around JSON serialization | New configuration sections can be added and saved cleanly with zero DOM comment conflict. |

## Stories and journeys

| ID | User | Need | Benefit | Flow or edge |
| --- | --- | --- | --- | --- |
| US-01 | Desktop user running in Program Files | Save settings and HUD position | No crashes or `UnauthorizedAccessException` | User drags HUD or saves Settings dialog; file writes to `%LOCALAPPDATA%\TokenHound\settings.json`. |
| US-02 | User facing system crash or power loss | Keep valid configuration | Settings never truncate to 0 bytes or corrupt | System powers off during save; next startup reads previous valid file without reset. |
| US-03 | Existing TokenHound user | Upgrade to new version | Existing custom HUD placement and provider toggles are preserved | First run detects absent LocalAppData settings, copies custom values from `appsettings.json`, and saves. |
| US-04 | Developer / Sysadmin | Configure global defaults or logging | Clean separation of concerns | Edits `appsettings.json` without fear of TokenHound stripping comments or rewriting sections. |

## Functional requirements

| ID | Requirement | Acceptance criterion |
| --- | --- | --- |
| FR-01 | Storage Path Resolution | Default user settings path resolves to `%LOCALAPPDATA%\TokenHound\settings.json`. Configurable or overridable for isolated unit/integration testing. |
| FR-02 | Read Hierarchy & Default Fallback | When reading a section (`Hud`, `Providers`, `Refresh`, `RateLimit`), load from user settings if present; if missing, fall back to `appsettings.json` (defaults); if absent there, use built-in policy defaults. |
| FR-03 | Atomic File Replacement | Writing user settings must serialize to a temporary file (`settings.json.tmp`) in the same directory, flush to disk, and replace the target file via `File.Move(..., overwrite: true)`. |
| FR-04 | Directory Creation | If `%LOCALAPPDATA%\TokenHound\` directory does not exist, the persistence service creates it automatically before writing. |
| FR-05 | Settings Migration | On first run when `%LOCALAPPDATA%\TokenHound\settings.json` does not exist, if `appsettings.json` contains customized `Hud` (non-null coordinates), `Providers`, or `Refresh` sections, migrate them to the user settings file. |
| FR-06 | Unified Section Persistence | Provide a thread-safe helper `UserSettingsStore` (or `UserSettingsService`) in `TokenHound.Infrastructure` that loads and saves `UserSettings` containing `Hud`, `Providers`, `Refresh`, and `RateLimit` blocks. |
| FR-07 | Store Adapter Migration | Refactor `HudPositionStore`, `ProviderSettingsStore`, `RefreshSettingsStore`, and `RateLimitSettingsStore` to use the unified user settings persistence path. |

## Non-functional requirements

| ID | Attribute | Limit or criterion |
| --- | --- | --- |
| NFR-01 | Repository Invariant (JSON with System.Text.Json) | Persist configuration as JSON strictly with `System.Text.Json`. No external database (e.g. SQLite) or non-standard JSON libraries. |
| NFR-02 | Repository Invariant (Core Purity) | `TokenHound.Core` remains 100% pure with zero filesystem, UI, or OS dependencies. Storage models and logic reside in `TokenHound.Infrastructure`. |
| NFR-03 | Atomic Fault Tolerance | A failed write (e.g. out of disk space) must never alter or corrupt the existing `settings.json`. Temp files must be cleaned up on failure. |
| NFR-04 | Thread & Process Safety | In-process writes are serialized via `SettingsFileGate`. File sharing permits concurrent readers (`FileShare.ReadWrite`). |
| NFR-05 | Performance & Non-blocking IO | Deserialization takes < 10ms; atomic persistence takes < 20ms and offers asynchronous (`SaveAsync`) and synchronous APIs. |

## User experience

This is an infrastructure persistence refactor. It requires no visual changes to existing windows:
- **Notch / HUD**: Moving the notch continues to persist coordinates seamlessly without delay or focus interruption.
- **Settings Dialog**: Clicking "Apply" / "Save" persists `Providers`, `Refresh`, and `RateLimit` seamlessly with feedback remaining unchanged.

## Constraints and dependencies

- Target framework: `net10.0` and `net10.0-windows`.
- Storage directory: `Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)` + `TokenHound`.
- File format: Standard UTF-8 JSON via `System.Text.Json`.
- Clean Architecture: Models in `TokenHound.Infrastructure.Configuration`, IO in `TokenHound.Infrastructure`.

## Out of scope

- Credential storage (credentials remain governed by Windows Credential Manager and DPAPI).
- SQLite storage for application settings (strictly forbidden by `AGENTS.md` config rule).
- Cloud synchronization or roaming profiles across machines.
- Adding new UI screens or modifying dialog visuals.

## Assumptions and sources

- Assumption: Storing mutable settings in `%LOCALAPPDATA%\TokenHound\settings.json` is the standard Windows convention for per-user desktop applications.
- Assumption: `appsettings.json` in the base directory remains deployed with the app as read-only defaults.
- Source: [AGENTS.md](file:///D:/MyProjects/TokenHound/AGENTS.md) ("Persist configuration as JSON with System.Text.Json").
- Source: [ARCHITECTURE.md](file:///D:/MyProjects/TokenHound/ARCHITECTURE.md) (Local data folder conventions under `%LOCALAPPDATA%\TokenHound\`).

## PRD acceptance gate

- [x] Every requirement has an ID and an observable criterion.
- [x] Metrics, boundaries, and out-of-scope items are explicit.
- [x] Internal rules came from the user or an identified organizational source.
- [x] Implementation details remain in the TechSpec.
