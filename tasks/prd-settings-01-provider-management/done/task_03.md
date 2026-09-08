# Stable execution context

Load in this exact order:

1. `tasks/prd-settings-01-provider-management/prd.md`
2. `tasks/prd-settings-01-provider-management/techspec.md`
3. This file

Use the current PRD and TechSpec versions already loaded; recover only missing or changed sources. This order does not guarantee a host cache hit.

---

# T03 — Resolve provider status into a presentation badge without touching Core

## Outcome

A pure App-layer resolver turns a monitored flag plus an optional `Snapshot` into a badge state, a visible text label, and the resource keys for its pill colors. Every `ProviderStatus` member has a label, plus `Disabled` for a toggled-off provider and `Checking…` for an enabled provider with no snapshot yet. `TokenHound.Core.Models.ProviderStatus` is not modified.

## Dependencies and boundaries

- Depends on: —
- Unblocks: T05
- In scope: the badge enum, the resolver, its tests, and the `Compile Include` links that make both testable.
- Out of scope: the pill brushes and XAML styles (T06), the row view model that consumes this (T05). The resolver returns resource *keys* as strings; it never references a WPF type.

## Traceability

| Source | Section | Obligation covered |
| --- | --- | --- |
| FR-05 | `prd.md#functional-requirements` | A styled badge per operational state, plus a transient "Checking..." before the first snapshot |
| OBJ-02 | `prd.md#outcomes-and-metrics` | Real-time status transparency per provider |
| NFR-02 | `prd.md#non-functional-requirements` | State conveyed as visible text, not color alone |
| NFR-05 | `prd.md#non-functional-requirements` | Core stays pure; presentation logic stays in `TokenHound.App` |
| A-03 | `prd.md#explicit-assumptions` | "Disabled" is visually distinct from an auth failure or network error |
| A-04 | `prd.md#explicit-assumptions` | Neutral "Checking..." until the first snapshot resolves |
| DEC-04 | `techspec.md#technical-decisions` | `Disabled`/`Checking` live in an App enum, never in `ProviderStatus` |
| R-3 | `techspec.md#prd-reconciliation-decided-with-the-user-this-session` | The PRD's badge set omits `Unsupported`, which Core actually produces |
| CMP-07, CMP-08, CMP-16 | `techspec.md#components-and-flow` | Badge enum, resolver, and the test-project compile links |

## Context to recover on demand

- Applicable skills: `dotnet-efficient-validation`, `no-workarounds`
- Existing code: `src/TokenHound.Core/Models/ProviderStatus.cs` — the authoritative member list is `Ok, Stale, NeedsAuth, AccessDenied, RateLimited, Unsupported`; there is no `Disabled`, despite what PRD OBJ-02 says
- Existing code: `src/TokenHound.App/ViewModels/ProviderRingViewModel.Status.cs` — existing wording per status, including "No usable quota available" for `Unsupported`; keep the badge labels consistent with it
- Existing code: `tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj` — the `<Compile Include Link=...>` block is how App view models become testable; this project is `net10.0` with no `UseWPF`
- Contract or integration: `prd.md#user-experience` — the pill color values per state; `techspec.md#risks-and-open-items` OPEN-01 for the `Unsupported` label

## Work

- [x] T03.1 Create `ProviderBadgeState` with `Checking, Ok, Stale, NeedsAuth, RateLimited, AccessDenied, Unsupported, Disabled`.
- [x] T03.2 Create static `ProviderBadgeResolver` mapping `(bool isMonitored, Snapshot? snapshot)` to a state, a display label, and background/foreground resource key names.
- [x] T03.3 Apply the PRD labels: "OK", "Needs Auth", "Rate Limited", "Stale", "Access Denied", "Disabled", "Checking...", plus "No Quota" for `Unsupported` per OPEN-01.
- [x] T03.4 Add the two files to the `Compile Include` block of `TokenHound.Infrastructure.Tests.csproj`.
- [x] T03.5 Add `ProviderBadgeResolverTests` covering TC-12.

## Acceptance criteria

- `isMonitored: false` yields `Disabled` regardless of the snapshot, including a null one.
- `isMonitored: true` with a null snapshot yields `Checking`.
- Every `ProviderStatus` member maps to a distinct state with a non-empty label — verified by enumerating `Enum.GetValues<ProviderStatus>()` so a future member fails the test rather than silently falling through.
- No label is empty or whitespace, so NFR-02's text-not-color-alone rule holds.
- Neither new file references `System.Windows`, `ICommand`, or any WPF type; the `net10.0` test project compiles them.
- `TokenHound.Core` is unchanged.

## Verification

- Unit: TC-12 — badge resolution for each `ProviderStatus`, for the unmonitored case, and for monitored-with-no-snapshot; label non-emptiness asserted across the whole enum.
- Integration: none — this is a pure function with no boundary.
- E2E: omitted by .NET desktop policy.
- Manual: none. Visual verification of the resulting pills is MAN-02, owned by T06.
- Commands:
  - `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-restore --nologo --verbosity:minimal`
  - `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*ProviderBadgeResolverTests*"`
- Environment dependency: none.
- Expected evidence: non-zero executed test count with exit code 0; the test project builds with the two new linked files.

## Affected files

- Create: `src/TokenHound.App/ViewModels/ProviderBadgeState.cs`
- Create: `src/TokenHound.App/ViewModels/ProviderBadgeResolver.cs`
- Create: `tests/TokenHound.Infrastructure.Tests/ViewModels/ProviderBadgeResolverTests.cs`
- Modify: `tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj`

## Observability and recovery

- Operational signal: none — pure presentation mapping with no side effect.
- Recovery: not applicable; the change is additive and reverting it removes nothing else depends on until T05.

## Handoff

- Produced result: `ProviderBadgeState` (8 members) and the static `ProviderBadgeResolver` map `(bool isMonitored, Snapshot?)` to a badge state, a visible label, and background/foreground brush resource key names. `isMonitored: false` returns `Disabled` for any snapshot including null; `isMonitored: true` with a null snapshot returns `Checking`. Labels are "OK", "Stale", "Needs Auth", "Rate Limited", "Access Denied", "No Quota" (`Unsupported`, OPEN-01 default), "Disabled", and "Checking...", consistent with `ProviderRingViewModel.Status.cs`. Neither file references `System.Windows`, `ICommand`, or any other presentation framework type; both compile into the `net10.0` test project. `TokenHound.Core` is unmodified.
- Changed files:
  - Create: `src/TokenHound.App/ViewModels/ProviderBadgeState.cs`
  - Create: `src/TokenHound.App/ViewModels/ProviderBadgeResolver.cs`
  - Create: `tests/TokenHound.Infrastructure.Tests/ViewModels/ProviderBadgeResolverTests.cs`
  - Modify: `tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj` (two `Compile Include ... Link` entries)
- Checks:
  - `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-restore --nologo --verbosity:minimal` -> 3 projects, 0 errors, 0 warnings; exit code 0.
  - `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*ProviderBadgeResolverTests*"` -> 13 tests passed, 0 failed; exit code 0 (TC-12).
  - Extra sanity build not required by this task: `rtk dotnet build src/TokenHound.App/TokenHound.App.csproj --no-restore --nologo --verbosity:minimal` -> 4 projects, 0 errors, 0 warnings; exit code 0, proving the two files also compile inside the WPF target.
  - E2E omitted by the .NET desktop policy of the TechSpec verification profile. No manual step belongs to T03; MAN-02 stays with T06.
- Validated state: working tree with T01/T02 changes present and untouched; `git status` shows `src/TokenHound.Core` clean. Projects `TokenHound.Infrastructure.Tests` (net10.0, MTP via `UseMicrosoftTestingPlatformRunner`) and `TokenHound.App` (net10.0-windows), Debug configuration, restored assets reused with `--no-restore`.
- Open items: OPEN-01 (the "No Quota" label for `Unsupported`) remains unconfirmed by the owner; the recorded default is implemented and a wording change touches only `LABEL_UNSUPPORTED` and one `ResolveLabel_WhenKnownState_ReturnsSpecifiedWording` case. T06 must declare the brush keys named by the resolver: `Badge{Ok,Stale,NeedsAuth,RateLimited,AccessDenied,Checking,Disabled}{Background,Foreground}Brush` — `Unsupported` deliberately reuses the `Stale` slate pair, per OPEN-01.

### ADR candidates

None - direct TechSpec implementation or local decision.
