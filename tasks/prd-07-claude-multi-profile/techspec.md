# TechSpec — Claude Code Multi-Profile Support

## Sources and traceability

- PRD: `tasks/prd-07-claude-multi-profile/prd.md`
- Applicable instructions, rules, and skills:
  - `AGENTS.md` (C# coding rules, MTP test invocation, sealed classes <= 300 lines, zero UI in Core).
  - `dotnet-efficient-validation` (`references/mtp.md`).
  - `sdd-create-techspec` (`references/dotnet.md`, `references/quality-dotnet.md`, `references/preparatory-refactoring.md`).
- Specs and design:
  - `docs/specs/03-PROVIDER-CLAUDE-CODE.md` §1 (Provider ID `claude` / `claude-<slug>`, Display Name `Claude` / `Claude (<slug>)`), §2 (Profile discovery algorithm and `.credentials.json` reading), §4 (Session monitoring per profile directory).
- Evidence in existing code:
  - `src/TokenHound.Infrastructure/Providers/Claude/ClaudeProfileDiscovery.cs`
  - `src/TokenHound.Infrastructure/Providers/Claude/ClaudeOAuthProvider.cs`
  - `src/TokenHound.Infrastructure/Providers/Claude/ClaudeSessionMonitor.cs`
  - `src/TokenHound.App/ViewModels/ProviderCatalog.cs`
  - `src/TokenHound.App/App.xaml.cs`
  - `src/TokenHound.App/ViewModels/NotchViewModel.cs`
  - `src/TokenHound.App/UI/Controls/TooltipCard.xaml.cs`

## Solution summary

Currently, TokenHound only registers a single static `ClaudeOAuthProvider` instance (`PROVIDER_ID = "claude"`). If a developer uses multiple Claude Code accounts (e.g. personal in `~/.claude` and work in `~/.claude-work`), TokenHound strictly reads the default profile and ignores any other accounts.

This technical specification realizes the design from `docs/specs/03-PROVIDER-CLAUDE-CODE.md`:
1. Introduces a lightweight `ClaudeProfile` descriptor in `TokenHound.Infrastructure`.
2. Implements `DiscoverProfiles()` in `ClaudeProfileDiscovery` to enumerate `.claude` and all `.claude-*` directories containing `.credentials.json`.
3. Parameterizes `ClaudeOAuthProvider` and `ClaudeSessionMonitor` so instances can be created for any specific `providerId` and profile directory.
4. Enhances `ProviderCatalog` to dynamically format names (`Claude Code (<slug>)`), icons (`Glyph.Claude`), and badges for any `claude-*` identifier.
5. Updates `App.xaml.cs` to discover all active profiles at startup and register a distinct provider and monitor for each in `UsageStore`.
6. Satisfies `FR-08` by routing the formatted profile name directly through `ProviderRingViewModel.ProviderName`, which `TooltipCard` renders in the flyout header.

## Technical decisions

| ID | PRD obligations | Decision | Reason and evidence | Alternatives and trade-offs |
| --- | --- | --- | --- | --- |
| DEC-04 | FR-01, FR-02, FR-03 | Add `ClaudeProfile` record and `DiscoverProfiles(bool onlyActive = true)` to `ClaudeProfileDiscovery`. | Conforms directly to `docs/specs/03-PROVIDER-CLAUDE-CODE.md` §1 & §2. Inspects `%USERPROFILE%\.claude` and `%USERPROFILE%\.claude-*`. | Alternative: hardcode a known list of profile slugs. Trade-off: static list fails to support arbitrary user workspace names. |
| DEC-05 | FR-04, NFR-02 | Parameterize `ClaudeOAuthProvider` with `providerId` and optional credentials file path / profile path. | Allows independent provider instances per account while keeping backward-compatible default constructors for existing unit tests. | Alternative: create a single aggregator provider that combines accounts. Trade-off: violates TokenHound's single-provider per ring HUD model and hides per-account quotas. |
| DEC-06 | FR-05 | Parameterize `ClaudeSessionMonitor` with `providerId` and `sessionsDirectory`. | Enables independent agent activity monitoring (busy/idle) for each profile's CLI sessions. | Alternative: single monitor scanning all directories. Trade-off: cannot correlate which specific HUD ring should pulse when an agent is active. |
| DEC-07 | FR-06 | Update `ProviderCatalog` to recognize `claude-*` slugs. | Resolves `ProviderName` as `$"Claude Code ({FormatSlug(slug)})"`, badge as `"C"`, and vector glyph as `"Glyph.Claude"`. | Alternative: create individual static entries. Trade-off: would require code edits for every new profile directory name. |
| DEC-08 | FR-04, FR-05 | Register all discovered active Claude profiles in `App.xaml.cs` (`RegisterClaude`). | Iterates over `ClaudeProfileDiscovery.DiscoverProfiles()`. If none found, registers default `"claude"` provider as fallback. Each profile gets an isolated `RateLimitPolicy`. | Alternative: register profiles lazily on first file write. Trade-off: rings would pop up unexpectedly during usage rather than at application startup. |
| DEC-09 | FR-08 | Rely on `ProviderName` binding in `TooltipCard` for account identification in popup header. | `TooltipCard.xaml.cs` sets `ProviderNameText.Text = ProviderName`. With `ProviderName` resolving to `Claude Code (work)`, the header immediately identifies the account. | Alternative: add a new subtitle property to `TooltipCard`. Trade-off: complicates XAML layout when the existing title is designed for provider/account identification. |
| DEC-10 | FR-07 | Update `NotchViewModel.ShouldFallbackToMock` to recognize any registered or discoverable Claude profile. | Prevents falling back to mock mode when the default `.claude` is absent but an isolated profile `.claude-work` is active and valid. | Alternative: only check default profile. Trade-off: users with isolated setups would be falsely forced into mock mode. |

## Components and flow

| ID | Component | New or modified | Responsibility | Dependencies |
| --- | --- | --- | --- | --- |
| CMP-01 | `ClaudeProfile.cs` | New | Record containing `ProviderId`, `DisplayName`, `DirectoryPath`, and `Slug`. | None |
| CMP-02 | `ClaudeProfileDiscovery.cs` | Modified | Enumerates default and `.claude-*` directories, producing `IReadOnlyList<ClaudeProfile>`. | CMP-01 |
| CMP-03 | `ClaudeOAuthProvider.cs` | Modified | Accepts custom `providerId` and credentials file path; reads telemetry from Anthropic API. | CMP-01, CMP-02 |
| CMP-04 | `ClaudeSessionMonitor.cs` | Modified | Accepts custom `providerId` and `sessionsDirectory` path; monitors live CLI activity. | CMP-01 |
| CMP-05 | `ProviderCatalog.cs` | Modified | Maps `claude` and `claude-*` IDs to display names, badges, and vector glyph keys. | None |
| CMP-06 | `App.xaml.cs` | Modified | Discovers active profiles and registers each with `UsageStore`. | CMP-02, CMP-03, CMP-04 |
| CMP-07 | `NotchViewModel.cs` | Modified | Validates presence of credentials across any Claude profile before mock fallback. | CMP-02, CMP-05 |

### Discovery and Execution Flow

```
[Application Startup]
        │
        ▼
ClaudeProfileDiscovery.DiscoverProfiles()
        │
        ├─► ~/.claude (if .credentials.json exists) ──► Profile("claude", "Claude Code", ...)
        │
        └─► ~/.claude-* (e.g. ~/.claude-work) ──────► Profile("claude-work", "Claude Code (work)", ...)
        │
        ▼
For each profile:
  1. Create ClaudeOAuthProvider(providerId, credsPath, isolatedRateLimitPolicy)
  2. Create ClaudeSessionMonitor(providerId, sessionsDir)
  3. Register with UsageStore
        │
        ▼
UsageStore polls usage snapshots & session liveness
        │
        ▼
NotchViewModel creates ProviderRingViewModel for each ProviderId
        │
        ▼
HUD renders separate rings:
  - Ring 1: "claude" -> Badge "C", Glyph "Glyph.Claude"
  - Ring 2: "claude-work" -> Badge "C", Glyph "Glyph.Claude"
Hovering on Ring 2 opens TooltipCard with header "Claude Code (work)"
```

## Contracts and data

### ClaudeProfile

```csharp
namespace TokenHound.Infrastructure.Providers.Claude;

/// <summary>
/// Represents an identified Claude Code configuration profile on disk.
/// </summary>
/// <param name="ProviderId">The unique provider identifier (e.g. "claude" or "claude-work").</param>
/// <param name="DisplayName">The human-readable display name (e.g. "Claude Code" or "Claude Code (work)").</param>
/// <param name="DirectoryPath">The absolute path to the profile configuration directory.</param>
/// <param name="Slug">The profile slug extracted from the directory name, or null for default.</param>
public sealed record ClaudeProfile(
    string ProviderId,
    string DisplayName,
    string DirectoryPath,
    string? Slug);
```

## Integrations and interfaces

- **NTFS File System**: Reads `%USERPROFILE%\.claude\.credentials.json` and `%USERPROFILE%\.claude-<slug>\.credentials.json` using `FileShare.ReadWrite | FileShare.Delete` via `SharedFileReader`.
- **Sessions Directory**: Reads `%USERPROFILE%\.claude\sessions\*.json` and `%USERPROFILE%\.claude-<slug>\sessions\*.json`.
- **Anthropic OAuth Usage API**: `GET https://api.anthropic.com/api/oauth/usage` with header `anthropic-beta: oauth-2025-04-20` and `Authorization: Bearer <accessToken>`.
- **HUD Popups**: `TooltipCard` renders `ProviderName` as its title.

## Errors, security, and recovery

- **Credentials Expired (`401` or `IsExpired`)**: Specific provider enters `NeedsAuth` state; other profiles remain active and unaffected.
- **Throttling (`429`)**: Handled by isolated `RateLimitPolicy` per provider instance. Backoff on one account never delays poll intervals of another account.
- **Malformed Profile Directory**: If a `.claude-*` directory lacks `.credentials.json` or has an invalid token, `DiscoverProfiles(onlyActive: true)` skips it without failing startup.
- **Read-Only Invariant**: TokenHound never writes tokens, refresh tokens, or configuration into `.credentials.json`.

## Sequencing

| Step | Depends on | Verifiable result |
| --- | --- | --- |
| 1. Profile Discovery & Model | — | `ClaudeProfile` created; `ClaudeProfileDiscovery.DiscoverProfiles` returns default and multi-profiles with tests passing. |
| 2. Provider & Monitor Parameterization | Step 1 | `ClaudeOAuthProvider` and `ClaudeSessionMonitor` accept custom `providerId` and paths; tests verify isolated snapshot fetching. |
| 3. Catalog & UI Formatting | Step 1 | `ProviderCatalog` resolves names and glyphs for `claude-<slug>`; `TooltipCard` header displays distinguished account name. |
| 4. Registration & Mock Guard | Steps 1, 2, 3 | `App.xaml.cs` registers all active profiles; `NotchViewModel` prevents false mock fallback when isolated profiles exist. |

## Test approach

- Profile: .NET 9.0 (C# 13), xUnit + AwesomeAssertions, Microsoft.Testing.Platform runner.
- Command: `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*Claude*"`
- E2E: omitted by .NET desktop policy.
- Command prerequisites: Build solution first (`rtk dotnet build TokenHound.slnx --no-restore`).

| ID | Obligations | Level | Scenario | Expected result | Command or project |
| --- | --- | --- | --- | --- | --- |
| TC-01 | FR-01, FR-02, FR-03 | Unit | Discover profiles with default and `.claude-work` directory present | Returns 2 profiles (`claude` and `claude-work`) with correct paths and display names. | `tests/TokenHound.Infrastructure.Tests` |
| TC-02 | FR-01, FR-02 | Unit | Discover profiles when directory lacks `.credentials.json` | Inactive directory is excluded from `DiscoverProfiles(onlyActive: true)`. | `tests/TokenHound.Infrastructure.Tests` |
| TC-03 | FR-04, NFR-02 | Unit | Fetch snapshot with custom `providerId` and profile directory | Returns `Snapshot` with matching custom `ProviderId` using the specified profile's credentials. | `tests/TokenHound.Infrastructure.Tests` |
| TC-04 | FR-05 | Unit | Session monitor with custom `providerId` and sessions directory | Scans specified directory and returns `AgentSession` with matching `ProviderId`. | `tests/TokenHound.Infrastructure.Tests` |
| TC-05 | FR-06, FR-08 | Unit | Catalog name, badge, and glyph resolution for `claude-work` | Name is `"Claude Code (work)"`, badge is `"C"`, glyph is `"Glyph.Claude"`. | `tests/TokenHound.Infrastructure.Tests` |
| TC-06 | FR-07 | Unit | Mock fallback check when only `.claude-personal` has credentials | Returns `false` (no fallback to mock). | `tests/TokenHound.Infrastructure.Tests` |

### Manual Acceptance Script

1. **Setup**: Create temporary directories `%USERPROFILE%\.claude` and `%USERPROFILE%\.claude-work` with test `.credentials.json` files containing dummy tokens.
2. **Launch**: Run `TokenHound.App`.
3. **Verify Rings**: Observe two rings rendered in HUD with Claude vector marks.
4. **Verify Popup Header**: Hover over the second ring. The popup card header must read `Claude Code (work)`.
5. **Cleanup**: Remove temporary profile directories.

## Quality profile

| ID | Rule | Class | Verification command | Prior justification |
| --- | --- | --- | --- | --- |
| QA-01 | `async void` outside event handler | blocking | `rtk rg -n --type cs @src 'async void' $files` | — |
| QA-02 | `.Result` / `.Wait()` in sync context | blocking | `rtk rg -n --type cs @src '\.Result\b\|\.Wait\(\)\|GetAwaiter\(\)\.GetResult\(\)' $files` | — |
| QA-03 | Service locator anti-pattern | blocking | `rtk rg -n --type cs @src 'GetRequiredService<\|GetService<' $files` | — |
| QA-04 | Empty catch block | blocking | `rtk rg -n --type cs @src 'catch\s*\{\s*\}' $files` | — |
| QA-05 | File length > 300 lines (AGENTS.md) | reservation | `rtk rg -c '^' --type cs @src $files` | `App.xaml.cs` pre-existing in baseline |
| QA-06 | Unjustified compiler warning suppression | blocking | `rtk rg -n --type cs @src '#nullable disable\|#pragma warning disable' $files` | — |

- Verification scope: Files in the task diff.
- Escalation trigger: 8+ reservations, a touched file above 500 lines, or duplication in 3+ places.

### Terrain baseline

| File | Lines | Public members | Constructor deps | Cases | Pre-existing hits | Destination |
| --- | --- | --- | --- | --- | --- | --- |
| `src/TokenHound.Infrastructure/Providers/Claude/ClaudeProfileDiscovery.cs` | 277 | 5 | 1 | 0 | None | Recorded |
| `src/TokenHound.Infrastructure/Providers/Claude/ClaudeOAuthProvider.cs` | 266 | 1 | 4 | 0 | None | Recorded |
| `src/TokenHound.Infrastructure/Providers/Claude/ClaudeSessionMonitor.cs` | 278 | 3 | 2 | 0 | None | Recorded |
| `src/TokenHound.App/ViewModels/ProviderCatalog.cs` | 92 | 4 | 0 | 0 | None | Recorded |
| `src/TokenHound.App/App.xaml.cs` | 415 | 0 | 0 | 0 | QA-05: 415 lines (exceeds 300 line target) | Recorded |
| `src/TokenHound.App/ViewModels/NotchViewModel.cs` | 265 | 3 | 2 | 0 | None | Recorded |

- Preparatory refactoring: **Not recommended**. Files are below saturation limits; changes are modular and additive.

## Observability and rollout

- Signals: Structured logs on application start noting the number of discovered Claude profiles and their IDs.
- Rollout: Single release deployment; transparently discovers profiles without user intervention.
- Backward Compatibility: Users with a single standard profile (`~/.claude`) experience identical behavior and ID (`"claude"`).

## Risks and open items

- Risk: A `.claude-*` directory exists but is an abandoned empty folder. Mitigation: `DiscoverProfiles(onlyActive: true)` verifies the existence of `.credentials.json` before registering.
- Open items: None.

## Relevant files

- Modify:
  - `src/TokenHound.Infrastructure/Providers/Claude/ClaudeProfileDiscovery.cs`
  - `src/TokenHound.Infrastructure/Providers/Claude/ClaudeOAuthProvider.cs`
  - `src/TokenHound.Infrastructure/Providers/Claude/ClaudeSessionMonitor.cs`
  - `src/TokenHound.App/ViewModels/ProviderCatalog.cs`
  - `src/TokenHound.App/App.xaml.cs`
  - `src/TokenHound.App/ViewModels/NotchViewModel.cs`
  - `tests/TokenHound.Infrastructure.Tests/Providers/ClaudeProfileDiscoveryTests.cs`
  - `tests/TokenHound.Infrastructure.Tests/Providers/ClaudeOAuthProviderTests.cs`
  - `tests/TokenHound.Infrastructure.Tests/Providers/ClaudeSessionMonitorTests.cs`
- Create:
  - `src/TokenHound.Infrastructure/Providers/Claude/ClaudeProfile.cs`
  - `tests/TokenHound.Infrastructure.Tests/ViewModels/ProviderCatalogTests.cs`
