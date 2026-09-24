# Stable execution context

Load in this exact order:

1. `tasks/prd-08-claude-quota-breakdown/codereview_1/codereview.md`
2. This file

Use the already loaded report version; recover relevant contracts and code afterward. This order does not guarantee a host cache hit.

---

# T03 — Keep unrecognized Claude quota labels neutral

## Outcome

A Claude quota window whose name is not canonical (`five_hour`, `seven_day`), `*_scoped`, or `seven_day_*`/`weekly_*` gets a neutral humanized label. It can never receive the "Current session (5h)" or "Weekly limit (7d)" base labels, however its name is spelled.

## Dependencies and boundaries

- Depends on: T01, T02 (done).
- Unblocks: re-review of the feature in an independent session.
- In scope: the Claude branch of `ProviderUsageRowFactory.ResolveQuotaWindowLabel` and one focused row test.
- Out of scope: non-Claude label rules, mapper canonicalization, ring selection, the pre-existing `ActiveBlock` reset fallback (report limitation), and OI-02 (accepted under DEC-11).

## Traceability

| Source | Section | Finding covered |
| --- | --- | --- |
| codereview_1/OI-01 | `codereview.md#optional-improvements` | Unrecognized Claude kind falls through to the generic session/weekly heuristic |
| FR-02, NFR-04 | `prd.md#functional-requirements` | Distinct labels; neutral label for an unfamiliar scope |
| DEC-05 | `techspec.md#technical-decisions` | Claude labels resolved from canonical identity before generic rules |

## Requirements

- For Claude providers (`claude`, `claude-<slug>`), a non-empty window name that `ResolveClaudeLabel` does not recognize is labeled by humanizing the reported identifier, for example `session_opus` → "Session opus". The generic `session`/`five`/`week`/`seven` heuristic must not apply to Claude.
- Canonical, scoped, and `seven_day_*`/`weekly_*` labels stay unchanged; non-Claude providers keep the generic rules unchanged.

## Context to recover on demand

- TechSpec: DEC-05; Contracts (label bullet).
- Rules and skills: `CLAUDE.md` C# style; `dotnet-efficient-validation`; `no-workarounds`.
- Code: `src/TokenHound.App/ViewModels/ProviderUsageRowFactory.cs` `ResolveQuotaWindowLabel`; `src/TokenHound.App/ViewModels/ProviderUsageRowFactory.Claude.cs` `ResolveClaudeLabel`, `Humanize`.

## Work

- [x] T03.1 Make the Claude label path total: an unrecognized Claude name returns its humanized identifier instead of falling through to `ResolveNamedLabel`.
- [x] T03.2 Add a row test in `ClaudeQuotaPresentationTests` with a `session_opus`-style and a `seven`-containing unrecognized name, asserting neutral labels distinct from the base labels. Keep the existing Claude and non-Claude tests green.

## Acceptance criteria

- `session_opus` on a Claude profile is labeled "Session opus", not "Current session (5h)".
- Existing `*ClaudeQuota*`, `*ProviderUsageRowFactory*`, and `ProviderRingViewModelTests` pass unchanged, as does the full Infrastructure.Tests suite.
- Touched files stay ≤ 300 lines, methods ≤ 30 lines; the QA-01/02/03/05 blocking commands have no new hits.

## Verification

- Unit: the new presentation test plus existing row/ring suites.
- Integration: App build (linked partial compiles in WPF).
- E2E: omitted by .NET desktop policy.
- Manual: none added; TC-07 remains the HIL 3 item.
- Environment dependency: none beyond SDK 10.0.401 and existing restore assets.
- Commands: `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-restore --nologo --verbosity:minimal`; `rtk dotnet build src/TokenHound.App/TokenHound.App.csproj --no-restore --nologo --verbosity:minimal`; `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1`; check `$LASTEXITCODE`.
- Expected evidence: 0-warning builds, the new test passing, and the full Infrastructure.Tests suite green with a count one higher than 781.

## Affected files

- Modify: `src/TokenHound.App/ViewModels/ProviderUsageRowFactory.cs` and/or `src/TokenHound.App/ViewModels/ProviderUsageRowFactory.Claude.cs`.
- Modify: `tests/TokenHound.Infrastructure.Tests/ViewModels/ClaudeQuotaPresentationTests.cs`.

## Observability and recovery

- Operational signal: row labels in Claude details.
- Recovery: source rollback of the touched files.

## Handoff

> Updated by `sdd-execute-corrections` during implementation.

- Produced result: For Claude providers, `ResolveQuotaWindowLabel` now returns `ResolveClaudeLabel` unconditionally. That method is non-nullable and humanizes any unrecognized name (for example `session_opus` → "Session opus"), so the generic session/weekly heuristic no longer applies to Claude. Non-Claude rules are unchanged.
- Changed files: `src/TokenHound.App/ViewModels/ProviderUsageRowFactory.cs` (Claude branch), `src/TokenHound.App/ViewModels/ProviderUsageRowFactory.Claude.cs` (`ResolveClaudeLabel` total), `tests/TokenHound.Infrastructure.Tests/ViewModels/ClaudeQuotaPresentationTests.cs` (new `Rows_KeepUnrecognizedKindsNeutral`), `tests/TokenHound.Infrastructure.Tests/ViewModels/ProviderRingViewModelTests.cs` (fixture name, see deviation).
- Deviation from the acceptance criterion "existing tests pass unchanged": `ProviderRingViewModelTests.UpdateFromSnapshot_WhenCalled_PopulatesRowsAndRaisesPropertyChanged` built a `claude` snapshot with a window named `"Session"` and expected "Current session (5h)" through the generic heuristic. After T01, no production path emits that name for Claude: `ClaudeQuotaWindowMapper` canonicalizes `session` to `five_hour`, and `MockUsageProvider` defaults to `mock`. The fixture now uses the canonical `five_hour`, and its expectation and intent are unchanged. Special-casing `"Session"` would reintroduce the heuristic that OI-01 removes.
- Checks (2026-09-23, SDK 10.0.401, native MTP, Debug): Infrastructure.Tests and App builds had 0 errors and 0 warnings. The first full run had 781 passed and 1 failed (the fixture above). After the fixture fix, the full Infrastructure.Tests run passed 782/782 with exit 0, and the focused `*Rows_KeepUnrecognizedKindsNeutral*` test passed 1/1. The App source is unchanged since its build. QA-01/02/03 had no hits in the touched files. QA-05: 176, 65, 163, and 232 lines. `git diff --check` is clean.
- Validated state: uncommitted worktree on base `6064266` plus this correction. Core was untouched, so Core.Tests 92/92 from the review remains valid.
- Open items: none for OI-01. OI-02 was accepted under DEC-11. TC-07 manual acceptance remains the HIL 3 item.
