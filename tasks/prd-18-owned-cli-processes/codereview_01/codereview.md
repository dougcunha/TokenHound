# Code review report — Owned CLI processes

## Summary

- Status: REJECTED
- Execution: delegated reviewer
- Git scope: `577a55a79f52e225c28f2e4fb92362c6b35a818d` to the current worktree (tracked changes and new files; HEAD remains at the base)
- Previous review: no prior formal report; the earlier read-only risk review observation is recorded below

## Sources and scope

| Source | Path or reference | State |
| --- | --- | --- |
| PRD | `tasks/prd-18-owned-cli-processes/prd.md` | read |
| TechSpec | `tasks/prd-18-owned-cli-processes/techspec.md` | read |
| Manifest and handoff | `tasks/prd-18-owned-cli-processes/tasks.md`, `done/task_01.md` | read; links resolve and T01 is located |
| Validation | `tasks/prd-18-owned-cli-processes/validation.md` | read; final evidence reused because no source or test changes followed it |
| Implementation | tracked diff, new process/session sources, tests, solution and feature artifacts | bounded against the stated base; includes untracked implementation files |
| Repository guidance | `AGENTS.md`, architecture documentation, `repository-cli-efficiency`, `no-workarounds`, `dotnet-efficient-validation` | read/applied |

## Coverage matrix

| Source | Obligation | Implementation | Test | State | Evidence |
| --- | --- | --- | --- | --- | --- |
| PRD FR-01 | Reuse one Codex server and serialize/correlate requests | `CodexAppServerClient`, `CodexServerProtocol` | `CodexSessionTests`, `OwnedProcessTests` | conformant | One initialization, sequential reuse, unique IDs, concurrent serialization and protocol filtering are covered in `validation.md:17-19`. |
| PRD FR-02 | Recover with bounded restart backoff | `CodexAppServerClient` | `CodexSessionTests` | conformant | Fake-clock backoff and recovery coverage; cap is 60 seconds per DEC-02. |
| PRD FR-03 | Own descendants, including after root exit or owner termination | `Processes/WindowsProcessLauncher`, `WindowsJob`, `WindowsProcessAttributes` | `OwnedProcessTests` | conformant | Job assignment is part of suspended process creation; tests cover descendants after root exit and owner death (`validation.md:19`). |
| PRD FR-04 | Graceful shutdown followed by whole-tree termination | `Processes/OwnedProcess.DisposeAsync` | `OwnedProcessTests` | conformant | Closes stdin, waits up to three seconds, then closes the job; tests cover EOF, timeout, and early root exit. |
| PRD FR-05 | Preserve usage fallback and borrowed credentials; use owned host for `gh` | `CodexUsageProvider`, `CopilotCredentialDiscovery`, `App.xaml.cs` | provider and credential discovery tests; App build | conformant | Rollout fallback remains; expected `gh` startup/read failures return to credential-source fallback, while caller cancellation propagates. App registers owned resources for disposal. |
| PRD NFR-01 | No shell/window; redirected stdio | `WindowsProcessNative`, `OwnedProcess`, launcher | `OwnedProcessTests` | conformant | `CREATE_NO_WINDOW`, redirected streams and explicit inherited-handle list are used. |
| PRD NFR-02 | Preserve cancellation, concurrency, response isolation and stderr draining | client/session and process host | `CodexSessionTests`, `OwnedProcessTests` | conformant | Active/queued cancellation, serialization, stale/malformed messages and stderr saturation are covered. |
| TechSpec TC-01–04 | Automated protocol, recovery, Windows ownership, provider and build checks | paths above | Infrastructure suite and App build | conformant | Final integrated run passed 1145 tests; build evidence is recorded below. |
| PRD/TechSpec TC-05 | One-hour desktop acceptance, live usage, normal/forced exit, Popup event observation | live TokenHound desktop session | manual | not verifiable | `validation.md:25-32` explicitly records these checks as not performed. |

## Compliance with rules and skills

| Rule or skill | State | Evidence |
| --- | --- | --- |
| Repository invariants and C# guidance | OK | Changed paths preserve Core boundaries; process ownership is in Infrastructure; cancellation and async disposal are propagated. |
| `repository-cli-efficiency` | OK | Graft context was used first; scoped `rtk` searches and diffs were used. |
| `no-workarounds` | OK | No workaround masking a lifecycle failure was found; expected `gh` launch/read failures are caught at the credential-source boundary. |
| `dotnet-efficient-validation` | OK | Reused project-scoped MTP command with `--minimum-expected-tests 1`; evidence includes exit code and executed count. |
| `sdd-review-code` fresh-context delegation | OK, with context noted | This reviewer was spawned without authoring-session context and did not write or change implementation code. The same reviewer context had performed the earlier read-only risk review, so the `gh` correction is assessed with that prior observation in view. |

## Quality profile

| ID | Rule | Class | Command/check | Hits | State |
| --- | --- | --- | --- | --- | --- |
| QA-01 | No sync-over-async or direct parameterless `Process.Kill()` | blocking | Scoped searches over changed production paths | 0 new | OK |
| QA-02 | No swallowed unexpected errors or credential/output logging | blocking | Diff and scoped error/logging review | 0 | OK; expected `gh` failures are handled at the source boundary, with no credential contents logged |
| QA-03 | Preserve cancellation and job ownership on every path | blocking | Lifecycle path review plus process/session tests | 0 | OK |
| QA-04 | Files <=300 lines and methods <=30 lines | reservation | Changed/new production files and reviewed diff | 0 new reservations | OK; `App.xaml.cs` exceeds 300 lines in the Terrain baseline and the diff only adds disposal registration |

- Terrain baseline: applied; the pre-existing `App.xaml.cs` size is discounted.
- Hits discounted by baseline: 1.
- Reservations accumulated in this feature: 0.
- Suggested escalation: no trigger fired.

## TechSpec adherence

| Decision or contract | State | Evidence |
| --- | --- | --- |
| DEC-01: single initialized Codex server; serialized, correlated requests | YES | `CodexAppServerClient` and `CodexServerProtocol`; protocol reuse, handshake, concurrency and filtering tests. |
| DEC-02: retire failed sessions and apply bounded, cancellable backoff | YES | TimeProvider-backed retry delay and fake-clock recovery tests. |
| DEC-03: atomic Windows job ownership and restricted handle inheritance | YES | `WindowsProcessLauncher`, `WindowsJob`, `WindowsProcessAttributes`; suspended creation and helper-process lifecycle tests. |
| DEC-04: graceful stdin close, bounded wait, then job cleanup even after root exit | YES | `OwnedProcess.DisposeAsync`; graceful, timeout and orphan-descendant tests. |
| DEC-05: shared host for `gh`, existing browser/update handoffs remain independent | YES | `CopilotCredentialDiscovery`; App registration and credential fallback tests. |

## Verified tasks

| Task | Location | State | Handoff and evidence |
| --- | --- | --- | --- |
| T01 | `done/task_01.md` | INCOMPLETE for feature acceptance | Implementation and automated checks are complete; independent review and TC-05 acceptance remain open. Handoff links to final validation evidence. |

## Executed validations

- Profile and exclusions: Windows/.NET 10 Infrastructure and App; desktop E2E omitted under repository policy.
- Validated state: current worktree, including the helper project in `TokenHound.slnx`; validation ran on Windows with .NET SDK 10.0.401, Debug configuration.
- Reused evidence: `validation.md`; parent confirmed no source or test changes since its final run.
- Manual acceptance: TC-05 remains essential and unverified; details are listed under limitations.

| Command | Result | Obligations covered |
| --- | --- | --- |
| `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-restore --nologo --verbosity:minimal` | passed; zero errors/warnings | TC-01–04 build |
| `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1` | passed; 1145 tests, exit 0 | TC-01–04 |
| `rtk dotnet build src/TokenHound.App/TokenHound.App.csproj --no-restore --nologo --verbosity:minimal` | passed; zero errors/warnings | TC-04 |
| `rtk git diff --check` | passed; exit 0 | diff hygiene |
| `rtk dotnet sln TokenHound.slnx list` | passed; helper project included | test project discovery |
| Post-test helper process enumeration | passed; zero `TokenHound.ProcessTestHelper` processes remained | TC-03 cleanup |
| Scoped TechSpec quality searches | passed; no new blocking hits or reservations | QA-01–04 |

## Findings

| ID | Severity | Source | Evidence | Impact | Proven recommendation |
| --- | --- | --- | --- | --- | --- |
| — | — | — | No current implementation defect identified in the reviewed lifecycle, protocol, fallback, or disposal paths. | — | — |

## Previous findings (re-review only)

| Review/ID | State | Current evidence |
| --- | --- | --- |
| Earlier read-only risk review / `gh` fallback | resolved | `CopilotCredentialDiscovery` now handles expected startup/read failures at `TryReadGhTokenAsync`; five regression cases cover missing executable, invalid start, broken pipe, access denied and cancellation. Final 1145-test run passed (`validation.md:21`). |

## Limitations and open items

- TC-05 is essential and not verifiable from helper tests: the one-hour desktop soak, real Codex/live quota and rollout fallback check, normal and Task Manager exits with descendant attribution, and Application Popup event 26 inspection remain unperformed. The SDD status rule therefore requires REJECTED until that evidence is recorded.
- The reviewer was a fresh subagent relative to the authoring session and made no implementation changes, but the same reviewer context had performed the earlier read-only risk review. The `gh` correction was therefore checked with the earlier observation in view rather than as a blind review.

## Conclusion

No current code defect was identified, and the automated lifecycle and protocol evidence conforms to FR-01–05, NFR-01/02, and TC-01–04. Status is REJECTED because essential TC-05 desktop evidence is pending. The earlier `gh` fallback observation is resolved and covered by passing regression tests.
