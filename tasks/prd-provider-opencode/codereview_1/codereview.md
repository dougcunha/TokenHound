# Code review report - OpenCode Provider Integration

## Summary

- Status: REJECTED
- Git scope: `Not delimited - no --base supplied; current relevant staged worktree, task artifacts, provider specification, and validation results reviewed`
- Previous review: `-` (no prior `codereview_*` folder under this feature)

## Sources and scope

| Source | Path or reference | State |
| --- | --- | --- |
| PRD | `tasks/prd-provider-opencode/prd.md` | read once |
| TechSpec | `tasks/prd-provider-opencode/techspec.md` | read once |
| Manifest | `tasks/prd-provider-opencode/tasks.md` | read |
| Task records | `tasks/prd-provider-opencode/done/task_01.md` through `task_04.md` | read; all required records present |
| Handoffs | Handoff sections in each `done/task_*.md`; no standalone handoff files | read |
| Provider specification | `docs/specs/12-PROVIDER-OPENCODE.md` and `docs/specs/README.md` | read |
| Implementation | Relevant staged files under `src/TokenHound.Infrastructure/Providers/OpenCode/`, app registration/configuration, and OpenCode tests | reviewable staged set |

The required `prd.md`, `techspec.md`, and `tasks.md` exist at the exact required paths. The manifest contains T01-T04 and each task is located under `done/`. No task, handoff, or checkpoint file is missing from the feature folder. The dependency graph is the declared acyclic sequence `T01 -> T02 -> T03 -> T04`.

Before this report was created, the working tree had no unstaged or untracked implementation files. The staged change set also contains `SettingsWindow.xaml` and `SettingsWindow.xaml.cs` changes without OpenCode-specific references; those unrelated settings-surface changes were excluded from the feature obligation set because no base commit was supplied. The report itself is the only new untracked review artifact. Historical attribution and comparison against a branch base are therefore not verifiable.

Local links in the PRD, TechSpec, manifest, and task records resolve, including the provider specification, architecture, repository rules, referenced provider files, contracts, and task anchors. The two external PRD sources were reachable during review: `https://opencode.ai/docs` and `https://github.com/anomalyco/opencode`.

## Coverage matrix

| Source | Obligation | Implementation | Test | State | Evidence |
| --- | --- | --- | --- | --- | --- |
| OBJ-01 | First-class `IUsageProvider` registered with provider ID `opencode` and valid snapshots | `OpenCodeUsageProvider.cs:18-22,72-74`; `App.xaml.cs:158-177,256-265` | `OpenCodeUsageProviderTests.cs:23-74` | conformant | Provider is registered with `UsageStore`, and successful snapshots are authoritative. |
| OBJ-02 | Zero-configuration credential extraction from the standard `auth.json` path | `OpenCodeCredentialDiscovery.cs:76-87,168-183` | `OpenCodeCredentialDiscoveryTests.cs:149-183,224-233` | conformant | Default path, environment precedence, and file fallback are covered. The task traceability matrix does not explicitly include OBJ-02; see CR-04. |
| OBJ-03 | Three authoritative quota windows with reset timestamps | `OpenCodeUsageProvider.cs:187-236`; `OpenCodeUsageDto.cs:9-63` | `OpenCodeUsageProviderTests.cs:49-74` | conformant | Three named windows, periods, percentage mapping, and reset values are mapped from the API DTO. |
| OBJ-04 | Rate-limit block persistence and backoff | `OpenCodeUsageProvider.cs:142-170`; `UsageStore.Refresh.cs:49-60,89-179` | `OpenCodeUsageProviderTests.cs:126-179` | non-conformant | HTTP 429 persistence and floor behavior work, but the required 200 `status: "rate-limited"` path and 429 `resetsAt` fallback are absent (CR-01, CR-02). |
| OBJ-05 | Real-time OpenCode process liveness | `OpenCodeActivityMonitor.cs:13-19,64-127` | `OpenCodeActivityMonitorTests.cs:29-153` | conformant | CLI and Desktop process names are discovered and active sessions are returned. The stronger provider-spec activity semantics are only partial; see CR-03. |
| FR-01 | Credential discovery from environment or `auth.json` with specified precedence | `OpenCodeCredentialDiscovery.cs:185-229` | `OpenCodeCredentialDiscoveryTests.cs:21-183` | conformant | Both environment variables and both JSON provider keys are tested. |
| FR-02 | Read-only credential access with `FileShare.ReadWrite | FileShare.Delete` and no credential writes | `SharedFileReader.cs:91-119`; `OpenCodeCredentialDiscovery.cs:115-131` | `OpenCodeCredentialDiscoveryTests.cs:185-208` | conformant | Shared reader uses read access and both sharing flags; discovery has no write or refresh path. |
| FR-03 | GET the specified endpoint with Bearer, `TokenHound`, and JSON headers | `OpenCodeApiClient.cs:152-161` | `OpenCodeApiClientTests.cs:25-56` | conformant | Request URL and all required headers are asserted. |
| FR-04 | Parse rolling, weekly, and monthly windows with exact percentage and reset values | `OpenCodeApiClient.cs:164-184`; `OpenCodeUsageProvider.cs:199-236` | `OpenCodeApiClientTests.cs:25-73`; `OpenCodeUsageProviderTests.cs:49-97` | conformant | Successful response parsing and domain mapping are tested, including clamping. |
| FR-05 | Handle HTTP 429 or `status == "rate-limited"`, parse `Retry-After` or `resetsAt`, and apply `RateLimitPolicy` | `OpenCodeApiClient.cs:186-228`; `OpenCodeUsageProvider.cs:130-184` | `OpenCodeApiClientTests.cs:113-175`; `OpenCodeUsageProviderTests.cs:126-179` | non-conformant | HTTP 429 `Retry-After` is covered, but DTO status values are ignored and 429 `resetsAt` is not carried to the provider (CR-01, CR-02). |
| FR-06 | Monitor `opencode` and `OpenCode` process presence | `OpenCodeActivityMonitor.cs:86-127` | `OpenCodeActivityMonitorTests.cs:101-153` | conformant | Both names, absent processes, PIDs, and cancellation are covered. Shared `ProcessDiscovery`/`ProcessLiveness` usage and database activity are not implemented (CR-03). |
| FR-07 | Resolve `Ok`, `NeedsAuth`, rate-limited, and stale states | `OpenCodeUsageProvider.cs:77-140,238-284` | `OpenCodeUsageProviderTests.cs:33-47,99-124,126-220` | non-conformant | 401, 403, 429, and network fallback are covered, but a successful payload marked rate-limited is returned as `Ok` (CR-01). The repository enum is `RateLimited`; the PRD's `RateLimitReached` name does not exist. |
| FR-08 | Register the default provider and settings schema entry | `App.xaml.cs:158-177,256-265`; `appsettings.json:6-25`; `UserSettingsFile.cs:20-22`; `ProviderCatalog.cs:14-87` | App build; static configuration review | conformant | Provider registration, default enablement, persisted-provider defaults, display metadata, and glyph key are wired; no dedicated OpenCode config-load test exists. |
| NFR-01 | Keep Core pure and isolate HTTP, file, and process code in Infrastructure | New provider code is under `src/TokenHound.Infrastructure`; app changes are limited to composition/presentation | Infrastructure and Core builds/tests | conformant | No Core source changed; builds passed. |
| NFR-02 | Do not invent data; map `UsedFraction` from `percent / 100.0` | `OpenCodeUsageProvider.cs:222-236`; DTO required fields in `OpenCodeUsageDto.cs:45-63` | `OpenCodeUsageProviderTests.cs:76-97` | conformant | Values are mapped from the response and clamped; no fallback denominator beyond the explicitly specified percentage scale is introduced. |
| NFR-03 | Borrow credentials without owning or refreshing them | `OpenCodeCredentialDiscovery.cs:105-132`; `SharedFileReader.cs:106-119` | Concurrent reader test; source review | conformant | The file is read-only and shared; no write or refresh operation exists. |
| NFR-04 | Timeout at most 10 seconds and enforce resilient backoff floors | `OpenCodeApiClient.cs:23-27,252-265`; `OpenCodeUsageProvider.cs:142-184` | `OpenCodeApiClientTests.cs:177-194,225-235`; `OpenCodeUsageProviderTests.cs:126-179` | non-conformant | Timeout and `Retry-After` floor pass, but the required reset-time fallback is incomplete (CR-02). |
| NFR-05 | Complete unit coverage for discovery, parsing, 429, and error states using in-memory handlers | OpenCode test classes under `tests/TokenHound.Infrastructure.Tests/Providers/OpenCode/` | 57 OpenCode tests passed | non-conformant | The required main paths are tested, but no test proves 200 rate-limited status handling, 429 reset-time fallback, or the provider-spec idle activity rule (CR-01, CR-02, CR-03). |

No obligation is orphaned in this review matrix. The missing objective-level trace for OBJ-02 is reported separately as an artifact finding rather than silently treated as coverage.

## Compliance with rules and skills

| Rule or skill | State | Evidence |
| --- | --- | --- |
| `sdd-review-code` source and task requirements | OK | Required sources, task locations, states, links, IDs, and dependencies were checked; this report is the next free `codereview_1`. |
| `AGENTS.md` Core purity | OK | No Core source changes; infrastructure/app builds passed. |
| `AGENTS.md` Borrow-Don't-Own | OK | `SharedFileReader.cs:30-35,106-111`; no auth-file writes. |
| `AGENTS.md` no invented limits | OK | Percentage-derived values and authoritative reset timestamps are mapped from the API; the 100-unit scale is explicit in the TechSpec contract. |
| `AGENTS.md` rate-limit deadline persistence | NOT OK | HTTP 429 deadlines are persisted by `UsageStore`, but the OpenCode 200 status path and reset-time fallback do not create the required deadline (CR-01, CR-02). |
| `AGENTS.md` MTP validation | OK | Native MTP was detected from `global.json` and project settings; every execution used `--minimum-expected-tests 1`. |
| `dotnet-efficient-validation` | OK | Same current code was built once, then filtered/full tests and app build were run without restore where valid. |
| Desktop .NET E2E policy | N/A by policy | E2E was omitted as required by TechSpec and repository policy; unit and build validation were still executed. |

## TechSpec adherence

| Decision or contract | State | Evidence |
| --- | --- | --- |
| DEC-01 - read-only credential discovery and environment fallback | YES | `OpenCodeCredentialDiscovery.cs:76-87,185-229`; credential tests passed. |
| DEC-02 - official endpoint and three official windows | YES | `OpenCodeApiClient.DEFAULT_ENDPOINT`; `OpenCodeUsageProvider.MapLimitWindows`; parsing/mapping tests passed. |
| DEC-03 - `RateLimitPolicy` for 429 and rate-limited responses | PARTIAL | 429 uses `CalculateDeadline`, but `status == "rate-limited"` and body `resetsAt` are not handled (CR-01, CR-02). |
| DEC-04 - process discovery for CLI and Desktop activity | PARTIAL | Both process names are checked, but implementation calls `Process.GetProcessesByName` directly and does not read the specified recent `opencode.db` activity or use the named shared liveness abstractions (CR-03). |
| DEC-05 - injectable HTTP transport and deterministic tests | YES | `OpenCodeApiClient` accepts a handler/client; all 57 OpenCode tests use in-memory handlers or deterministic delegates. |
| CMP-01 - `OpenCodeAuthDto` | YES | `OpenCodeAuthDto.cs:6-17`. |
| CMP-02 - usage and error DTOs | PARTIAL | Success/error DTOs exist, but the DTO model has no 429 reset-time field needed by the contract (CR-02). |
| CMP-03 - credential discovery | YES | `OpenCodeCredentialDiscovery.cs`; read-only and precedence tests passed. |
| CMP-04 - API client | PARTIAL | Endpoint, headers, timeout, and 429 header parsing pass; status and reset-time rate limits are incomplete (CR-01, CR-02). |
| CMP-05 - usage provider | PARTIAL | Registration and three-window snapshots pass; status state handling is incomplete (CR-01). |
| CMP-06 - activity monitor | PARTIAL | Process presence passes, but the provider specification's database activity and shared process abstraction contract is not met (CR-03). |
| CMP-07 - default app configuration | YES | `appsettings.json:6-25`, `UserSettingsFile.cs:20-22`, and app build. |
| TC-01 - credential discovery tests | YES | `OpenCodeCredentialDiscoveryTests`; covered by the OpenCode filter and full infrastructure run. |
| TC-02 - usage parsing tests | YES | `OpenCodeApiClientTests` successful response coverage. |
| TC-03 - 429 tests | PARTIAL | Header seconds/date/absent-header behavior is covered; body reset-time fallback is not. |
| TC-04 - provider snapshot and status tests | PARTIAL | Mapping, 401, 403, 429, stale, and cancellation are covered; 200 rate-limited status is not. |
| TC-05 - activity monitor tests | PARTIAL | Process presence and cancellation are covered; idle-vs-busy recent database activity is not. |

## Verified tasks

| Task | Location | State | Handoff and evidence |
| --- | --- | --- | --- |
| T01 | `done/task_01.md` | COMPLETE | Manifest marks done; handoff at `done/task_01.md:79-93`; current credential tests and full suite pass. |
| T02 | `done/task_02.md` | COMPLETE for declared task criteria; source obligation gap remains | Handoff at `done/task_02.md:87-107`; current API tests pass. FR-05 reset-time handling remains non-conformant. |
| T03 | `done/task_03.md` | COMPLETE for declared task criteria; source obligation gap remains | Handoff at `done/task_03.md:86-100`; current provider tests pass. 200 rate-limited status remains non-conformant. |
| T04 | `done/task_04.md` | COMPLETE for declared task criteria; provider-spec gap remains | Handoff at `done/task_04.md:82-102`; activity filter, full infrastructure/core tests, and app build pass. CR-03 remains. |

The manifest's `[x]` state and handoff claims are internally consistent with the task-local acceptance criteria. They do not override the non-conformant PRD/TechSpec obligations identified above.

## Executed validations

- Profile and exclusions: .NET 10 / `net10.0`; native Microsoft.Testing.Platform from `global.json`; `TokenHound.Infrastructure.Tests` and `TokenHound.Core.Tests`; E2E omitted by desktop .NET policy.
- Validated state: current staged provider implementation, app registration/configuration, task artifacts, and current test/build outputs. `git diff --cached --check` passed.
- Reused evidence: task handoff commands were treated as historical evidence and rerun against the current staged code; no prior code review was reused.
- Manual acceptance: no live OpenCode credential/network call or HUD process/database acceptance was executed. These are not replaced by unit tests; provider-spec activity behavior remains not verifiable at runtime and is also contradicted by the current implementation (CR-03).

| Command | Result | Obligations covered |
| --- | --- | --- |
| `rtk git diff --cached --check` | passed | Staged review scope hygiene |
| `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-restore --nologo --verbosity:minimal` | passed; 0 errors, 0 warnings | NFR-01; current infrastructure implementation compiles |
| `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*OpenCode*"` | passed; 57 tests | T01-T04 OpenCode unit scenarios; TC-01 to TC-05 partial coverage |
| `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1` | passed; 663 tests | T04 regression gate; infrastructure compatibility |
| `rtk dotnet test --project tests/TokenHound.Core.Tests/TokenHound.Core.Tests.csproj --minimum-expected-tests 1` | passed; 89 tests | NFR-01 and core policy regression |
| `rtk dotnet build src/TokenHound.App/TokenHound.App.csproj --no-restore --nologo --verbosity:minimal` | passed; 0 errors, 0 warnings | FR-08/CMP-07 app registration/configuration compilation |
| Local link inventory and external fetch of PRD sources | passed | Source/link integrity |

## Findings

| ID | Severity | Source | Evidence | Impact | Proven recommendation |
| --- | --- | --- | --- | --- | --- |
| CR-01 | High | FR-05, FR-07, DEC-03, CMP-05 | `OpenCodeApiClient.cs:132-135` treats every successful response as ordinary usage; `OpenCodeUsageDto.cs:45-63` stores `Status` but `OpenCodeUsageProvider.cs:109-116,187-220` never evaluates it. Current tests use only `status: "ok"` (`OpenCodeUsageProviderTests.cs:21,49-74`). | A 200 response whose usage window is `rate-limited` is shown as `ProviderStatus.Ok`; no `ActiveBlock` or deadline is produced, so polling can continue against a rate-limited account and the HUD reports false health. | Validate the usage-window status before creating an OK snapshot. Convert a rate-limited response into the same `RateLimitPolicy`/`UsageBlock` path as HTTP 429, using the authoritative reset value, and add a test for a 200 `status: "rate-limited"` payload. |
| CR-02 | High | FR-05, NFR-04, DEC-03, CMP-02/CMP-04 | `OpenCodeApiClient.cs:214-227` copies only `Retry-After` into `OpenCodeRateLimitException`; `OpenCodeErrorResponse` has no reset timestamp (`OpenCodeUsageDto.cs:69-87,111-123`); `OpenCodeUsageProvider.cs:172-184` falls back only to a cached rolling window and otherwise lets `RateLimitPolicy` use its floor. | A 429 with no `Retry-After`, no prior successful snapshot, and a server `resetsAt` is retried after the local floor instead of the authoritative reset, violating the required backoff contract and risking repeated requests during the quota block. | Parse and carry the server reset timestamp through the client exception/DTO, then calculate the block from that absolute deadline when the header is absent. Add a no-cache 429 test proving the reset timestamp is honored. |
| CR-03 | Medium | `docs/specs/12-PROVIDER-OPENCODE.md:130-184`, DEC-04, CMP-06, TC-05 | `OpenCodeActivityMonitor.cs:64-81` returns `Busy` and sets `LastActivityUtc` to `now` for any present process; production discovery is direct `Process.GetProcessesByName` (`:105-127`). The provider specification requires read-only `opencode.db` recent activity, `Busy` only within 60 seconds, otherwise `Idle`, plus `ProcessDiscovery`/`ProcessLiveness`. | An open but idle OpenCode process is reported as actively busy, causing misleading HUD state and active polling cadence. The documented local activity/token telemetry and shared liveness contract are not implemented. | Add a read-only SQLite activity reader using `SafeSqliteReader`, distinguish recent activity from idle process presence, and use or define the shared process discovery/liveness abstractions; alternatively revise the provider specification and TechSpec before approval. Add tests for recent and stale database activity. |
| CR-04 | Low | PRD OBJ-02 and task traceability | `tasks/prd-provider-opencode/tasks.md:23-42` maps FR/NFR/TC IDs but has no OBJ-02 row; `done/task_01.md:32-39` also omits OBJ-02 even though `prd.md:17-21` declares zero-configuration onboarding as an outcome. | The implementation is present, but the manifest's coverage gate does not provide an explicit objective-level trace for zero-configuration onboarding, weakening review and re-review evidence. | Add an OBJ-02 trace row to `tasks.md` and T01, or explicitly link OBJ-02 to FR-01 and its tests. |

## Previous findings (re-review only)

| Review/ID | State | Current evidence |
| --- | --- | --- |
| `-` | not applicable | No previous review exists under `tasks/prd-provider-opencode/`. |

## Limitations and open items

- No `--base` was supplied. The review cannot prove the complete branch delta or distinguish the feature changes from unrelated staged changes; it is limited to the current relevant worktree and the task handoffs.
- No live OpenCode account, network response, or Windows HUD acceptance was used. E2E omission is required by the desktop .NET policy, but it does not approve unexecuted manual behavior.
- The PRD names `ProviderStatus.RateLimitReached`, while the repository contract exposes `ProviderStatus.RateLimited`. The implementation uses the existing repository enum; the source terminology should be reconciled before a future review.
- The provider specification includes local SQLite activity/token telemetry, while the PRD/TechSpec task plan primarily covers process presence and does not create a SQLite component. This scope inconsistency is reflected in CR-03 rather than silently treated as satisfied.
- The manifest and task handoffs claim completion, but CR-01 through CR-04 leave requirement, resilience, activity, and traceability gaps. A green test count does not prove those untested scenarios.

## Conclusion

The OpenCode provider is integrated, builds cleanly, and passes all executed unit/regression validations: 57 OpenCode tests, 663 infrastructure tests, 89 core tests, and the WPF app build. It cannot be approved because the implementation does not handle the explicitly required 200 `status: "rate-limited"` response, does not honor an un-cached 429 `resetsAt` fallback, and does not meet the provider specification's idle/database activity contract. The missing OBJ-02 task trace is an additional review-artifact gap. Status is therefore `REJECTED`.
