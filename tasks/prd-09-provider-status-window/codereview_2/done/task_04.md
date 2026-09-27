# Stable execution context

Load in this exact order:

1. `tasks/prd-09-provider-status-window/codereview_2/codereview.md`
2. This file

Use the already loaded report version; recover relevant contracts and code afterward. This order does not guarantee a host cache hit.

---

# T04 — Alert colour for blocked accounts' status messages

## Outcome

In the provider status window, a blocked account's status message appears in the alert colour, following the HUD tooltip's error rule. Every other status message stays grey.

## Dependencies and boundaries

- Depends on: — (T01, T02, T03 are in `done/`)
- Unblocks: re-review `codereview_3` (in another session)
- In scope: a blocked flag on `ProviderStatusAccount`, set by `ProviderStatusProjection.CreateAccount`; a `DataTrigger` in `ProviderStatusWindow.xaml` that paints the status message with `StatusAlertBrush`; unit tests.
- Out of scope: `codereview_2/CR-02` (the earliest vs latest reset, and whether a fraction-less block counts as exhausted), which is a HIL 3 decision; the dimming and `back in` rules (DEC-06); the HUD tooltip; Core models.

## Traceability

| Source | Section | Finding covered |
| --- | --- | --- |
| codereview_2/CR-01 | `codereview.md#findings` | Status message is always grey; the PRD UX asks for the alert colour when the account is blocked |
| PRD §User experience | `prd.md#user-experience` | "status messages … appear under the account name in grey, or in the alert colour when the account is blocked" |
| NFR-03 | `prd.md#non-functional-requirements` | Consistency with the HUD, whose tooltip uses its error colours for `RateLimited` and `AccessDenied` (`TooltipCard.xaml.cs:UpdateStatusMessage`) |
| workflow DEC-07 | `workflow.md#DEC-07` | Human authorization for this scope |

## Requirements

- An account is blocked when its snapshot has `Status` `RateLimited` or `AccessDenied` (the HUD tooltip's error rule), or `ActiveBlock.IsBlocked` is true.
- A pending account (no snapshot) and an account with any other status are not blocked.
- A blocked account's status message uses `StatusAlertBrush`; all other status messages keep `TextSecondaryBrush`.
- No data is invented: the flag comes only from existing snapshot fields.

## Context to recover on demand

- TechSpec: `techspec.md#components-and-flow` (CMP-02, CMP-04, CMP-07), `#quality-profile`.
- Rules and skills: `AGENTS.md` C# rules and MTP commands; `dotnet-efficient-validation`.
- Code:
  - `src/TokenHound.App/ViewModels/ProviderStatusAccount.cs`: the account record.
  - `src/TokenHound.App/ViewModels/ProviderStatusProjection.cs:CreateAccount`: where the account is built.
  - `src/TokenHound.App/UI/Windows/ProviderStatusWindow.xaml:134-139,158-162`: the status TextBlock and the account template triggers.
  - `src/TokenHound.App/UI/Controls/TooltipCard.xaml.cs:UpdateStatusMessage`: the HUD rule to mirror.
  - `tests/TokenHound.Infrastructure.Tests/ViewModels/ProviderStatusProjectionTests.cs`: existing NeedsAuth and Cline free-limit scenarios.

## Work

- [x] T04.1 Add `IsBlocked` to `ProviderStatusAccount` and set it in `ProviderStatusProjection.CreateAccount` from the rule above.
- [x] T04.2 Add a `DataTrigger` on `IsBlocked` in the account template that paints the status message with `StatusAlertBrush`.
- [x] T04.3 Add unit tests: `RateLimited`, `AccessDenied`, and `ActiveBlock.IsBlocked` give `IsBlocked` true; `NeedsAuth`/`Ok` and a pending account give false.
- [x] T04.4 Build the App and the test project; run the focused class, then the full Infrastructure and Core test projects; run QA-01..QA-05 over the touched files.

## Acceptance criteria

- The new tests pass, and each one fails if the rule changes (checked by reasoning over the assertions).
- The full `tests/TokenHound.Infrastructure.Tests` run passes with more than 821 tests, and `tests/TokenHound.Core.Tests` passes with 92.
- `TokenHound.App` builds with 0 warnings. No blocking QA hit is introduced, and touched files stay ≤ 300 lines with methods ≤ 30 lines.
- No file under `src/TokenHound.Core` or `src/TokenHound.Infrastructure` changes.

## Verification

- Unit: `ProviderStatusProjectionTests` blocked-flag scenarios.
- Integration: none.
- E2E: omitted by .NET desktop policy.
- Manual: optional visual check of a rate-limited account in the window when one exists, added to the HIL 3 manual items (owner: user). The XAML trigger is checked by inspection and the App build.
- Environment dependency: none.
- Commands:
  - `rtk dotnet build src/TokenHound.App/TokenHound.App.csproj --no-restore --nologo --verbosity:minimal`
  - `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-restore --nologo --verbosity:minimal`
  - `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*ProviderStatusProjectionTests*"`
  - `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests --no-build --no-restore -- --minimum-expected-tests 1`
  - `rtk dotnet test --project tests/TokenHound.Core.Tests --no-build --no-restore -- --minimum-expected-tests 1`
- Expected evidence: exit codes 0, test counts, and QA output over the touched files.

## Affected files

- Modify: `src/TokenHound.App/ViewModels/ProviderStatusAccount.cs`, `src/TokenHound.App/ViewModels/ProviderStatusProjection.cs`, `src/TokenHound.App/UI/Windows/ProviderStatusWindow.xaml`, `tests/TokenHound.Infrastructure.Tests/ViewModels/ProviderStatusProjectionTests.cs`
- Create: —

## Observability and recovery

- Operational signal: none.
- Recovery: revert the four files.

## Handoff

> Updated by `sdd-execute-corrections` during implementation.

- Produced result: `ProviderStatusAccount.IsBlocked` is true when the snapshot `Status` is `RateLimited` or `AccessDenied` (the HUD tooltip's error rule in `TooltipCard.xaml.cs:UpdateStatusMessage`), or when `ActiveBlock.IsBlocked` is true. `ProviderStatusProjection.ResolveBlocked` sets it, and pending accounts keep the default false. In `ProviderStatusWindow.xaml`, the account template's status `TextBlock` (`x:Name="StatusMessage"`) switches to `StatusAlertBrush` through a new `IsBlocked` `DataTrigger`. Other status messages stay `TextSecondaryBrush`.
- Changed files: modified `src/TokenHound.App/ViewModels/ProviderStatusAccount.cs` (+3, 41 lines), `src/TokenHound.App/ViewModels/ProviderStatusProjection.cs` (+5, 96 lines; `CreateAccount` 27 lines), `src/TokenHound.App/UI/Windows/ProviderStatusWindow.xaml` (+4, 177 lines), and `tests/TokenHound.Infrastructure.Tests/ViewModels/ProviderStatusProjectionTests.cs`. The test file gained `CreateAccount_ByStatus_MarksBlockedLikeTheHud` (5 cases), `CreateAccount_WithActiveBlock_FollowsItsBlockedFlag` (2 cases), and `IsBlocked` false assertions in the NeedsAuth and pending tests. No Core or Infrastructure file changed, and all files are LF.
- Checks:
  - `rtk dotnet build src/TokenHound.App/TokenHound.App.csproj --no-restore --nologo --verbosity:minimal`: 0 errors, 0 warnings. The XAML compiles.
  - `rtk dotnet build tests/TokenHound.Infrastructure.Tests/...csproj --no-restore --nologo --verbosity:minimal`: 0 errors, 0 warnings.
  - Focused `--filter-class "*ProviderStatusProjectionTests*"`: 16 passed (9 before + 7 new cases).
  - Full `tests/TokenHound.Infrastructure.Tests`: 828 passed, exit 0. `tests/TokenHound.Core.Tests`: 92 passed, exit 0.
  - QA-01..QA-05 over the touched `.cs` files: no hits.
  - The assertions fail if the rule changes. Dropping `AccessDenied` or `RateLimited` flips the `true` cases, dropping `ActiveBlock` flips the `true` block case, and widening the rule flips the `Ok`/`NeedsAuth`/`Stale`/`false`-block cases.
- Validated state: git base `53da181` plus the T01/T02/T03 working tree and this correction; Debug; net10.0-windows (App) and net10.0 (tests); 2026-09-27.
- Open items: the alert colour has not been seen on screen. It needs a rate-limited or access-denied account, so it is added to the HIL 3 manual items (owner: user); the trigger was checked by inspection and the XAML build. `codereview_2/CR-02` stays a HIL 3 decision. This session issued `codereview_2` and made this correction, so `codereview_3` must run in another session.
