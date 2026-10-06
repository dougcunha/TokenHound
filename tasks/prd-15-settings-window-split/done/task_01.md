# Stable execution context

Load in this exact order:

1. `tasks/prd-15-settings-window-split/prd.md`
2. `tasks/prd-15-settings-window-split/techspec.md`
3. This file

Use the current PRD and TechSpec versions already loaded; recover only missing or changed sources. This order does not guarantee a host cache hit.

---

# T01 — Record text baselines

## Outcome

`tasks/prd-15-settings-window-split/baseline/` holds, taken from `git_base` `90d6748`, the sorted unique `x:Key` set (QA-03), the sorted `AutomationProperties.Name` and `.HelpText` multiset (QA-04), and the line counts of `SettingsWindow.xaml` and `DialogResources.xaml`. The handoff states whether a screen capture can be saved to a file.

## Dependencies and boundaries

- Depends on: —
- Unblocks: T02, T03
- In scope: baseline text files; a one-shot capture-to-file probe through Windows MCP `PowerShell`.
- Out of scope: any change under `src/`; storing screenshots as acceptance evidence (DEC-07 compares builds in the same session).

## Traceability

| Source | Section | Obligation covered |
| --- | --- | --- |
| QA-03, QA-04, QA-01, QA-02 | `techspec.md#quality-profile` | Baselines the later diffs compare against |
| DEC-07 | `techspec.md#technical-decisions` | Text baselines only; probe for durable capture |
| Step 1 | `techspec.md#dependency-sequencing` | Baseline before any change |

## Context to recover on demand

- Applicable skills and rules: `repository-cli-efficiency`
- Existing code: `src/TokenHound.App/UI/Windows/SettingsWindow.xaml`, `src/TokenHound.App/UI/Styles/DialogResources.xaml`, `src/TokenHound.App/App.xaml`

## Work

- [x] T01.1 Read the files from `git_base` with `git show 90d6748:<path>`, not the working tree, so a rerun after later tasks still yields the base.
- [x] T01.2 Write `baseline/keys.txt`: `x:Key` values from `UI/Styles/DialogResources.xaml` and `UI/Windows/SettingsWindow.xaml`, sorted unique, one per line.
- [x] T01.3 Write `baseline/automation-names.txt`: `AutomationProperties.Name` and `AutomationProperties.HelpText` entries (property and value) from `SettingsWindow.xaml`, sorted with duplicates kept; confirm 52 lines (41 Name + 11 HelpText).
- [x] T01.4 Write `baseline/line-counts.txt` (1051 and 561 expected) and `baseline/README.md` with the exact commands used, so T06 reruns them over the new file set.
- [x] T01.5 Probe: with Windows MCP `PowerShell`, try saving a PNG of the primary display to the scratchpad; record the result in the handoff and do not add it to the repository.

## Acceptance criteria

- The three baseline files exist and match the counts above; `README.md` lists the commands.
- The probe result (works or not, with the error) is in the handoff.

## Verification

- Unit: none (no code change).
- Integration: none.
- E2E: omitted by .NET desktop policy.
- Manual: none.
- Commands: `git show 90d6748:src/TokenHound.App/UI/Windows/SettingsWindow.xaml | rg -o 'AutomationProperties.(Name|HelpText)="[^"]+"' | sort | wc -l` → 52.
- Environment dependency: Windows MCP for the probe only.
- Expected evidence: `baseline/*` files and the handoff.

## Affected files

- Create: `tasks/prd-15-settings-window-split/baseline/keys.txt`, `automation-names.txt`, `line-counts.txt`, `README.md`

## Observability and recovery

- Operational signal: none.
- Recovery: delete `baseline/` and rerun; it is derived from `git_base`.

## Handoff

- Produced result: text baselines from `git_base` `90d6748` under `baseline/`: `keys.txt` (66 unique keys: 60 dialog + 6 Settings), `automation-names.txt` (52 entries: 41 Name + 11 HelpText), `line-counts.txt` (SettingsWindow.xaml 1051, DialogResources.xaml 561), and `README.md` with the generate and compare commands (QA-01..QA-05, QA-07).
- Changed files: `tasks/prd-15-settings-window-split/baseline/keys.txt`, `automation-names.txt`, `line-counts.txt`, `README.md` (new). No change under `src/`.
- Checks: the README compare commands, run on the unchanged tree, print `QA-03 identical`, `QA-04 identical`, `QA-05 none`, `QA-07 unchanged`. Capture probe (T01.5): Windows MCP `PowerShell` with `System.Drawing` `CopyFromScreen` saved a PNG (133 KB) of real desktop content, but the process is DPI-unaware, so `PrimaryScreen.Bounds` reported 1280x720 and the capture held only a top-left logical crop. A full capture needs DPI awareness (`SetProcessDPIAware`) and virtual-screen bounds. The probe file showed private browser content and was deleted. Per DEC-07, captures stay secondary evidence; T06 compares base and head in the same session.
- Validated state: HEAD `90d6748`; worktree has only pre-existing changes plus the feature folder.
- Open items: none.

### ADR candidates

None - direct TechSpec implementation or local decision.
