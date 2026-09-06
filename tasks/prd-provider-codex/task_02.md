# Stable execution context

Load in this exact order:

1. `tasks/prd-provider-codex/prd.md`
2. `tasks/prd-provider-codex/techspec.md`
3. This file

---

# T02 — Codex AppServer JSON-RPC Client

## Outcome

Implements `CodexAppServerClient` to locate `codex.exe`, launch it with `app-server`, execute the stdio JSON-RPC 2.0 handshake (`initialize`, `initialized`, `account/rateLimits/read`), and parse account rate limits with a 10-second timeout watchdog.

## Work

- [x] T02.1 Implement `CodexRateLimitsDto.cs` (or DTO records) under `TokenHound.Infrastructure/Providers/Codex/`.
- [x] T02.2 Implement `CodexAppServerClient.cs` with executable discovery (`ChatGPT\resources\codex.exe`, `~/.codex/bin/codex.exe`, and PATH) and injectable runner or process factory for unit testing.
- [x] T02.3 Implement stdio JSON-RPC protocol exchanging `initialize`, `initialized`, and `account/rateLimits/read`.
- [x] T02.4 Implement strict 10s cancellation watchdog ensuring child processes are killed if unresponsive.
- [x] T02.5 Implement `CodexAppServerClientTests.cs` verifying happy path parsing, missing executable handling, and timeout behavior.

## Acceptance criteria

- Resolves `codex.exe` path or returns null if not installed.
- Correctly parses JSON-RPC response `id == 2` extracting `primary` (usedPercent, windowDurationMins, resetsAt) and `secondary` windows.
- Parses `rateLimitReachedType` (e.g., `rate_limit_reached`).
- Kills child process and handles timeouts/cancellations safely.
- Unit tests pass with `rtk dotnet test`.

## Verification

- Command: `rtk dotnet test tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*CodexAppServerClientTests*"`

## Handoff

- Produced result: Implemented Codex executable discovery, injectable stdio process hosting, JSON-RPC handshake, rate-limit DTO parsing, and a strict ten-second timeout watchdog.
- Changed files: `src/TokenHound.Infrastructure/Providers/Codex/CodexAppServerClient.cs`; `src/TokenHound.Infrastructure/Providers/Codex/CodexRateLimitsDto.cs`; `tests/TokenHound.Infrastructure.Tests/Providers/Codex/CodexAppServerClientTests.cs`.
- Checks: `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-restore --nologo --verbosity:minimal`; `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*CodexAppServerClientTests*"`.
- Validated state: Build passed with 0 errors; focused MTP test run passed 4 tests with 0 test warnings. Build reported the pre-existing NU1903 SQLitePCLRaw vulnerability warning.
- Open items: None for T02. T03-T05 remain pending in the provider plan.

### ADR candidates

None.
