# TokenHound Implementation Roadmap

This document establishes the delivery sequence, PRD boundaries, and parallelization strategy for TokenHound on Windows 11 (.NET 10).

---

## Strategy: Tracer Bullet & Parallel Provider Swarms

Implementation follows an initial vertical validation spike ("Tracer Bullet") followed by decoupled autonomous provider swarms executed in parallel Git worktrees.

```text
Phase 1: Foundation & Tracer Bullet (Sequential)
├── [PRD 01] tasks/prd-core-foundation/
│        ├── Pure domain models (Snapshot, LimitWindow, UsageBlock, ProviderStatus)
│        ├── Domain contracts (IUsageProvider, IActivityMonitor, ICredentialStore)
│        ├── Resilience policies (BackoffCalculator, RateLimitPolicy, RefreshSchedulePolicy)
│        ├── Infrastructure primitives (SafeSqliteReader, WindowsCredentialManager, ProcessLiveness, SharedFileReader)
│        └── MockUsageProvider & Static Test Fixtures
│
└── [PRD 02] tasks/prd-tracer-bullet-claude/
         ├── Minimal WPF HUD Notch window (AllowsTransparency, WS_EX_NOACTIVATE, top-screen pill)
         ├── Basic ProviderRing & TooltipCard controls
         ├── Real Claude Code provider integration (% 5h, % weekly, sessions/ PID liveness)
         └── End-to-end pipeline verification with fallback to MockUsageProvider

Phase 2: Parallel Provider Swarms (Independent Git Worktrees)
├── [PRD 03] tasks/prd-provider-codex/          # OpenAI Codex (stdio JSON-RPC codex app-server + rollout logs)
├── [PRD 04] tasks/prd-provider-glm/            # Z.ai GLM Coding Plan (config key discovery + multi-cluster REST)
├── [PRD 05] tasks/prd-provider-cursor/         # Cursor (SafeSqliteReader on state.vscdb + WorkOS cookie)
├── [PRD 06] tasks/prd-provider-antigravity/    # Antigravity / Gemini (CredReadW + ephemeral TCP port + gRPC-Web)
├── [PRD 07] tasks/prd-provider-perplexity/     # Perplexity (WebView2 isolated profile + unidirectional counters)
└── [PRD 08] tasks/prd-provider-copilot/        # GitHub Copilot (borrowed gh OAuth token + copilot_internal/user + events.jsonl heuristic)

Phase 3: Presentation Polish & Settings
├── [PRD 09] tasks/prd-settings-01-provider-management/   # [Completed] Per-provider enablement toggles, status badges & engine gating
├── [PRD 10] tasks/prd-settings-02-cadence-and-retries/     # [Ready] Active/idle polling cadence & safe 429 retry floor configuration
├── [PRD 11] tasks/prd-system-tray-notifyicon/              # [Planned] System tray NotifyIcon, context menu, show/hide notch & 'Refresh Now'
├── [PRD 12] tasks/prd-hud-visual-polish/                   # [Planned] Bézier capsule geometry (NotchGeometry), inverse curves & DWM Mica styling
└── [PRD 13] tasks/prd-hud-placement-multimonitor/          # [Planned] ScreenEdgeDetector, multi-monitor docking & position persistence
```

---

## Phase 3 PRD Track Details

### [PRD 10] Polling Cadence and Rate-Limit Retry Configuration (`tasks/prd-settings-02-cadence-and-retries/`)
- **Status**: Fully specified (PRD, TechSpec, microtasks T01–T05).
- **Scope**:
  - Live reconfiguration of active (default 180s, min 30s) and idle (default 300s, min 30s) polling timers in `UsageStore`.
  - Configurable HTTP 429 retry floor in `RateLimitPolicy` with inviolable 60-second safety clamp.
  - Non-destructive atomic persistence to `appsettings.json` under `"Refresh"` and `"RateLimit"` sections.
  - Inline input validation, reset to defaults, and settings dialog integration.

### [PRD 11] System Tray Integration (`tasks/prd-system-tray-notifyicon/`)
- **Status**: Complete.
- **Scope**:
  - Native Windows taskbar notification area (`NotifyIcon`) integration using `Hardcodet.NotifyIcon.Wpf` or Win32 Shell_NotifyIcon.
  - Context menu actions: "Show/Hide Notch", "Refresh Now" (triggers `UsageStore.RefreshNowAsync`), "Settings...", "About...", and "Exit".
  - Double-click or left-click action to toggle HUD Notch visibility.
  - Background lifecycle support: application stays alive in the tray when the Notch window is closed or hidden (`ShutdownMode.OnExplicitShutdown`).

### [PRD 12] HUD Bézier Geometry & Visual Polish (`tasks/prd-hud-visual-polish/`)
- **Status**: Planned.
- **Scope**:
  - Smooth Bézier capsule geometry (`NotchGeometry`) with inverse rounded corners anchored to the screen edge (matching `docs/design/2026-08-28-usage-notch-design.md`).
  - Windows 11 DWM backdrop effects (Mica / Acrylic) via `DwmSetWindowAttribute`.
  - Non-rectangular click-through hit-testing (`WM_NCHITTEST` returning `HTTRANSPARENT` outside the capsule outline).
  - Micro-animations for provider consumption rings (smooth arc progress interpolation, pulse on active session execution).

### [PRD 13] Multi-Monitor & Edge Docking (`tasks/prd-hud-placement-multimonitor/`)
- **Status**: Planned.
- **Scope**:
  - Multi-monitor detection (`ScreenEdgeDetector` / Win32 `EnumDisplayMonitors`).
  - Screen edge docking options (Top Center, Top Right, Screen Right vertical pill).
  - Monitor DPI awareness and multi-monitor coordinate clamping.
  - Position and preferred display persistence in `appsettings.json` via `HudPositionStore`.

---

## SDD Execution Standards for Autonomous Agents

1. **Microtask Sizing**: Each execution task (`task_*.md`) must strictly encompass **1 production class + 1 test class** (<= 150-200 lines added).
2. **TDD Validation**: Every task must be verified with `rtk dotnet test --project <path> --no-build --no-restore -- --minimum-expected-tests 1`.
3. **Workspace Isolation**: Concurrently executing provider agents must use dedicated Git worktrees (`git worktree add ../TokenHound-worktree-<provider> feat/provider-<provider>`) to avoid MSBuild file locks on Windows.
4. **Code Review Gate**: Each PRD must be audited against its requirements via `sdd-review-code` before merging into `main`.
