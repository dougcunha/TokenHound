# Stable execution context

Load in this exact order:

1. `tasks/prd-provider-antigravity/prd.md`
2. `tasks/prd-provider-antigravity/techspec.md`
3. This file

---

# T01 — Antigravity Process & Ephemeral TCP Port Discovery

## Outcome

Implements `AntigravityEndpointDiscovery` to detect active `language_server.exe` instances on Windows, extracting the CSRF token from command line arguments and listening ephemeral TCP ports via `iphlpapi.dll` (`GetExtendedTcpTable`).

## Work

- [x] T01.1 Implement `AntigravityEndpointDiscovery.cs` under `TokenHound.Infrastructure/Providers/Antigravity/`.
- [x] T01.2 Query WMI (`Win32_Process`) or native process arguments to extract `--csrf_token <token>`.
- [x] T01.3 Query native `iphlpapi.dll` (`GetExtendedTcpTable`) to identify listening TCP ports for the target PID.
- [x] T01.4 Implement injectable process/port provider delegates for reliable unit testing.
- [x] T01.5 Implement `AntigravityEndpointDiscoveryTests.cs` verifying parsing, multiple listening ports, and graceful handling of missing processes.

## Acceptance criteria

- Extracts CSRF token from matching command-line arguments.
- Identifies listening TCP ports for a given PID.
- Returns null safely when Antigravity/Language Server is not running.
- Tests pass with `rtk dotnet test`.

## Verification

- Command: `rtk dotnet test tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*AntigravityEndpointDiscoveryTests*"`

## Handoff

- Produced result: `AntigravityEndpoint` DTO and `AntigravityEndpointDiscovery` implementing WMI argument parsing and Win32 `GetExtendedTcpTable` port resolution.
- Changed files:
  - `src/TokenHound.Infrastructure/TokenHound.Infrastructure.csproj`
  - `src/TokenHound.Infrastructure/Providers/Antigravity/AntigravityEndpoint.cs`
  - `src/TokenHound.Infrastructure/Providers/Antigravity/AntigravityEndpointDiscovery.cs`
  - `tests/TokenHound.Infrastructure.Tests/Providers/Antigravity/AntigravityEndpointDiscoveryTests.cs`
- Checks: `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*AntigravityEndpointDiscoveryTests*"` (5 tests passed, 0 failures, exit code 0).
- Validated state: DiscoverEndpoint extracts CSRF token, binds listening ports, handles missing processes, and respects AGENTS.md rules.
- Open items: None.

### ADR candidates

None.
