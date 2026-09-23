# Stable execution context

Load in this exact order:

1. `tasks/prd-07-claude-multi-profile/prd.md`
2. `tasks/prd-07-claude-multi-profile/techspec.md`
3. This file

Use the current PRD and TechSpec versions already loaded; recover only missing or changed sources. This order does not guarantee a host cache hit.

---

# T01 — Profile discovery model and directory enumeration

## Outcome

Introduce the `ClaudeProfile` record and `DiscoverProfiles(bool onlyActive = true)` method in `ClaudeProfileDiscovery` to reliably find both default and `.claude-*` directories with active credentials.

## Dependencies and boundaries

- Depends on: —
- Unblocks: T02, T03
- In scope: `ClaudeProfile.cs`, updates to `ClaudeProfileDiscovery.cs`, and corresponding unit tests in `ClaudeProfileDiscoveryTests.cs`.
- Out of scope: Changes to provider polling or WPF views.

## Traceability

| Source | Section | Obligation covered |
| --- | --- | --- |
| FR-01 | `prd.md#functional-requirements` | Multi-profile enumeration |
| FR-02 | `prd.md#functional-requirements` | Active profile qualification |
| FR-03 | `prd.md#functional-requirements` | Unique provider identification |
| CMP-01 | `techspec.md#components-and-flow` | `ClaudeProfile.cs` record definition |
| CMP-02 | `techspec.md#components-and-flow` | `ClaudeProfileDiscovery.DiscoverProfiles` method |
| TC-01 | `techspec.md#test-approach` | Discover profiles with default and work dirs |
| TC-02 | `techspec.md#test-approach` | Inactive directory exclusion |

## Context to recover on demand

- Applicable skills and rules: `AGENTS.md` (sealed classes, XML comments, <= 300 lines).
- Existing code: `src/TokenHound.Infrastructure/Providers/Claude/ClaudeProfileDiscovery.cs`
- Provider spec: `docs/specs/03-PROVIDER-CLAUDE-CODE.md` §1 & §2

## Work

- [x] T01.1 Create `src/TokenHound.Infrastructure/Providers/Claude/ClaudeProfile.cs` with `ProviderId`, `DisplayName`, `DirectoryPath`, and `Slug`.
- [x] T01.2 Implement `DiscoverProfiles(bool onlyActive = true)` in `ClaudeProfileDiscovery.cs` checking default `.claude` and `.claude-*` folders.
- [x] T01.3 Add helper to format profile display name (`Claude Code` for default, `Claude Code (<slug>)` for isolated).
- [x] T01.4 Add unit tests in `ClaudeProfileDiscoveryTests.cs` for active vs inactive directories and slug extraction.

## Acceptance criteria

- `DiscoverProfiles()` returns a `ClaudeProfile` for `%USERPROFILE%\.claude` (if active) with `ProviderId == "claude"`.
- `DiscoverProfiles()` returns a `ClaudeProfile` for each `%USERPROFILE%\.claude-<slug>` with `ProviderId == "claude-<slug>"` and `Slug == "<slug>"`.
- Inactive folders without `.credentials.json` are excluded when `onlyActive` is true.

## Verification

- Unit: Test discovery with multiple temporary profile directories (`.claude`, `.claude-work`, and an inactive `.claude-empty`).
- Integration: Not applicable.
- E2E: Omitted by .NET desktop policy.
- Commands: `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*ClaudeProfileDiscoveryTests*"`
- Environment dependency: None.
- Expected evidence: All unit tests pass in `ClaudeProfileDiscoveryTests`.

## Affected files

- Modify: `src/TokenHound.Infrastructure/Providers/Claude/ClaudeProfileDiscovery.cs`
- Modify: `tests/TokenHound.Infrastructure.Tests/Providers/ClaudeProfileDiscoveryTests.cs`
- Create: `src/TokenHound.Infrastructure/Providers/Claude/ClaudeProfile.cs`
- Create: `src/TokenHound.Infrastructure/Providers/Claude/ClaudeProfileDiscovery.Parsing.cs`

## Observability and recovery

- Operational signal: DiscoverProfiles returns immutable collection of `ClaudeProfile`.
- Recovery: Revert changes to `ClaudeProfileDiscovery.cs` and remove `ClaudeProfile.cs`.

## Handoff

> Updated by `sdd-execute-task` during implementation.

- Produced result: Created `ClaudeProfile` record and implemented `ClaudeProfileDiscovery.DiscoverProfiles(onlyActive)`. Decomposed `ClaudeProfileDiscovery` with `ClaudeProfileDiscovery.Parsing.cs` to maintain file length <= 210 lines. Verified active and inactive profile discovery and slug extraction with unit tests.
- Changed files:
  - `src/TokenHound.Infrastructure/Providers/Claude/ClaudeProfile.cs`
  - `src/TokenHound.Infrastructure/Providers/Claude/ClaudeProfileDiscovery.cs`
  - `src/TokenHound.Infrastructure/Providers/Claude/ClaudeProfileDiscovery.Parsing.cs`
  - `tests/TokenHound.Infrastructure.Tests/Providers/ClaudeProfileDiscoveryTests.cs`
- Checks:
  - `rtk dotnet build TokenHound.slnx --no-restore` (0 errors, 0 warnings)
  - `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*ClaudeProfileDiscoveryTests*"` (16 passed, 0 failed)
  - Quality profile verification: zero hits on blocking QA rules across all touched files.
- Validated state: Clean build and all 16 tests passing.
- Reconciled after corrections: Corrected active-profile qualification via read-only parsing in `ClaudeProfileDiscovery.Parsing.cs` (T04/CR-01). Benchmarked discovery latency (T07/CR-04) satisfying amended NFR-03 under DEC-19. All 23 focused discovery tests passing.
- Open items: None.

### ADR candidates

None - direct TechSpec implementation.
