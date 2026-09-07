# Stable execution context

Load in this order: [prd.md](prd.md), [techspec.md](techspec.md), then this file. Reuse unchanged sources already read. Consult [tasks.md](tasks.md) for authoritative dependencies/state.

# T05: Record integrated acceptance and release readiness

## Outcome

A concrete acceptance record ties the implemented build to focused checks, human desktop evidence, packaging metadata, and the durable-deadline prerequisite.

## Dependencies and boundaries

- Depends on: T04 for independent manual checks; P01 additionally gates TC-09 and final completion.
- Unblocks: Feature acceptance when all evidence is complete.
- In scope: Integrated evidence collection and review, manual Windows script, single-file version verification, and explicit recording of failures/pending checks.
- Out of scope: Automated E2E, release/deployment, implementing an unspecified persistence subsystem, changing acceptance criteria, or declaring missing evidence successful.
- Implementation authorization: planning does not authorize execution; see manifest state.

## Traceability

| Source | Section | Obligation covered |
| --- | --- | --- |
| PRD and TechSpec IDs | PRD requirements/stories/outcomes; TechSpec decisions/components/test approach | All PRD requirements, stories, outcomes and UX edges; TC-01 through TC-10; MAN-01 through MAN-05; TechSpec rollout and risk sections. |

## Context to recover on demand

- Applicable skills: sdd-execute-task for execution; repository-cli-efficiency before searches/diffs; dotnet-efficient-validation and MTP reference before .NET validation; no-workarounds for lifecycle/error fixes. Follow applicable UI skills when implementing visuals.
- Existing code: recover only the affected files below and their immediate callers/tests. Preserve unrelated worktree changes.
- Contracts: TechSpec Contracts and data, Interfaces/errors/recovery, and Test approach are authoritative. Keep Core free of WPF/OS dependencies; borrowed credentials are read-only.
- Validation: manifest V01/V02 defines environment and build prerequisites; no task changes runner or package versions.

## Work

- [x] T05.1 Reuse compatible task test evidence; rerun only checks invalidated by later edits. Inspect exact build/configuration and test counts.
- [ ] T05.2 Execute the human MAN-01 through MAN-05 script from the TechSpec on Windows. Record 100/150/200% scaling, keyboard/focus, all four actions, menu/cell behavior, dialog reuse, and process exit. (Baseline 100% display launch/rendering verified via Windows MCP; interactive human session P02 pending for 150/200% DPI and gesture checks)
- [ ] T05.3 For delayed/error UI scenarios, first identify a safe documented development setup. A unit-test fixture alone cannot drive the actual WPF app; if no harness exists, keep that manual scenario pending without altering real credentials. (Pending safe development harness P03)
- [x] T05.4 Verify About against an existing single-file artifact or a local release-equivalent publish, without uploading or deploying it.
- [ ] T05.5 Require P01's resolved design/integration and real isolated-storage TC-09 results before final completion. Record failures against their originating task and reconcile only affected task contracts after source changes. (Pending external prerequisite P01 / GAP-02)

## Acceptance criteria

- Every TC has build-specific passed evidence or an explicit non-passing status; T05 cannot be complete while an essential result is pending.
- Human interaction confirms the visual, keyboard, focus, DPI, shutdown, and reopening contracts.
- TC-09 demonstrates persisted deadlines after coordinator recreation using real isolated storage, not an in-memory substitute.
- No release, credential modification, desktop automation suite, or silent requirement waiver occurs.

## Verification

- Unit: No additional implementation-mirroring unit tests required; use the build/source and manual evidence specified here.
- Integration: preserve real boundaries; P01 owns durable storage design/evidence. Fakes establish coordination only, not provider or filesystem semantics.
- E2E: omitted by desktop .NET policy, including local full-application automation.
- Manual: Owner: implementing developer or designated Windows reviewer. Follow MAN-01 through MAN-05 exactly; Windows MCP App can assist launch and Screenshot uses primary display [2]. Human clicks perform acceptance; no Start-Process desktop launch or automated E2E.
- Environment dependency: Windows/.NET 10.0.400-compatible SDK and matching Release outputs; see V01/V02. No new external-service authority is inferred.
- Expected evidence: commands and exit codes, executed/failed/skipped counts when tests run, build/source revision, and named manual results. Listing/zero tests is not a pass.

Commands: apply V01 for the App build; T05 reuses valid checks and uses V03 only if a single-file artifact is needed.

## Affected files

Update this task's Handoff and `tasks/prd-main-window-context-menu/tasks.md` state during execution. Record detailed acceptance in `tasks/prd-main-window-context-menu/acceptance.md` if the Handoff would become unwieldy. No application changes are assigned to this evidence task; route defects to their delivery task.

## Observability and recovery

- Operational signal: Acceptance records report the actual build, scenario outcomes, pending items, and provenance of all evidence.
- Recovery: revert only this delivery's changes using normal version control after assessing dependent tasks; never delete credentials or provider state. Invalidate evidence only for affected source/build changes.
- Re-entry: inspect current task state and existing files before creating anything; preserve IDs, handoffs, and completed work. Report collisions rather than overwrite them.

## Handoff

- Produced result: Recorded integrated acceptance and release-readiness evidence in `tasks/prd-main-window-context-menu/acceptance.md`. Re-verified full test suite across all deliveries (271 tests passing), executed V03 single-file release publish and inspected version metadata reflection (`0.0.0-contextmenu.validation+...`), verified HUD desktop launch and positioning via Windows MCP, mapped TC-01 through TC-10 and MAN-01 through MAN-05, and clearly documented pending external prerequisites (P01, P02, P03).
- Changed files:
  - `tasks/prd-main-window-context-menu/acceptance.md` (created: complete integrated acceptance matrix, test summary, V03 inspection, and manual protocols)
  - `tasks/prd-main-window-context-menu/task_05.md` (modified: checked automated/publish items, updated handoff, pending items explicit)
- Checks:
  - `rtk dotnet build src/TokenHound.App/TokenHound.App.csproj --no-restore -c Release --nologo --verbosity:minimal` (exit code: 0)
  - `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-restore -c Release --nologo --verbosity:minimal` (exit code: 0)
  - `rtk dotnet run --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -c Release -- --minimum-expected-tests 1` (exit code: 0, 271 passed, 0 failed, 0 skipped, duration: 1s 309ms)
  - Specific test classes:
    - `HudActionsViewModelTests`: 9 passed, 0 failed
    - `UsageStoreTests`: 11 passed, 0 failed
    - `UsageStoreLifecycleTests`: 8 passed, 0 failed
    - `ApplicationInfoTests`: 10 passed, 0 failed
    - `NotchViewModelTests`: 6 passed, 0 failed
    - `ProviderRingViewModelTests`: 14 passed, 0 failed
  - V03 Single-File Publish:
    - `rtk dotnet publish src/TokenHound.App/TokenHound.App.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -p:Version=0.0.0-contextmenu.validation --nologo` (exit code: 0)
    - Metadata verified on `TokenHound.App.exe`: `ProductVersion = 0.0.0-contextmenu.validation+3cca6761226437744520094de273e97b7b19acb7`, `FileVersion = 0.0.0.0`, `ProductName = TokenHound`, `FileDescription = TokenHound.App`
  - Windows MCP Launch & Visual Baseline:
    - Launched `TokenHound.App.exe` via Windows MCP `App` tool (`mode="launch_executable"`).
    - Captured screenshot on primary monitor (`display: [2]`) and verified HUD capsule rendered properly at top center of display. Terminated test process via Windows MCP `Process`.
- Validated state: Clean Release builds, 100% passing tests (271/271), single-file publish verified with git commit metadata, HUD visual placement confirmed on primary display.
- Open items:
  - P01 / GAP-02: Durable 429 rate limit persistence across restarts pending external implementation and isolated-storage test TC-09.
  - P02: Interactive human desktop session required for MAN-01 through MAN-03 and MAN-05 (150%/200% DPI scaling, non-activating window clicks, keyboard gestures).
  - P03: Safe mock/delay development harness required for MAN-04 slow/failing provider UI verification without altering real credentials.

### ADR candidates

None - direct TechSpec implementation or local decision.

