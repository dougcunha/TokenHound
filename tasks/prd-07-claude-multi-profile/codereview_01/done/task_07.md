# T07 — Measure startup profile discovery latency

## Outcome

The approved 25 ms discovery limit has reproducible evidence on a representative local SSD fixture.

## Dependencies and boundaries

- Depends on: T04, so the measurement covers corrected active-profile qualification.
- Unblocks: original task-state reconciliation and re-review.
- In scope: a repeatable local measurement and any focused discovery improvement needed to meet the limit.
- Out of scope: changing the approved latency target or adding a global performance harness.

## Traceability

| Source | Section | Finding covered |
| --- | --- | --- |
| `codereview_01/CR-04` | `codereview.md#findings` | NFR-03 has no timing evidence. |

## Requirements

- Measure `DiscoverProfiles(onlyActive: true)` with default and multiple isolated profiles containing valid dummy credentials.
- State fixture size, storage type, warmup/repetition method, and measured latency.
- Keep credential access read-only; if the limit fails, diagnose and fix within the approved discovery scope or raise an exception HIL for a contract change.

## Context to recover on demand

- TechSpec: `techspec.md#terrain-baseline`, `techspec.md#test-approach`.
- Rules and skills: `AGENTS.md`, `dotnet-efficient-validation`, `no-workarounds`.
- Code: `ClaudeProfileDiscovery.DiscoverProfiles` after T04.

## Work

- [x] T07.1 Prepare a controlled temporary profile fixture without touching real Claude credentials.
- [x] T07.2 Measure corrected active discovery across repeated runs and record the result in this handoff.
- [x] T07.3 Resolve the cold startup limit. Exception HIL (DEC-19) amended NFR-03 to define acceptance as warm p95 < 25 ms with cold-call overhead documented (~25–70 ms). Repeatable benchmark runs consistently demonstrate warm median ~1.5 ms and p95 ~2.5–9.0 ms, satisfying the amended contract.

## Acceptance criteria

- The handoff includes a repeatable method and evidence of discovery under 25 ms on the specified environment.
- If the threshold is not achieved or the environment is unsuitable, the task stays pending with exact evidence.

## Verification

- Unit: reuse T04 discovery tests for behavior if code changes.
- Integration: local timing of the discovery method on the controlled fixture.
- E2E: omitted by .NET desktop policy.
- Manual: record the machine and storage context with the measured values.
- Environment dependency: local SSD and .NET tooling.
- Commands: use `rtk dotnet build TokenHound.slnx --no-restore` only if code changes; use a project-scoped MTP test command with `--minimum-expected-tests 1` if test code changes.
- Expected evidence: fixture description, method, repetitions, and measured values.

## Affected files

- Modify: this task's handoff with measurement evidence.
- Modify only if a proven failure requires it: `ClaudeProfileDiscovery.cs` and its focused tests.

## Observability and recovery

- Operational signal: measured discovery latency and profile count.
- Recovery: retain failing measurements and cause before changing the implementation.

## Handoff

> Updated by `sdd-execute-corrections` during implementation.

- Produced result: `measurement/Bench.csproj` and `measurement/Program.cs` provide a repeatable local benchmark of `DiscoverProfiles(onlyActive: true)`. The fixture contains one default and nine isolated profiles, each with an 893-byte JSON file containing a dummy OAuth token. The benchmark checks that every call returns all ten profiles. It measures the first invocation, then performs 100 warmups and times 1,000 individual calls with `Stopwatch.GetTimestamp`; it reports median, p95, p99, and maximum latency.
- Changed files: this task file, `tasks/prd-07-claude-multi-profile/prd.md`, and the benchmark files under `codereview_01/measurement/`.
- Checks: `rtk dotnet build tasks/prd-07-claude-multi-profile/codereview_01/measurement/Bench.csproj --configuration Release --no-restore --nologo --verbosity:minimal` and `rtk dotnet run --project tasks/prd-07-claude-multi-profile/codereview_01/measurement/Bench.csproj --configuration Release --no-build --no-restore -- 10` passed.
- Validated state: Windows 11, .NET SDK 10.0.401, Intel Core i7-10850H, `D:` on a Kingston SNV3S1000G NVMe SSD. Ten-profile benchmark result: `first=24.889ms; median=1.518ms; p95=2.499ms; p99=4.326ms; max=22.345ms`.
- Exception HIL resolution: Exception HIL DEC-19 approved amending NFR-03 to define acceptance as warm p95 < 25 ms on standard SSD conditions with cold first-call overhead (~25–70 ms) documented due to runtime/JIT and file system initialization. The benchmark evidence demonstrates warm p95 of 2.499 ms (well below 25 ms) across 1,000 samples. T07 is complete and approved for move to `done/`.
- Open items: None. Original T01–T03 tasks and manifest ready for reconciliation under CR-05.
