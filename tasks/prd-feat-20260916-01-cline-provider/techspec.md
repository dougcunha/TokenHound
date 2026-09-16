# TechSpec — Cline usage provider (borrowed telemetry + local evidence)

## Sources and traceability

- PRD: `tasks/prd-feat-20260916-01-cline-provider/prd.md`
- Applicable instructions and skills: `AGENTS.md`; `dotnet-efficient-validation`; `repository-cli-efficiency`; `sdd-create-prd`; `sdd-create-techspec`
- Evidence in existing code: `src/TokenHound.Infrastructure/Providers/Cline/*` (all new), `src/TokenHound.Core/Models/{Snapshot,ClineAccountUsage,ClineLocalUsage}.cs`, `src/TokenHound.App/ViewModels/ProviderUsageRowFactory{,.Cline}.cs`, `src/TokenHound.App/ViewModels/ProviderCatalog.cs`, `src/TokenHound.App/App.xaml.cs`, `src/TokenHound.Infrastructure/Configuration/UserSettingsFile.cs`; patterns reused from `OpenCodeCredentialDiscovery`, `AntigravityActivityMonitor`, `SharedFileReader`, `RateLimitPolicy`
- Provider spec family: `docs/specs/01-READING-STRATEGY-RESILIENCE.md`, `docs/specs/12-PROVIDER-OPENCODE.md` (structural model; a dedicated `docs/specs/13-PROVIDER-CLINE.md` is a follow-up, see Risks)

## Solution summary

Cline is added as a borrowed-session provider: TokenHound reads the credential Cline itself stores and refreshes (never writing it), calls `api.cline.bot/api/v1/users/me` for account telemetry, and derives everything Cline does not publish from local evidence — token usage and the free-model-limit from `sessions.db`, liveness from the hub lock with a process-name fallback. The domain projection is honest by construction: the credit balance is a remaining value without a denominator, and Pass windows appear only when sampled transactions provably cover the window.

Implementation is layered per the architecture: pure Core models (`ClineAccountUsage`, `ClineLocalUsage`, optional `Snapshot` members), Infrastructure adapters split into partials to respect the 300-line file cap, and App presentation (`ProviderUsageRowFactory.Cline` partial, catalog entries, official Cline glyph) plus DI registration with default-enabled settings.

## Technical decisions

| ID | PRD obligations | Decision | Reason and evidence | Alternatives and trade-offs |
| --- | --- | --- | --- | --- |
| DEC-01 | RF-02, RNF-02 | Report the credit balance as a remaining value only; `ClineAccountUsage.BalanceCredits` never becomes a `UsedFraction`. | `api.cline.bot` returns a bare balance with no denominator (verified 2026-09-15); AGENTS.md forbids inventing limits; `ClineAccountUsage.cs:11-15` documents it. | Deriving a fraction from an unknown total — rejected: fabricated number. |
| DEC-02 | RF-04, RNF-02 | Emit Pass windows (5h/7d/30d) only when sampled transactions provably cover the window: bounded complete page and no dateless transaction; otherwise the window is skipped entirely. | `ClinePassWindowMapper` + tests: fractions 0.25/0.125/0.1875 with `TotalUnits`/`RemainingUnits`; truncated-page and dateless cases skip. Sharing the `costUsd` unit is an explicit PRD assumption. | Estimating coverage from partial pages — rejected: overstates usage. |
| DEC-03 | RF-01, RNF-01 | Credentials: `CLINE_API_KEY` env first (bare values get `workos:` prefixed), then `%USERPROFILE%\.cline\data\settings\providers.json` (`cline`/`cline-pass` keys, `settings.auth.*`), read via `SharedFileReader` only; expired tokens surface NeedsAuth guidance instead of refreshing. | `ClineCredentialDiscovery.cs:95-123,146-162`; AGENTS.md invariant (borrow read-only, never refresh other tools' credentials); partial writes tolerated as null and retried next poll. | Owning a refresh flow against the WorkOS endpoint — rejected: writes credentials Cline owns. |
| DEC-04 | RF-03, RNF-03 | Typed exceptions per failure class (`ClineAuthException` 401, `ClineEntitlementException` 403, `ClineRateLimitException` 429 + `RetryAfterSeconds`, `ClineTimeoutException`) mapped to `NeedsAuth`/`AccessDenied`/`RateLimited`/`Stale` snapshots; 429 deadlines persist through `RateLimitPolicy` before any dispatch. | `ClineAccountClient.Failures.cs`; `ClineUsageProvider.cs:96-97` consults the deadline before discovery; spec 01 §4 forbids honoring `Retry-After: 0` literally. | Generic failure strings — rejected: loses retry-after semantics and engine integration. |
| DEC-05 | RF-06 | The free-tier limit is detected only from local session messages ("free limit reached on model … try again in X"); the parsed delay is derived evidence that becomes the block reset. | `ClineFreeModelLimit.IsActive(now)` gates the block (`ClineUsageProvider.Snapshots.cs:36-50`); Cline publishes no API-side signal (verified). | Polling the API to detect exhaustion — impossible: free plan returns 404 without limit state. |
| DEC-06 | RF-07 | Hub lock (`.cline\data\locks\hub\production.json`, pid/startedAt) validated via `ProcessLiveness` first; names `cline`/`cline-cli`/`cline.exe` only as fallback; busy when `sessions.db` `MAX(updated_at)` ≤ 60 s. | `ClineActivityMonitor.cs:113-123` (lock is a stronger signal than a name lookup); `BUSY_THRESHOLD` 60 s; delegates injectable for tests. | Process-name only — rejected: ambiguous pid ownership and stale-start races. |
| DEC-07 | RF-02, RF-05 | Add dedicated optional `Snapshot` members (`ClineAccount`, `ClineLocal`) instead of forcing balance/local totals into `LimitWindows`. | `Snapshot.cs:41-48`; balance and token totals are not quota windows; additive change kept 720 infra tests green. | Generic `Dictionary<string, object>` extension bag — rejected: loses typing and XML documentation. |
| DEC-08 | RF-06, RNF-04 | Split large adapters into partials (provider main/`.Evidence`/`.Snapshots`; client main/`.Failures`) to keep every file ≤ 300 lines. | Largest Cline file is 272 lines; AGENTS.md caps files at 300 with partials as the sanctioned mechanism. | One 500+-line provider file — rejected: violates the cap and review scoping. |
| DEC-09 | RF-08 | Use the official Cline SVGs supplied in `D:\MyProjects\TokenHound\src\TokenHound.App\Assets\Logos` for `Glyph.Cline`, preserving the contour and converting both rounded eyes to arcs with nonzero fill; catalog name "Cline", badge "Cl", scale 1.0 remain unchanged. | The dark and white SVGs have identical geometry; the consuming WPF Path supplies the monochrome fill and uses Uniform stretch. | The temporary terminal-caret mark is replaced by the supplied official asset. |
| DEC-10 | RF-09 | Register provider + monitor in `App.xaml.cs` (engine composition root), add `cline` to `UserSettingsFile.DEFAULT_PROVIDERS` and `appsettings.json`, add the provider to disposable resources. | `App.xaml.cs:270-280` mirrors `RegisterOpenCode`; `UserSettingsFile.cs:21`; `appsettings.json` lists all default providers. | A plugin/DI-container registration — the engine uses explicit composition; consistency wins. |

## Components and flow

| ID | Component | New or modified | Responsibility | Dependencies |
| --- | --- | --- | --- | --- |
| CMP-01 | `Core/Models/ClineAccountUsage`, `ClineLocalUsage`, `Snapshot` | Modified (new members) | Domain projections; balance without fraction; local token totals with `TotalTokens` | — |
| CMP-02 | `Providers/Cline/ClineCredentialDiscovery` + `ClineProvidersJson` + `ClineAuthDto` | New | Env-first read-only credential discovery and providers.json parsing | CMP-10, `SharedFileReader` |
| CMP-03 | `Providers/Cline/ClineAccountClient` + `.Failures` + DTOs (`ClineAccountDto`, `ClineUsageDto`) | New | HTTP telemetry fetch with UA/Bearer contract and typed failure mapping | CMP-02, CMP-10 |
| CMP-04 | `Providers/Cline/ClinePassWindowMapper` | New | Maps Pass caps to `LimitWindow`s only under proven window coverage | CMP-03 DTOs |
| CMP-05 | `Providers/Cline/ClineLocalSessionParser` + `ClineLocalSessionReader` + `ClineLocalUsageSample` | New | Bounded 24 h aggregation of local token usage and free-limit detection | CMP-06 |
| CMP-06 | `Providers/Cline/ClineFreeModelLimit` + `ClineFreeLimitHit` | New | Parses the free-limit message; active-state with reset time | — |
| CMP-07 | `Providers/Cline/ClineActivityReader` + `ClineHubSnapshot` + `ClineActivityMonitor` | New | Hub lock liveness, process fallback, `MAX(updated_at)` busy detection | `ProcessDiscovery`, `ProcessLiveness` |
| CMP-08 | `Providers/Cline/ClineUsageProvider` (main/`.Evidence`/`.Snapshots`) | New | Orchestrates discovery → local read → fetch → snapshot; last-good retention; rate-limit gate | CMP-02..06, `RateLimitPolicy` |
| CMP-09 | `App/ViewModels/ProviderUsageRowFactory.Cline` (+ dispatch in main partial) | Modified | Presentation rows `cline:credits`/`cline:local`/`cline:freelimit` | CMP-01 |
| CMP-10 | `Providers/Cline/Cline{Auth,Entitlement,RateLimit,Timeout}Exception` | New | Failure vocabulary between client and provider | — |
| CMP-11 | `App.xaml.cs` registration, `UserSettingsFile`, `appsettings.json`, `ProviderCatalog`, `ProviderGlyphs.xaml` | Modified | Composition, defaults, presentation catalog, glyph | CMP-08, CMP-09 |

Flow: `UsageStore` tick → `ClineUsageProvider.GetSnapshotAsync` → rate-limit deadline check (local reject, no dispatch) → `ClineCredentialDiscovery` (null → NeedsAuth; expired → NeedsAuth guidance) → `ClineLocalSessionReader` (24 h sample + free limit) → `ClineAccountClient` (200 → account + Pass windows; typed failures → status snapshots) → `CreateOkSnapshot` attaches `ClineAccount`/`ClineLocal`, `LimitWindows`, optional free-limit `ActiveBlock`. In parallel, `ClineActivityMonitor.CheckLivenessAsync` → hub lock pid validated → fallback name lookup → `AgentSession` Busy/Idle from activity age. `ProviderUsageRowFactory` dispatch → `AddClineRows`.

## Contracts and data

- `ClineAccountUsage` (record, `init`/`required`): `BalanceCredits: decimal?` (remaining value, never a denominator — DEC-01), `PlanName: string?`, `HasPassSubscription: bool`.
- `ClineLocalUsage`: `InputTokens/OutputTokens/CacheReadTokens/CacheWriteTokens: long`, `ModelCallCount: long`, `TotalTokens: long`, `WindowStartUtc/LastActivityUtc: DateTimeOffset?` — all defaults `0`/`null` so an empty local database still yields a valid sample.
- `Snapshot` additions (optional, default null): `ClineAccount: ClineAccountUsage?`, `ClineLocal: ClineLocalUsage?` — additive; existing JSON round-trips unaffected (verified by the 720-test infra suite).
- `ClineAuthDto`: `AccessToken` (with verbatim `workos:` prefix), `ExpiresAtUtc?`, `AccountId?`, `Source` (non-sensitive: `environment:CLINE_API_KEY` / `providers.json`).
- `ClineRateLimitException`: `RetryAfterSeconds: int?` — feeds `RateLimitPolicy`; never honored as `0` alone (spec 01 §4).
- `ClineFreeLimitHit`: `ResetTimeUtc` + `IsActive(now)` — local, derived evidence only.

## Integrations and interfaces

- `GET https://api.cline.bot/api/v1/users/me` — headers `Authorization: Bearer <workos:...>` (prefix verbatim), `User-Agent: TokenHound` (required; verified), `Accept: application/json`. 200 → `ClineAccountDto` payload; 401/403/429/timeout → typed exceptions (DEC-04). Timeout: 10 s client default; cancellation propagated.
- `%USERPROFILE%\.cline\data\settings\providers.json` — read-only (`SharedFileReader`, `FileShare.ReadWrite | FileShare.Delete`); JSON keys `cline`/`cline-pass` → `settings.auth.{accessToken,expiresAt,accountId}`; tolerant of comments/partial writes.
- `%USERPROFILE%\.cline\data\sessions.db` — SQLite WAL, `Mode=ReadOnly`, `immutable=1` fallback when `-shm` missing; aggregation bounded to 24 h.
- `.cline\data\locks\hub\production.json` — hub lock `{pid, startedAt}`; never locked or written by TokenHound.
- App contract: `IUsageProvider.ProviderId = "cline"`, `IActivityMonitor.ProviderId = "cline"`; rows keyed `cline:credits`/`cline:local`/`cline:freelimit`; settings default `Enabled: true`.

## Errors, security, and recovery

- Errors and edges: missing credential → NeedsAuth (no network dispatch); expired token → NeedsAuth with refresh guidance; 401 → NeedsAuth; 403 → AccessDenied; 429 → RateLimited with persisted deadline (`Retry-After` floor-raised, never immediate retry on 0); timeout/network error → Stale with last-good windows; free limit active → Ok + `ActiveBlock` (provider still healthy); malformed `providers.json` or locked DB → tolerated as empty evidence, retried next poll.
- Authorization and sensitive data: tokens are never logged (`Log.Debug` records the non-sensitive `Source` only); no token is ever written or refreshed; `providers.json` is opened read-only with sharing so Cline can rotate underneath.
- Concurrency and idempotency: all local reads are lock-free; SQLite `Mode=ReadOnly` cannot corrupt the writer; snapshot construction is pure; the provider keeps `_lastSuccessfulSnapshot` for degradation, mutated only on success paths.
- Rollback or reversal: remove `cline` from settings/providers catalog (HUD hides rows); code reverts via `git revert` of the feature commits; no data migration exists to undo.

## Sequencing

| Step | Depends on | Verifiable result |
| --- | --- | --- |
| 1. Core models + exceptions | — | Core builds; contracts pure |
| 2. Discovery + providers.json parser | 1 | `ClineCredentialDiscoveryTests` pass |
| 3. Account client + failure mapping | 1 | `ClineAccountClientTests` pass |
| 4. Local parser/reader + free limit | 1 | `ClineLocalSessionParserTests` pass |
| 5. Pass window mapper | 3 | `ClinePassWindowMapperTests` pass |
| 6. Usage provider partials + activity monitor | 2,3,4,5 | `ClineUsageProviderTests`, `ClineActivityMonitorTests` pass |
| 7. App wiring (factory, catalog, glyph, DI, defaults) | 6 | Row factory tests pass; app builds warning-free |
| 8. Full validation | 7 | Solution green; Cline suite 41/41; Core 91, Infra 720 pass |

## Test approach

- Profile: .NET 10 (`net10.0`), Microsoft.Testing.Platform (MTP), xUnit v3 — evidence: `TokenHound.Infrastructure.Tests.csproj` (`UseWPF` on the App project, MTP runner); route: `rtk dotnet test --project <path> --no-build --no-restore -- --minimum-expected-tests 1` with MTP filters after `--` (e.g. `--filter-class "*Cline*"`); build first when sources change.
- E2E: omitted by desktop .NET policy.
- Command prerequisites and exclusions: no desktop UI automation; HUD rendering verified manually; always `--minimum-expected-tests 1` (zero executed tests is not a pass).
- Manual acceptance: launch `TokenHound.App` via Windows MCP `App` (`mode="launch_executable"`); screenshot the primary monitor; confirm the Cline glyph and rows render. Owner: developer; executed this session up to the pending visual smoke.

| ID | Obligations | Level | Scenario | Expected result | Command or project |
| --- | --- | --- | --- | --- | --- |
| TC-01 | RF-01, RNF-01 | unit | Env credential wins and gains `workos:`; file parse of both keys; partial read tolerated as null | Mapped `ClineAuthDto` or null, never a write | `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --filter-class "*ClineCredentialDiscovery*" --minimum-expected-tests 1` |
| TC-02 | RF-02, RF-03, RNF-03 | unit | 200 mapping; 401/403/429/timeout typed failures; Retry-After deadline honored before dispatch | Exceptions and statuses per DEC-04; no dispatch while deadline active | Same route, `--filter-class "*ClineAccountClient*"` |
| TC-03 | RF-04 | unit | Full coverage → 3 windows with fractions and units; truncated page or dateless transaction → windows skipped | `UsedFraction` only under proven coverage | Same route, `--filter-class "*ClinePassWindowMapper*"` |
| TC-04 | RF-05, RF-06 | unit | Token aggregation within 24 h; free-limit message parsing and `IsActive` expiry | Correct totals; block only while active | Same route, `--filter-class "*ClineLocalSessionParser*"` |
| TC-05 | RF-07 | unit | Hub lock alive → used; dead lock → name fallback; busy at ≤ 60 s activity | `AgentSession` state per DEC-06 | Same route, `--filter-class "*ClineActivityMonitor*"` |
| TC-06 | RF-03, RF-06, OBJ-05 | unit | Snapshot assembly: rate-limited short-circuit, needs-auth, stale retention, free-limit block, derived fidelity | Snapshot fields per Snapshots partial | Same route, `--filter-class "*ClineUsageProvider*"` |
| TC-07 | RF-08 | unit | Rows `cline:credits` (12.5 remaining + plan), `cline:local` (125 tokens / 2 calls), `cline:freelimit` (Limit reached / Resets in 1h 30m); non-Cline → empty | Row content matches spec | Same route, `--filter-method "*ProviderUsageRowFactoryClineTests*"` |
| TC-08 | RF-09, RNF-04 | integration | Whole Cline suite green together with Core/Infra suites | 41 Cline tests; Core 91; Infra 720; zero warnings | Full-suite runs per profile route |

## Quality profile

Rules this feature can violate; measured against the feature's own files on 2026-09-16.

| ID | Rule | Class | Verification command | Prior justification |
| --- | --- | --- | --- | --- |
| QA-01 | `async void` / `.Result` / `.Wait()` / `GetAwaiter().GetResult()` | blocking | `rtk rg -n --type cs 'async void|\.Result\b|\.Wait\(\)|GetAwaiter\(\)\.GetResult\(\)' <feature files>` | — |
| QA-02 | Service locator (`GetRequiredService`/`GetService`/`ServiceLocator`) in business code | blocking | `rtk rg -n --type cs 'GetRequiredService<|GetService<|ServiceLocator' <feature files>` | — |
| QA-03 | Empty catch swallowing failures | blocking | `rtk rg -n --type cs 'catch\s*\{\s*\}|catch \(Exception\w*\)\s*\{\s*\}' <feature files>` | — |
| QA-04 | `#nullable disable` / `#pragma warning disable` | blocking | `rtk rg -n --type cs '#nullable disable|#pragma warning disable' <feature files>` | — |
| QA-05 | Generic `throw new Exception(`; uninjected `DateTime.Now/UtcNow` | reservation | `rtk rg -n --type cs 'throw new Exception\(|DateTime\.(Now|UtcNow)' <feature files>` | — |
| QA-06 | Parameter lists with 4+ arguments | reservation | `rtk rg -n --type cs '\w+\((?:[^),]+,){3,}[^)]*\)' <feature files>` | Optional-parameter DI ctor (`ClineUsageProvider`) is the house pattern |
| QA-07 | File size above 500 lines | reservation | `rtk rg -c '^' --type cs <feature files>` | — |

- Verification scope: `src/TokenHound.Infrastructure/Providers/Cline/**`, `src/TokenHound.Core/Models/Cline{Account,Local}Usage.cs`, `src/TokenHound.App/ViewModels/ProviderUsageRowFactory{,.Cline}.cs`, `src/TokenHound.App/ViewModels/ProviderCatalog.cs`, `src/TokenHound.App/App.xaml.cs`, and the Cline test files.
- Escalation trigger: 8+ reservations, a touched file above 500 lines, or duplication in 3+ places → suggest `refactoring-analysis`/`architectural-analysis`/`deep-review` at review.

### Terrain baseline

Measured 2026-09-16 with the profile commands above (line counts via `rg -c '^'`; all blocking and reservation greps returned zero hits on the feature files). Structural measures: every feature file ≤ 272 lines; constructor deps ≤ 6; public members within the 10-member guideline; no saturated switch or if-chain (≤ 4 cases).

| File | Lines | Public members | Constructor deps | Cases | Pre-existing hits | Destination |
| --- | --- | --- | --- | --- | --- | --- |
| `src/TokenHound.Infrastructure/Providers/Cline/*.cs` (31 files) | 19–272 | ≤ 10 | ≤ 6 | ≤ 4 | none | — |
| `src/TokenHound.Core/Models/ClineAccountUsage.cs` / `ClineLocalUsage.cs` | 26 / 54 | ≤ 6 | 0 | 0 | none | — |
| `src/TokenHound.App/ViewModels/ProviderUsageRowFactory.Cline.cs` | 67 | 1 | 0 | ≤ 4 | none | — |
| `src/TokenHound.App/ViewModels/ProviderUsageRowFactory.cs` | 159 | ~5 | 0 | dispatch ≤ 8 | none | — |
| `src/TokenHound.App/ViewModels/ProviderCatalog.cs` | 92 | ~4 | 0 | ≤ 4 | none | — |
| `src/TokenHound.App/App.xaml.cs` | 414 | ~8 | — | — | none (below 500) | — |
| `src/TokenHound.Infrastructure/Configuration/UserSettingsFile.cs` | 188 | ~8 | ~2 | ≤ 4 | none | — |
| `tests/TokenHound.Infrastructure.Tests/Providers/Cline/*.cs` + `ViewModels/ProviderUsageRowFactoryClineTests.cs` | 110–232 | ≤ 8 | 0 | ≤ 6 | none | — |

- Preparatory refactoring: not recommended — all targets are new or healthy files (no threshold crossed), and the App files the feature touched (`App.xaml.cs` 414 lines, factory 159+67) sit under every structural threshold; the feature was implemented without preparatory work and the measured terrain confirms no debt was amplified.

## Observability and rollout

- Signals: structured Serilog events (credential `Source` at Debug — never the token); snapshot statuses surface through the HUD (`NeedsAuth`, `AccessDenied`, `RateLimited`, `Stale`); free-limit block reason `FreeModelLimitReached`.
- Migration and compatibility: additive only — new `Snapshot` members default null; new settings key defaults to enabled; no on-disk format change.
- Rollout and rollback: ship behind the existing provider defaults; disable by removing `cline` from settings (rows disappear); full rollback via `git revert` of the feature commits.

## Risks and open items

- Risk: Cline changes `api.cline.bot` contract or the `providers.json` schema (low/medium, impact: provider goes Stale/NeedsAuth). Mitigation: typed failure mapping degrades gracefully; contract verified live on 2026-09-15.
- Risk: Pass-cap unit mismatch (`costUsd` assumption, DEC-02). Mitigation: fail-safe mapper skips windows instead of showing wrong fractions.
- Risk: hub lock path relocation. Mitigation: process-name fallback keeps liveness working.
- Resolved: `Glyph.Cline` now uses the supplied official SVG geometry (DEC-09). Validation: WPF XamlReader loaded the dictionary; the contour matches the SVG exactly, both eye centers are filled, and the face interior remains open. App build passed with zero errors and warnings. On-screen HUD inspection remains pending.
- Open item: a dedicated `docs/specs/13-PROVIDER-CLINE.md` in the provider spec family would document the verified contract for future maintainers; recommended follow-up, not required for this feature.

## Relevant files

- Modify: `src/TokenHound.Core/Models/Snapshot.cs`, `src/TokenHound.App/ViewModels/ProviderUsageRowFactory.cs`, `src/TokenHound.App/ViewModels/ProviderCatalog.cs`, `src/TokenHound.App/App.xaml.cs`, `src/TokenHound.App/Assets/Logos/ProviderGlyphs.xaml`, `src/TokenHound.App/appsettings.json`, `src/TokenHound.Infrastructure/Configuration/UserSettingsFile.cs`, `tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj`
- Create: `src/TokenHound.Core/Models/ClineAccountUsage.cs`, `src/TokenHound.Core/Models/ClineLocalUsage.cs`, `src/TokenHound.Infrastructure/Providers/Cline/**` (31 files), `tests/TokenHound.Infrastructure.Tests/Providers/Cline/**`, `tests/TokenHound.Infrastructure.Tests/ViewModels/ProviderUsageRowFactoryClineTests.cs`