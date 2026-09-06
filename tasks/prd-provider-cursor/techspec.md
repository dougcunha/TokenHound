# TechSpec — Provider Adapter: Cursor

## Sources and traceability

- PRD: [tasks/prd-provider-cursor/prd.md](file:///D:/MyProjects/Ideas/TokenHound/tasks/prd-provider-cursor/prd.md)
- Architecture: [ARCHITECTURE.md](file:///D:/MyProjects/Ideas/TokenHound/ARCHITECTURE.md)
- Domain Model: [CONTEXT.md](file:///D:/MyProjects/Ideas/TokenHound/CONTEXT.md)
- Specification: [docs/specs/04-PROVIDER-CURSOR.md](file:///D:/MyProjects/Ideas/TokenHound/docs/specs/04-PROVIDER-CURSOR.md)

## Technical decisions

- **DEC-01 (SQLite State Extraction)**: Read `%APPDATA%\Cursor\User\globalStorage\state.vscdb` using `SafeSqliteReader`. Query `ItemTable` for `cursorAuth/accessToken`, `cursorAuth/stripeMembershipAuthId`, `cursorAuth/cachedEmail`, and `cursorAuth/stripeMembershipType`.
- **DEC-02 (Cookie Authentication)**: Pass credentials as `Cookie: WorkosCursorSessionToken=<stripeMembershipAuthId>::<accessToken>`. Do NOT pass `Authorization: Bearer` which returns 401/403.
- **DEC-03 (Allowance Calculation)**: Map `individualUsage.plan.totalPercentUsed` directly to `UsedFraction = totalPercentUsed / 100.0` (TotalUnits = 100.0, RemainingUnits = 100.0 - totalPercentUsed). Never compute `used / limit` directly on free accounts.
- **DEC-04 (Composer State Tracking)**: Query `composerHeaders` table for unarchived runs (`isArchived = 0 ORDER BY recency DESC LIMIT 40`). Check `unfinishedRunAt` alongside `ProcessLiveness.IsProcessAlive` on `Cursor.exe` within a 15-minute staleness window.
- **DEC-05 (Zero Fake Data)**: Never invent limits. If on-demand is disabled or unmeasured, omit on-demand window.

## Components and flow

```text
TokenHound.Infrastructure/Providers/Cursor/
├── CursorSessionDiscovery.cs       # Reads state.vscdb ItemTable for auth keys
├── CursorApiClient.cs              # HTTP client sending WorkosCursorSessionToken cookie
├── CursorComposerReader.cs         # Queries composerHeaders for active/pending runs
├── CursorUsageProvider.cs          # Implements IUsageProvider mapping quota metrics
└── CursorActivityMonitor.cs        # Implements IActivityMonitor with process liveness
```

## Relevant files

- Create:
  - `src/TokenHound.Infrastructure/Providers/Cursor/CursorSessionDiscovery.cs`
  - `src/TokenHound.Infrastructure/Providers/Cursor/CursorApiClient.cs`
  - `src/TokenHound.Infrastructure/Providers/Cursor/CursorComposerReader.cs`
  - `src/TokenHound.Infrastructure/Providers/Cursor/CursorUsageProvider.cs`
  - `src/TokenHound.Infrastructure/Providers/Cursor/CursorActivityMonitor.cs`
  - `tests/TokenHound.Infrastructure.Tests/Providers/Cursor/CursorSessionDiscoveryTests.cs`
  - `tests/TokenHound.Infrastructure.Tests/Providers/Cursor/CursorApiClientTests.cs`
  - `tests/TokenHound.Infrastructure.Tests/Providers/Cursor/CursorComposerReaderTests.cs`
  - `tests/TokenHound.Infrastructure.Tests/Providers/Cursor/CursorUsageProviderTests.cs`
  - `tests/TokenHound.Infrastructure.Tests/Providers/Cursor/CursorActivityMonitorTests.cs`
