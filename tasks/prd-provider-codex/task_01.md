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

- [x] T01.1 Implement `CodexAuthDiscovery.cs` under `TokenHound.Infrastructure/Providers/Codex/`.
- [x] T01.2 Use `SharedFileReader.ReadAllTextAsync` to read `auth.json`.
- [x] T01.3 Decode Base64Url payload of JWT and parse JSON properties `email` and `https://api.openai.com/auth` -> `chatgpt_plan_type`.
- [x] T01.4 Implement `CodexAuthDiscoveryTests.cs` verifying parsing, missing files, and invalid tokens.

## Acceptance criteria

- Parses valid `auth.json` extracting email and plan type (e.g., `plus`, `team`, `pro`).
- Returns null safely on missing file or malformed JWT without throwing unhandled exceptions.
- Tests pass with `rtk dotnet test`.

## Verification

- Command: `rtk dotnet test tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*CodexAuthDiscoveryTests*"`

## Handoff

- Produced result: Implemented read-only Codex auth discovery with null-safe JWT Base64Url claim parsing.
- Changed files: `src/TokenHound.Infrastructure/Providers/Codex/CodexAuthDiscovery.cs`; `tests/TokenHound.Infrastructure.Tests/Providers/Codex/CodexAuthDiscoveryTests.cs`.
- Checks: `rtk dotnet restore tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --nologo --verbosity:minimal`; `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-restore --nologo --verbosity:minimal`; `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*CodexAuthDiscoveryTests*"`.
- Validated state: Build passed with 0 errors; focused MTP test run passed 7 tests with 0 test warnings. Restore/build reported the pre-existing NU1903 SQLitePCLRaw vulnerability warning.
- Open items: None for T01. T02-T05 remain pending in the provider plan.

### ADR candidates

None.
