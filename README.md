# TokenHound 🐶

> **A non-intrusive Windows 11 desktop HUD monitoring LLM usage, rate limits, and agent activity across AI coding tools.**

[![CI](https://github.com/dougcunha/TokenHound/actions/workflows/ci.yml/badge.svg)](https://github.com/dougcunha/TokenHound/actions/workflows/ci.yml)
[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Windows 11](https://img.shields.io/badge/Platform-Windows%2011-0078D4?logo=windows11)](https://www.microsoft.com/windows)
[![WPF](https://img.shields.io/badge/UI-WPF-blue)](https://github.com/dotnet/wpf)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

TokenHound is a lightweight, peripheral desktop notch / HUD crafted specifically for Windows 11. It provides continuous, glanceable insight into token consumption, rate limit reset windows, and live agent activity across local AI coding assistants—without stealing window focus or cluttering your workspace.

---

## Key Features

- **Screen-Edge Floating Notch**: Glides at the top or edge of your screen with smooth Bézier curvature, Win11 Mica/Acrylic styling, and minimal visual intrusion.
- **Zero Focus Disruption (`WS_EX_NOACTIVATE`)**: Built using native Win32 interop so the notch never steals keyboard or window focus from your IDE, terminal, or browser.
- **Click-Through Transparency**: Outside the notch's capsule contours, mouse clicks pass directly through to whatever application is behind it via `WM_NCHITTEST` handling.
- **"Borrow, Don't Own" Principle**: Never initiates competing logins or asks for raw passwords. TokenHound safely reads existing local sessions (Windows Credential Manager, DPAPI, SQLite WAL databases) in read-only mode (`FileShare.ReadWrite | FileShare.Delete`).
- **Zero Fake Data**: Strictly reports honest telemetry. If an API provider only exposes remaining quota without a total limit, TokenHound will never invent percentages or artificial denominators.
- **HTTP 429 Resilience & Persistence**: Backoff deadlines and rate-limit states are persisted to `%LOCALAPPDATA%\TokenHound\`, guaranteeing that restart cycles will never hammer provider endpoints under active penalty windows.
- **Ultra-Lean Resource Usage**: Operates continuously at approximately ~30–45 MB RAM in idle desktop state.

---

## Supported Providers

| Provider | Integration Mechanism | Monitored Telemetry |
| :--- | :--- | :--- |
| **Claude Code** | `%USERPROFILE%\.claude\.credentials.json` | OAuth token, 5-hour rolling session quotas, 7-day limits |
| **Cursor** | Local SQLite (`state.vscdb`, WAL mode) | Session requests, plan thresholds, active agent status |
| **OpenAI Codex** | stdio JSON-RPC bridge & rollout logs | Execution tokens, rate-limit resets, active command states |
| **Google Antigravity / Gemini** | Language server IPC & Windows Credential Manager (`gemini:antigravity`) | Quota tier, prompt tokens, session transcripts |
| **Z.ai GLM** | Local config discovery & direct metering API | Coding plan usage, daily quota allocation |
| **Perplexity** | Isolated WebView2 session borrowing | Real-time search queries and rate limit quotas |

---

## Architecture & Solution Structure

TokenHound is architected following Clean Architecture principles on **.NET 10**:

```text
TokenHound/
├── TokenHound.slnx                         # Solution file (native .NET 10 SLNX format)
├── ARCHITECTURE.md                         # Detailed architecture and technology rationale
├── AGENTS.md                               # Repository coding standards and invariants
│
├── src/
│   ├── TokenHound.Core/                    # Pure domain models, contracts, and policies (net10.0)
│   │   ├── Models/                         # Immutable snapshots, limit windows, agent sessions
│   │   ├── Contracts/                      # IUsageProvider, IActivityMonitor, ICredentialStore
│   │   └── Policies/                       # BackoffCalculator, RateLimitPolicy, RefreshSchedulePolicy
│   │
│   ├── TokenHound.Infrastructure/          # Windows 11 OS interop, storage, and providers (net10.0)
│   │   ├── Engine/                         # UsageStore, historical state persistence
│   │   ├── Security/                       # Windows Credential Manager P/Invoke, DPAPI
│   │   ├── Storage/                        # SQLite WAL reader, safe shared file access
│   │   ├── System/                         # Process discovery, TCP table helper, power events
│   │   └── Providers/                      # Provider adapters (Claude, Cursor, Codex, Gemini, etc.)
│   │
│   └── TokenHound.App/                     # Windows Presentation Foundation desktop host (net10.0-windows)
│       ├── UI/                             # Windows, HUD controls, Bézier geometry, animations
│       ├── Interop/                        # Win32 window styles, DWM blur, mouse hook
│       └── ViewModels/                     # MVVM state and provider collection bindings
│
├── tests/
│   ├── TokenHound.Core.Tests/              # Unit tests for domain logic and backoff calculators
│   └── TokenHound.Infrastructure.Tests/    # Integration tests for storage, parsing, and DPAPI
│
└── docs/
    ├── specs/                              # 10 deep technical specifications per provider
    └── design/                             # Visual design assets, layout coordinates, and mockups
```

---

## Getting Started

### Prerequisites

- **Windows 11** (Build 22000 or higher recommended)
- **[.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)** (`10.0.400` or newer)

### Building the Project

Clone the repository and build using the .NET CLI:

```powershell
# Clone the repository
git clone https://github.com/dougcunha/TokenHound.git
cd TokenHound

# Restore and build the solution
dotnet restore TokenHound.slnx
dotnet build TokenHound.slnx --no-restore
```

### Running Tests

Execute the unit test suite:

```powershell
dotnet test TokenHound.slnx --no-build
```

---

## Technical Documentation

For in-depth specifications and implementation guides, explore the `docs/` folder:

- [ARCHITECTURE.md](ARCHITECTURE.md) - Rationale for WPF (.NET 10) vs. WinUI 3, design principles, and memory targets.
- [docs/specs/01-READING-STRATEGY-RESILIENCE.md](docs/specs/01-READING-STRATEGY-RESILIENCE.md) - Polling intervals, 429 backoff math, and offline caching.
- [docs/specs/02-WINDOWS-CREDENTIALS-SECURITY.md](docs/specs/02-WINDOWS-CREDENTIALS-SECURITY.md) - Credential Manager P/Invoke, DPAPI encryption, and SQLite WAL mechanics.
- [docs/specs/](docs/specs/) - Dedicated specifications for each supported AI provider.

---

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
