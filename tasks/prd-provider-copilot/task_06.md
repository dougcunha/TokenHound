# Stable execution context

Load in this exact order:

1. `tasks/prd-provider-copilot/prd.md`
2. `tasks/prd-provider-copilot/techspec.md`
3. This file

Use the approved PRD and TechSpec versions. T01 through T05 must be approved in the current integrated state before this task starts.

---

# T06 - Validate the integrated desktop feature

## Outcome

The integrated Copilot feature has project-scoped automated evidence, an App compilation result, and manual Windows MCP evidence for the HUD/activity/focus journeys, with desktop E2E explicitly omitted.

## Dependencies and boundaries

- Depends on: T01, T02, T03, T04, and T05
- Unblocks: —
- In scope: affected-project build/test commands, test-count and exit-code capture, integrated evidence review, Windows MCP launch/screenshot/manual acceptance, and open-item reporting.
- Out of scope: changing source to make a validation pass, solution-wide E2E, live endpoint failure fabrication, credential refresh/login, commit/push/deploy, and resolving deferred HIL1 product choices.

## Traceability

| Source | Section | Obligation covered |
| --- | --- | --- |
| FR-07, FR-11, FR-12 | `prd.md#functional-requirements` | Integrated status/history, deadline, cadence, activity, and Notch evidence. |
| NFR-01 through NFR-07 | `prd.md#non-functional-requirements` | Security, fidelity, resilience, schema/timing, architecture, and HUD checks. |
| AC-01 through AC-10 | `prd.md#acceptance-criteria` | End-to-end acceptance matrix using automated fixtures and manual desktop steps. |
| DEC-09, DEC-10 | `techspec.md#technical-decisions` | Native MTP validation, desktop E2E omission, and preserved deferrals. |
| TC-01 through TC-12 | `techspec.md#test-approach` | Full planned validation evidence and manual script. |

## Context to recover on demand

- Applicable skills: `dotnet-efficient-validation`, `repository-cli-efficiency`, `no-workarounds`.
- Existing code: affected Core, Infrastructure, and App projects plus Windows MCP App/Screenshot route required by `AGENTS.md`.
- Contract or integration: TechSpec `Test approach`, manual acceptance steps, `Observability and rollout`, and `Risks and open items`.

## Work

- [ ] T06.1 Confirm restore assets are valid; restore only affected projects when assets are missing or package references changed.
- [ ] T06.2 Build Core tests, Infrastructure tests, and the WPF App separately with the exact no-restore commands from the TechSpec; preserve each exit code.
- [ ] T06.3 Run Core tests and the Infrastructure Copilot/UsageStore filters with native MTP and `--minimum-expected-tests 1`; record executed counts and stdout failures.
- [ ] T06.4 Review the integrated diff against PRD/TechSpec and confirm no E2E target entered an aggregate command, no Core dependency was added, and no credential-owned file changed.
- [ ] T06.5 Launch `TokenHound.App` through Windows MCP App with `mode="launch_executable"`, inspect the primary monitor with Screenshot `display: [2]`, and record the Copilot ring/status/activity result.
- [ ] T06.6 Exercise a local Copilot session with an approved existing auth state; confirm recent qualifying write plus host is Busy, freshness/host loss returns Idle, and quota refresh cadence remains unchanged.
- [ ] T06.7 Click and drag the Notch while another application is foreground; confirm focus remains with that application. Record any manual limitation without masking it.

## Acceptance criteria

- Every required build/test command exits successfully and executes at least one test; failures are inspected from stdout and not suppressed.
- Core and Infrastructure checks remain project-scoped; App compilation is separate; no desktop E2E is run or included in an aggregate suite.
- Automated fixtures cover the TC-01 through TC-12 matrix, including IP-01 evidence or its explicit acceptance gap.
- The manual HUD screenshot and interaction confirm existing non-activating/click-through behavior and generic Copilot presentation.
- Any unresolved essential validation, IP-01 gap, or manual limitation is reported as open and prevents feature completion/review approval.

## Verification

- Unit: `rtk dotnet test --project tests/TokenHound.Core.Tests/TokenHound.Core.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1`.
- Integration: `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*Copilot*"` and the separate `"*UsageStore*"` filter.
- E2E: omitted by .NET desktop policy; do not rename desktop automation as integration.
- Manual: Windows MCP App launch, primary-monitor Screenshot `[2]`, activity transition, foreground interaction, and focus/non-activation script; owner is the coordinator or desktop reviewer.
- Commands: `rtk dotnet build tests/TokenHound.Core.Tests/TokenHound.Core.Tests.csproj --no-restore --nologo --verbosity:minimal`; `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-restore --nologo --verbosity:minimal`; `rtk dotnet build src/TokenHound.App/TokenHound.App.csproj --no-restore --nologo --verbosity:minimal`; then the project-scoped test commands above.
- Environment dependency: Windows 11, .NET SDK 10.0.400, native Microsoft.Testing.Platform, Windows MCP App/Screenshot tools, and an approved existing Copilot/gh session. Do not paste a PAT or create credentials.
- Expected evidence: build/test exit codes, non-zero test counts, stdout failure inspection, manual Screenshot, and a concise acceptance record.

## Affected files

- Modify: None expected. Validation output is recorded in the task handoff/review artifacts, not by altering source to satisfy a check.
- Create: None required by this task.

## Observability and recovery

- Operational signal: test stdout/counts, build exit codes, Windows Screenshot, manual acceptance notes, and explicit open-item list.
- Recovery: stop at the first essential failure, preserve stdout and current diff, return the finding to the owning task, and do not broaden scope or weaken a test.

## Handoff

> Updated by `sdd-execute-task` during implementation.

- Produced result: Pending execution.
- Changed files: Pending execution.
- Checks: Pending execution.
- Validated state: Pending execution (code/diff, configuration, projects, and environment).
- Open items: Pending execution.

### ADR candidates

Pending execution. `sdd-execute-task` replaces this text with structured candidates or `None - direct TechSpec implementation or local decision`.
