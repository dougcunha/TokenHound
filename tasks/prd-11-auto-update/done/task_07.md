# Stable execution context

Load in this exact order:

1. `tasks/prd-11-auto-update/prd.md`
2. `tasks/prd-11-auto-update/techspec.md`
3. This file

Use the current PRD and TechSpec versions already loaded; recover only missing or changed sources. This order does not guarantee a host cache hit.

---

# T07 — Periodic check with tray balloon notification

## Outcome

A scheduler runs the first evaluation 60 s after startup and then every 5 min, re-reading settings each tick, and calls the check service only when a check is due. When the result is an available, non-skipped update, the tray shows a balloon without taking focus; clicking it opens the update dialog in the Available state.

## Dependencies and boundaries

- Depends on: T03, T06
- Unblocks: —
- In scope: CMP-15 (`UpdateCheckScheduler`), `ITrayIcon.ShowNotification` + `NotificationClicked`, `TaskbarIconAdapter` (Hardcodet balloon), `TrayIconHost` forwarding, scheduler lifetime in `App.Updates.cs` (stopped by the application lifetime token), TC-17, TC-21.
- Out of scope: settings UI (T09); apply (T08).

## Traceability

| Source | Section | Obligation covered |
| --- | --- | --- |
| US-01 | `prd.md#stories-and-journeys` | Told when a new version exists |
| US-05 | `prd.md#stories-and-journeys` | Skipped version / disabled checks not nagging |
| FR-03 | `prd.md#functional-requirements` | Periodic, first shortly after startup, live settings, 0/disabled |
| FR-05 | `prd.md#functional-requirements` | Notify; Later asks again at next due check |
| NFR-03 | `prd.md#non-functional-requirements` | At most one scheduled check per interval |
| NFR-04 | `prd.md#non-functional-requirements` | Off the UI thread, cancellable |
| DEC-03, DEC-12 | `techspec.md#technical-decisions` | Due-ness/tick, balloon without focus |
| CMP-15, CMP-18 | `techspec.md#components-and-flow` | Scheduler, tray notification |
| TC-17, TC-21 | `techspec.md#test-approach` | Tests |
| MA-4 | `techspec.md#test-approach` | Periodic manual script |

## Context to recover on demand

- Applicable skills and rules: `AGENTS.md` (`TimeProvider`, `CancellationToken`, `ConfigureAwait(false)` in Infrastructure).
- Existing code: `src/TokenHound.App/UI/Tray/{ITrayIcon,TaskbarIconAdapter,TrayIconHost}.cs`; `tests/TokenHound.Infrastructure.Tests/Tray/TrayIconHostTests.cs` (fake tray); `tests/.../ViewModels/ManualTimeProvider.cs`; `ApplicationLifetime.LifetimeToken`.
- Contract or integration: `techspec.md#integrations-and-interfaces` (tray).

## Work

- [x] T07.1 `UpdateCheckScheduler.RunAsync(ct)` with `TimeProvider`: initial delay 60 s, tick 5 min, reload `UpdateSettings`, `UpdatePolicy.IsCheckDue` with `lastCheckUtc`, call `UpdateCheckService`, raise `UpdateAvailable(ReleaseInfo)`.
- [x] T07.2 `ITrayIcon.ShowNotification(title, message)` and `NotificationClicked`; implement in `TaskbarIconAdapter` via `ShowBalloonTip` / `TrayBalloonTipClicked`; forward in `TrayIconHost`; update the fake tray in tests.
- [x] T07.3 `App.Updates.cs`: start the scheduler with the lifetime token, marshal `UpdateAvailable` to the UI thread, show the balloon, open the dialog on click.
- [x] T07.4 Tests TC-17, TC-21.

## Acceptance criteria

- With a fake clock: no check before 60 s; a check at 60 s when due; none again until the interval elapses; changing the interval in settings takes effect on the next tick; `Enabled=false` or interval 0 → no checks.
- An `Available` result raises one balloon; a `Skipped` result raises none.
- Clicking the balloon opens the dialog showing the available release.
- The scheduler stops when the lifetime token is cancelled, with no unobserved exception.

## Verification

- Unit: TC-17, TC-21.
- Integration: none.
- E2E: omitted by .NET desktop policy.
- Manual: MA-4 at the visual check.
- Commands: `rtk dotnet build TokenHound.slnx --no-restore`; `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests --no-build --no-restore -- --minimum-expected-tests 1`.
- Environment dependency: network for MA-4 only.
- Expected evidence: passing `UpdateCheckSchedulerTests`, updated `TrayIconHostTests`.

## Affected files

- Modify: `src/TokenHound.App/UI/Tray/{ITrayIcon,TaskbarIconAdapter,TrayIconHost}.cs`, `src/TokenHound.App/App.Updates.cs`, `tests/TokenHound.Infrastructure.Tests/Tray/TrayIconHostTests.cs`.
- Create: `src/TokenHound.Infrastructure/Updates/UpdateCheckScheduler.cs`, `tests/TokenHound.Infrastructure.Tests/Updates/UpdateCheckSchedulerTests.cs`.

## Observability and recovery

- Operational signal: `UpdateCheckStarted {Trigger=Scheduled}`; scheduler stop logged at shutdown.
- Recovery: `Update.Enabled=false` disables the scheduler without a code change.

## Handoff

> Updated by `sdd-execute-task` during implementation.

- Produced result: `UpdateCheckScheduler` (Infrastructure; delegates for settings, last-check time, and the check so it is testable without HTTP): `RunAsync(ct)` waits 60 s, then evaluates every 5 min through `TickAsync`, which re-reads settings, uses `UpdatePolicy.IsCheckDue`, runs the check, and raises `UpdateAvailable` (event args are the whole `UpdateCheckOutcome`, not just `ReleaseInfo`, so the dialog can render the exact outcome behind the balloon) only for `Available` (a `Skipped` outcome raises nothing). Cancellation ends `RunAsync` quietly; other exceptions in a tick are logged and the loop continues. `ITrayIcon.ShowNotification` + `NotificationClicked`, implemented in `TaskbarIconAdapter` with Hardcodet `ShowBalloonTip`/`TrayBalloonTipClicked` (balloon does not take focus); `TrayIconHost.ShowNotification` (safe when uninitialized/degraded/failing) and `NotificationClicked` (dispatched to the UI thread, unsubscribed on dispose). `UpdateViewModel.ShowOutcome` renders a known outcome. `App.Updates.StartUpdateScheduler` (one call in `OnStartup` after `StartMcpServer`) builds the scheduler with the `Scheduled` trigger, tracks its task in the lifetime, shows the balloon on the UI thread, and a balloon click opens the dialog with the stored outcome (no second request).
- Changed files: created `src/TokenHound.Infrastructure/Updates/UpdateCheckScheduler.cs`, `tests/TokenHound.Infrastructure.Tests/Updates/{UpdateCheckSchedulerTests,DelayTimeProvider}.cs`, `tests/TokenHound.Infrastructure.Tests/Tray/{FakeTrayIcon,TrayIconHostNotificationTests}.cs`; modified `src/TokenHound.App/UI/Tray/{ITrayIcon,TaskbarIconAdapter,TrayIconHost}.cs`, `src/TokenHound.App/App.Updates.cs`, `src/TokenHound.App/App.xaml.cs` (+1 call), `src/TokenHound.App/ViewModels/UpdateViewModel.cs` (+`ShowOutcome`), `tests/TokenHound.Infrastructure.Tests/Tray/TrayIconHostTests.cs` (fake tray moved to its own file to keep the file under 300 lines).
- Checks: `rtk dotnet build TokenHound.slnx --no-restore` -> 0 errors, 0 warnings; full Infrastructure suite -> 908 passed (TC-17: 7 scheduler tests incl. 60 s / 5 min delays, due-ness, live settings, disabled/0, availability event, cancellation, contained failures; TC-21: 2 host tests). Quality profile over the T07 files: QA-01..QA-03, QA-05, QA-07 empty; QA-06 largest `App.xaml.cs` 454 (baseline 449; +5 lines are call sites), others <= 295.
- Validated state: base `a8bd1bf` + T01..T06 + the files above; Debug.
- Open items: reservation QA-04 `tests/TokenHound.Infrastructure.Tests/Updates/DelayTimeProvider.cs:52,56` (wall-clock `DateTime.UtcNow` bounds a 5 s test wait, not logic). Reservation QA-08 `UpdateCheckScheduler.cs` constructor has 4 parameters (3 delegates + optional clock; the single-line regex misses multi-line signatures, checked by reading). A failed check (no deadline) leaves `lastCheckUtc` unchanged, so it is retried at the next 5-minute tick (12/h, under the 60/h budget); the persisted gate still blocks after a 429. The balloon shows only for scheduled checks; MA-4 pending at the visual check.

### ADR candidates

None - direct TechSpec implementation or local decision.
