# Stable execution context

Load in this exact order:

1. `tasks/prd-provider-copilot/codereview_001/codereview.md`
2. `tasks/prd-provider-copilot/task_06.md`
3. This file

Use the current rejected review and T06 handoff. Do not edit `codereview_001/codereview.md`.

---

# T07 - Complete Windows MCP desktop acceptance for Copilot integration

## Outcome

The integrated Copilot feature has recorded Windows MCP evidence for the HUD display, activity transitions, and foreground-focus behavior required by CR-01, or the task remains explicitly pending when the required environment is unavailable.

## Dependencies and boundaries

- Depends on: T06, Windows MCP App/Screenshot tools, and an approved existing Copilot/gh session
- Unblocks: Re-review of `codereview_001` and the T06 completion decision
- In scope: T06.5 through T06.7 manual acceptance, evidence capture, and the T06 handoff update
- Out of scope: product-source changes, desktop E2E automation, credential creation or refresh, PAT entry, and deferred HIL1 presentation decisions

## Traceability

| Source | Section | Finding covered |
| --- | --- | --- |
| `codereview_001/CR-01` | `codereview.md#findings` | Required Windows MCP launch, Screenshot, activity, and focus evidence was not executed. |
| T06.5-T06.7 | `task_06.md#work` | The three incomplete manual acceptance steps remain open. |
| NFR-07, AC-10 | `prd.md#non-functional-requirements`, `prd.md#acceptance-criteria` | Copilot must preserve non-activating HUD behavior and the integrated activity/cadence states. |
| TC-11, DEC-09 | `techspec.md#test-approach`, `techspec.md#technical-decisions` | App compilation and manual desktop validation are required; desktop E2E remains omitted. |

## Requirements

- Launch `TokenHound.App` through the Windows MCP App tool with `mode="launch_executable"` and inspect the primary monitor with Screenshot `display: [2]`.
- Use only an approved existing Copilot/gh authentication state. Do not paste a PAT, log a token, create credentials, or alter Copilot-owned files.
- Confirm a recent qualifying Copilot write together with a live qualifying host produces Busy, and that freshness expiry or host loss produces Idle while the existing quota cadence remains intact.
- With another application foreground, click and drag the Notch and confirm the foreground application remains active.
- Preserve the existing `WM_MOUSEACTIVATE`/`MA_NOACTIVATE` and non-activating window contract; do not change source to make a manual check pass.

## Context to recover on demand

- TechSpec: `test-approach#TC-11`, `technical-decisions#DEC-09`, and `risks-and-open-items`
- Rules/skills: repository desktop validation policy, `repository-cli-efficiency`, `no-workarounds`
- Code: `src/TokenHound.App/UI/Windows/NotchWindow.xaml.cs` and `src/TokenHound.App/Interop/WindowStyles.cs` - existing non-activation and focus-preservation behavior under test
- Code: `src/TokenHound.Infrastructure/Providers/Copilot/CopilotActivityMonitor.cs` and `CopilotProcessHostDetector.cs` - activity and qualifying-host behavior under test

## Work

- [ ] T07.1 Confirm the existing App build is current and launchable; retain the project-scoped build evidence without running desktop E2E.
- [ ] T07.2 Launch the App through Windows MCP, capture Screenshot `display: [2]`, and record the Copilot ring/status/activity presentation.
- [ ] T07.3 Exercise an approved existing Copilot session and record Busy from a qualifying recent write plus host, then Idle after freshness or host loss; record that the 60/300-second quota cadence is unchanged.
- [ ] T07.4 Put another application in the foreground, click and drag the Notch, and record that foreground focus does not change.
- [ ] T07.5 Update `task_06.md` with the evidence and request a fresh review decision without changing `codereview_001/codereview.md`.

## Acceptance criteria

- A Windows MCP Screenshot of primary monitor `display: [2]` and the observed Copilot presentation are recorded.
- The approved-session activity script records both Busy and Idle transitions with the required host/freshness conjunction and no cadence regression.
- The foreground application remains foreground during both Notch click and drag interaction.
- T06.5 through T06.7 are checked off only when their evidence exists; otherwise the task and T06 remain pending with the exact environment limitation recorded.
- No product source, credential-owned state, or existing review report is modified.

## Verification

- Unit: existing T04 activity/host tests remain the automated evidence; no new unit behavior is authorized by this correction.
- Integration: existing T05/T06 project-scoped validation remains the automated evidence; rerun the App build if the integrated worktree changed.
- E2E: omitted by .NET desktop policy; the Windows MCP route is manual acceptance, not a renamed E2E test.
- Manual: Windows MCP App launch, Screenshot `display: [2]`, approved-session activity transition, and foreground click/drag check; owner is the coordinator or desktop reviewer.
- Environment dependency: Windows 11, .NET SDK 10.0.400, Windows MCP App/Screenshot tools, and an approved existing Copilot/gh session. Missing tools or session state is an explicit pending item.
- Commands: `rtk dotnet build src/TokenHound.App/TokenHound.App.csproj --no-restore --nologo --verbosity:minimal`.
- Expected evidence: build exit code, Screenshot reference, Busy/Idle observations, unchanged foreground window, and updated T06 handoff.

## Affected files

- Modify: `tasks/prd-provider-copilot/task_06.md` - record the manual evidence and completion or pending state.
- Create: None.
- Do not modify: `tasks/prd-provider-copilot/codereview_001/codereview.md` and product source.

## Observability and recovery

- Operational signal: App build output, Windows MCP Screenshot, observed status/activity transitions, foreground-window observation, and the T06 handoff.
- Recovery: If the App, approved session, or Windows MCP route is unavailable, stop the manual sequence, preserve the limitation, and return the task as pending. Do not substitute static inspection, fabricated activity, or desktop E2E.

## Handoff

> Updated by `sdd-execute-task` during implementation.

- Produced result: Pending execution.
- Changed files: `tasks/prd-provider-copilot/task_07.md` handoff only; no product source or review report changed.
- Checks: `rtk dotnet build src/TokenHound.App/TokenHound.App.csproj --no-restore --nologo --verbosity:minimal` passed with exit 0, 4 projects, 0 errors, and 3 pre-existing NU1903 warnings. Windows MCP App/Screenshot checks were not executed because those tools are not exposed in this session; no desktop E2E was run.
- Validated state: The current App code compiles. T07.2 Screenshot `display: [2]`, T07.3 approved-session Busy/Idle activity, and T07.4 foreground click/drag checks remain unverified.
- Open items: CR-01 remains open. The missing Windows MCP App/Screenshot route and approved-session manual environment prevent T07 completion and keep T06.5 through T06.7 pending.
