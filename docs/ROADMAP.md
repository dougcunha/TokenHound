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

Phase 3: Presentation Polish & Settings (Final)
└── [PRD 09] tasks/prd-hud-polish-settings/
         ├── Bézier capsule geometry (NotchGeometry) and Windows 11 DWM Mica/Acrylic styling
         ├── Screen edge and multi-monitor detection (ScreenEdgeDetector)
         ├── System Tray integration (NotifyIcon) with context menu and 'Refresh Now'
         └── Settings window (SettingsWindow) for provider toggle and HUD position
```

---

## SDD Execution Standards for Autonomous Agents

1. **Microtask Sizing**: Each execution task (`task_*.md`) must strictly encompass **1 production class + 1 test class** (<= 150-200 lines added).
2. **TDD Validation**: Every task must be verified with `rtk dotnet test --project <path> --no-build --no-restore -- --minimum-expected-tests 1`.
3. **Workspace Isolation**: Concurrently executing provider agents must use dedicated Git worktrees (`git worktree add ../TokenHound-worktree-<provider> feat/provider-<provider>`) to avoid MSBuild file locks on Windows.
4. **Code Review Gate**: Each PRD must be audited against its requirements via `sdd-review-code` before merging into `main`.
