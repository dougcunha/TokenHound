# Implementation plan — Auto-update from GitHub Releases

## Stable sources

- PRD: `tasks/prd-11-auto-update/prd.md`
- TechSpec: `tasks/prd-11-auto-update/techspec.md`

> Common sources come before the task and mutable state; read order does not guarantee a cache hit.

## Dependency graph

| ID | Delivery | Depends on | Unblocks |
| --- | --- | --- | --- |
| T01 | Core version/release model and update policies with tests | — | T03, T04 |
| T02 | `Update` settings, `update-state.json`, persisted GitHub rate-limit gate | — | T03, T09 |
| T03 | GitHub release client and single-flight check service | T01, T02 | T06, T07 |
| T04 | Verified download and install-mode/writability detection | T01 | T08 |
| T05 | Instance mutex, portable swap with rollback, installer launcher, Inno script + marker | — | T08 |
| T06 | Tray "Check for Updates…" + update dialog (check, Later, Skip) | T03 | T07, T08 |
| T07 | Periodic scheduler + tray balloon → dialog | T03, T06 | — |
| T08 | Update now end-to-end: download, apply, shutdown, `--updated` startup cleanup | T04, T05, T06 | — |
| T09 | "Updates" settings tab (enable, interval) | T02 | — |

Suggested order: T01 → T02 → T03 → T04 → T05 → T06 → T07 → T08 → T09.

## Traceability matrix

| Source ID | Source section | Obligation | Tasks | Evidence or test |
| --- | --- | --- | --- | --- |
| OBJ-01 | `prd.md#outcomes-and-metrics` | Learn about newer release in-app | T06, T07 | MA-1, MA-4 |
| OBJ-02 | `prd.md#outcomes-and-metrics` | One confirmation, restart on new version | T08 | MA-2, MA-3 |
| OBJ-03 | `prd.md#outcomes-and-metrics` | No call while deadline in force | T02, T03 | TC-08, TC-11 |
| US-01 | `prd.md#stories-and-journeys` | Told when new version exists | T07 | TC-17, MA-4 |
| US-02 | `prd.md#stories-and-journeys` | Check now from tray | T06 | TC-20, MA-1 |
| US-03 | `prd.md#stories-and-journeys` | Portable apply | T05, T08 | TC-14, TC-18, MA-2 |
| US-04 | `prd.md#stories-and-journeys` | Installed apply | T05, T08 | TC-15, TC-18, MA-3 |
| US-05 | `prd.md#stories-and-journeys` | Skip version / turn off checks | T01, T06, T07, T09 | TC-03, TC-04, TC-18, TC-19 |
| FR-01 | `prd.md#functional-requirements` | Query latest, compare with running version | T01, T03 | TC-01..03, TC-09 |
| FR-02 | `prd.md#functional-requirements` | Skip prereleases and drafts | T01, T03 | TC-03, TC-09 |
| FR-03 | `prd.md#functional-requirements` | Configurable periodic check | T01, T02, T07, T09 | TC-04, TC-07, TC-17, TC-19 |
| FR-04 | `prd.md#functional-requirements` | Tray check always shows a result | T06 | TC-18, TC-20, MA-1 |
| FR-05 | `prd.md#functional-requirements` | Notify + Update now / Later / Skip | T01, T06, T07 | TC-03, TC-18, TC-21, MA-4 |
| FR-06 | `prd.md#functional-requirements` | Marker-based install mode | T04, T05 | TC-13, MA-3 |
| FR-07 | `prd.md#functional-requirements` | Asset per mode × arch | T01, T08 | TC-05, TC-18 |
| FR-08 | `prd.md#functional-requirements` | Temp download + integrity | T01, T04, T08 | TC-06, TC-12, TC-18 |
| FR-09 | `prd.md#functional-requirements` | Portable swap/restart/rollback | T05, T08 | TC-14, TC-18, MA-2 |
| FR-10 | `prd.md#functional-requirements` | Silent installer + restart | T05, T08 | TC-15, TC-18, MA-3 |
| FR-11 | `prd.md#functional-requirements` | Non-writable folder handling | T04, T08 | TC-13, TC-18, MA-2 |
| FR-12 | `prd.md#functional-requirements` | GitHub rate limits under 429 invariant | T02, T03 | TC-08, TC-11 |
| FR-13 | `prd.md#functional-requirements` | Failure shown; no duplicate checks | T03, T06 | TC-10, TC-18 |
| NFR-01 | `prd.md#non-functional-requirements` | Policies pure in Core | T01 | Core project has no UI/OS refs (review) |
| NFR-02 | `prd.md#non-functional-requirements` | HTTPS, repo URLs, verify, no token | T03, T04 | TC-09, TC-12 |
| NFR-03 | `prd.md#non-functional-requirements` | Network budget | T02, T03, T07 | TC-11, TC-17 |
| NFR-04 | `prd.md#non-functional-requirements` | Off UI thread, cancellable | T03, T04, T07, T08 | TC-12, TC-17, TC-18 |
| NFR-05 | `prd.md#non-functional-requirements` | x64/arm64, per-user, no elevation | T01, T05 | TC-05, MA-3 |
| NFR-06 | `prd.md#non-functional-requirements` | JSON via `System.Text.Json` | T02, T09 | TC-07, TC-19 |
| UX-1..UX-3 | `prd.md#user-experience` | Tray entry, prompt, specific messages | T06, T08 | TC-18, TC-20, MA-1, MA-2 |
| UX-4 | `prd.md#user-experience` | Interval editable in settings | T09 | TC-19 |
| CON-3 | `prd.md#constraints-and-dependencies` | Installer installs marker | T05 | ISCC build, MA-3 |
| DEC-01..DEC-02 | `techspec.md#technical-decisions` | Version source and comparison | T01, T03 | TC-01, TC-02 |
| DEC-03 | `techspec.md#technical-decisions` | Settings + schedule | T02, T07, T09 | TC-07, TC-17 |
| DEC-04 | `techspec.md#technical-decisions` | Update rate-limit gate | T02, T03 | TC-08 |
| DEC-05 | `techspec.md#technical-decisions` | Marker file | T04, T05 | TC-13 |
| DEC-06 | `techspec.md#technical-decisions` | Asset selection | T01, T08 | TC-05 |
| DEC-07 | `techspec.md#technical-decisions` | Integrity rules | T04 | TC-12 |
| DEC-08 | `techspec.md#technical-decisions` | Portable swap mechanism | T05, T08 | TC-14, MA-2 |
| DEC-09 | `techspec.md#technical-decisions` | Installer args + `[Code]` | T05, T08 | TC-15, MA-3 |
| DEC-10 | `techspec.md#technical-decisions` | Instance mutex | T05, T08 | TC-16 |
| DEC-11 | `techspec.md#technical-decisions` | Writability probe | T04, T08 | TC-13 |
| DEC-12 | `techspec.md#technical-decisions` | Single dialog + balloon | T06, T07 | TC-18, TC-21 |
| DEC-13 | `techspec.md#technical-decisions` | Partial App file, separate settings VM | T06, T09 | Quality profile QA-06 |
| TC-01..TC-06 | `techspec.md#test-approach` | Core tests | T01 | Core.Tests |
| TC-07, TC-08 | `techspec.md#test-approach` | Settings and gate tests | T02 | Infrastructure.Tests |
| TC-09..TC-11 | `techspec.md#test-approach` | Client and service tests | T03 | Infrastructure.Tests |
| TC-12, TC-13 | `techspec.md#test-approach` | Downloader and detector tests | T04 | Infrastructure.Tests |
| TC-14..TC-16 | `techspec.md#test-approach` | Applier, launcher, mutex tests | T05 | Infrastructure.Tests |
| TC-17, TC-21 | `techspec.md#test-approach` | Scheduler, tray notification | T07 | Infrastructure.Tests |
| TC-18 | `techspec.md#test-approach` | Update view model | T06, T08 | Infrastructure.Tests (linked) |
| TC-19 | `techspec.md#test-approach` | Settings view model | T09 | Infrastructure.Tests (linked) |
| TC-20 | `techspec.md#test-approach` | Tray entry | T06 | Infrastructure.Tests |
| MA-1..MA-4 | `techspec.md#test-approach` | Manual acceptance | T06, T07, T08 | Visual check + HIL 3 |

## Tasks

- [T01 — Core update domain](done/task_01.md): versions, release model, update and asset policies, digest parsing.
- [T02 — Update settings, state, and rate-limit gate](done/task_02.md): `Update` section, `update-state.json`, persisted GitHub gate.
- [T03 — GitHub release client and check service](done/task_03.md): `releases/latest`, policy evaluation, single-flight checks.
- [T04 — Verified download and install-mode detection](done/task_04.md): size/digest-verified download, marker and writability.
- [T05 — Apply mechanisms and installer script](done/task_05.md): mutex, portable swap with rollback, silent setup launch, Inno changes.
- [T06 — Manual check from the tray](done/task_06.md): tray entry and update dialog with check, Later, Skip.
- [T07 — Periodic check with balloon](done/task_07.md): scheduler and non-focus notification.
- [T08 — Update now end-to-end](done/task_08.md): download, apply, shutdown, `--updated` cleanup.
- [T09 — Updates settings tab](done/task_09.md): enable and interval editing.

## Coverage gate

- Coverage: pass — every OBJ, US, FR, NFR, UX item, CON-3, DEC, and TC maps to at least one task; CON-1/CON-2/CON-4 are satisfied through FR-01/FR-07/FR-12 and NFR-01/NFR-06 tasks.
- Traceability: pass — each task table cites PRD and TechSpec IDs.
- Dependencies: pass — acyclic; T05 and T02 are independent roots with T01.
- Atomicity: pass — each task delivers one reviewable slice with its tests; T08 is the largest (orchestration only; mechanisms live in T04/T05).
- Executability: pass — commands from `AGENTS.md`; `TokenHound.slnx` exists at the root.
- Validation profile: pass — E2E omitted by .NET desktop policy; unit/integration in Core.Tests and Infrastructure.Tests; manual MA-1..MA-4 at the visual check and HIL 3.
- Idempotency: pass — settings/state writes are atomic; swap-journal cleanup is idempotent; re-running a task's tests has no external effects.

## Assumptions and open items

- Assumption: PRD defaults (prereleases ignored, 24 h interval, size + optional SHA-256) and DEC-08/DEC-09 are confirmed at the merged HIL 1+2.
- Assumption: Inno Setup `CheckForMutexes` and `{param:}` behave as documented; verified by the T05 compile and MA-3.
- Open item: MA-2/MA-3 need a GitHub release newer than the test build; the first real auto-update can only be exercised after the release that ships this feature (owner: human, affects OBJ-02 evidence at HIL 3).
- Required environment: Inno Setup 6 for T05 script compile and MA-3 (FR-06, FR-10) — authorization: local tool use under HIL 2; network to api.github.com/github.com for MA-1..MA-4.

## State

- [x] T01 — done
- [x] T02 — done
- [x] T03 — done
- [x] T04 — done
- [x] T05 — done
- [x] T06 — done
- [x] T07 — done
- [x] T08 — done
- [x] T09 — done

## Problems and solutions

- None.
