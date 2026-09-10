# TokenHound Architecture: Windows 11 on .NET 10 / C#

This document defines the official architecture, technological decisions, UI comparison (WPF vs. WinUI 3), and complete folder structure for **TokenHound**.

---

## 1. Technological Decision: Why WPF (.NET 10) Over WinUI 3?

For a desktop companion functioning as a **screen-edge floating notch / HUD** with non-rectangular geometry and continuous lightweight background operation, our technology choice was driven by technical feasibility and windowing stability on Windows 11.

### Technical Comparison

| Project Requirement | **WPF (.NET 10)** *(Selected)* | **WinUI 3 (Windows App SDK)** |
| :--- | :--- | :--- |
| **Non-Rectangular Transparent Windows** | **Native & Mature Support**: `AllowsTransparency="True"`, `WindowStyle="None"`, `Background="Transparent"`, and Bézier curvature drawing via `PathGeometry`. | **Limitations & Bugs**: `AppWindow` in Windows App SDK suffers from long-standing bugs with per-pixel alpha transparency and resizing ghost borders. |
| **Non-Activating Window (*No-Activate HUD*)** | **Straightforward**: Injection of Win32 styles `WS_EX_NOACTIVATE`, `WS_EX_TOOLWINDOW`, and `WS_EX_TOPMOST` via `HwndSource` so the HUD never steals focus from the editor/terminal. | **Unstable**: Requires complex low-level HWND manipulation; popups and flyouts frequently steal OS focus. |
| **Click-Through Transparency** | **Simple**: Intercepts `WM_NCHITTEST` returning `HTTRANSPARENT` outside the notch capsule outline, allowing clicks to pass directly to underlying windows. | **Complex**: Requires experimental `InputNonClientPointerSource` APIs with limited support for non-rectangular regions. |
| **Idle Memory Footprint** | **Low (~30 to 45 MB)** running continuously in the system tray. | **High (~90 to 160 MB)** due to Windows App SDK runtime overhead and the WinUI 3 compositor. |
| **System Tray Integration** | **Native & Consolidated** via lightweight libraries such as `Hardcodet.NotifyIcon.Wpf`. | **No native support**: Relies on third-party components and community wrappers. |
| **Windows 11 Fluent & DWM Design** | **Integrated in .NET 10**: Native Windows 11 theme support (`SystemTheme="Windows11"`), DWM rounded corners, and Mica/Acrylic effects via `DwmSetWindowAttribute`. | **Native**: Built-in Fluent interface controls out-of-the-box. |
| **Distribution / Packaging** | **Single File**: `PublishSingleFile=true`, ReadyToRun, or Native AOT without MSIX dependencies or external runtimes. | **Heavier**: Requires MSIX packaging or Windows App SDK runtime redistribution. |

---

## 2. Design Principles & Architectural Invariants

1. **Strict Layer Separation (Clean / Hexagonal Architecture)**:
   - `TokenHound.Core` (`net10.0`) is a pure class library with zero UI (WPF) or OS-specific dependencies (Win32/Registry). It is 100% unit-testable in memory.
   - `TokenHound.Infrastructure` (`net10.0`) encapsulates all external integrations: Windows Credential Manager, DPAPI, local SQLite databases, HTTP networking, and stdio IPC.
   - `TokenHound.App` (`net10.0-windows`) concentrates the WPF presentation layer, ViewModels, window lifecycle, and bindings.
2. **Zero Fake Data (Honest Telemetry)**:
   - Never invent denominators or percentages when an API reports only remaining quantities (e.g., Perplexity). The `usedFraction` property remains `null`.
3. **Borrow-Don't-Own (No Competing Accounts)**:
   - Always leverage local tokens and sessions created by official developer tools on Windows, avoiding duplicate accounts or browser sign-in loops.
4. **Rate Limit Resilience & Persistence**:
   - HTTP 429 penalties, retry deadlines, and last-known valid readings survive application restarts via a persistent state cache in `%LOCALAPPDATA%\TokenHound\`.

---

## 3. Solution Folder Structure

```text
TokenHound/
├── TokenHound.slnx                         # Main .NET 10 solution (native SLNX format)
├── AGENTS.md                               # Repository engineering rules and invariants
├── CLAUDE.md                               # Agent instructions synchronized with AGENTS.md
├── ARCHITECTURE.md                         # This architecture document
│
├── src/
│   ├── TokenHound.Core/                    # [Domain Layer / Pure .NET]
│   │   ├── Models/                         # Immutable records and domain models
│   │   │   ├── Snapshot.cs                 # Full provider snapshot
│   │   │   ├── LimitWindow.cs              # Quota limit window (session, weekly, etc.)
│   │   │   ├── UsageBlock.cs               # Active rate-limit blocks (e.g. rateLimitReached)
│   │   │   ├── ProviderStatus.cs           # Ok, Stale, NeedsAuth, AccessDenied, etc.
│   │   │   ├── Fidelity.cs                 # Official, Derived, Manual
│   │   │   └── AgentSession.cs             # Liveness state (Busy, Waiting, Idle)
│   │   ├── Contracts/                      # System interfaces and contracts
│   │   │   ├── IUsageProvider.cs           # Standard quota provider contract
│   │   │   ├── IActivityMonitor.cs         # Real-time agent activity monitoring
│   │   │   └── ICredentialStore.cs         # Credential storage abstraction
│   │   └── Policies/                       # Pure domain policies
│   │       ├── BackoffCalculator.cs        # Exponential backoff calculation for HTTP 429
│   │       ├── RateLimitPolicy.cs          # Deadline evaluation and Retry-After floor logic
│   │       └── RefreshSchedulePolicy.cs    # Polling cadence policy (60s active vs 300s idle)
│   │
│   ├── TokenHound.Infrastructure/          # [Infrastructure Layer & Windows 11 OS]
│   │   ├── Engine/                         # Central orchestration engine
│   │   │   ├── UsageStore.cs               # Global state and lifecycle coordinator
│   │   │   └── UsageArchive.cs             # State and deadline disk persistence
│   │   ├── Security/                       # Windows credential access
│   │   │   ├── WindowsCredentialManager.cs # P/Invoke advapi32.dll (CredReadW for gemini:antigravity)
│   │   │   ├── DpapiStorage.cs             # Local encryption using ProtectedData
│   │   │   └── CredentialCache.cs          # In-memory timestamp-anchored cache
│   │   ├── Storage/                        # Local files and database access
│   │   │   ├── SafeSqliteReader.cs         # Concurrent WAL reader with fallback to immutable=1
│   │   │   └── SharedFileReader.cs         # Safe reading with FileShare.ReadWrite | FileShare.Delete
│   │   ├── System/                         # Windows 11 OS interop
│   │   │   ├── ProcessLiveness.cs          # PID validation and StartTimeUtc verification
│   │   │   ├── ProcessDiscovery.cs         # WMI command-line discovery (Win32_Process)
│   │   │   ├── TcpTableHelper.cs           # TCP table query (iphlpapi.dll) for ephemeral ports
│   │   │   └── SystemPowerEvents.cs        # PowerModes.Resume detection (system wake-up)
│   │   └── Providers/                      # Provider-specific implementations
│   │       ├── Claude/                     # ClaudeOAuthProvider, ClaudeSessionMonitor, ClaudeProfileDiscovery
│   │       ├── Cursor/                     # CursorLocalProvider, CursorActivityMonitor
│   │       ├── Codex/                      # CodexBridge (stdio JSON-RPC), CodexRolloutReader, CodexActivityMonitor
│   │       ├── Antigravity/                # AntigravityLanguageServerBridge, GoogleCloudCodeClient, TranscriptReader
│   │       ├── Glm/                        # GlmProvider, GlmKeyDiscovery, GlmConsoleRouter
│   │       └── Perplexity/                 # PerplexityWebViewProvider
│   │
│   └── TokenHound.App/                     # [Presentation Layer / WPF .NET 10]
│       ├── UI/
│       │   ├── Windows/                    # Application windows
│       │   │   ├── NotchWindow.xaml        # Floating screen-edge HUD window
│       │   │   ├── SettingsWindow.xaml     # Settings window
│       │   │   └── WebViewDialog.xaml      # Modal WebView2 dialog (Perplexity)
│       │   ├── Controls/                   # Custom visual controls
│       │   │   ├── ProviderRing.xaml       # Percentage and activity indicator ring
│       │   │   ├── TooltipCard.xaml        # Quota details hover card
│       │   │   └── SettingsOrb.xaml        # Settings access button
│       │   ├── Geometry/                   # Screen geometry
│       │   │   └── NotchGeometry.cs        # Bézier path calculation and hit-test areas
│       │   ├── Placement/                  # Window positioning (namespace avoids shadowing WPF `Geometry`)
│       │   │   ├── ScreenBounds.cs         # Work area and virtual screen rectangle
│       │   │   ├── NotchPlacement.cs       # Top-edge centering and visibility clamping
│       │   │   └── ScreenEdgeDetector.cs   # Taskbar detection and work area bounds
│       │   ├── Tray/                       # System Tray notification icon and lifecycle
│       │   │   ├── ITrayIcon.cs            # Notification area abstraction
│       │   │   ├── NotchVisibilityController.cs # Notch hide/show state machine
│       │   │   ├── TrayMenuItemKey.cs      # Menu item identifiers
│       │   │   ├── TrayMenuModel.cs        # Menu structure, copy constants, and DTOs
│       │   │   ├── TrayIconViewModel.cs    # Presentation model translating tray actions
│       │   │   ├── TrayIconHost.cs         # Lifecycle coordinator and graceful degradation
│       │   │   └── TaskbarIconAdapter.cs   # Hardcodet.NotifyIcon.Wpf implementation
│       │   └── Animations/                 # Smooth transitions and HUD animations
│       ├── Interop/                        # Native Win32 window hooks
│       │   ├── WindowStyles.cs             # WS_EX_NOACTIVATE, WS_EX_TOOLWINDOW, WS_EX_TOPMOST
│       │   ├── DwmHelper.cs                # DWM Mica/Acrylic effects and rounded corners
│       │   └── MouseHook.cs                # Cursor tracking for notch expansion
│       ├── ViewModels/                     # MVVM ViewModels
│       │   ├── NotchViewModel.cs           # Active rings and HUD state
│       │   └── SettingsViewModel.cs        # Provider visibility and preferences
│       └── App.xaml / App.xaml.cs          # Application entry point, composition, and shutdown
│
└── tests/
    ├── TokenHound.Core.Tests/              # Unit tests for domain models, policies, and decoders
    └── TokenHound.Infrastructure.Tests/    # Integration tests for SQLite, JSON parsing, and DPAPI
```

---

## 4. Implementation Roadmap

1. Establish `TokenHound.slnx` containing `TokenHound.Core`, `TokenHound.Infrastructure`, and `TokenHound.App`.
2. Implement domain models and validation policies in `TokenHound.Core`.
3. Implement the security layer, WAL reader, and provider clients in `TokenHound.Infrastructure` following specs in `docs/specs/`.
4. Build the screen-edge HUD in `TokenHound.App` with WPF, `HwndSource` interop, and click-through hit testing.
