# T01: Own background CLI trees and reuse Codex

Read `prd.md`, `techspec.md`, then this task. No dependencies.

## Work and acceptance

- Implement the shared CLI host with atomic Windows job containment, redirected stdio, stderr draining, graceful shutdown, and forced tree cleanup (FR-03/04, NFR-01).
- Reuse Codex with serialized, correlated requests and bounded recovery/backoff (FR-01/02, NFR-02).
- Wire provider ownership into app shutdown and use the host for gh without changing credentials or snapshots (FR-05).
- Add protocol and OS process regression tests; run Infrastructure tests and build App (TC-01 through TC-04).
- Record the one-hour manual acceptance as pending (TC-05); no claim about real Windows popups without observation.

Scope: Infrastructure Processes, Codex client/provider, Copilot credential discovery, App ownership registration, matching tests, and durable provider documentation. No other feature artifacts, release, or credentials.

## Handoff

Produced result: persistent, serialized Codex queries with unique response IDs, initialization acknowledgment, cancellation, and exponential recovery. Shared Windows process host creates kill-on-close job membership atomically with restricted stdio inheritance, drains stderr, and closes stdin before bounded tree termination. Codex disposal is registered in both application cleanup paths; gh uses the same host. Intentional browser and update handoffs remain independent.

Changed files: Infrastructure Processes (five files), Codex client/discovery/protocol/process wrappers/provider, Copilot credential discovery, App.xaml.cs, project/solution wiring, provider specs, protocol tests, real process tests, and the console helper project.

Checks: Infrastructure project build with no errors or warnings; all 1145 Infrastructure tests passed, exit code 0, with `--minimum-expected-tests 1`; App build with no errors or warnings; diff whitespace check passed; no helper process remained after validation. Exact commands and scope: [validation.md](../validation.md).

Validated state: .NET SDK 10.0.401, Debug/net10.0 Infrastructure tests and net10.0-windows App on Windows. The solution subsequently adds the already-built helper project under /tests/; `dotnet sln ... list` confirms membership. No source changed after the final successful test/App build.

Independent risk review found one gh startup/fallback regression before approval; corrected at the credential-source boundary with five regression cases. Open items: formal independent review and TC-05 real desktop one-hour soak, normal/Task Manager exits, live quota comparison, and Application Popup observation. These remain acceptance obligations; they are not claimed from helper-process tests. No new blocking quality hits or reservations found by the author. ADR candidates: None, direct TechSpec implementation.
