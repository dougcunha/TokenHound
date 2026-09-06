# Stable execution context

Load in this exact order:

1. `tasks/prd-provider-codex/prd.md`
2. `tasks/prd-provider-codex/techspec.md`
3. This file

---

# T01 — Codex Auth & JWT Account Discovery

## Outcome

Implements `CodexAuthDiscovery` reading `%USERPROFILE%\.codex\auth.json`, decoding the JWT `id_token` without external cryptographic dependencies to extract the user's `email` and subscription plan (`chatgpt_plan_type`).

## Work

- [ ] T01.1 Implement `CodexAuthDiscovery.cs` under `TokenHound.Infrastructure/Providers/Codex/`.
- [ ] T01.2 Use `SharedFileReader.ReadAllTextAsync` to read `auth.json`.
- [ ] T01.3 Decode Base64Url payload of JWT and parse JSON properties `email` and `https://api.openai.com/auth` -> `chatgpt_plan_type`.
- [ ] T01.4 Implement `CodexAuthDiscoveryTests.cs` verifying parsing, missing files, and invalid tokens.

## Acceptance criteria

- Parses valid `auth.json` extracting email and plan type (e.g., `plus`, `team`, `pro`).
- Returns null safely on missing file or malformed JWT without throwing unhandled exceptions.
- Tests pass with `rtk dotnet test`.

## Verification

- Command: `rtk dotnet test tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*CodexAuthDiscoveryTests*"`

## Handoff

- Produced result: Pending execution.
- Changed files: Pending execution.
- Checks: Pending execution.
- Validated state: Pending execution.
- Open items: Pending execution.

### ADR candidates

None.
