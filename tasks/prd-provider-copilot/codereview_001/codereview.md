# Code review report - provider-copilot

## Summary

- Status: REJECTED
- Git scope: `5b7622a2586794c193aa140c2064c47faa52fa55..current worktree`, including staged, unstaged, and new files
- Previous review: none

## Sources and scope

| Source | Path or reference | State |
| --- | --- | --- |
| PRD | `tasks/prd-provider-copilot/prd.md` | read |
| TechSpec | `tasks/prd-provider-copilot/techspec.md` | read |
| Manifest | `tasks/prd-provider-copilot/tasks.md` | read |
| Implementation | T01 through T05 source/test changes, task handoffs, and integrated worktree diff | delimited by checkpoint base and current worktree |
| Review instructions | `sdd-review-code` template and repository instructions | read |

The review includes the current product/source/test changes and task artifacts. The worktree contains pre-existing staged task-checkpoint edits that were preserved and reviewed as part of the current integrated state.

## Coverage matrix

| Source | Obligation | Implementation | Test | State | Evidence |
| --- | --- | --- | --- | --- | --- |
| FR-01, FR-03 through FR-08; AC-01 through AC-05, AC-08, AC-09 | Discover a borrowed Copilot credential, issue the bounded quota request, parse finite open-map data, and map auth/unsupported/stale/overage outcomes | `src/TokenHound.Infrastructure/Providers/Copilot/` | Copilot credential, config, API, parser, and provider tests | partial | Automated provider coverage passes. IP-01 remains open because the official plaintext config property was not verified, so the default config fallback is intentionally unavailable. |
| FR-07, FR-11; AC-04 through AC-06, AC-10 | Retain last-good data, persist absolute rate-limit deadlines, and gate dispatch after restart | `UsageArchive*`, `UsageStore*`, `SnapshotRetentionPolicy.cs`, shared rate-limit policies | UsageArchive, UsageStore, activity, Core policy tests | conformant | Restart, stale, auth-clearing, deadline, cadence, and Retry-After tests pass; full Infrastructure test project reports 324 passed and Core reports 43 passed. |
| FR-09, FR-10; AC-07 | Detect recent Copilot activity only with a qualifying live host, freshness, shared reads, and debounce | `CopilotActivityMonitor.cs`, `CopilotProcessHostDetector.cs` | Copilot activity and host detector tests | conformant | Focused Copilot filter and full Infrastructure suite pass; no session/log fixture is modified. |
| FR-12; NFR-05, NFR-07; AC-01, AC-03, AC-08, AC-10 | Feed generic snapshot/activity/status data through the existing App and Notch paths | `App.xaml.cs`, `NotchViewModel.cs`, `ProviderRingViewModel*` | ViewModel tests; App build | conformant for automated scope | App build passes, activity event routing tests pass, and existing catalog/glyph and generic status paths are reused. |
| NFR-01, NFR-02, NFR-06 | Keep Core pure and credentials read-only | Core project and Infrastructure credential/file boundaries | Core build; source/dependency inspection; credential tests | conformant | Core has no project/package references or UI, OS, HTTP, filesystem, or process dependencies. No credential-owned file was changed. |
| NFR-07; TC-11; AC-10 | Preserve non-activating, click-through HUD behavior | `NotchWindow.xaml.cs`, `WindowStyles.cs` unchanged | Static diff inspection; App build | not verifiable | The source diff is unchanged and the App compiles, but the required Windows MCP launch, Screenshot, foreground interaction, and drag check were unavailable. |
| TC-01 through TC-10, TC-12 | Execute project-scoped automated evidence with non-zero counts and no aggregate desktop E2E | Core.Tests, Infrastructure.Tests, App project | Native MTP and separate builds | conformant | All executed commands used project scope and `--minimum-expected-tests 1`; no desktop E2E command was run. |
| T06.5 through T06.7 | Confirm manual HUD, activity, and focus behavior on Windows MCP | None completed in this session | Windows MCP App/Screenshot route | pending | The required tool route is unavailable, so this essential acceptance remains open. |

## Compliance with rules and skills

| Rule or skill | State | Evidence |
| --- | --- | --- |
| Core purity | OK | Core build passes; project/dependency inspection found no UI, OS, HTTP, filesystem, or process dependency. |
| Credential ownership | OK | Copilot readers use read-only/shared access; discovery never writes or refreshes borrowed credentials; no credential-owned file changed. |
| Rate-limit contract | OK | Durable absolute deadlines and the non-immediate zero floor are tested in Core and Infrastructure. |
| MTP validation | OK | Native `dotnet test --project` commands use `--minimum-expected-tests 1` and xUnit MTP class filters. |
| Repository CLI efficiency | OK | Searches and diff checks were scoped and prefixed with `rtk`; final diff check is clean. |
| No-workarounds review | OK | The file-size issue was corrected by separating concerns into partial files; no test suppression or fallback was introduced to hide a defect. |
| File/class size rule | OK for affected files | Affected source files are all at or below 300 lines after the structural split. |
| Desktop E2E policy | OK | Desktop E2E was omitted as required by the .NET desktop policy; the omission is not treated as manual acceptance. |
| Required manual desktop evidence | NOT OK | Windows MCP App/Screenshot tools were unavailable, leaving T06.5 through T06.7 unverified. |

## TechSpec adherence

| Decision or contract | State | Evidence |
| --- | --- | --- |
| DEC-01, DEC-02, DEC-06 | YES | Existing Core contracts are extended additively and shared policy/retention behavior is covered by Core tests. |
| DEC-03 and IP-01 | PARTIAL | Official precedence and validated target derivation are implemented. The plaintext fallback is disabled by default because no sanitized official fixture/property evidence was available. |
| DEC-04, DEC-05 | YES | The provider uses a single bounded GET, bearer authorization, JSON acceptance, cancellation, typed error mapping, and no retry loop. |
| DEC-07, DEC-08 | YES | Activity monitor, host conjunction, debounce, independent activity timer, and existing ring event route are implemented and tested. |
| DEC-09, DEC-10 | YES for automated route | Native MTP project-scoped validation and preserved HIL1 deferrals are documented; manual desktop validation remains pending. |

## Verified tasks

| Task | Location | State | Handoff and evidence |
| --- | --- | --- | --- |
| T01 | `task_01.md` | COMPLETE | Core seams and policies implemented; 43 Core tests pass. |
| T02 | `task_02.md` | COMPLETE | TokenHound archive, deadline persistence, activity plumbing, and tests implemented; current Infrastructure suite passes. |
| T03 | `task_03.md` | COMPLETE WITH ACCEPTANCE GAP | Provider path and tests pass; IP-01 plaintext fallback gap is explicitly recorded and remains disabled. |
| T04 | `task_04.md` | COMPLETE | Activity monitor and host detector tests pass. |
| T05 | `task_05.md` | COMPLETE | App registration, disposal, event routing, generic statuses, and App build pass; focus source remains unchanged. |
| T06 | `task_06.md` | INCOMPLETE | Automated validation is complete. Manual Windows MCP checks are not verifiable and block approval. |

## Executed validations

- Profile and exclusions: .NET SDK 10.0.400 with native Microsoft.Testing.Platform; Core, Infrastructure, and App were validated separately. Desktop E2E was omitted under the .NET desktop policy.
- Validated state: current worktree after the partial-file size cleanup, with restore assets already valid and no source changes after the final build/test set except task/review documentation.
- Reused evidence: T01 through T04 handoffs were rechecked against the current integrated source and current full test run; older test counts were superseded by the current counts below.
- Manual acceptance: not verifiable because no Windows MCP App/Screenshot tools are exposed in this session. This is an essential limitation, not a passed check.

| Command | Result | Obligations covered |
| --- | --- | --- |
| `rtk dotnet build tests/TokenHound.Core.Tests/TokenHound.Core.Tests.csproj --no-restore --nologo --verbosity:minimal` | passed, exit 0 | Core compilation and purity |
| `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-restore --nologo --verbosity:minimal` | passed, exit 0; 81 pre-existing warnings | Infrastructure compilation |
| `rtk dotnet build src/TokenHound.App/TokenHound.App.csproj --no-restore --nologo --verbosity:minimal` | passed, exit 0; 3 pre-existing NU1903 warnings | App integration compilation |
| `rtk dotnet test --project tests/TokenHound.Core.Tests/TokenHound.Core.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1` | passed, 43 tests | Core policy/model obligations |
| `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*Copilot*"` | passed, 37 tests | T03/T04 Copilot provider and activity obligations |
| `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*UsageStore*"` | passed, 24 tests | T02/T05 archive, scheduling, and activity-store obligations |
| `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1` | passed, 324 tests | Integrated Infrastructure regression coverage |
| `rtk git diff HEAD --check` | passed, clean | Repository hygiene |

## Findings

| ID | Severity | Source | Evidence | Impact | Proven recommendation |
| --- | --- | --- | --- | --- | --- |
| CR-01 | High | TC-11, T06.5-T06.7, AC-10 | `task_06.md` and `workflow.md` record that Windows MCP App/Screenshot tools were unavailable; no launch, display `[2]` screenshot, approved-session activity transition, or foreground drag check was executed. | The essential non-activating HUD and live activity acceptance is not verifiable, so the integrated feature cannot be approved. | Run T06.5 through T06.7 through the required Windows MCP App/Screenshot route with an approved existing session, then update the T06 handoff and rerun the review decision. |
| CR-02 | High | IP-01, DEC-03, AC-09, TC-02 | `task_03.md` records that the official CLI 1.0.83 isolated fake-token run returned 401 without creating config state, and no token property was verified. `CopilotConfigReader` therefore leaves plaintext fallback disabled unless an explicit verified path is injected. | The documented config fallback path is unavailable. Credential discovery still works through environment, `gh`, and keychain branches, but T06 explicitly requires the IP-01 gap to remain open. | Obtain a sanitized official fixture in isolated `COPILOT_HOME`, verify one exact property path, add a fixture test, and enable only that allowlisted path. Do not guess or recursively search token-shaped values. |

## Previous findings

No previous review exists.

## Limitations and open items

- Windows MCP App and Screenshot tools are unavailable in this session. T06.5 through T06.7 remain not verifiable and are essential to acceptance.
- IP-01 remains an explicit, documented acceptance gap. The default plaintext config fallback is intentionally disabled; no real token was stored or logged.
- HIL1-01 through HIL1-06 remain deferred product decisions exactly as approved in the workflow and TechSpec.
- The Infrastructure build retains the pre-existing `NU1903` SQLite advisory and existing xUnit1051 cancellation analyzer warnings. They did not fail the build or tests and were not suppressed.

## Conclusion

The implementation is automated-test green and adheres to the Core, credential, rate-limit, activity, App-composition, and non-activating-source contracts that can be verified locally. The review is REJECTED because the required Windows MCP manual evidence is unavailable and IP-01 remains an explicit acceptance gap. No source workaround should be added to bypass either limitation. Approval requires the manual desktop script and a product decision on whether the unverified plaintext fallback must be enabled after fixture evidence is obtained.
