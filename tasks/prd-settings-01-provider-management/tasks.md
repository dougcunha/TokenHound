# Implementation plan — Provider Enablement, Status Visibility & Engine Gating in Settings

## Stable sources

- PRD: `tasks/prd-settings-01-provider-management/prd.md`
- TechSpec: `tasks/prd-settings-01-provider-management/techspec.md`

> Common sources before the task and mutable state; read order does not guarantee a cache hit.

## Dependency graph

| ID | Delivery | Depends on | Unblocks |
| --- | --- | --- | --- |
| T01 | `"Providers"` section persists to `appsettings.json` without disturbing `Hud`, `Refresh`, `Log`; every gap defaults to enabled | — | T05, T07 |
| T02 | `UsageStore` skips disabled providers on both refresh paths and in activity polling; re-enable dispatches one refresh honoring rate limits | — | T04, T05, T07 |
| T03 | Provider status resolves to a badge state and visible label, including `Disabled`, `Checking…`, and `Unsupported`, with Core untouched | — | T05 |
| T04 | HUD shows rings only for monitored providers, restoring them at their original index | T02 | T07 |
| T05 | Settings view models list every registered provider and apply toggles live to engine, badge, and disk | T01, T02, T03 | T06 |
| T06 | Settings dialog renders provider rows, badge pills, and keyboard-accessible toggles in dark mode | T05 | T07 |
| T07 | Startup applies stored enablement before the HUD is built; end-to-end journey and manual acceptance close | T01, T02, T04, T06 | — |

Acyclic. Three independent roots (T01, T02, T03) can run in parallel; T07 is the single sink. File collisions are serialized by the graph: `TokenHound.Infrastructure.Tests.csproj` is edited by T03 then T05 (T05 depends on T03), and no other file is touched by two tasks.

## Traceability matrix

| Source ID | Source section | Obligation | Tasks | Evidence or test |
| --- | --- | --- | --- | --- |
| OBJ-01 | `prd.md#outcomes-and-metrics` | Toggling off removes the ring and halts polling | T02, T04, T06 | TC-05, TC-06, TC-10; MAN-05 |
| OBJ-02 | `prd.md#outcomes-and-metrics` | Distinct styled badge per operational state | T03, T05, T06 | TC-12, TC-13; MAN-02 |
| OBJ-03 | `prd.md#outcomes-and-metrics` | Immediate effect, no restart, no Apply | T02, T04, T05 | TC-08, TC-10, TC-13 |
| OBJ-04 | `prd.md#outcomes-and-metrics` | Toggles persist across restarts, other sections preserved | T01, T07 | TC-01; MAN-04 |
| OBJ-05 | `prd.md#outcomes-and-metrics` | Dark mode, keyboard, non-activating HUD, single-instance modeless | T06, T07 | MAN-01, MAN-02, MAN-03 |
| FR-01 | `prd.md#functional-requirements` | List all registered providers with name and glyph | T05, T06 | TC-14 |
| FR-02 | `prd.md#functional-requirements` | One toggle per provider, default enabled | T05, T06 | TC-13, TC-14 |
| FR-03 | `prd.md#functional-requirements` | Immediate HUD ring visibility sync | T04 | TC-10 |
| FR-04 | `prd.md#functional-requirements` | Engine gating on ticks and manual refresh; no I/O; re-enable queues a refresh | T02 | TC-05, TC-06, TC-07, TC-08, TC-15 |
| FR-05 | `prd.md#functional-requirements` | Real-time badge, reactive to snapshots, with "Checking..." | T03, T05, T06 | TC-12, TC-13 |
| FR-06 | `prd.md#functional-requirements` | Persist under `"Providers"`, preserving other sections | T01 | TC-01, TC-03, TC-04 |
| FR-07 | `prd.md#functional-requirements` | Startup resolution; missing entries default to enabled | T01, T07 | TC-02; MAN-04 |
| FR-08 | `prd.md#functional-requirements` | Modeless single-instance lifecycle | T06 | MAN-01 (existing `DialogService` behavior preserved) |
| FR-09 | `prd.md#functional-requirements` | Esc / Close dismiss; save on interaction, no Apply button | T05, T06 | TC-13; MAN-01 |
| FR-10 | `prd.md#functional-requirements` | Zero enabled providers is clean, no fallback mock | T04, T07 | TC-11; MAN-04 |
| NFR-01 | `prd.md#non-functional-requirements` | Dark theme brushes, semantic pills, 100/150/200% scaling | T06 | MAN-02 |
| NFR-02 | `prd.md#non-functional-requirements` | Tab order, Space toggle, Esc, automation names, text not color alone | T03, T06 | TC-12 (label non-emptiness); MAN-01 |
| NFR-03 | `prd.md#non-functional-requirements` | HUD `MA_NOACTIVATE` invariant preserved | T07 | MAN-03 |
| NFR-04 | `prd.md#non-functional-requirements` | <50 ms toggle, non-blocking disk write, zero overhead when disabled | T02, T05, T07 | TC-15; MAN-05 |
| NFR-05 | `prd.md#non-functional-requirements` | Core pure; config in Infrastructure; view models in App | T01, T03 | Build + review; Core unchanged |
| NFR-06 | `prd.md#non-functional-requirements` | No credential reads when gated; rate-limit deadlines honored on re-enable | T02, T07 | TC-07, TC-09; MAN-05 |
| US-01 | `prd.md#stories-and-journeys` | Disable unused providers, rings vanish, polling stops, persists | T02, T04, T06, T07 | TC-05, TC-10; MAN-04 |
| US-02 | `prd.md#stories-and-journeys` | Needs-Auth badge diagnoses expired credentials, transitions to OK | T03, T05 | TC-12, TC-13 |
| US-03 | `prd.md#stories-and-journeys` | Rate-Limited badge distinguishes cooldown from failure | T03, T05 | TC-12 |
| US-04 | `prd.md#stories-and-journeys` | Re-enabling dispatches a refresh and restores the ring | T02, T04 | TC-08, TC-10 |
| US-05 | `prd.md#stories-and-journeys` | Restart retains configuration | T01, T07 | TC-01, TC-02; MAN-04 |
| US-06 | `prd.md#stories-and-journeys` | Keyboard-only navigation | T06 | MAN-01 |
| A-01 | `prd.md#explicit-assumptions` | `{"Providers": {"<id>": {"Enabled": bool}}}`, unlisted default true | T01 | TC-02, TC-03 |
| A-02 | `prd.md#explicit-assumptions` | Immediate in-memory apply, no Apply button | T05 | TC-13 |
| A-03 | `prd.md#explicit-assumptions` | "Disabled" visually distinct from an auth or network failure | T03 | TC-12 |
| A-04 | `prd.md#explicit-assumptions` | Neutral "Checking..." until the first snapshot | T03 | TC-12 |
| A-05 | `prd.md#explicit-assumptions` | Empty capsule stays visible and draggable | T04, T07 | TC-11; MAN-04 |
| DEC-01 | `techspec.md#technical-decisions` | `JsonNode` DOM section store mirroring `HudPositionStore` | T01 | TC-01 |
| DEC-02 | `techspec.md#technical-decisions` | Runtime `ProviderId` canonical; `antigravity` read alias | T01 | TC-03 |
| DEC-03 | `techspec.md#technical-decisions` | Gating registry in a `UsageStore.Gating.cs` partial | T02 | TC-05 – TC-09 |
| DEC-04 | `techspec.md#technical-decisions` | Badge states in an App enum, not `ProviderStatus` | T03 | TC-12 |
| DEC-05 | `techspec.md#technical-decisions` | Dictionary + order list, index-preserving re-insert | T04 | TC-10 |
| DEC-06 | `techspec.md#technical-decisions` | Delegate view models, no `ICommand`, engine-first ordering | T05 | TC-13 |
| DEC-07 | `techspec.md#technical-decisions` | Shared per-path gate for `appsettings.json` writers | T01 | TC-01 + `HudPositionStoreTests` regression |
| DEC-08 | `techspec.md#technical-decisions` | Enablement applied before `NotchViewModel` construction | T07 | MAN-04 |
| DEC-09 | `techspec.md#technical-decisions` | Clear activity state on disable, keep the snapshot | T02 | TC-15 |
| R-1 | `techspec.md#prd-reconciliation-decided-with-the-user-this-session` | Rows enumerate live registrations, Copilot included | T05 | TC-14 |
| R-2 | `techspec.md#prd-reconciliation-decided-with-the-user-this-session` | `gemini` canonical, `antigravity` alias | T01 | TC-03 |
| R-3 | `techspec.md#prd-reconciliation-decided-with-the-user-this-session` | `Disabled` absent from Core; `Unsupported` needs a badge | T03 | TC-12 |
| CMP-01 – CMP-04 | `techspec.md#components-and-flow` | Settings record, store, file gate, `HudPositionStore` change | T01 | TC-01 – TC-04 |
| CMP-05, CMP-06 | `techspec.md#components-and-flow` | Gating partial and modified refresh/activity partials | T02 | TC-05 – TC-09, TC-15 |
| CMP-07, CMP-08 | `techspec.md#components-and-flow` | Badge enum and resolver | T03 | TC-12 |
| CMP-09, CMP-10 | `techspec.md#components-and-flow` | Row and dialog view models | T05 | TC-13, TC-14 |
| CMP-11 | `techspec.md#components-and-flow` | `NotchViewModel` ring filtering | T04 | TC-10, TC-11 |
| CMP-12 – CMP-14 | `techspec.md#components-and-flow` | Window markup, dialog resources, dialog service factory | T06 | MAN-01, MAN-02 |
| CMP-15 | `techspec.md#components-and-flow` | `App.xaml.cs` startup wiring | T07 | MAN-03 – MAN-05 |
| CMP-16 | `techspec.md#components-and-flow` | Test-project compile links | T03, T05 | Build |
| TC-01 – TC-04 | `techspec.md#test-approach` | Persistence: preservation, defaults, alias, corruption | T01 | `ProviderSettingsStoreTests` |
| TC-05 – TC-09 | `techspec.md#test-approach` | Gating both refresh paths, activity, re-enable, rate limit | T02 | `UsageStoreGatingTests` |
| TC-10, TC-11 | `techspec.md#test-approach` | Ring removal/restore and all-disabled state | T04 | `NotchViewModelTests` |
| TC-12 | `techspec.md#test-approach` | Badge resolution across every state | T03 | `ProviderBadgeResolverTests` |
| TC-13, TC-14 | `techspec.md#test-approach` | Toggle chain and row enumeration | T05 | `SettingsViewModelTests` |
| TC-15 | `techspec.md#test-approach` | Activity state cleared on disable | T02 | `UsageStoreGatingTests` |
| MAN-01, MAN-02 | `techspec.md#test-approach` | Keyboard and visual acceptance | T06 | Manual script, owner Douglas Cunha |
| MAN-03 – MAN-05 | `techspec.md#test-approach` | Focus invariant, restart persistence, gating overhead | T07 | Manual script, owner Douglas Cunha |

## Tasks

- [T01 — Persist provider enablement in `appsettings.json` without losing sibling sections](done/task_01.md): the `"Providers"` section round-trips with `Hud`, `Refresh`, and `Log` intact, and every gap defaults to enabled.
- [T02 — Gate background polling and activity monitoring per provider in `UsageStore`](done/task_02.md): disabled providers get no fetch, no activity poll, and no I/O; re-enabling dispatches one rate-limit-aware refresh.
- [T03 — Resolve provider status into a presentation badge without touching Core](done/task_03.md): every status, plus `Disabled` and `Checking…`, maps to a state and a visible label with `ProviderStatus` unchanged.
- [T04 — Show HUD rings only for monitored providers, restoring them in place](done/task_04.md): rings follow enablement immediately and return to their original index.
- [T05 — Build the Settings view models that list providers and apply toggles live](done/task_05.md): every registered provider becomes a row whose toggle drives engine, badge, and disk with no Apply step.
- [T06 — Give the Settings dialog a real body: provider rows, badge pills, and keyboard access](done/task_06.md): the empty shell becomes a usable, keyboard-navigable dark-mode dialog.
- [T07 — Apply stored enablement at startup and close end-to-end acceptance](done/task_07.md): stored preferences take effect before the HUD is built, and the manual script closes what tests cannot reach.

## Coverage gate

- Coverage: **pass**. All 5 objectives, 10 functional requirements, 6 non-functional requirements, 6 stories, 5 assumptions, 9 decisions, 16 components, 15 test cases, and 5 manual steps map to a task. Out-of-scope limits from `prd.md#out-of-scope` are respected and unplanned: cadence and retry configuration (owned by `prd-settings-02-cadence-and-retries`), in-app login flows, new provider types, HUD geometry customization, and system tray integration.
- Traceability: **pass**. Every task carries source IDs and sections; every matrix row names concrete evidence. No obligation resolves only to "review".
- Dependencies: **pass**. Acyclic, three parallel roots, one sink. File collisions are serialized: the only file touched by two tasks is `TokenHound.Infrastructure.Tests.csproj` (T03 then T05, already ordered by dependency).
- Atomicity: **pass with one noted exception**. T01 – T05 are vertical slices carrying implementation and tests together. T06 is markup and window lifecycle with no automatable test under the desktop policy — its acceptance is MAN-01 and MAN-02, recorded rather than waived. T01, T02, and T03 are foundations, each justified by unlocking two or more deliveries.
- Executability: **pass**. Every command is the repository's real runner, taken from `AGENTS.md` and confirmed against `global.json` (`test.runner: Microsoft.Testing.Platform`, SDK 10.0.400) — native MTP via `dotnet test --project ... -- --minimum-expected-tests 1`, never VSTest `--filter` or `--logger`.
- Validation profile: **pass**. `TokenHound.App` is `net10.0-windows` with `UseWPF` and `WinExe`, so **E2E is omitted by .NET desktop policy** — no full-application launch test, no WPF UI automation, and no aggregate suite that would trigger one; existing tests are left in place. Both test projects are `net10.0` with `UseMicrosoftTestingPlatformRunner` and xunit.v3. App view models stay WPF-free so they compile into `TokenHound.Infrastructure.Tests` via the existing `Compile Include Link` pattern. Environment: Windows 11 and SDK 10.0.400+ for all automated work, with no network, credentials, or live provider required. MAN-01 – MAN-05 additionally need a real desktop session and Windows MCP; authorization exists, the owner runs them locally. Preserved gap: NSubstitute proves the *dispatch decision*, not that a real adapter opens no socket or SQLite handle — NFR-04 and NFR-06 close on MAN-05, not on a unit test.
- Idempotency: **pass**. Persistence is a full-map overwrite of one section; `SetProviderEnabled` is idempotent and raises only on change; re-running any task's tests has no external side effect beyond a temp directory that its fixture deletes.

## Assumptions and open items

- Assumption: the two reconciliations decided with the user this session hold — Settings enumerates registered providers dynamically (Copilot included, R-1), and `gemini` is the canonical persisted key with `antigravity` accepted as a read alias (R-2). If either is revisited, T05/TC-14 and T01/TC-03 are the affected derivatives.
- Assumption: no Apply or Save button, per A-02. Every toggle writes on interaction.
- Open item — **OPEN-01**: PRD FR-05 enumerates six badge states and omits `ProviderStatus.Unsupported`, which Core actually produces. The recorded default is a seventh pill labeled "No Quota" in the neutral slate treatment. Affects T03 (work item T03.3) and TC-12. Decision owner: Douglas Cunha. **Not blocking** — the default is implemented, and a wording change touches one string and one assertion.
- Open item — **OPEN-02**: the PRD sizes the dialog for four providers (~460×380 DIPs); five rows need roughly 420 DIPs of height. The recorded default raises `Height`/`MinHeight` and keeps `ResizeMode="NoResize"`; a `ScrollViewer` for provider #7 and beyond is deferred, not designed. Affects T06 (work item T06.4) and NFR-01. Decision owner: Douglas Cunha. **Not blocking**.
- Required environment: MAN-01 and MAN-02 (T06) and MAN-03 – MAN-05 (T07) — obligations NFR-01, NFR-02, NFR-03, NFR-04, NFR-06, OBJ-04, OBJ-05 — need a real Windows 11 desktop session with Windows MCP (`App` with `mode="launch_executable"`; `Start-Process` renders to an isolated desktop) and at least one authenticated provider to observe a non-`Checking` badge. Authorization exists; the owner runs these locally. Until executed, those obligations remain pending acceptance.

## State

- [x] T01 — done
- [x] T02 — done
- [x] T03 — done
- [x] T04 — done
- [x] T05 — done
- [x] T06 — done (code); MAN-01 pass, MAN-02 partial — see note
- [x] T07 — done (code + MAN-01, MAN-03, MAN-04 pass; MAN-02, MAN-05 partial)

## Problems and solutions

- T01, caught in review: `ProviderSettingsStore.ApplyStates` removed the `"antigravity"` alias key unconditionally, before writing any canonical value, so a save whose map carried no `gemini` id deleted a stored `"antigravity": { "Enabled": false }` and left nothing in its place — the preference silently reverted to *enabled* on the next `Load`, contradicting the acceptance criterion that file keys unknown to the build survive a save. DEC-02 authorizes removing the alias as part of the canonical **rewrite**, not as an unconditional delete. Fixed by moving the removal inside `WriteEnabled`, guarded so it runs only in the same operation that emits `"gemini"`; pinned by `SaveAsync_WhenCanonicalKeyIsNotSupplied_KeepsStoredAliasPreference`, which asserts both the loaded value and the raw file and fails against the old code.
- T02, verified in review rather than assumed: `IsProviderEnabled` reports `true` for any id absent from `_providers`, and `SetProviderEnabled` no-ops for an unregistered id, so a monitor whose `ProviderId` has no matching `IUsageProvider` can never be gated. Confirmed harmless in production: all five registered pairs share one id (`claude`, `gemini` for both `AntigravityUsageProvider` and `AntigravityActivityMonitor`, `codex`, `cursor`, `copilot`), so every monitor is gateable. The gap is reachable only from tests that register a monitor without a provider, as `UsageStoreActivityTests` does. FR-04 and NFR-06 are unaffected.
- T03 -> T06 handoff detail: `ProviderBadgeResolver` names its brush resource keys `Badge{Ok,Stale,NeedsAuth,RateLimited,AccessDenied,Checking,Disabled}{Background,Foreground}Brush`. `Unsupported` deliberately reuses the `Stale` slate pair per OPEN-01, so `DialogResources.xaml` must define **14** brushes, not 16. T06 must match these key names exactly or the pills silently fall back to the default style.
- T04, caught in review: a UTF-8 BOM was introduced into `NotchViewModel.cs` and `NotchViewModelTests.cs`, which had none at `HEAD` and were the only BOM-carrying files in their folders. Cause: writing C# source through Python with the `utf-8-sig` encoding. It showed up as phantom line-1 churn in the diff. Both files rewritten as BOM-less UTF-8; verify with `head -c 3 <file> | xxd -p` returning `757369`. **Executors editing `.cs` files through a script must write plain `utf-8`, never `utf-8-sig`.**
- T04, a review finding I raised and then withdrew: I reported `using System.Linq;` as dead in `NotchViewModel.cs`. It is not — three call sites remain (`FirstOrDefault` at line 120, `Any` and `FirstOrDefault` in `ShouldFallbackToMock`); only one of four LINQ uses was replaced. The directive is redundant only because `ImplicitUsings` is enabled repo-wide, which was already true at `HEAD`, and all four LINQ-using files in that folder declare it explicitly. Left in place. An `ImplicitUsings` cleanup would be repo-wide and belongs outside this feature.
- T05, reviewed and accepted rather than flagged: `OnMonitoringChanged` evaluates `BuildSettings()` synchronously and then dispatches an untracked `PersistAsync`. Two toggles within milliseconds could in principle have their continuations reordered, letting an older map win the last write. Not treated as a defect: the maps are always full engine-state snapshots, `SettingsFileGate` serializes the writes, toggles are human-paced, and `techspec.md#errors-security-and-recovery` already records this case as safe. Revisit only if a non-interactive caller ever drives toggles programmatically.
- T05 local decision, verified not invented: the two untracked continuations use `.ConfigureAwait(false)` in the App layer, which `AGENTS.md` normally reserves for Core and Infrastructure. Confirmed precedent at `src/TokenHound.App/ApplicationLifetime.cs:116-153`. These continuations only log and must not marshal back to the UI thread (NFR-04).
- **OPEN-03 (new, raised in T06 review) — `Disabled` pill contrast fails WCAG AA.** The PRD prescribes `#757575` text on a `#1AFFFFFF` pill; composited over `SurfaceCardBackgroundColor` `#121214` that is `#2A2A2C`, giving **3.11:1** — below the 4.5:1 normal-text threshold, though it passes the 3.0:1 large-text one. Measured independently with the WCAG checker, matching the executor's figure. It is the only one of the seven pairs below AA; the rest land between 5.79:1 and 13.58:1. `#9A9A9A` reaches **5.09:1** and keeps the muted read. The PRD hex is implemented verbatim and was **not** silently adjusted. Decision owner: Douglas Cunha. Affects NFR-01, NFR-02, CMP-13. **Not blocking** — the badge always renders its state as text, so NFR-02's text-not-color-alone rule holds regardless.
- **T06 manual acceptance is structurally deferred to after T07.** MAN-01 (keyboard) and MAN-02 (dark mode, 100/150/200% scaling) belong to T06, but until T07.3 passes the `Func<SettingsViewModel>` factory into `DialogService`, the dialog opens with a null `DataContext` and an empty card — so neither script can be executed meaningfully. T06 is recorded done for its code deliverable only; NFR-01, NFR-02, and OBJ-05 acceptance stays OPEN and is closed by the feature-level manual pass run after T07, together with MAN-03 – MAN-05. No manual step has been executed yet.
- T06 ADR candidate pending promotion after QA: **T06-ADR-01**, resolving view-model resource keys in the view through a single `IValueConverter` (`src/TokenHound.App/UI/Converters/ResourceKeyConverter.cs`), keeping brush and geometry lookups out of the WPF-free view models. Full context is in `done/task_06.md`.
- T06 -> T07 handoff detail: `DialogService.ShowSettings(Window? owner = null, Func<SettingsViewModel>? viewModelFactory = null)` takes the factory as the **second, optional** parameter, deliberately, so the existing call at `App.xaml.cs:168` kept compiling while `App.xaml.cs` was outside T06's write scope. **T07 must actually pass the factory** — omitting it fails silently with an empty dialog rather than with a build error.
- **Manual acceptance executed by the orchestrator on 2026-09-08**, primary display 3440x1440 @ 100%, worktree Debug build launched via Windows MCP. Full evidence table in `done/task_07.md`. **MAN-01, MAN-03 and MAN-04 pass. MAN-02 and MAN-05 are PARTIAL and remain open:** MAN-02 verified dark mode and legibility only at 100% scaling — 125/150/200% were not exercised because changing the owner's display scaling is invasive; MAN-05 saw no fetch, fault or credential entry for the disabled providers, but over a ~3-minute window rather than the two full 180 s polling cycles the script specifies. NFR-01, NFR-04 and NFR-06 are therefore supported by evidence but not fully closed. Also not directly observed: DEC-08's *no-flash* guarantee — the post-restart end state is correct and the code ordering is right, but a transient flash during window construction would not be caught by a post-hoc screenshot.
- Environment note for anyone repeating the manual script: a second TokenHound instance (the owner's installed build at `D:\Apps\TokenHound`) runs permanently on this machine. Identify the worktree instance by process path and window rect before interacting — both HUD capsules look identical and clicking the wrong one edits the owner's real `appsettings.json`.
- T01 environment note: this worktree had no `project.assets.json`, so the task's `--no-restore` build failed once with `NETSDK1004`. A one-time `dotnet restore` of the test project resolved it; the task's literal commands then ran unchanged. Later tasks in this feature can assume the worktree is restored.
