# PRD — Claude Code Multi-Profile Support

## Problem and context

Developers and AI engineers frequently maintain multiple Claude Code accounts and configurations (for example, separating personal projects from work/client environments, or using different subscription tiers). Claude Code enables this through directory conventions and the `CLAUDE_CONFIG_DIR` environment variable, placing profiles in `.claude` (default) and `.claude-<slug>` directories under `%USERPROFILE%`.

Currently, TokenHound only registers a single static Claude provider instance (`ProviderId = "claude"`). Its discovery mechanism selects either the default profile or the first multi-profile alphabetically, completely ignoring any second or subsequent active Claude profiles. As a result, users cannot observe quotas, rate limits, or session activity across all their Claude Code accounts simultaneously in the TokenHound HUD.

## Outcomes and metrics

| ID | Expected outcome | Metric or evidence |
| --- | --- | --- |
| OBJ-01 | Automatic discovery of all active Claude profiles | Given multiple valid Claude profile directories (`.claude` and `.claude-*`), TokenHound registers and tracks every active profile. |
| OBJ-02 | Simultaneous multi-ring HUD display | Each active profile produces a distinct provider ring in the HUD with its own 5-hour and 7-day usage meters. |
| OBJ-03 | Consistent visual identity | Profile rings display formatted names (`Claude` for default, `Claude (<slug>)` for isolated profiles), retain the official Claude vector mark (`Glyph.Claude`), and have optical scale aligned. |
| OBJ-04 | Isolated rate limiting and session monitoring | Rate-limiting (HTTP 429) or terminal activity in one profile does not block, delay, or falsely indicate activity in another profile. |

## Stories and journeys

| ID | User | Need | Benefit | Flow or edge |
| --- | --- | --- | --- | --- |
| US-01 | Developer with personal & work accounts | Observe quotas for both accounts in real time | Avoid unexpected 5-hour session quota exhaustion on either account without switching config directories | Starting TokenHound with both `~/.claude` and `~/.claude-work` populated shows two Claude rings side-by-side in the HUD. |
| US-02 | Developer running Claude agent in isolated profile | See live activity ring for the active workspace | Immediately know when Claude is thinking/executing commands in the isolated profile | When Claude runs in `~/.claude-work`, the `Claude (work)` ring pulses/animates while the default `Claude` ring remains in its current idle/ready state. |
| US-03 | Developer with only the default account | Normal single-account operation unchanged | Zero regression or UX impact for users with a standard single account | Starting TokenHound with only `~/.claude` registers a single `Claude` ring identical to previous behavior. |
| US-04 | Developer with no default profile but one isolated profile | Discover and display the isolated profile seamlessly | Works even if `~/.claude` was deleted or never initialized | TokenHound discovers `~/.claude-personal` and presents `Claude (personal)`. |

## Functional requirements

| ID | Requirement | Acceptance criterion |
| --- | --- | --- |
| FR-01 | Multi-profile enumeration | `ClaudeProfileDiscovery` must enumerate the default profile (`.claude`) and all existing `%USERPROFILE%\.claude-*` directories, extracting their slug identifier. |
| FR-02 | Active profile qualification | A profile directory is considered active and eligible for registration if it contains a `.credentials.json` file with a readable OAuth access token. |
| FR-03 | Unique provider identification | Each discovered profile must have a deterministic provider ID: `"claude"` for the default profile, and `"claude-<slug>"` (case-normalized) for isolated profiles. |
| FR-04 | Dedicated provider instances | The application startup (`RegisterClaude`) must register a distinct `ClaudeOAuthProvider` instance in `UsageStore` for each active profile, pointing directly to that profile's credentials. |
| FR-05 | Dedicated activity monitors | For each registered Claude profile, a corresponding `ClaudeSessionMonitor` must be registered targeting that profile's `sessions/` directory. |
| FR-06 | Provider catalog mapping | `ProviderCatalog` must resolve display names as `"Claude (<slug>)"` (or formatted title case), badge glyphs, and vector mark `"Glyph.Claude"` for any provider ID matching `"claude-*"` as well as `"claude"`. |
| FR-07 | Mock fallback awareness | `NotchViewModel.ShouldFallbackToMock` and credential checks must evaluate all registered or discoverable Claude profiles so that having credentials in any valid profile prevents unnecessary fallback to mock telemetry. |
| FR-08 | Account identification in HUD popup | The HUD details flyout/popup header must clearly display the account/profile distinction (e.g., `Claude Code (work)` or `Claude Code (default)`) so the user can immediately distinguish which account's limits and metrics are shown. |

## Non-functional requirements

| ID | Attribute | Limit or criterion |
| --- | --- | --- |
| NFR-01 | Architectural purity | Zero UI or OS dependencies added to `TokenHound.Core`. Models, contracts, and policies remain pure. |
| NFR-02 | Independent rate limits | Each `ClaudeOAuthProvider` instance uses an isolated `RateLimitPolicy` and backoff tracker to prevent cross-account rate limit contamination. |
| NFR-03 | Performance | Startup enumeration of Claude profile directories must complete with warm p95 < 25 ms under standard SSD conditions (first/cold invocation documented at ~25–70 ms under runtime/JIT and file system overhead). |
| NFR-04 | Credential safety | Credentials are read-only (`FileShare.ReadWrite | FileShare.Delete`). No token refresh or token writing is performed by TokenHound. |

## User experience

- In the Notch / HUD, each discovered Claude profile receives an individual ring.
- Ring badges/names:
  - Default profile: `Claude Code` (badge `C`, vector glyph `Glyph.Claude`).
  - Isolated profile (e.g. `claude-work`): `Claude (work)` (badge `Cw` or `C`, vector glyph `Glyph.Claude`).
- HUD Details Popup / Tooltip:
  - The popup header prominently displays the provider display name distinguishing the account (e.g. `Claude Code (work)` vs `Claude Code`).
  - Displays detailed breakdown for that specific account's 5-hour rolling session and 7-day quota.
- Error states: If one account's credentials expire (`NeedsAuth`), that specific ring indicates `Execute 'claude login' in terminal` while other accounts continue reporting valid usage.

## Constraints and dependencies

- Conforms to technical specification in `docs/specs/03-PROVIDER-CLAUDE-CODE.md`.
- Conforms to project guidelines in `AGENTS.md` (no token writing, C# coding styles, sealed classes <= 300 lines, test requirements with MTP).
- No external NuGet dependencies required.

## Out of scope

- Setting or modifying `CLAUDE_CONFIG_DIR` or launching Claude CLI sessions from TokenHound.
- Interactive authentication or OAuth login flows inside the TokenHound GUI.
- Arbitrary custom directory paths outside `%USERPROFILE%\.claude*` without configuration.

## Assumptions and sources

- Assumption: Claude Code follows the standard Anthropic convention where isolated profiles reside at `%USERPROFILE%\.claude-<slug>` with a `.credentials.json` file inside. Impact if wrong: custom directories outside this pattern would require a config override mechanism.
- Source: `docs/specs/03-PROVIDER-CLAUDE-CODE.md` §1 & §2.
- Source: Anthropic Claude Code CLI directory structures.

## PRD acceptance gate

- [x] Every requirement has an ID and an observable criterion.
- [x] Metrics, boundaries, and out-of-scope items are explicit.
- [x] Internal rules came from the user or an identified project source.
- [x] Implementation details remain in the TechSpec.
