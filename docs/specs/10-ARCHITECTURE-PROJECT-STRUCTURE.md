# Technical Specification: Architecture and Project Structure (.NET 10 / C#)

This document specifies the global software architecture, UI framework trade-offs, and complete folder layout for **TokenHound** on **Windows 11**.

---

## 1. UI Framework Decision: WPF (.NET 10) vs. WinUI 3

The application operates as a floating screen-edge HUD anchored to one of the display's borders (Top, Bottom, Left, or Right), featuring inverse rounded corners (notch / pill shape), hover animations, click-through pass-through outside capsule contours, and background execution without stealing user keyboard focus.

| Requirement | **WPF (.NET 10)** *(Recommended)* | **WinUI 3 (Windows App SDK)** |
| :--- | :--- | :--- |
| **Irregular Transparent Windows** | **Excellent**. `AllowsTransparency="True"`, `WindowStyle="None"`, `Background="Transparent"`, and `PathGeometry` clipping. | **Unstable**. Historical difficulties in the WinUI compositor removing borders and handling per-pixel alpha transparency. |
| **Non-Activating Window (*No-Activate HUD*)** | **Direct via Win32**. `HwndSource` applying `WS_EX_NOACTIVATE`, `WS_EX_TOOLWINDOW`, and `WS_EX_TOPMOST`. | **Complex**. Requires low-level P/Invoke on `AppWindow`; flyouts frequently steal OS focus. |
| **Click-Through Transparency** | **Simple**. Intercepts `WM_NCHITTEST` returning `HTTRANSPARENT` outside capsule geometry. | **Limited**. `InputNonClientPointerSource` has constrained support for arbitrary geometric paths. |
| **Background Memory Footprint** | **~30 to 45 MB** at idle. | **~90 to 160 MB** due to Windows App SDK runtime overhead. |
| **System Tray Integration** | **Consolidated** via lightweight libraries (e.g. `Hardcodet.NotifyIcon.Wpf`). | **No native support**. Relies on third-party community wrappers. |
| **Windows 11 Fluent Visuals** | **Integrated**. .NET 10 supports native Windows 11 themes (`SystemTheme="Windows11"`) and DWM Mica/Acrylic. | **Native**. Fluent controls built into the runtime. |
| **Packaging & Distribution** | **Single File**: `PublishSingleFile=true`, no external runtime or MSIX requirement. | **Heavier**: Requires MSIX packaging or redistributing the Windows App SDK runtime. |

**Verdict**: **WPF (.NET 10)** is the superior technical choice for this class of desktop utility on Windows 11.

---

## 2. Solution Folder Layout

```text
TokenHound/
├── TokenHound.slnx                         # Main .NET 10 solution (native SLNX format)
├── AGENTS.md                               # Repository coding standards and invariants
├── CLAUDE.md                               # Synchronized agent instructions
├── ARCHITECTURE.md                         # Architecture rationale and design principles
├── README.md                               # Project documentation and quickstart
├── LICENSE                                 # MIT License
│
├── src/
│   ├── TokenHound.Core/                    # [Pure Domain Library] Zero OS or UI dependencies
│   │   ├── Models/                         # Snapshot, LimitWindow, UsageBlock, ProviderStatus, Fidelity
│   │   ├── Contracts/                      # IUsageProvider, IActivityMonitor, ICredentialStore
│   │   └── Policies/                       # BackoffCalculator, RateLimitPolicy, RefreshSchedulePolicy
│   │
│   ├── TokenHound.Infrastructure/          # [Infrastructure & OS] Windows 11 implementations
│   │   ├── Engine/                         # UsageStore, UsageArchive (disk cache)
│   │   ├── Security/                       # WindowsCredentialManager (advapi32.dll), DpapiStorage
│   │   ├── Storage/                        # SafeSqliteReader (WAL/immutable), SharedFileReader
│   │   ├── System/                         # ProcessLiveness, ProcessDiscovery (WMI), TcpTableHelper, PowerEvents
│   │   └── Providers/                      # Providers (Claude, Cursor, Codex, Antigravity, Glm, Perplexity)
│   │
│   └── TokenHound.App/                     # [WPF Presentation] Screen-edge HUD and Settings
│       ├── UI/
│       │   ├── Windows/                    # NotchWindow (HUD), SettingsWindow, WebViewDialog
│       │   ├── Controls/                   # ProviderRing, TooltipCard, SettingsOrb
│       │   ├── Geometry/                   # NotchGeometry, NotchPlacement
│       │   └── Animations/                 # Ring animations and expansion transitions
│       ├── Interop/                        # Win32 styles, DwmHelper (Mica), MouseHook
│       ├── ViewModels/                     # NotchViewModel, SettingsViewModel
│       └── App.xaml / Program.cs
│
└── tests/
    ├── TokenHound.Core.Tests/              # Unit tests for domain models and policies
    └── TokenHound.Infrastructure.Tests/    # Integration tests for SQLite, JSON, and file reading
```
