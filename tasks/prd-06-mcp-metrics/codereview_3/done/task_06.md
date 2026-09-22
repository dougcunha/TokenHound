# Stable execution context

Load in this exact order:

1. `tasks/prd-06-mcp-metrics/codereview_3/codereview.md` — CR-01
2. This file

---

# T06 — Validate Antigravity's live source without assuming official metrics

## Outcome

The mandatory `*Antigravity*` scoped test run passes when a running `agy` process yields a truthful derived transcript fallback, while retaining checks of official and derived source semantics.

## Dependencies and boundaries

- Depends on: T05 implementation and `codereview_3/CR-01`.
- Unblocks: reconciliation of T05 validation and a new independent review.
- In scope: the live Antigravity test's expectation and, if useful for the 300-line style target, its location in a separate test class/file.
- Out of scope: provider production behavior, MCP or HUD contracts, test skipping, manual acceptance, and unrelated flaky tests.

## Traceability

| Source | Section | Finding covered |
| --- | --- | --- |
| `codereview_3/CR-01` | `codereview.md#findings` | A live test treats process presence as proof of official metrics despite the provider's documented fallback. |
| T05.6 | `../done/task_05.md#work` | Full scoped Antigravity class validation must pass. |

## Requirements

- A live `agy` process may yield `Official` data when its official source responds or `Derived` data when transcript fallback succeeds; process presence alone does not determine fidelity.
- The test must still check a meaningful invariant of each successful source: nonempty windows, and for a derived used-count window, `UsedUnits` set with `RemainingUnits` and unknown denominator fields null.
- Deterministic tests for official language-server and derived transcript paths remain present and passing.

## Context to recover on demand

- TechSpec: `techspec.md#test-approach`, especially TC-07.
- Rules and skills: `AGENTS.md`, `dotnet-efficient-validation`, `no-workarounds`.
- Code: `AntigravityUsageProvider.cs:GetSnapshotAsync` — source fallback order; `AntigravityUsageProviderTests.cs:44-55` — invalid live assumption.

## Work

- [x] T06.1 Replace the live test's process-based `Official` assertion with source-appropriate assertions that retain real coverage when `agy` is running and permit its supported fallback.
- [x] T06.2 Keep or move the test in a file that respects the repository's class and file-length rules; preserve deterministic official and transcript tests.
- [x] T06.3 Build the Infrastructure test project; run the full `*Antigravity*` class filter, the three T05 Antigravity cases, and the `*Mcp*` filter on the same build. Run QA-01–QA-07 blocking checks on touched C# files.

## Acceptance criteria

- The full `*Antigravity*` filter executes at least one test and passes with zero failures in the current environment where the former test returned `Derived`.
- A live successful `Derived` reading verifies `UsedUnits` and null `RemainingUnits`; a successful `Official` reading verifies nonempty official windows. Non-success states are not invented as success.
- No production code changes, skipped tests, or broad suppression are used to make the run green.
- Current MCP and deterministic T05 tests still pass; no new blocking quality profile hit.

## Verification

- Unit: existing deterministic language-server and transcript tests.
- Integration: live provider test in the `*Antigravity*` scoped run, plus MCP SSE tests in `*Mcp*`.
- E2E: omitted by .NET desktop policy.
- Manual: none added; DEC-22 and T05 handoff remain acceptance evidence.
- Environment dependency: local `agy` may be running; a successful status is not guaranteed by process presence.
- Commands: `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-restore --nologo --verbosity:minimal`; `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class '*Antigravity*'`; same command with `--filter-class '*Mcp*'`.
- Expected evidence: pass counts, exit codes, and inspected diff.

## Affected files

- Modify: `tests/TokenHound.Infrastructure.Tests/Providers/Antigravity/AntigravityUsageProviderTests.cs`.
- Create if needed: `tests/TokenHound.Infrastructure.Tests/Providers/Antigravity/AntigravityLiveUsageProviderTests.cs`.

## Observability and recovery

- Operational signal: MTP failure output identifies test and returned fidelity.
- Recovery: restore the prior live test from the Git diff if the new assertions fail to reflect the provider contract; preserve the immutable review report.

## Handoff

> Updated by `sdd-execute-corrections` during implementation.

- Produced result: Moved the live test to its own `AntigravityLiveUsageProviderTests` class. A successful reading must have windows; a derived transcript reading must expose `UsedUnits` and null remaining/unknown limit fields; official success remains accepted with windows. A non-success reading still verifies provider identity and timestamp without claiming unavailable metrics. The original deterministic official and transcript tests remain in `AntigravityUsageProviderTests`.
- Changed files: `tests/TokenHound.Infrastructure.Tests/Providers/Antigravity/AntigravityUsageProviderTests.cs` (removed only the old live test and unused `System.Diagnostics` import); new `tests/TokenHound.Infrastructure.Tests/Providers/Antigravity/AntigravityLiveUsageProviderTests.cs`. No production code changed.
- Checks: Infrastructure.Tests build passed with 0 errors and 0 warnings. Native MTP `--filter-class '*Antigravity*'` passed 41/41 (including the formerly failing live case and all three T05 derived-count cases); `--filter-class '*Mcp*'` passed 22/22; both enforced `--minimum-expected-tests 1` and exited 0. QA-01–QA-05 had no hits in touched files; QA-06's 12 hits are existing calls/lexical matches in the old test file and none in the new file; QA-07 is 296 lines for the existing file and 41 for the new file. `git diff --check` passed.
- Validated state: `bc4208d` plus the two uncommitted test-file changes and SDD records, Debug, .NET SDK 10.0.401, Windows with a running `agy` process returning derived fidelity.
- Open items: New independent code review required. DEC-22/T05 manual acceptance evidence remains for HIL 3.
