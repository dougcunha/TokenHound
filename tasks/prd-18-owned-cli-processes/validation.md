# Validation evidence

Environment: Windows, .NET SDK 10.0.401, Debug configuration. No package additions. E2E omitted by desktop .NET policy.

## Commands and observed results

- `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-restore --nologo --verbosity:minimal`: 4 projects, zero errors, zero warnings.
- `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1`: 1145 passed, exit code 0 (explicitly preserved via `$LASTEXITCODE`), 13.0 seconds on the final run.
- `rtk dotnet build src/TokenHound.App/TokenHound.App.csproj --no-restore --nologo --verbosity:minimal`: zero errors, zero warnings.
- `rtk git diff --check`: exit code 0.
- `rtk dotnet sln TokenHound.slnx list`: includes the six projects, including the process helper.
- OS helper process enumeration after the run: zero `TokenHound.ProcessTestHelper` processes.
- `graft build`: refreshed the local ignored graph.

## Behavioral coverage

Eight new protocol tests cover reuse, initialization once, concurrent serialization, cancellation of queued and active requests, disposal during I/O, delayed exponential startup recovery, exited-server recovery, and stale/malformed/notification filtering. Existing Codex provider mapping and fallback tests remain passing.

Seven real Windows helper tests cover graceful EOF shutdown, stderr beyond pipe capacity, forced tree termination after the grace period, descendants surviving a root exit, killing only the owning process without managed cleanup, twenty quota exchanges using one process, and timeout cancellation on a real pipe. The intentional `Kill(entireProcessTree: false)` is limited to the crash simulation test; production termination uses the whole tree and job closure.

The independent risk review identified a gh startup regression before task approval: exceptions could bypass credential fallback. The correction handles expected failures at the credential-source boundary. Five additional regression cases cover missing executable, invalid start, broken pipe, access denied, and cancellation propagation. All pass in the final integrated run.

Formal review: [codereview_01/codereview.md](codereview_01/codereview.md), literal status `REJECTED` solely because essential TC-05 manual evidence is missing; no current implementation defect was identified. Subsequent style cleanup expanded one Marshal.Copy call and used ordinal case-insensitive comparisons in the test helper. The full 1145-test run and App build above were repeated successfully afterward; final acceptance/re-review remains open.

Quality checks: scoped search found no new sync-over-async, async void, nullable suppression, or direct parameterless `Process.Kill()`. Matches on `ICodexProcess.Kill()` and fake-process kill implement whole-tree ownership or test contracts. New production files are below 300 lines and new production methods at most 30 lines, verified against the refreshed graph. App.xaml.cs size is pre-existing; its diff only registers ownership.

## Pending real desktop acceptance (TC-05)

1. Run the newly built app with Codex enabled and confirm live quotas and rollout fallback still behave as expected.
2. Observe for one hour. Check Git processes older than two minutes and trace their origin to TokenHound; the global count in the supplied report can include unrelated developer tools.
3. Close normally, then repeat with Task Manager ending TokenHound. Verify no app-owned Codex/Git descendants remain.
4. Inspect System/Application Popup event 26 for new 0xc0000142 errors attributable to the app's process tree.

Not performed: one-hour desktop soak, real Codex/marketplace Git reproduction, live account quotas, or popup log observation. Helper tests prove process ownership mechanics, not disappearance of the originally reported popup in a live user session. No running user app was replaced and no release or commit was created.
