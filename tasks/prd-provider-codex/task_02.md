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

- [ ] T02.1 Implement `CodexRateLimitsDto.cs` (or DTO records) under `TokenHound.Infrastructure/Providers/Codex/`.
- [ ] T02.2 Implement `CodexAppServerClient.cs` with executable discovery (`ChatGPT\resources\codex.exe`, `~/.codex/bin/codex.exe`, and PATH) and injectable runner or process factory for unit testing.
- [ ] T02.3 Implement stdio JSON-RPC protocol exchanging `initialize`, `initialized`, and `account/rateLimits/read`.
- [ ] T02.4 Implement strict 10s cancellation watchdog ensuring child processes are killed if unresponsive.
- [ ] T02.5 Implement `CodexAppServerClientTests.cs` verifying happy path parsing, missing executable handling, and timeout behavior.

## Acceptance criteria

- Resolves `codex.exe` path or returns null if not installed.
- Correctly parses JSON-RPC response `id == 2` extracting `primary` (usedPercent, windowDurationMins, resetsAt) and `secondary` windows.
- Parses `rateLimitReachedType` (e.g., `rate_limit_reached`).
- Kills child process and handles timeouts/cancellations safely.
- Unit tests pass with `rtk dotnet test`.

## Verification

- Command: `rtk dotnet test tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*CodexAppServerClientTests*"`

## Handoff

- Produced result: Pending execution.
- Changed files: Pending execution.
- Checks: Pending execution.
- Validated state: Pending execution.
- Open items: Pending execution.

### ADR candidates

None.
