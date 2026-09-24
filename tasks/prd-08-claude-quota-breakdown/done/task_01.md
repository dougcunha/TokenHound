# Stable execution context

Load in this order:

1. `tasks/prd-08-claude-quota-breakdown/prd.md`
2. `tasks/prd-08-claude-quota-breakdown/techspec.md`
3. This file

Recover only a source missing or changed since the current session. The manifest owns state and dependency links.

---

# T01 — Normalize Claude quota windows

## Outcome

Claude provider snapshots contain the five-hour session, overall weekly usage, and each valid reported model or scoped quota as distinct, ordered windows with only API-supported values. A later 429 retains the whole successful breakdown.

## Dependencies and boundaries

- Depends on: —.
- Unblocks: T02.
- In scope: Core's explicit-fraction contract and focused test extraction (EX-01), response data contract, defensive mapping, known-alias deduplication, rollover fallback, provider integration, per-profile and 429 regression evidence.
- Out of scope: HUD labels/layout, spend and credits, new requests or token refresh, unrelated providers.

## Traceability

| Source | Section | Obligation covered |
| --- | --- | --- |
| OBJ-01–OBJ-03, US-01–US-04, FR-01, FR-03, FR-04, FR-05, FR-06, FR-07, FR-08 | `prd.md#functional-requirements` | Accurate quota snapshots, order, optionality, independence, stale behavior. |
| NFR-01, NFR-02, NFR-03, NFR-05 | `prd.md#non-functional-requirements` | Read-only credential and network discipline, data integrity, Core purity. |
| DEC-02–DEC-04, DEC-06, DEC-07, DEC-09; CMP-01–CMP-03, CMP-07 | `techspec.md#technical-decisions` | Core fraction, response and mapper contracts, local extraction, existing gate. |
| TC-01–TC-04 | `techspec.md#test-approach` | JSON, alias, malformed, profile, and rate-limit evidence. |

## Context to recover on demand

- Applicable skills: `sdd-execute-task`, `no-workarounds`, `repository-cli-efficiency`, `dotnet-efficient-validation`; follow the approved TechSpec quality profile.
- Existing code: `LimitWindow.UsedFraction`, `DomainModelsTests`, `ClaudeUsageResponse`, `ClaudeWindowDto`, and `ClaudeOAuthProvider` at the spans in TechSpec; `UsageStore.Refresh` owns persisted 429 dispatch gating.
- Approved exception: `exception-01.md` for the Core contract and test extraction; workflow DEC-10 records authorization.
- Provider spec: `docs/specs/03-PROVIDER-CLAUDE-CODE.md` §3 for `limits`, aliases, rollover, and backoff.
- Runner: `global.json` native MTP; read `.agents/skills/dotnet-efficient-validation/references/mtp.md` before tests.

## Work

- [x] T01.1 Preserve an explicitly assigned fraction without `TotalUnits` in Core, and move/update `LimitWindow` tests so touched files meet the 300-line limit. Extend the Claude response contract for optional `limits` and semantic top-level quota fields; distinguish missing utilization from 0%.
- [x] T01.2 Add a focused internal Claude mapper with per-entry validation, proven-alias deduplication, deterministic order, and null count/total fields. Extract the old mapping from `ClaudeOAuthProvider` into it, retaining standard names and existing status flow.
- [x] T01.3 Add focused tests for Core explicit and absent fractions, scoped and named quotas, valid 0%, null and malformed entries, aliases, rollover, percent-only data, independent profiles, and stale carry-forward. Reuse existing `UsageStoreGatingTests` to verify persisted deadline behavior; add a focused gap test only if that coverage is absent.
- [x] T01.4 Build Core.Tests and Infrastructure.Tests, run new and existing Core model and Claude provider/store tests, inspect the diff and quality profile, and record evidence in the handoff and manifest.

## Acceptance criteria

- `limits` and valid `seven_day_*` objects become windows without extra network traffic; unrelated fields do not.
- Five-hour and overall weekly entries appear at most once and remain first when available, even at session rollover.
- A 0% quota is retained; null, absent, out-of-range, and nonnumeric percentages do not fabricate rows.
- `RemainingUnits` and `TotalUnits` stay null for percentage-only Claude data.
- Core preserves an explicit fraction with null total while remaining-only windows have null fraction; both touched Core test files finish at or below 300 lines.
- Separate profile snapshots remain separate; 429/stale snapshots keep every prior window, and the store does not dispatch before a persisted deadline.
- Source files and methods meet project limits; Core stays free of UI/OS dependencies, and credential/retry behavior is unchanged.

## Verification

- Unit: Core model tests for explicit-percent and remaining-only windows, plus mapper/deserialization fixtures for valid, absent, malformed, duplicate, and rollover cases; expected windows and quantities asserted.
- Integration: in-process HTTP handler exercises provider success→429 and separate profile responses; existing store gate tests establish pre-dispatch behavior. This double proves local contract handling, not the live Anthropic response.
- E2E: omitted by .NET desktop policy.
- Manual: deferred to T02/HIL 3; live API availability is separately reported.
- Commands: restore once only if assets are missing; build Core.Tests then Infrastructure.Tests with `rtk dotnet build <project> --no-restore --nologo --verbosity:minimal`; run `rtk dotnet test --project tests/TokenHound.Core.Tests/TokenHound.Core.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*LimitWindowTests*"`, the existing `DomainModelsTests`, then `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*ClaudeQuota*"` and targeted existing `ClaudeOAuthProviderTests`/`UsageStoreGatingTests`. Preserve and check `$LASTEXITCODE`.
- Environment dependency: SDK 10.0.401, existing package assets or one restore; no live credential is needed for automated tests.
- Expected evidence: nonzero passing Core and Infrastructure test counts, affected-project builds, snapshot assertions, and diff/quality findings.

## Affected files

- Modify: `src/TokenHound.Infrastructure/Providers/Claude/ClaudeUsageResponse.cs`, `ClaudeWindowDto.cs`, `ClaudeOAuthProvider.cs`.
- Modify: `src/TokenHound.Core/Models/LimitWindow.cs`, `tests/TokenHound.Core.Tests/Models/DomainModelsTests.cs`.
- Create: `tests/TokenHound.Core.Tests/Models/LimitWindowTests.cs`, `src/TokenHound.Infrastructure/Providers/Claude/ClaudeQuotaWindowMapper.cs`, and focused `ClaudeQuota*` tests under `tests/TokenHound.Infrastructure.Tests/Providers/`.

## Observability and recovery

- Operational signal: existing provider status and structured rate-limit transition logs; do not log response payloads.
- Recovery: source rollback of T01 files; no persisted schema migration.

## Handoff

> Updated by `sdd-execute-task` during implementation. Started by an earlier session that did not record it and was completed by the resuming coordinator (workflow REC-04).

- Produced result: Core `LimitWindow.UsedFraction` is now a plain nullable auto-property, so an explicit fraction survives without `TotalUnits`. The new internal `ClaudeQuotaWindowMapper` reads valid `limits` entries first, then top-level `five_hour`/`seven_day` as rollover fallbacks, then semantic `seven_day_*` objects. It canonicalizes `session`/`weekly_all`/`weekly_<model>`, keys scoped kinds by group or scope, skips null/malformed/out-of-range/non-quota entries individually, and orders the result session, overall weekly, then the rest by name and group. Every window has only a percent-derived fraction, an API-supplied reset, and a proven period; counts and totals stay null. `ClaudeWindowDto` exposes nullable `Utilization`/`ResetsAt` and uses the mapper's shared value parsing. `ClaudeOAuthProvider` delegates to the mapper; its credential, status, stale, and 429 flow is unchanged.
- Changed files: `src/TokenHound.Core/Models/LimitWindow.cs`; `src/TokenHound.Infrastructure/Providers/Claude/{ClaudeOAuthProvider,ClaudeUsageResponse,ClaudeWindowDto}.cs`; new `ClaudeQuotaWindowMapper.cs`; `tests/TokenHound.Core.Tests/Models/DomainModelsTests.cs` (LimitWindow tests moved out, 302→204 lines); new `tests/TokenHound.Core.Tests/Models/LimitWindowTests.cs`; `tests/TokenHound.Infrastructure.Tests/Providers/ClaudeOAuthProviderTests.cs` (remaining counts now null); new `ClaudeQuotaProviderTests.cs`; `tests/TokenHound.Infrastructure.Tests/Mcp/McpMetricsReaderTests.cs` (one expectation; see manifest Problems and solutions).
- Checks (2026-09-23, SDK 10.0.401, native MTP, Debug, uncommitted worktree on base `6064266`): builds of Core.Tests and Infrastructure.Tests had 0 errors and 0 warnings. `LimitWindowTests` 5/5, `DomainModelsTests` 6/6, full Core.Tests 92/92, `*ClaudeQuota*` 5/5 (rerun after the final fixture edit), `ClaudeOAuthProviderTests` 14/14, `ClaudeOAuthClientTests` 11/11, `UsageStoreGatingTests` 9/9 (pre-dispatch deadline), full Infrastructure.Tests 775/775 before the final fixture-only edit. Every run used `--minimum-expected-tests 1` and exited 0.
- Acceptance evidence: `limits`, `seven_day_*`, ignored `extra_usage`/`seven_day_breakdown`, and precedence over top-level aliases are covered by `ReportedQuotas_AreOrderedAndKeepOnlyReportedMeasurements`. Valid 0%, malformed/null/out-of-range entries, and session rollover to the top-level fallback are covered by `MalformedEntries_AreSkippedIndividuallyAtRollover`. Distinct scoped groups are covered by `ScopedGroups_AreNotDeduplicatedByKindOrPercentage`, independent profiles by `SeparateProfiles_KeepTheirOwnQuotaBreakdowns`, and full 429 carry-forward with `Retry-After: 0` by `RateLimitAfterSuccess_KeepsTheWholeBreakdown`. `UsageStoreGatingTests` covers the store gate. Explicit and absent Core fractions are covered by `LimitWindowTests`.
- Quality profile (changed C# files): QA-01/02/03 have no hits. QA-05: every changed source file is at most 300 lines (mapper 226, provider 221). `ClaudeOAuthProviderTests.cs` is 440 lines, which is pre-existing debt with an unchanged line count; it is not in the Terrain baseline, so it is recorded here as a baseline gap. QA-04 reservation: the private positional record `QuotaCandidate` in `ClaudeQuotaWindowMapper.cs:12` has four members. It is a private value carrier, not an API. The `McpMetricsReaderTests.cs:20` hit is pre-existing. Mapper methods are at most 30 lines, and calls with four or more arguments are split per `CLAUDE.md`.
- Validated state: Core has no new UI/OS dependency. Credentials are still read-only through the unchanged `ClaudeProfileDiscovery`/`ClaudeOAuthClient` path. There are no new requests or retries, and raw payloads are not logged.
- Open items: HUD labels and exact ring selection belong to T02. Live manual acceptance (TC-07) is deferred to T02/HIL 3. The App project was not built in T01 because no App source changed; T02 builds it.

### ADR candidates

None - direct TechSpec implementation under DEC-09 (EX-01) and local decisions.
