# Implementation plan - Polling Cadence and Rate-Limit Retry Configuration in Settings

## Stable sources

- PRD: `tasks/prd-settings-02-cadence-and-retries/prd.md` (read 2026-09-08)
- TechSpec: `tasks/prd-settings-02-cadence-and-retries/techspec.md` (read 2026-09-08)

No prior `tasks.md`, `task_*.md`, or `done/task_*.md` exists for this slice. Task IDs begin at T01.

## Dependency graph

| ID | Delivery | Depends on | Unblocks |
| --- | --- | --- | --- |
| T01 | Configurable, safe HTTP 429 retry floor in Core | - | T04 |
| T02 | Live cadence reconfiguration that preserves in-flight work | - | T04 |
| T03 | Atomic, non-destructive Refresh and RateLimit persistence | P-01 | T04 |
| T04 | Validated cadence settings application flow | T01, T02, T03 | T05 |
| T05 | Accessible Settings UI, startup composition, and desktop acceptance | T04 | - |

`T01` and `T02` are deliberately separate foundations: each changes an independent runtime boundary, carries its own regression tests, and is required by the application flow. `T03` is blocked only by P-01; the remaining tasks stay planned but cannot be executed until its output exists.

## Traceability matrix

| Source ID | Source section | Obligation | Tasks | Evidence or test |
| --- | --- | --- | --- | --- |
| OBJ-01, OBJ-02 | `prd.md#outcomes-and-metrics` | Expose, accept, and persist cadence and retry-floor configuration | T03, T04, T05 | TC-01, TC-02, TC-05 to TC-09; MAN-01 |
| OBJ-03, FR-02 to FR-06 | `prd.md#functional-requirements` | Enforce 30s/60s floors and `Idle >= Active` while editing | T01, T04, T05 | TC-03, TC-05, TC-06; MAN-01 |
| OBJ-04, FR-08, NFR-04 | `prd.md#outcomes-and-metrics`, `prd.md#non-functional-requirements` | Update only Refresh and RateLimit safely, preserving unrelated configuration | T03 | TC-01, TC-02, fault-injection persistence test |
| OBJ-05, FR-09, FR-10, NFR-05 | `prd.md#functional-requirements` | Apply cadence and retry floor live without breaking active work or deadlines | T01, T02, T04, T05 | TC-03, TC-04; MAN-02 |
| OBJ-06, FR-07 | `prd.md#functional-requirements` | Restore 180s, 300s, and 60s defaults in the form | T04, T05 | TC-07; MAN-01 |
| FR-11 | `prd.md#functional-requirements` | Discard un-applied changes on Cancel, Escape, and close | T04, T05 | TC-08; MAN-03 |
| NFR-01, NFR-02 | `prd.md#non-functional-requirements` | Keep Core pure and make floors inviolable even for invalid external settings | T01, T03, T04 | Core and configuration regression tests |
| NFR-03 | `prd.md#non-functional-requirements` | Provide accessible, scaled, dark-surface settings controls | T05 | MAN-01 at 100%, 150%, and 200% scaling |
| DEC-01 to DEC-06, CMP-01 to CMP-13 | `techspec.md#technical-decisions`, `techspec.md#components-and-flow` | Implement the specified component seams and test locations | T01 to T05 | Task acceptance and affected-file lists |
| TC-01 to TC-10, MAN-01 to MAN-03 | `techspec.md#test-approach` | Preserve the specified test and manual acceptance matrix | T01 to T05 | Focused MTP commands and manual scripts |

## Tasks

- [T01 - Make the retry floor dynamically safe](task_01.md): Add and prove a thread-safe effective floor without weakening recorded rate-limit deadlines.
- [T02 - Reconfigure polling cadence without interrupting refreshes](task_02.md): Safely replace the timer schedule and idle threshold in the running usage store.
- [T03 - Persist cadence and retry settings atomically](task_03.md): Implement the approved lossless persistence strategy for both configuration sections.
- [T04 - Apply validated cadence settings through the view model](task_04.md): Deliver headlessly tested validation, defaults, apply, and discard behavior.
- [T05 - Integrate the Settings dialog and startup composition](task_05.md): Bind the flow to the desktop UI and complete manual desktop acceptance.

## Coverage gate

- Coverage: Conditional pass. Every source obligation is mapped; FR-08/NFR-04 implementation is blocked by P-01 rather than weakened.
- Traceability: Pass. PRD objectives, FRs, NFRs, TechSpec decisions/components, test cases, and manual scripts are represented above.
- Dependencies: Pass. The graph is acyclic; T01 and T02 may proceed in parallel. T03 is the only blocked node.
- Atomicity: Conditional pass. P-01 must correct DEC-03 before T03 starts; the current cited `HudPositionStore` uses `File.WriteAllText` after JSON DOM serialization, which is neither atomic nor comment/format preserving.
- Executability: Conditional pass. T01 and T02 have executable focused MTP checks. T03 to T05 require P-01, then use the recorded focused MTP commands and Windows desktop script.
- Validation profile: Pass. Core and Infrastructure use MTP/xUnit unit and integration tests. E2E is omitted by the desktop .NET profile; MAN-01 to MAN-03 are required on Windows with an interactive desktop and the existing local provider authorization/configuration.
- Idempotency: Pass. Each task changes an owned contract once, and persistence tests use isolated temporary configuration files. Re-running Reset, Apply, and Save must produce the same resolved settings without duplicating JSON properties.

## Assumptions and open items

- Assumption: The already-present SettingsWindow and SettingsViewModel are the completed host from `settings-01-provider-management`; its task folder is absent, but the source files and composition path exist.
- P-01 (TechSpec owner): Amend or replace DEC-03 before T03. `System.Text.Json.Nodes` drops comments and normalizes formatting, while `File.WriteAllText` is not atomic; neither can satisfy FR-08 and NFR-04 as written. The approved replacement must specify a lossless section-update strategy and atomic replacement behavior, including the first-write case and recovery on failure. A custom text-preserving writer or an approved dependency are materially different technical choices.
- Required environment: T01/T02/T04 use .NET SDK 10.0.400 and existing MTP projects. T05 manual verification requires Windows 11 interactive desktop, TokenHound executable, `appsettings.json`, and existing provider credentials; no credential writes are authorized.

## State

- [ ] T01 - pending
- [ ] T02 - pending
- [ ] T03 - blocked by P-01
- [ ] T04 - pending, depends on T03
- [ ] T05 - pending, depends on T04

## Problems and solutions

- DEC-03 cites `HudPositionStore` as a format/comment-preserving atomic pattern. Source inspection shows that it serializes a `JsonObject` with `WriteIndented = true` and writes it directly with `File.WriteAllText`. Preserve the acceptance contract by resolving P-01; do not copy this behavior as a workaround.
- DEC-04 claims `UsageStoreLifetime.StartTimer` drains its previous loop. Source inspection shows it cancels and disposes the prior timer token without awaiting the old loop. T02 must repair this lifecycle contract so reconfiguration does not cancel an in-flight provider query.
