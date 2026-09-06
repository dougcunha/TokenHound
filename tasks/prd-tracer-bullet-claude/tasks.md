# Implementation plan — Tracer Bullet: Minimal Notch HUD and Claude Code Provider

## Stable sources

- PRD: [tasks/prd-tracer-bullet-claude/prd.md](file:///D:/MyProjects/Ideas/TokenHound/tasks/prd-tracer-bullet-claude/prd.md)
- TechSpec: [tasks/prd-tracer-bullet-claude/techspec.md](file:///D:/MyProjects/Ideas/TokenHound/tasks/prd-tracer-bullet-claude/techspec.md)

## Dependency graph

| ID | Delivery | Depends on | Unblocks |
| --- | --- | --- | --- |
| T01 | Claude Profile & Credential Discovery | — | T03 |
| T02 | Claude OAuth Client (Network & DTOs) | — | T03 |
| T03 | Claude OAuth Provider Adapter (IUsageProvider) | T01, T02 | T05 |
| T04 | Claude Session Monitor (IActivityMonitor) | — | T05 |
| T05 | Central UsageStore Engine Coordinator | T03, T04 | T08 |
| T06 | Win32 WindowStyles Interop Helper | — | T09 |
| T07 | WPF Visual Controls: ProviderRing & TooltipCard | — | T09 |
| T08 | NotchViewModel & Mock Fallback Wiring | T05 | T09 |
| T09 | Minimal NotchWindow Shell & App Integration | T06, T07, T08 | — |

## Traceability matrix

| Source ID | Source section | Obligation | Tasks | Evidence or test |
| --- | --- | --- | --- | --- |
| FR-01 | `prd.md#functional-requirements` | Claude credentials discovery | T01 | `TC-01` (`ClaudeProfileDiscoveryTests.cs`) |
| FR-02 | `prd.md#functional-requirements` | Anthropic OAuth usage telemetry query | T02 | `TC-02` (`ClaudeOAuthClientTests.cs`) |
| FR-03 | `prd.md#functional-requirements` | Claude usage to domain Snapshot mapping | T03 | `TC-03` (`ClaudeOAuthProviderTests.cs`) |
| FR-04 | `prd.md#functional-requirements` | Claude session monitor with PID liveness | T04 | `TC-04` (`ClaudeSessionMonitorTests.cs`) |
| FR-05 | `prd.md#functional-requirements` | Central UsageStore polling coordination | T05 | `TC-05` (`UsageStoreTests.cs`) |
| FR-06 | `prd.md#functional-requirements` | Win32 WS_EX_NOACTIVATE styling | T06 | `TC-06` (`WindowStylesTests.cs`) |
| FR-07 | `prd.md#functional-requirements` | Minimal NotchWindow capsule | T09 | `TC-08` (Manual focus & visual check) |
| FR-08 | `prd.md#functional-requirements` | ProviderRing circular indicator | T07 | Visual XAML compilation & rendering |
| FR-09 | `prd.md#functional-requirements` | TooltipCard detailed hover card | T07 | Visual XAML compilation & rendering |
| FR-10 | `prd.md#functional-requirements` | NotchViewModel with Mock fallback | T08 | `TC-07` (`NotchViewModelTests.cs`) |

## Tasks

- [T01 — Claude Profile & Credential Discovery](done/task_01.md): Discovers and parses .credentials.json across profiles.
- [T02 — Claude OAuth Client (Network & DTOs)](done/task_02.md): Queries Anthropic OAuth usage endpoint with required beta headers.
- [T03 — Claude OAuth Provider Adapter](done/task_03.md): Implements IUsageProvider mapping 5h/weekly quotas into domain Snapshots.
- [T04 — Claude Session Monitor](done/task_04.md): Implements IActivityMonitor verifying session files and PID liveness.
- [T05 — Central UsageStore Engine Coordinator](done/task_05.md): Manages polling schedules, registered providers, and change events.
- [T06 — Win32 WindowStyles Interop Helper](done/task_06.md): Applies WS_EX_NOACTIVATE and WS_EX_TOOLWINDOW styles.
- [T07 — WPF Visual Controls: ProviderRing & TooltipCard](done/task_07.md): Implements vector arc ring and hover details card.
- [T08 — NotchViewModel & Mock Fallback Wiring](done/task_08.md): Binds provider state to UI with automatic Mock fallback.
- [T09 — Minimal NotchWindow Shell & App Integration](done/task_09.md): Assembles top-center floating capsule and launches application.

## Coverage gate

- Coverage: Pass — All functional requirements (FR-01 to FR-10) mapped to execution tasks.
- Traceability: Pass — Full trace from PRD obligations to technical components and test cases.
- Dependencies: Pass — Acyclic DAG; independent tasks (T01, T02, T04, T06, T07) unblocked from the start.
- Atomicity: Pass — Each task consists of 1 production component + 1 test class (<= 150-200 lines).
- Executability: Pass — Non-UI components covered by automated MTP tests; UI shell covered by manual acceptance script.
- Validation profile: Pass — E2E omitted by .NET desktop policy; verified with MTP unit/integration tests and manual check.
- Idempotency: Pass — Clean creation of independent files without shared mutable state.

## Assumptions and open items

- Assumption: Claude Code token format remains stable with `accessToken` string field.
- Required environment: Windows 11 desktop with .NET 10 SDK.
- Open items: None.

## State

- [x] T01 — Claude Profile & Credential Discovery
- [x] T02 — Claude OAuth Client (Network & DTOs)
- [x] T03 — Claude OAuth Provider Adapter
- [x] T04 — Claude Session Monitor
- [x] T05 — Central UsageStore Engine Coordinator
- [x] T06 — Win32 WindowStyles Interop Helper
- [x] T07 — WPF Visual Controls: ProviderRing & TooltipCard
- [x] T08 — NotchViewModel & Mock Fallback Wiring
- [x] T09 — Minimal NotchWindow Shell & App Integration

## Problems and solutions

- None.
