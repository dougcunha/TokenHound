# Provider adapters and HUD

Read this reference only when the refactoring target includes provider adapters, credential or database readers, rate-limit handling, or HUD windows.

- Characterize each provider adapter before changing it: record the recorded API response or local file it parses and the exact snapshot it produces, as test data under `tests/TokenHound.Infrastructure.Tests/Providers/<Provider>/`. Check the adapter against its spec in `docs/specs/`.
- Treat `usedFraction`, reset times, and error states as contracts. A refactoring that invents a limit or denominator, or turns `null` into a number, is not behavior-preserving.
- Preserve the read-only borrowing of credentials owned by other tools (`FileShare.ReadWrite | FileShare.Delete`) and the SQLite `Mode=ReadOnly` / `immutable=1` fallback; cover both paths with tests before moving that code.
- Cover persisted 429 deadlines and `Retry-After: 0` with tests before moving network dispatch.
- HUD window behavior (`WM_MOUSEACTIVATE` returning `MA_NOACTIVATE`, `SWP_NOZORDER` without `SWP_SHOWWINDOW`) has no automated coverage: record a manual script with the Windows MCP launch and screenshot steps from `AGENTS.md`.
- Preserve the boundaries in `ARCHITECTURE.md`: moving code between `TokenHound.Core`, `TokenHound.Infrastructure`, and `TokenHound.App` is structural, but a UI or OS dependency in `Core` is a regression.
