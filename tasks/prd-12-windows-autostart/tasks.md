# Implementation plan — Start with Windows setting

## Stable sources

- PRD: `tasks/prd-12-windows-autostart/prd.md`
- TechSpec: `tasks/prd-12-windows-autostart/techspec.md`

> Common sources come before the task and mutable state; read order does not guarantee a cache hit.

## Dependency graph

| ID | Delivery | Depends on | Unblocks |
| --- | --- | --- | --- |
| T01 | Startup service: reads, creates, and deletes `TokenHound.lnk` and honors the Windows "Startup apps" flag, tested on a temp folder | — | T02 |
| T02 | Settings "General" tab toggle, wired to the service, plus installer compatibility (silent keeps state, interactive pre-selects and removes, uninstall cleans up) | T01 | — |

## Traceability matrix

| Source ID | Source section | Obligation | Tasks | Evidence or test |
| --- | --- | --- | --- | --- |
| FR-01 | `prd.md#functional-requirements` | Entry is `<Startup>\TokenHound.lnk`, the installer's file | T01, T02 | TC-02, TC-03; T02 keeps the `.iss` icon name (TC-10) |
| FR-02 | `prd.md#functional-requirements` | State read from the OS: shortcut exists and is not disabled in Startup apps | T01 | TC-01, TC-02, TC-05; MA-2 |
| FR-03 | `prd.md#functional-requirements` | Enable writes or overwrites the shortcut to the running exe and clears the disabled flag | T01 | TC-03, TC-04, TC-05 |
| FR-04 | `prd.md#functional-requirements` | Disable deletes the shortcut; an absent shortcut is fine | T01 | TC-06 |
| FR-05 | `prd.md#functional-requirements` | General tab, first, with the Apply pattern | T02 | TC-07, TC-08; MA-1 |
| FR-06 | `prd.md#functional-requirements` | A failure shows an inline error and keeps the real state | T02 | TC-09 |
| FR-07 | `prd.md#functional-requirements` | Silent setup keeps the shortcut as it was | T02 | TC-10 / MA-3 |
| FR-08 | `prd.md#functional-requirements` | Interactive setup pre-selects from state; unticked removes | T02 | TC-10 / MA-4 |
| FR-09 | `prd.md#functional-requirements` | Uninstall removes the shortcut | T02 | TC-10 / MA-4 |
| NFR-01 | `prd.md#non-functional-requirements` | Core stays pure; seams; tests never touch the real profile | T01 | QA-04 grep; TC-02..06 use a temp folder and a fake |
| NFR-02 | `prd.md#non-functional-requirements` | Per-user only | T01 | Review: `SpecialFolder.Startup` and `HKCU` only |
| NFR-03 | `prd.md#non-functional-requirements` | Under 200 ms, no visible stall | T01, T02 | DEC-11; MA-1 |
| NFR-04 | `prd.md#non-functional-requirements` | Automation name and help text | T02 | TC-11 |
| US-01..US-03 | `prd.md#stories-and-journeys` | Journeys | T01, T02 | MA-1..MA-4 |
| DEC-01..DEC-05, DEC-11 | `techspec.md#technical-decisions` | Service design | T01 | TC-01..TC-06 |
| DEC-06, DEC-07, DEC-12 | `techspec.md#technical-decisions` | ViewModel, tab, and mode parity | T02 | TC-07..TC-09, TC-11 |
| DEC-08..DEC-10 | `techspec.md#technical-decisions` | Installer `[Code]`, `Check`, `[UninstallDelete]` | T02 | TC-10 |
| TC-01..TC-06 | `techspec.md#test-approach` | Policy and service tests | T01 | Core.Tests, Infrastructure.Tests |
| TC-07..TC-11 | `techspec.md#test-approach` | ViewModel tests, installer compile, manual scripts | T02 | Infrastructure.Tests, `build-installer.ps1`, MA-1..MA-4 |

## Tasks

- [T01 — Startup shortcut service](done/task_01.md): Core decodes the "Startup apps" flag, and Infrastructure reads, writes, and deletes `TokenHound.lnk` through seams, with tests.
- [T02 — Settings toggle and installer compatibility](done/task_02.md): the General tab toggle drives the service, and the installer respects the user's choice on update, interactive setup, and uninstall.

## Coverage gate

- Coverage: pass. Every FR, NFR, US, DEC, and TC is mapped. The installer and Windows-flag behavior is manual (TC-10, TC-11) by nature.
- Traceability: pass.
- Dependencies: pass. It is a linear DAG, T01 → T02.
- Atomicity: pass. T01 is one module (Core policy plus Infrastructure/Startup). T02 is the vertical UI slice plus installer, about 6 files.
- Executability: pass. The commands come from `AGENTS.md`, and ISCC is present locally.
- Validation profile: pass. E2E is omitted by .NET desktop policy. The COM tests need Windows (local, CI `windows-latest`).
- Idempotency: pass. Enable and Disable are idempotent, and the tests use unique temp folders.

## Assumptions and open items

- Assumption: the `StartupApproved` odd-first-byte encoding (PRD Assumptions). It is checked by MA-2.
- Open item: none.
- Required environment: Windows with Inno Setup 6 (present at `%LOCALAPPDATA%\Programs\Inno Setup 6\ISCC.exe`) for TC-10. The human signs off MA-1..MA-4 at the visual check.

## State

- [x] T01 — done
- [x] T02 — done. MA-1..MA-4 passed at the visual check (2026-09-29, human text "Tudo OK"; see workflow.md Events).

## Problems and solutions

- None.
