# Code review report - OpenCode Provider Integration

## Summary

- Status: APPROVED WITH RESERVATIONS
- Git scope: `29fa9d7..index` (staged OpenCode feature delta; no `--base` argument supplied)
- Previous review: `tasks/prd-provider-opencode/codereview_1/codereview.md`

## Sources and scope

| Source | Path or reference | State |
| --- | --- | --- |
| PRD | `tasks/prd-provider-opencode/prd.md` | read once (v2: FR-07 renamed to `ProviderStatus.RateLimited`) |
| TechSpec | `tasks/prd-provider-opencode/techspec.md` | read once (v2: CMP-08, DEC-04 SQLite activity) |
| Manifest | `tasks/prd-provider-opencode/tasks.md` | read; lists T01-T04 only |
| Correction records | `tasks/prd-provider-opencode/codereview_1/codereview.md` and `done/task_05.md` to `done/task_09.md` | read; all required records present |
| Handoffs | `## Handoff` sections in `done/task_01.md` to `done/task_09.md`; no standalone handoff files | read |
| Provider specification | `docs/specs/12-PROVIDER-OPENCODE.md`, `docs/specs/README.md` | read |
| Implementation | Staged files under `src/TokenHound.Infrastructure/Providers/OpenCode/`, `src/TokenHound.Infrastructure/System/ProcessDiscovery.cs`, app registration/configuration, and the four OpenCode test classes | reviewable staged set |

`prd.md`, `techspec.md`, and `tasks.md` exist at the exact required paths, and every linked file resolves (PRD, TechSpec, `AGENTS.md`, `ARCHITECTURE.md`, provider references, contracts, and test projects). The feature-wide staged delta is well delimited because `HEAD` (`29fa9d7`, "feat(opencode): add opencode specs") contains only the specification artifacts and every implementation change is staged on top of it. Two staged `SettingsWindow.xaml` / `SettingsWindow.xaml.cs` changes belong to the separate settings feature and were excluded from the OpenCode obligation set, matching `codereview_1`.

## Coverage matrix

| Source | Obligation | Implementation | Test | State | Evidence |
| --- | --- | --- | --- | --- | --- |
| OBJ-01 | First-class `IUsageProvider` (`ProviderId = "opencode"`) with valid snapshots | `OpenCodeUsageProvider.cs:18-22,73-75`; `App.xaml.cs:177,256-266` | `OpenCodeUsageProviderTests.cs:25-32,51-75` | conformant | Registered in `UsageStore`; success snapshot is `Fidelity.Official`. |
| OBJ-02 | Zero-configuration extraction from the standard `auth.json` path | `OpenCodeCredentialDiscovery.cs:76-88,168-183` | `OpenCodeCredentialDiscoveryTests.cs:149-183,224-233` | conformant | Default path, env precedence, and file fallback covered. |
| OBJ-03 | Three authoritative windows with reset timestamps | `OpenCodeUsageProvider.cs:245-266`; `OpenCodeUsageDto.cs:45-64` | `OpenCodeUsageProviderTests.cs:51-75` | conformant | Rolling/Weekly/Monthly names, periods, fractions, and `resetsAt` mapped from the DTO. |
| OBJ-04 | Rate-limit block persistence and backoff | `OpenCodeUsageProvider.cs:112-115,152-207,284-294`; `UsageStore.Refresh.cs` (unchanged) | `OpenCodeUsageProviderTests.cs:79-121,174-251` | conformant | Both HTTP 429 and 200 `rate-limited` produce `ActiveBlock` with an enforced deadline (CR-01/CR-02 of `codereview_1` resolved). |
| OBJ-05 | Real-time process liveness | `OpenCodeActivityMonitor.cs:74-128`; `OpenCodeActivityReader.cs:38-67` | `OpenCodeActivityMonitorTests.cs:80-203` | conformant | Validated process presence plus 60-second SQLite activity drives `Busy`/`Idle` (CR-03 resolved). |
| FR-01 | Borrow key from env or `auth.json` (`opencode-go`, then `opencode`) | `OpenCodeCredentialDiscovery.cs:29-39,76-88,185-229` | `OpenCodeCredentialDiscoveryTests.cs:21-183` | conformant | Both env vars and both JSON keys tested. |
| FR-02 | `FileAccess.Read` + `FileShare.ReadWrite | FileShare.Delete`, never write | `SharedFileReader.cs:106-119`; `OpenCodeCredentialDiscovery.cs:105-132` | `OpenCodeCredentialDiscoveryTests.cs:185-208` | conformant | Shared read access while a writer handle is open; no write path. |
| FR-03 | GET endpoint with Bearer, `TokenHound`, JSON headers | `OpenCodeApiClient.cs:17-24,152-162` | `OpenCodeApiClientTests.cs:26-56` | conformant | URL, authorization, user agent, and accept headers asserted. |
| FR-04 | Parse rolling/weekly/monthly with exact percentage and reset | `OpenCodeApiClient.cs:164-184`; `OpenCodeUsageProvider.cs:245-282` | `OpenCodeApiClientTests.cs:26-56,58-73`; `OpenCodeUsageProviderTests.cs:51-75,123-144` | conformant | Parsing, clamping, and `RemainingUnits` verified. |
| FR-05 | Handle 429 or `status == "rate-limited"`, parse `Retry-After`/`resetsAt`, apply `RateLimitPolicy` | `OpenCodeApiClient.cs:186-229`; `OpenCodeUsageProvider.cs:112-115,136-207,236-243` | `OpenCodeApiClientTests.cs:113-224`; `OpenCodeUsageProviderTests.cs:77-121,173-251` | conformant | 200 status path, header path, body `resetsAt` fallback, and `Retry-After: 0` floor all covered. |
| FR-06 | Monitor `opencode` and `OpenCode` | `OpenCodeActivityMonitor.cs:20-21,111-128`; `ProcessDiscovery.cs:15-56` | `OpenCodeActivityMonitorTests.cs:101-150` | conformant | Both process names, absent/invalid PID, and liveness-validation cases tested. |
| FR-07 | Resolve `Ok`, `NeedsAuth`, `AccessDenied`, `RateLimited`, `Stale` | `OpenCodeUsageProvider.cs:77-150,224-330` | `OpenCodeUsageProviderTests.cs:33-47,146-171,173-251,282-335` | conformant | Success, 401, 403, 429, 200 rate-limited, cached stale, and initial-failure paths covered. |
| FR-08 | Default provider configuration and settings schema entry | `App.xaml.cs:177,256-266`; `appsettings.json:22-24`; `UserSettingsFile.cs:21`; `ProviderCatalog.cs:22,41,62,84` | App build; static configuration review | conformant | Provider enabled by default with display name, badge, glyph, and glyph scale. |
| NFR-01 | Pure Core; HTTP/file/process code only in Infrastructure | No `TokenHound.Core` source changed; all new code in `TokenHound.Infrastructure`/`TokenHound.App` | Infrastructure and Core builds/tests | conformant | Core test project unchanged and green (89 tests). |
| NFR-02 | No invented data; `UsedFraction = percent / 100.0` | `OpenCodeUsageProvider.cs:268-282` | `OpenCodeUsageProviderTests.cs:123-144` | conformant | Values are clamped from the response; no fallback denominator. |
| NFR-03 | Borrow-Don't-Own credentials | `OpenCodeCredentialDiscovery.cs:99-132`; `SharedFileReader.cs:106-119` | `OpenCodeCredentialDiscoveryTests.cs:185-208` | conformant | Read-only, shared, no refresh/write. |
| NFR-04 | Timeout <= 10s; backoff and `Retry-After` floor | `OpenCodeApiClient.cs:23-27,47-62,253-266`; `OpenCodeUsageProvider.cs:152-207` | `OpenCodeApiClientTests.cs:226-243,274-284`; `OpenCodeUsageProviderTests.cs:173-251` | conformant | Timeout bound, header seconds/date, zero-second floor, and reset fallback tested. |
| NFR-05 | Unit coverage for discovery, parsing, 429, and errors via in-memory handlers | Four OpenCode test classes | 66 OpenCode tests passed | conformant | Dedicated classes cover all required scenarios with mock handlers and temp files. |
| TC-01 | Credential discovery tests | `OpenCodeCredentialDiscoveryTests.cs` | 20 tests (filtered run) | conformant | Precedence, parsing, file-share, and cancellation. |
| TC-02 | Usage parsing tests | `OpenCodeApiClientTests.cs:26-73`; `OpenCodeUsageProviderTests.cs:51-75` | included in 66 | conformant | 3-window parse and mapping. |
| TC-03 | 429 rate-limit tests | `OpenCodeApiClientTests.cs:113-224`; `OpenCodeUsageProviderTests.cs:173-251` | included in 66 | conformant | Header, date, absent header, body reset, zero seconds. |
| TC-04 | Provider status/block tests | `OpenCodeUsageProviderTests.cs:33-121,146-335` | included in 66 | conformant | 401/403/429/200-rate-limited/stale/cancel. |
| TC-05 | Activity liveness and SQLite activity | `OpenCodeActivityMonitorTests.cs:38-217` | 13 tests (filtered run) | conformant | Absent/recent/stale/unavailable, WAL and immutable fallback, cancellation. |

No obligation is orphaned. `codereview_1` findings CR-01 to CR-04 are all resolved (see the previous-findings table).

## Compliance with rules and skills

| Rule or skill | State | Evidence |
| --- | --- | --- |
| `sdd-review-code` sources, tasks, links, and IDs | OK | Sources present; all links resolve; next free suffix is `codereview_2`. |
| `AGENTS.md` Core purity | OK | No `TokenHound.Core` change; Core tests green. |
| `AGENTS.md` Borrow-Don't-Own | OK | `SharedFileReader.cs:106-119`; no auth-file writes. |
| `AGENTS.md` no invented limits | OK | `percent / 100.0` from authoritative payload; explicit `TotalUnits = 100`. |
| `AGENTS.md` rate-limit deadline persistence and no immediate retry | OK | `OpenCodeUsageProvider.cs:85-86,152-207`; 200 and 429 paths create deadlines; `Retry-After: 0` floored. |
| `AGENTS.md` SQLite read-only WAL with immutable fallback | OK | `OpenCodeActivityReader.cs:46-50` via `SafeSqliteReader`; WAL and missing-`-shm` fallback tested. |
| `AGENTS.md` file/class <= 300 lines, methods <= 30, nesting <= 3 | NOT OK | `OpenCodeUsageProvider.cs` is 331 lines (CR-03). Methods and nesting are within limits. |
| `AGENTS.md` MTP validation with `--minimum-expected-tests 1` | OK | Native MTP detected; every execution used the minimum-tests flag. |
| `dotnet-efficient-validation` | OK | Built once, then reused for filtered and full runs without restore. |
| Desktop .NET E2E policy | N/A by policy | E2E omitted per TechSpec and repository policy; no essential manual acceptance is required by the TechSpec. |
| Diff hygiene (`git diff --cached --check`) | NOT OK | Trailing whitespace in `docs/specs/12-PROVIDER-OPENCODE.md:149` (CR-04). |

## TechSpec adherence

| Decision or contract | State | Evidence |
| --- | --- | --- |
| DEC-01 - read-only discovery with env fallback | YES | `OpenCodeCredentialDiscovery.cs:76-88,185-229`; TC-01 pass. |
| DEC-02 - official endpoint and three windows | YES | `OpenCodeApiClient.DEFAULT_ENDPOINT`; `OpenCodeUsageProvider.cs:245-266`; TC-02 pass. |
| DEC-03 - `RateLimitPolicy` for 429 and rate-limited responses | YES | `OpenCodeUsageProvider.cs:112-207`; TC-03/TC-04 pass. |
| DEC-04 - validated process liveness plus `session.time_updated` | YES | `OpenCodeActivityMonitor.cs:74-128`; `OpenCodeActivityReader.cs:38-67`; TC-05 pass. |
| DEC-05 - injectable HTTP transport | YES | `OpenCodeApiClient.cs:47-85`; all OpenCode tests use in-memory handlers. |
| CMP-01 - `OpenCodeAuthDto` | YES | `OpenCodeAuthDto.cs:6-17`. |
| CMP-02 - usage and error DTOs | YES | `OpenCodeUsageDto.cs:9-130` (includes `OpenCodeErrorResponse.ResetsAt`). |
| CMP-03 - credential discovery | YES | `OpenCodeCredentialDiscovery.cs:14-257`. |
| CMP-04 - API client | YES | `OpenCodeApiClient.cs:108-267`; reset timestamp carried on the exception. |
| CMP-05 - usage provider | YES | `OpenCodeUsageProvider.cs:18-331`. |
| CMP-06 - activity monitor | YES | `OpenCodeActivityMonitor.cs:13-129`. |
| CMP-07 - default app configuration | YES | `appsettings.json:22-24`; `UserSettingsFile.cs:21`; app build. |
| CMP-08 - SQLite activity reader | YES | `OpenCodeActivityReader.cs:11-81`. |
| `Errors, security, and recovery` network clause (`ProviderStatus.Error`) | PARTIAL | The implementation returns `Stale` (`OpenCodeUsageProvider.cs:320-330`); no `ProviderStatus.Error` member exists, so the TechSpec clause is unsatisfiable as written (CR-01). |
| TC-01 - credential discovery tests | YES | `OpenCodeCredentialDiscoveryTests` green. |
| TC-02 - usage parsing tests | YES | `OpenCodeApiClientTests`/`OpenCodeUsageProviderTests` green. |
| TC-03 - 429 tests | YES | Header/date/absent/body-reset/zero-seconds green. |
| TC-04 - provider status tests | YES | 401/403/429/200-rate-limited/stale green. |
| TC-05 - activity tests | YES | Busy/Idle/null plus WAL/immutable fallback green. |

## Verified tasks

| Task | Location | State | Handoff and evidence |
| --- | --- | --- | --- |
| T01 | `done/task_01.md` | COMPLETE | Handoff `done/task_01.md:79-97`; credential tests green. |
| T02 | `done/task_02.md` | COMPLETE | Handoff `done/task_02.md:87-111`; API tests green. |
| T03 | `done/task_03.md` | COMPLETE | Handoff `done/task_03.md:86-104`; provider tests green. |
| T04 | `done/task_04.md` | COMPLETE | Handoff `done/task_04.md:82-106`; activity and config tests green. |
| T05 (`codereview_1/CR-01`) | `done/task_05.md` | COMPLETE | Handoff `done/task_05.md:84-92`; `OpenCodeUsageProviderTests.cs:77-121` proves 200 rate-limited blocking. |
| T06 (`codereview_1/CR-02`) | `done/task_06.md` | COMPLETE | Handoff `done/task_06.md:89-97`; `OpenCodeApiClientTests.cs:177-224` and `OpenCodeUsageProviderTests.cs:199-251` prove reset fallback. |
| T07 (`codereview_1/CR-03`) | `done/task_07.md` | COMPLETE | Handoff `done/task_07.md:91-101`; `OpenCodeActivityMonitorTests.cs:152-203` proves SQLite activity. |
| T08 (`codereview_1/CR-04`) | `done/task_08.md` | COMPLETE | Handoff `done/task_08.md:79-87`; OBJ-02 row present at `tasks.md:25`. |
| T09 (terminology) | `done/task_09.md` | COMPLETE | Handoff `done/task_09.md:81-90`; PRD/TechSpec/manifest use `ProviderStatus.RateLimited`. |

The `tasks.md` manifest records only T01-T04; T05-T09 are not indexed there (CR-02). Completed task criteria themselves are consistent with their handoffs.

## Executed validations

- Profile and exclusions: .NET SDK 10.0.400, `net10.0`, native Microsoft.Testing.Platform from `global.json`; `TokenHound.Infrastructure.Tests` and `TokenHound.Core.Tests`; E2E omitted by desktop .NET policy.
- Validated state: current staged OpenCode implementation and tests at `HEAD = 29fa9d7`; no unstaged or untracked implementation files.
- Reused evidence: `codereview_1` handoffs were used as historical context only; all checks were rerun against the current staged code because the source changed after `codereview_1`.
- Manual acceptance: no live OpenCode credential/network call or Windows HUD rendering was executed. The TechSpec requires no manual script and E2E is omitted by policy, so this is a recorded limitation, not unexecuted essential work.

| Command | Result | Obligations covered |
| --- | --- | --- |
| `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-restore --nologo --verbosity:minimal` | passed; 3 projects, 0 errors, 0 warnings | NFR-01; all infrastructure code compiles |
| `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*OpenCode*"` | passed; 66 tests | T01-T09; TC-01 to TC-05 |
| `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1` | passed; 672 tests | Regression gate |
| `rtk dotnet test --project tests/TokenHound.Core.Tests/TokenHound.Core.Tests.csproj --minimum-expected-tests 1` | passed; 89 tests | NFR-01 and core policy regression |
| `rtk dotnet build src/TokenHound.App/TokenHound.App.csproj --no-restore --nologo --verbosity:minimal` | passed; 4 projects, 0 errors, 0 warnings | FR-08/CMP-07 composition |
| `git diff --cached --check` | failed; exit 2 - trailing whitespace at `docs/specs/12-PROVIDER-OPENCODE.md:149` | CR-04 |
| Local link inventory | passed | Source/link integrity |

## Findings

| ID | Severity | Source | Evidence | Impact | Proven recommendation |
| --- | --- | --- | --- | --- | --- |
| CR-01 | Low | `techspec.md:189`, FR-07 | The network-error clause requires `ProviderStatus.Error` on failure without a cached reading, but the enum exposes no `Error` member (`ProviderStatus.cs:6-36`); the implementation returns `Stale` (`OpenCodeUsageProvider.cs:320-330`) and a test asserts it (`OpenCodeUsageProviderTests.cs:307-321`). Every other provider also returns `Stale`. | Reviewers and future implementers are directed to an impossible contract; behavior is otherwise consistent with the repository. | Replace `ProviderStatus.Error` with `ProviderStatus.Stale` in the TechSpec so the clause matches the enum and the implementation. No code change is required. |
| CR-02 | Low | `tasks.md` manifest, T05-T09 | `tasks.md:47-52` and `tasks.md:76-82` list and mark only T01-T04, while `done/task_05.md` to `done/task_09.md` exist and are complete; the matrix rows for FR-05/FR-07 (`tasks.md:30,32`) still cite only T02/T03. Several handoffs also cite `tasks/prd-provider-opencode/task_XX.md` rather than `done/task_XX.md`. | The manifest is not a complete index of executed work; reviewers cannot see the correction tasks from the plan, weakening re-review traceability. | Add T05-T09 rows to the dependency graph, coverage/state sections, and FR-05/FR-07 source rows, or record the correction workstream as an explicit manifest section. |
| CR-03 | Low | `AGENTS.md` file-length rule | `src/TokenHound.Infrastructure/Providers/OpenCode/OpenCodeUsageProvider.cs` is 331 lines, the only provider source file above the 300-line limit (next largest is exactly 300). | The size rule is violated for the primary adapter, increasing review and maintenance cost. | Extract rate-limit block creation and snapshot factory helpers into a partial or a dedicated collaborator to return the file under 300 lines. |
| CR-04 | Low | Diff hygiene | `git diff --cached --check` exits 2 with `docs/specs/12-PROVIDER-OPENCODE.md:149: trailing whitespace` (`+SELECT ` inside the SQL sample). | The repository whitespace check fails on the feature delta; tooling that treats `--check` as a gate would reject the change. | Remove the trailing space after `SELECT` in the SQL sample. |

No Critical or High findings remain. `codereview_1` items CR-01 to CR-04 are resolved.

## Previous findings (re-review only)

| Review/ID | State | Current evidence |
| --- | --- | --- |
| `codereview_1/CR-01` | resolved | `OpenCodeUsageProvider.cs:112-115,236-243` converts a 200 `status: "rate-limited"` window into `ProviderStatus.RateLimited` with an `ActiveBlock`; proven by `OpenCodeUsageProviderTests.cs:77-121`. |
| `codereview_1/CR-02` | resolved | `OpenCodeApiClient.cs:214-229` and `OpenCodeRateLimitException.cs:19-47` carry `resetsAt`; `OpenCodeUsageProvider.cs:152-207` honors it when `Retry-After` is absent; proven by `OpenCodeApiClientTests.cs:177-224` and `OpenCodeUsageProviderTests.cs:199-251`. |
| `codereview_1/CR-03` | resolved | `OpenCodeActivityMonitor.cs:74-128` and `OpenCodeActivityReader.cs:38-67` read `session.time_updated` read-only; `Busy` only within 60 seconds, `Idle` otherwise, null when no validated process; proven by `OpenCodeActivityMonitorTests.cs:80-203`. |
| `codereview_1/CR-04` | resolved | `tasks.md:25` now contains the explicit `OBJ-02` trace row linking T01, FR-01, TC-01, and `OpenCodeCredentialDiscoveryTests`. |

## Limitations and open items

- No `--base` argument was supplied. The scope was delimited as `29fa9d7..index` because `HEAD` contains only the specification artifacts; unrelated staged `SettingsWindow` changes were excluded. A caller-provided base would make the boundary explicit.
- No live OpenCode Go credential, endpoint response, or Windows HUD rendering was exercised. E2E omission is required by desktop policy and the TechSpec defines no manual script, so this does not block approval, but runtime HUD presentation of the three windows and the activity ring remains unverified outside unit tests.
- `OpenCodeActivityReader` interprets `session.time_updated` as Unix milliseconds. The provider specification does not document the stored encoding, so correctness against a real `opencode.db` is an assumption that unit tests with a synthetic database cannot prove.
- The repository-level `ProviderStatus` enum has no `Unsupported`-style error state for a first-failure network condition; `Stale` is used, matching all other providers (CR-01).

## Conclusion

The four defects that caused the `codereview_1` rejection are corrected and independently verified: a 200 `status: "rate-limited"` payload now yields a blocked `RateLimited` snapshot, an un-cached 429 `resetsAt` is honored, the activity monitor reads read-only SQLite `session.time_updated` with the 60-second busy rule, and the OBJ-02 trace exists. All PRD objectives and functional/non-functional requirements are mapped to implementation and tests, and all executed validations pass: 66 OpenCode tests, 672 infrastructure tests, 89 core tests, and the WPF app build. The remaining findings are low-severity artifact and hygiene issues (an impossible `ProviderStatus.Error` reference in the TechSpec, a manifest that omits T05-T09, one 331-line adapter file, and a trailing space in the spec), none of which leaves a requirement, security, or essential evidence pending. Status is `APPROVED WITH RESERVATIONS` pending those documentation and size corrections.
