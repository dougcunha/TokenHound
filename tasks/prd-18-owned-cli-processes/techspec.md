# TechSpec: Owned CLI processes

## Decisions and flow

| ID | Obligations | Decision |
| --- | --- | --- |
| DEC-01 | FR-01, NFR-02 | Serialize client requests with a semaphore; initialize once, correlate unique IDs, and consume notifications and malformed lines without mixing responses |
| DEC-02 | FR-02, FR-05 | Retire failed sessions, preserve null/rollout fallback, propagate caller cancellation, and use TimeProvider-backed exponential restart backoff capped at one minute |
| DEC-03 | FR-03, NFR-01 | Share an owned-process host in Infrastructure. Windows creates a non-inheritable SafeHandle job with KILL_ON_JOB_CLOSE and passes JOB_LIST and HANDLE_LIST at CreateProcessW, so ownership is atomic and only stdio handles are inherited |
| DEC-04 | FR-04 | Async disposal closes stdin and waits three seconds, then kills the entire tree; always close the job even if the root already exited. IDisposable provides immediate cleanup for unexpected app exit |
| DEC-05 | FR-05 | Use the same process host for gh; leave intentional browser/update handoffs independent |

Components: `Processes/` owns native handles, redirected streams, stderr draining, and shutdown; Codex protocol/session and executable discovery remain in `Providers/Codex/`; provider exposes disposal and application lifetime tracks it. No new packages or Core OS dependencies.

Native references: [Job objects](https://learn.microsoft.com/en-us/windows/win32/procthread/job-objects), [process attributes](https://learn.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-updateprocthreadattribute). Atomic JOB_LIST replaces post-start AssignProcessToJobObject to remove the startup race. Any containment failure fails startup rather than leaving an unprotected process running.

## Tests and quality

.NET 10, net10.0 Infrastructure, WPF App, xUnit v3 MTP v2 via SDK native MTP (`global.json`). Build once per code version, then run project tests with `--no-build --no-restore -- --minimum-expected-tests 1`. E2E: omitted by desktop .NET policy. OS process integration tests use isolated helper processes, not the desktop UI or real credentials.

| ID | Requirements | Evidence |
| --- | --- | --- |
| TC-01 | FR-01, NFR-02 | Protocol tests: reuse, concurrency, correlation, handshake, cancellation |
| TC-02 | FR-02, FR-05 | Fake-clock restart/backoff tests and existing provider fallback tests |
| TC-03 | FR-03, FR-04, NFR-01 | Windows helper process tests: graceful EOF, forced tree cleanup, owner killed, early root exit, stderr saturation |
| TC-04 | FR-05 | Existing Codex and Copilot tests plus App build |
| TC-05 | OBJ-01 | One-hour Windows manual acceptance in PRD, pending |

Blocking quality rules: no sync-over-async; no simple Process.Kill(); no swallowed unexpected errors; no credential logging; preserve cancellation and job ownership on every path. Verify scoped `rg` patterns and diff review. Reservation rules: files above 300 lines and methods above 30 lines. Native interop signatures/structs necessarily carry multiple parameters/fields (DEC-03); keep calls vertically formatted.

Terrain baseline: Codex client 295 lines (five public methods, three injected options), provider 192 lines, credential discovery 193 lines, App.xaml.cs 464 lines (pre-existing size and long methods). No saturated switch or six-dependency constructor is extended. Extract the existing executable discovery and process implementation locally while implementing, with no preparatory feature. Existing broad catch in Codex and empty race catches are replaced in touched lifecycle code. App changes are limited to ownership registration.

No migrations. Rollback by reverting this feature. Risks: native handle lifetime, pipe cancellation, external CLI version differences; covered by Windows process tests, protocol tests, and explicit manual limits. Diagnostic logs include errors and restart delay, never raw stdout or stderr/token contents.
