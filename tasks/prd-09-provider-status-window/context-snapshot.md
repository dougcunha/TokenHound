# Context snapshot — 09-provider-status-window

> Hints for the next session, not an authority: artifacts, manifests, handoffs, reports, and code win on conflict. Protocol: `.agents/skills/sdd-snapshot/SKILL.md`.

## Header

- status: closed
- generated: 2026-09-27
- stage: acceptance
- stage_source: codereview_5/
- covers_through: codereview_5/codereview.md
- authored_code: no
- git_head: 53da181
- worktree: changed: src/TokenHound.App (T01+T02+T04..T06), tests/TokenHound.Infrastructure.Tests (T01..T04), .agents/skills (pre-existing), tasks/
- next_step: — (feature completed; HIL 3 accepted in workflow DEC-14)
- other_eligible: —
- superseded_by: —

## Load map

| Tier | Load when |
| --- | --- |
| `now` | Session start: header, next step brief, open threads waiting on the user |
| `on-select` | Chosen unit matches a trigger: task or finding ID, affected file, or traceability ID |
| `on-edit` | About to edit or create a path matching a trigger |
| `on-run` | About to run a matching command, or it just failed |
| `on-demand` | A gist is not enough: follow its `src:` pointer, that section only |

Review sessions load only the header, next step brief, `Open threads`, and `on-run` entries.

Entry shape: `- [ID] (when: tier: trigger; trigger) gist — src: path#section; until: condition`

## Next step brief

- Why next: `codereview_5` is `APPROVED WITH RESERVATIONS`. Its only finding, CR-01, was already accepted in DEC-09, so the reservations HIL reuses that decision (workflow REV-05). What remains is HIL 3.
- Read first: checkpoint.json, workflow.md#REV-05, codereview_5/codereview.md (Summary, Limitations), jev-summary.md, techspec.md MA-01..MA-06.
- On acceptance: mark the checkpoint `completed` and this snapshot `closed`. No commit or push is authorized (DEC-04).
- Applicable entries: O-01, O-02.

## Decisions

- [D-01] (when: on-select: FR-07; FR-10; techspec) Percent is USED, not remaining, even though the reference image shows remaining; exhausted = used >= 1.0 — src: workflow.md#DEC-02; until: DEC-02 replaced

## Learnings

- [L-02] (when: on-edit: src/TokenHound.App/ViewModels/**; on-run: dotnet test) Infrastructure.Tests is net10.0 and compiles App VMs via `<Compile Include>` links; a new VM file not linked is untested, and any WPF type in it breaks the test build — src: techspec.md#test-approach; until: feature completed

- [L-03] (when: on-edit: tasks/**; src/**; tests/**) Python text-mode writes on Windows emit CRLF; the repo commits LF. Write with `newline=''` or bytes. `ProviderUsageRowFactoryClineTests.cs` already had CRLF working-copy endings (index LF) and was left as is — src: tasks.md#problems-and-solutions; until: feature completed

- [L-04] (when: on-run: dotnet build) A plain `rtk dotnet build` right after another session's build is up to date and prints no warnings; use `--no-incremental` when the warning count is evidence — src: codereview_4/codereview.md#executed-validations; until: feature completed

## Code map


## Open threads

- [O-01] (when: now; acceptance) HIL 3 manual items: MA-05 (exhausted account visual) pending; MA-06 150% scaling and >= 10 rows not verified; T05 Windows 10 fallback not verified; T06 horizontal scrollbar and Settings `Cadence & Rate Limits` tab not exercised. The T04 alert status and MA-08 were seen on screen (REC-07) — src: codereview_5/codereview.md#limitations-and-open-items; until: HIL 3 decided

- [O-02] (when: now; acceptance; codereview_4/CR-01) CR-01 (DEC-06 earliest reset; fraction-less block not dimmed) accepted as an open item in DEC-09; not to be asked again — src: workflow.md#DEC-09; until: HIL 3 decided
