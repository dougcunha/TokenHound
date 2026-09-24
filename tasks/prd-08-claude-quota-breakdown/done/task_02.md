# Stable execution context

Load in this order:

1. `tasks/prd-08-claude-quota-breakdown/prd.md`
2. `tasks/prd-08-claude-quota-breakdown/techspec.md`
3. This file

Recover only a source missing or changed since the current session. The manifest owns state and dependency links.

---

# T02 — Present Claude quota breakdown

## Outcome

The Claude details list distinguishes the session, overall weekly, named model, and scoped quotas. The ring's session and weekly summaries stay unmeasured when their canonical windows are absent instead of borrowing an additional quota.

## Dependencies and boundaries

- Depends on: T01 approved with integrated evidence.
- Unblocks: HIL 3 manual acceptance and code review.
- In scope: Claude-specific row labels/scope and exact ring summary selection, focused ViewModel tests, desktop acceptance script.
- Out of scope: HUD geometry, colors, animations, new views, and non-Claude provider behavior.

## Traceability

| Source | Section | Obligation covered |
| --- | --- | --- |
| OBJ-01–OBJ-03, US-01–US-04, FR-01–FR-07 | `prd.md#functional-requirements` | Visible breakdown and faithful base summaries. |
| NFR-03, NFR-04, NFR-05 | `prd.md#non-functional-requirements` | Honest quantities, accessible labels, pure Core. |
| DEC-05, CMP-04, CMP-05 | `techspec.md#technical-decisions` | Claude label and ring selection contract. |
| TC-05–TC-07 | `techspec.md#test-approach` | ViewModel and live HUD evidence. |

## Context to recover on demand

- Applicable skills: `sdd-execute-task`, `no-workarounds`, `repository-cli-efficiency`, `dotnet-efficient-validation`; follow TechSpec quality profile.
- Existing code: `ProviderUsageRowFactory.ResolveQuotaWindowLabel` and `ProviderRingViewModel.FindLimitWindow`, with caller spans found by graft; inspect T01's canonical names in its handoff.
- UI source: `docs/design/2026-08-28-usage-notch-design.md` hover detail list; PRD controls the session-first behavior where the older design differs.
- Runner and desktop: native MTP for linked App ViewModels; Windows MCP `App` and `Screenshot` for manual evidence.

## Work

- [x] T02.1 Add Claude-specific labels before generic period rules. Preserve the reported scope and avoid treating a 7-day model quota as the overall weekly limit. Keep the edited methods within the project 30-line limit.
- [x] T02.2 Select exact `five_hour` and `seven_day` names for Claude ring summary, including `claude-<slug>` profiles; keep generic selection unchanged for other providers.
- [x] T02.3 Add focused row and ring tests for named, scoped, 0%, absent base, profile-specific, and non-Claude cases. Confirm no fake secondary amount appears.
- [x] T02.4 Build the test project and App, run focused plus existing row/ring tests, inspect the integrated diff and quality profile, and prepare the manual HUD evidence or record the live-data limitation.

## Acceptance criteria

- Additional Claude windows have distinct readable labels and scope text; 0% is visible, null quotas have no row.
- The five-hour window alone drives Claude's primary ring, and only `seven_day` drives its overall weekly summary. An additional quota cannot substitute for either.
- Percentage-only Claude rows show percent used without a fabricated remaining count or reset time.
- Existing non-Claude label/ring tests continue to pass; no HUD layout or Core dependency changes.

## Verification

- Unit: linked ViewModel tests cover labels, scope, quantities, canonical base selection, and non-Claude compatibility.
- Integration: build the WPF App and infrastructure test project; the provider-to-ViewModel path uses T01's snapshot fixture without opening a desktop during automated tests.
- E2E: omitted by .NET desktop policy.
- Manual: launch `TokenHound.App` through Windows MCP `App` (`mode="launch_executable"`), capture primary monitor with `Screenshot` (`display: [2]`), and inspect the current Claude profile's detail list and ring against TechSpec TC-07. Owner: coordinator; report if the endpoint is rate limited or no optional quota exists then.
- Commands: `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-restore --nologo --verbosity:minimal`; `rtk dotnet build src/TokenHound.App/TokenHound.App.csproj --no-restore --nologo --verbosity:minimal`; `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*ClaudeQuota*"`; targeted existing `ProviderUsageRowFactoryTests` and `ProviderRingViewModelTests` with the same minimum/filter syntax. Preserve and check `$LASTEXITCODE`.
- Environment dependency: SDK 10.0.401, current restore assets or one restore, and a user-visible Windows desktop for manual acceptance.
- Expected evidence: nonzero passing counts, App build, diff/quality findings, and screenshot or an explicit live-data limitation.

## Affected files

- Modify: `src/TokenHound.App/ViewModels/ProviderUsageRowFactory.cs`, `ProviderRingViewModel.cs`.
- Create: focused `ClaudeQuota*` tests under `tests/TokenHound.Infrastructure.Tests/ViewModels/`.

## Observability and recovery

- Operational signal: existing Claude status text, details rows, and ring values.
- Recovery: source rollback of T02 files; no data migration or credential change.

## Handoff

> Updated by `sdd-execute-task` during implementation.

- Produced result: Claude rows (`claude` and `claude-<slug>`) now resolve labels from the canonical T01 names before the generic rules. `five_hour` shows "Current session (5h)" and `seven_day` shows "Weekly limit (7d)". A `seven_day_<x>`/`weekly_<x>` quota shows "<X> weekly limit" (for example "Opus weekly limit"); an unknown suffix is humanized, with no model guessed. `*_scoped` shows "<Kind> scoped limit", and the reported group stays in `ScopeText`. Non-canonical Claude names fall through to the unchanged generic rules. The Claude ring selects `five_hour` and `seven_day` by exact name only; if either is absent, that summary is unmeasured. Non-Claude selection is unchanged. `ResolveQuotaWindowLabel` was split into Copilot, named, and period helpers so it meets the 30-line method limit.
- Changed files: `src/TokenHound.App/ViewModels/ProviderUsageRowFactory.cs` (label split plus shared label constants); new `src/TokenHound.App/ViewModels/ProviderUsageRowFactory.Claude.cs` (a partial, following the existing Copilot/Cline pattern; `IsClaudeProvider`, `FindClaudeBaseWindow`, Claude labels); `src/TokenHound.App/ViewModels/ProviderRingViewModel.cs` (`FindLimitWindow` takes the snapshot and routes Claude to exact selection); `tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj` (links the new partial); new `tests/TokenHound.Infrastructure.Tests/ViewModels/ClaudeQuotaPresentationTests.cs`.
- Checks (2026-09-23, SDK 10.0.401, native MTP, Debug, on top of the T01 worktree): Infrastructure.Tests and `TokenHound.App` builds had 0 errors and 0 warnings. `*ClaudeQuota*` 11/11 (5 provider + 6 presentation), `*ProviderUsageRowFactory*` 23/23, `ProviderRingViewModelTests` 19/19, full Infrastructure.Tests 781/781. Every run used `--minimum-expected-tests 1` and exited 0.
- Acceptance evidence: distinct labels, scope, visible 0%, no fake secondary amount or reset, for the default and a named profile: `Rows_LabelEveryClaudeQuotaDistinctly`. Unknown suffix without a model guess: `Rows_HumanizeUnknownSuffixWithoutGuessingAModel`. Absent base windows stay unmeasured: `Ring_WithOnlyAdditionalQuotas_LeavesBaseSummariesUnmeasured`. Exact canonical selection with an additional quota listed first: `Ring_SelectsExactCanonicalWindows`. Unchanged non-Claude behavior: `NonClaudeProvider_KeepsGenericSelectionAndLabels` plus the existing row and ring suites. The presentation tests use snapshots with T01's mapper names, which T01's provider tests prove; no automated test opens a desktop.
- Quality profile (T02 files): QA-01/02/03 have no hits. QA-05: `ProviderRingViewModel.cs` 287 (was 282), row factory 175, Claude partial 65, tests 145, all at most 300. The edited methods are at most 30 lines (`FindLimitWindow` is 29). QA-04 reservation: `ClaudeQuotaPresentationTests.cs:15` `DateTimeOffset` constructor (a BCL call, same pattern as the existing tests). No new public members; `IsClaudeProvider`/`FindClaudeBaseWindow` are internal.
- Validated state: no HUD geometry, style, or Core change in T02, and no change to non-Claude behavior.
- Open items: **TC-07 manual HUD acceptance is pending.** The Windows MCP `App`/`Screenshot` tools are not available in this session, and a shell launch renders on an isolated desktop (`CLAUDE.md`). Run the TechSpec TC-07 script at HIL 3 with the App built from this state.

### ADR candidates

None - direct TechSpec implementation (DEC-05) and a local placement decision (Claude partial file).
