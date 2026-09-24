# TechSpec — Claude quota breakdown

## Sources and traceability

- Approved PRD: `tasks/prd-08-claude-quota-breakdown/prd.md` (workflow DEC-01).
- Approved exception: `exception-01.md` (workflow DEC-10) extends T01 to the Core `LimitWindow` contract and focused tests.
- Rules: repository `AGENTS.md`; `sdd-create-techspec`, `no-workarounds`, `dotnet-efficient-validation`, and `repository-cli-efficiency`.
- Provider contract: `docs/specs/03-PROVIDER-CLAUDE-CODE.md` §3. The HUD's list layout is described in `docs/design/2026-08-28-usage-notch-design.md`; its older most-constrained headline description yields to the approved PRD and current session-first behavior.
- Existing code: `ClaudeUsageResponse`, `ClaudeWindowDto`, `ClaudeOAuthProvider.MapLimitWindows`, `ProviderUsageRowFactory.ResolveQuotaWindowLabel`, `ProviderRingViewModel.FindLimitWindow`, and `UsageStore.Refresh` rate-limit gate.
- Evidence: the account response observed on 2026-09-23 had `limits` entries `session`, `weekly_all`, and `weekly_scoped`, plus null optional model fields. The raw response and credentials are not stored here.

## Solution summary

Normalize the existing Claude usage response into a single ordered collection of `LimitWindow` values. Read valid `limits` entries and semantic top-level windows (`five_hour`, `seven_day`, and `seven_day_*`). Preserve known aliases as one window, keep the top-level session and overall weekly values as rollover fallbacks, and ignore optional fields without a valid percentage. Correct the Core `LimitWindow` contract so an explicitly reported fraction survives without a unit denominator. The existing request, credential discovery, polling, and rate-limit gate remain the integration path.

The HUD uses Claude-specific labels for additional windows and exact session and overall-weekly selection for the ring summary. Model names come from recognized response keys or scope strings; an opaque scoped entry receives a neutral label and its reported group, if readable. Existing behavior for other providers stays as it is.

## Technical decisions

| ID | PRD obligations | Decision | Reason and evidence | Alternatives and trade-offs |
| --- | --- | --- | --- | --- |
| DEC-02 | FR-01, FR-03, NFR-02 | Read `limits` and semantic `seven_day_*` top-level objects from the same response; ignore unrelated opaque fields, spend, and extra usage. | The observed response contains optional quota and non-quota fields; the endpoint is undocumented. | Hard-coded model fields would miss future model quotas. Treating every object with `utilization` as a quota would mislabel internal telemetry. |
| DEC-03 | FR-03, FR-06, NFR-03 | Accept finite percentages in `[0,100]`, including zero; represent missing utilization as absent. Populate `UsedFraction` and an API-supplied reset only. Leave count and total fields null. | Current `ClaudeWindowDto.Utilization` defaults to zero when omitted, and `CreateLimitWindow` fabricates a 100-unit allowance. | Clamping invalid percentages or deriving remaining units would make invalid data look authoritative. |
| DEC-04 | FR-04, FR-05, OBJ-02 | Canonicalize `session`/`five_hour`, `weekly_all`/`seven_day`, and `weekly_<suffix>`/`seven_day_<suffix>`; prefer a valid `limits` entry over its top-level alias. Add top-level session and overall weekly when absent from `limits`. Order session, overall weekly, then additional keys and scopes deterministically. | The provider spec documents session rollover and duplicate top-level representations. | Merge only provable aliases; ambiguous scoped entries stay distinct rather than being merged on matching percentages or reset times. |
| DEC-05 | FR-02, FR-05, NFR-04 | Resolve Claude labels from the canonical window identity before generic period-based labels. Select only exact `five_hour` and `seven_day` names for Claude's ring summary; absent canonical windows remain unmeasured. | Generic seven-day labels erase model identity, and `FindLimitWindow` currently falls back to arbitrary first/second windows. | The generic provider selection path remains untouched for non-Claude providers. |
| DEC-06 | NFR-05 | Extract the Claude mapping from `ClaudeOAuthProvider` to a focused internal mapper within the feature task. | The provider is 276 lines and has a six-parameter constructor; adding dynamic mapping in place risks the 300-line project limit. The extraction changes no public contract and existing provider tests characterize standard behavior. | No preparatory refactoring feature is needed: this local extraction fits one task and does not touch the saturated constructor. |
| DEC-07 | FR-07, FR-08, NFR-01, NFR-02 | Keep provider instances and `UsageStore` backoff persistence per profile. Verify that a 429 retains all last-success windows and that the store blocks dispatch until its recorded deadline. | `UsageStore.Refresh` checks `_backoffDeadlines` before refresh and persists blocked deadlines; the provider already carries last-success windows. | No new polling, credential write, or retry path is introduced. |
| DEC-09 | FR-01, FR-06, NFR-03, NFR-05 | Return an explicitly assigned `LimitWindow.UsedFraction` even when `TotalUnits` is null; absence still returns null. Move its tests from the 302-line `DomainModelsTests.cs` into `LimitWindowTests.cs` and update the contradictory expectation. | `LimitWindow.cs:23-29` currently suppresses API-reported percentages without a total. Approved EX-01 authorizes this Core contract amendment. | Setting a fake total would invent a unit denominator; a second fraction property would duplicate the same measurement. |

## Components and flow

| ID | Component | New or modified | Responsibility | Dependencies |
| --- | --- | --- | --- | --- |
| CMP-01 | `ClaudeUsageResponse.cs`, `ClaudeWindowDto.cs` | Modified | Expose optional `limits`, dynamic top-level quota fields, and nullable utilization. | Existing `System.Text.Json` client. |
| CMP-02 | `ClaudeQuotaWindowMapper.cs` | New | Validate, normalize, deduplicate, and order Claude windows. | CMP-01; Core `LimitWindow`. |
| CMP-03 | `ClaudeOAuthProvider.cs` | Modified | Call CMP-02 and retain the existing status, stale, and 429 flow. | CMP-02. |
| CMP-04 | `ProviderUsageRowFactory.cs` | Modified | Give Claude additional windows distinct labels and reported scope text. | CMP-02 names and `GroupName`. |
| CMP-05 | `ProviderRingViewModel.cs` | Modified | Use exact Claude session and overall-weekly windows for summary values. | CMP-02 names. |
| CMP-06 | Focused tests in `tests/TokenHound.Infrastructure.Tests/` | New | Prove API parsing, mapping, HUD projection, and stale/ring edges. | CMP-01–CMP-05. |
| CMP-07 | `src/TokenHound.Core/Models/LimitWindow.cs` and focused Core tests | Modified/new | Preserve explicitly reported fractions without a unit total and keep remaining-only fractions null. | DEC-09; CMP-02. |

Flow: one OAuth GET → deserialized response → CMP-02 windows using the amended CMP-07 fraction contract → provider snapshot → existing store and profile routing → HUD rows and ring summary. No API endpoint, storage schema, or package change is planned.

## Contracts and data

- `ClaudeUsageResponse.Limits`: optional JSON array. Malformed or non-object entries are ignored individually; a wholly malformed JSON document remains a client error. `JsonExtensionData` retains unknown top-level JSON values for selective `seven_day_*` inspection.
- `ClaudeWindowDto.Utilization`: nullable percentage. Missing/null values do not create windows. Existing valid numeric responses keep their current meaning.
- A `limits` entry needs a nonempty string `kind` and a finite numeric `percent` in `[0,100]`. `resets_at`, `group`, and `scope` are optional; only readable strings are used as display scope. Invalid reset values are treated as absent for that entry without inventing a time.
- A top-level `seven_day_*` field qualifies only when it is an object with a valid numeric `utilization`. `seven_day_breakdown` and other objects without utilization produce no window. Top-level `five_hour` and `seven_day` use the same validity rule.
- Canonical names are `five_hour`, `seven_day`, `seven_day_<reported suffix>`, or the reported non-alias `kind` (for example `weekly_scoped`). Identity for a scoped kind includes its readable group or scope when present. Case-insensitive canonical identity prevents proven aliases from duplicating. No deduplication by equal percentages or reset timestamps.
- Every mapped `LimitWindow` has `UsedFraction = percent / 100` and a reset only if supplied. `RemainingUnits`, `TotalUnits`, and monetary fields remain null. `Period` is five hours for session, seven days for known weekly kinds, and null when the kind does not prove a period.
- Core contract: `LimitWindow.UsedFraction` returns its explicitly assigned nullable value independent of `TotalUnits`. When the provider reports only a remaining count, it must assign no fraction. This changes no serialized field name or stored schema.
- Claude labels follow Claude Code's `/usage`: `Current session`, `Current week (all models)`, and `Current week (<scope>)`. For `weekly_scoped`, the scope is the API-reported `scope.model.display_name` (else `scope.surface`), for example `Current week (Fable)`; `group` is only the limit category (`weekly`) and is never shown as a scope. Without a readable scope the label is `Current week (scoped)`. For `seven_day_opus` or `weekly_opus`, the label is `Current week (Opus)`. An unknown suffix is humanized from the reported identifier without inventing a model identity.

## Integrations and interfaces

- Anthropic OAuth usage GET and headers remain as documented in provider spec §3. The client performs one request per existing refresh and passes the same cancellation token.
- `UsageStore` remains responsible for checking persisted deadlines before provider dispatch and saving future 429 deadlines. The provider carries every last-success window through `RateLimited`/`Stale` snapshots. A retry hint of zero never triggers an immediate repeat.
- Existing Claude profile IDs route responses and snapshots independently; no shared mapper state or extra credential access is introduced.
- HUD details use the existing quota-row list. The ring's session and weekly summary use only canonical Claude base windows; absence yields null rather than substituting an additional quota.

## Errors, security, and recovery

- Optional malformed quota entries are skipped without discarding other valid windows. A malformed JSON document, HTTP error, or cancellation keeps the current provider boundary semantics.
- Credentials remain read-only with `FileShare.ReadWrite | FileShare.Delete`; neither tokens nor raw responses are logged or persisted.
- Mapping is deterministic and stateless. Per-profile rate-limit deadlines and last-success snapshots remain independent.
- Reversal is a source rollback of CMP-01–CMP-05 and CMP-07; no persisted data migration is needed.

## Sequencing

| Step | Depends on | Verifiable result |
| --- | --- | --- |
| T01 — Normalize Claude quota windows | — | Provider snapshots contain valid additional windows, stable base windows, no fabricated counts, and full stale carry-forward. |
| T02 — Present Claude quota breakdown | T01 | HUD rows have distinct labels, and the ring never promotes an additional quota to session/overall weekly summary. |

## Test approach

- Profile: Core and Infrastructure target `net10.0`; App is WPF `net10.0-windows` (`UseWPF=true`); Core.Tests and Infrastructure.Tests target `net10.0`, with the latter linking affected App ViewModels. `global.json` selects SDK 10.0.400 with latest-feature roll-forward and native Microsoft.Testing.Platform; installed SDK observed as 10.0.401 with xUnit v3 MTP.
- E2E: omitted by .NET desktop policy. No aggregate suite that includes E2E will run.
- Prerequisites: use existing restore assets if valid. Otherwise restore the affected Core.Tests, Infrastructure.Tests, and App projects once. Serialize builds sharing `bin/` and `obj/`; build the Core test project, infrastructure test project, and App with `rtk dotnet build <project> --no-restore --nologo --verbosity:minimal`, then use only `--no-build --no-restore` tests. Preserve `$LASTEXITCODE` and require at least one executed test.
- Focused MTP command: `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*ClaudeQuota*"`. Run existing `ClaudeOAuthProviderTests` and `ProviderUsageRowFactoryTests` classes separately after the affected changes. Use `rtk proxy` only if a failing test's details are filtered.
- Core MTP command: `rtk dotnet test --project tests/TokenHound.Core.Tests/TokenHound.Core.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*LimitWindowTests*"`. Run the remaining `DomainModelsTests` class as a regression check.
- Manual acceptance (owner: coordinator with user-visible desktop): build `TokenHound.App`; launch its executable through Windows MCP `App` with `mode="launch_executable"`; capture the primary monitor with Windows MCP `Screenshot` using `display: [2]`. Open the Claude profile detail and verify session and overall weekly first, an additional scoped/model quota when the live API supplies one (including 0%), readable scope, no null-model rows, and no fabricated remaining count. If live quota data is unavailable or rate limited, record that live acceptance remains pending instead of treating a fixture as proof of the account state.

| ID | Obligations | Level | Scenario | Expected result | Command or project |
| --- | --- | --- | --- | --- | --- |
| TC-01 | FR-01, FR-03 | Unit | `limits` has scoped/model entries, optional top-level model entry, null and 0% values. | Only valid reported windows appear; 0% remains visible. | New `ClaudeQuota*` tests, Infrastructure.Tests. |
| TC-02 | FR-04, FR-05, OBJ-02 | Unit | Base aliases and weekly model aliases coexist; `session` disappears at rollover. | One window per proven identity; top-level session remains first and overall weekly second. | New `ClaudeQuota*` tests. |
| TC-03 | FR-06, NFR-03 | Unit | Core receives an explicit fraction with no total; a separate remaining-only window has no fraction; Claude response has percent only and malformed optional entries. | Explicit fraction survives, remaining-only fraction is null, unreported values remain null, and valid quota neighbors survive. | `LimitWindowTests`, `DomainModelsTests`, and new `ClaudeQuota*` tests. |
| TC-04 | FR-07, FR-08, NFR-02 | Integration with HTTP handler and store | Profiles have different responses; success is followed by 429, including `Retry-After: 0`. | Profile windows stay separate; all last-success windows remain; store skips dispatch before deadline. | New provider/store tests plus existing Claude provider tests. |
| TC-05 | FR-02, NFR-04 | Unit | Claude details include scoped and named model windows. | Each row has distinct text/scope, correct percent, optional reset, and no fake remaining count. | New row tests plus existing row factory tests. |
| TC-06 | FR-05, OBJ-02 | Unit | Claude snapshot contains only additional windows. | Ring session and overall-weekly values are unmeasured; other providers retain their current selection. | New ring tests. |
| TC-07 | US-01–US-04, OBJ-01–OBJ-03 | Manual | Run the user-visible HUD against a current Claude profile. | The live breakdown and ring follow the PRD when the endpoint supplies the quota. | Windows MCP App and Screenshot. |

## Quality profile

Rules apply to changed C# files only. A new blocking hit fails the task; a reservation is reported. Existing hits below are baseline, not new findings.

| ID | Rule | Class | Verification command | Prior justification |
| --- | --- | --- | --- | --- |
| QA-01 | No sync-over-async (`.Result`, `.Wait()`, `GetAwaiter().GetResult()`) | blocking | `rtk rg -n '\.Result\b|\.Wait\(\)|GetAwaiter\(\)\.GetResult\(\)' <changed-cs-files>` | — |
| QA-02 | No empty or catch-and-default exception handler | blocking | `rtk rg -n 'catch\s*\{\s*\}|catch \(Exception\w*\)\s*\{\s*\}' <changed-cs-files>` plus review | — |
| QA-03 | No nullable or warning suppression | blocking | `rtk rg -n '#nullable disable|#pragma warning disable' <changed-cs-files>` | — |
| QA-04 | Four or more parameters require review for a parameter object | reservation | `rtk rg -n '\w+\((?:[^),]+,){3,}[^)]*\)' <changed-cs-files>` plus signature review | Existing provider and ring constructors remain unchanged. |
| QA-05 | Source file must stay at or below 300 lines | blocking | `rtk rg -c '^' <changed-cs-files>` | Project `AGENTS.md` threshold; no existing source target exceeds it. |

- Verification scope: only C# files changed by each task, excluding generated `bin/` and `obj/`.
- Escalation trigger: eight or more new reservation hits, a touched file above 500 lines, or duplicated new logic in three places. No heavy audit runs inside this flow.

### Terrain baseline

| File | Lines | Public members | Constructor deps | Cases | Pre-existing hits | Destination |
| --- | --- | --- | --- | --- | --- | --- |
| `src/TokenHound.Infrastructure/Providers/Claude/ClaudeUsageResponse.cs` | 21 | 2 | 0 | 0 | None in selected QA rules. | Record. |
| `src/TokenHound.Core/Models/LimitWindow.cs` | 64 | 9 | 0 | 0 | None in selected QA rules. | Amend the getter under DEC-09. |
| `tests/TokenHound.Core.Tests/Models/DomainModelsTests.cs` | 302 | 10 | 0 | 0 | QA-05: two lines over the project maximum before T01. | Extract four `LimitWindow` tests under DEC-09; both resulting files must be at most 300 lines. |
| `src/TokenHound.Infrastructure/Providers/Claude/ClaudeWindowDto.cs` | 22 | 2 | 0 | 0 | None in selected QA rules. | Record. |
| `src/TokenHound.Infrastructure/Providers/Claude/ClaudeOAuthProvider.cs` | 276 | 7 | 6 | 0 | QA-04: six-parameter constructor at line 62; four-parameter `CreateLimitWindow` at line 214. | Absorb mapping extraction under DEC-06; leave constructor unchanged. |
| `src/TokenHound.App/ViewModels/ProviderUsageRowFactory.cs` | 159 | 2 | 0 | 0 | None in selected QA rules. Existing label method exceeds the project 30-line method limit; simplify it while editing. | Absorb in T02. |
| `src/TokenHound.App/ViewModels/ProviderRingViewModel.cs` | 282 | 21 | 4 | 0 | QA-04: four-parameter constructor at line 34. | Record; T02 touches two methods and adds no public member. |

- Preparatory refactoring: not recommended. The provider's six-dependency constructor and ring's 21 public members cross structural thresholds, but the feature does not extend either saturated structure. The provider mapping extraction and Core test extraction are local and fit T01. Remeasure if another change reaches these files before implementation.

## Observability and rollout

- Existing provider status, stale indicator, and structured rate-limit transitions remain the operational signals. Do not log raw usage payloads or credentials.
- No migration or feature flag is required. A new response field becomes visible only if it meets the mapping contract.
- Rollout is a local App build and normal restart; rollback is source reversal. Live acceptance may depend on the account receiving an additional quota at test time.

## Risks and open items

- Risk: undocumented Anthropic fields may change shape. Defensive per-entry parsing preserves valid windows; fixtures cover observed and documented shapes. Unknown opaque fields are not identified as quotas without a semantic key or `limits` entry.
- Risk: two differently named entries may describe one quota without a provable alias. They remain separate until evidence establishes a safe mapping; merging equal numeric values would hide distinct quotas.
- Risk: the Core fraction contract is public. The provider assignment audit found no current production path with a non-null fraction and null total, but external consumers could observe a newly preserved explicit value. Core and provider regression tests constrain this change.
- Open item for acceptance only: the live endpoint may omit optional quotas or be rate limited during the manual HUD check. Unit/integration tests prove mapping, while live display remains separately reported.

## Relevant files

- Modify: `src/TokenHound.Infrastructure/Providers/Claude/ClaudeUsageResponse.cs`, `ClaudeWindowDto.cs`, `ClaudeOAuthProvider.cs`.
- Modify: `src/TokenHound.Core/Models/LimitWindow.cs` and `tests/TokenHound.Core.Tests/Models/DomainModelsTests.cs`.
- Modify: `src/TokenHound.App/ViewModels/ProviderUsageRowFactory.cs`, `ProviderRingViewModel.cs`.
- Create: `tests/TokenHound.Core.Tests/Models/LimitWindowTests.cs`, `src/TokenHound.Infrastructure/Providers/Claude/ClaudeQuotaWindowMapper.cs`, and focused `ClaudeQuota*` test files under `tests/TokenHound.Infrastructure.Tests/`.
