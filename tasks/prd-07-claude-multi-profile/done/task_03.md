# Stable execution context

Load in this exact order:

1. `tasks/prd-07-claude-multi-profile/prd.md`
2. `tasks/prd-07-claude-multi-profile/techspec.md`
3. This file

Use the current PRD and TechSpec versions already loaded; recover only missing or changed sources. This order does not guarantee a host cache hit.

---

# T03 — Catalog mapping, app startup registration, and mock guard

## Outcome

Extend `ProviderCatalog` to format names, badges, and vector glyphs for `claude-*` IDs; register all active Claude profiles at startup in `App.xaml.cs`; update `NotchViewModel` mock fallback logic; and ensure the HUD details flyout header displays the clear account distinction.

## Dependencies and boundaries

- Depends on: T01, T02
- Unblocks: —
- In scope: `ProviderCatalog.cs`, `App.xaml.cs`, `NotchViewModel.cs`, and corresponding tests.
- Out of scope: Backend telemetry endpoints or Anthropic API changes.

## Traceability

| Source | Section | Obligation covered |
| --- | --- | --- |
| FR-06 | `prd.md#functional-requirements` | Provider catalog mapping |
| FR-07 | `prd.md#functional-requirements` | Mock fallback awareness |
| FR-08 | `prd.md#functional-requirements` | Account identification in HUD popup |
| CMP-05 | `techspec.md#components-and-flow` | `ProviderCatalog.cs` mapping |
| CMP-06 | `techspec.md#components-and-flow` | `App.xaml.cs` registration |
| CMP-07 | `techspec.md#components-and-flow` | `NotchViewModel.cs` mock guard |
| TC-05 | `techspec.md#test-approach` | Catalog name, badge, and glyph resolution |
| TC-06 | `techspec.md#test-approach` | Mock fallback check with isolated profile |

## Context to recover on demand

- Applicable skills and rules: `AGENTS.md` (WPF UI guidelines, test execution).
- Existing code:
  - `src/TokenHound.App/ViewModels/ProviderCatalog.cs`
  - `src/TokenHound.App/App.xaml.cs`
  - `src/TokenHound.App/ViewModels/NotchViewModel.cs`
  - `src/TokenHound.App/UI/Controls/TooltipCard.xaml.cs`
- Provider spec: `docs/specs/03-PROVIDER-CLAUDE-CODE.md` §1 & §2

## Work

- [x] T03.1 Update `ProviderCatalog.cs` to match `claude-*`:
  - `ResolveDefaultName`: `"Claude Code (<slug>)"`.
  - `ResolveDefaultBadge`: `"C"`.
  - `ResolveGlyphKey`: `"Glyph.Claude"`.
  - `ResolveGlyphScale`: `0.9676`.
- [x] T03.2 In `App.xaml.cs` `RegisterClaude`, use `ClaudeProfileDiscovery.DiscoverProfiles(onlyActive: true)` to register a separate `ClaudeOAuthProvider` and `ClaudeSessionMonitor` for each active profile (with isolated `RateLimitPolicy`). Fallback to default `"claude"` provider if none are active.
- [x] T03.3 In `NotchViewModel.cs`, adapt `ShouldFallbackToMock` and `HasClaudeCredentials` to consider any registered or discoverable Claude profile.
- [x] T03.4 Add unit tests in `ProviderCatalogTests.cs` and `NotchViewModelTests.cs` verifying multi-profile catalog resolution and mock fallback.

## Acceptance criteria

- `ProviderCatalog.ResolveDefaultName("claude-work")` returns `"Claude Code (work)"`.
- `ProviderCatalog.ResolveGlyphKey("claude-work")` returns `"Glyph.Claude"`.
- Hovering over a `claude-work` HUD ring displays `"Claude Code (work)"` in the popup header.
- Having valid credentials in `~/.claude-work` prevents falling back to mock mode even if `~/.claude` is absent.

## Verification

- Unit: Test `ProviderCatalogTests` for standard and custom slugs.
- Unit: Test `NotchViewModelTests` for mock fallback behavior.
- Manual: Run manual acceptance script from TechSpec to verify dual ring HUD rendering.
- Commands: `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*ProviderCatalog*"`
- Expected evidence: All unit tests pass; HUD displays distinct rings and popup headers.

## Affected files

- Modify: `src/TokenHound.App/ViewModels/ProviderCatalog.cs`
- Modify: `src/TokenHound.App/App.xaml.cs`
- Modify: `src/TokenHound.App/ViewModels/NotchViewModel.cs`
- Create: `tests/TokenHound.Infrastructure.Tests/ViewModels/ProviderCatalogTests.cs`
- Modify: `tests/TokenHound.Infrastructure.Tests/ViewModels/NotchViewModelTests.cs`

## Observability and recovery

- Operational signal: `RegisterClaude` registers all active profiles on startup.
- Recovery: Revert modifications to `App.xaml.cs`, `ProviderCatalog.cs`, and `NotchViewModel.cs`.

## Handoff

> Updated by `sdd-execute-task` during implementation.

- Produced result: Extended `ProviderCatalog` to dynamically format names, badges, vector glyph keys, and scale for `claude-*` IDs. Updated `App.xaml.cs` `RegisterClaude` to discover and register all active Claude profiles with dedicated provider and session monitor instances. Updated `NotchViewModel` mock fallback to inspect any active Claude profile ring. Created `ProviderCatalogTests` and added isolated profile tests to `NotchViewModelTests`.
- Changed files:
  - `src/TokenHound.App/ViewModels/ProviderCatalog.cs`
  - `src/TokenHound.App/App.xaml.cs`
  - `src/TokenHound.App/ViewModels/NotchViewModel.cs`
  - `tests/TokenHound.Infrastructure.Tests/ViewModels/ProviderCatalogTests.cs`
  - `tests/TokenHound.Infrastructure.Tests/ViewModels/NotchViewModelTests.cs`
- Checks:
  - `rtk dotnet build TokenHound.slnx --no-restore` (0 errors, 0 warnings)
  - `rtk dotnet test ... --filter-class "*ProviderCatalogTests*"` (14 passed, 0 failed)
  - `rtk dotnet test ... --filter-class "*NotchViewModelTests*"` (12 passed, 0 failed)
  - `TokenHound.Core.Tests`: 91 passed, 0 failed.
  - `TokenHound.Infrastructure.Tests`: 762 passed, 0 failed.
  - Quality profile verification: zero new blocking hits introduced.
- Validated state: Clean build and all 853 tests across solution passing.
- Reconciled after corrections: Routed snapshot fallback through all-profile check and verified mock fallback behavior with isolated profile and NeedsAuth profile in `NotchViewModelTests` (T05/CR-02). 13 view-model tests passing.
- Open items: None.

### ADR candidates

None - direct TechSpec implementation.
