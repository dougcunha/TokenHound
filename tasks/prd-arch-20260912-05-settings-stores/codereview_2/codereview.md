# Code review report: Refactoring settings stores onto one section store

## Summary

- Status: APPROVED
- Git scope: implementation commit `633dd21` against base `1deb22886e61a43313309764c300c05a0711fe22`, plus the current PRD and TechSpec documentation overlay
- Previous review: `tasks/prd-arch-20260912-05-settings-stores/codereview_1/codereview.md`
- Review result: 0 findings, 0 optional improvements; all four previous findings resolved

## Sources and scope

| Source | Path or reference | State |
| --- | --- | --- |
| PRD | `tasks/prd-arch-20260912-05-settings-stores/prd.md` | read; acceptance evidence reconciled and re-read |
| TechSpec | `tasks/prd-arch-20260912-05-settings-stores/techspec.md` | read; decisions, safety net, profile, and baseline checked and re-read after clarification |
| Manifest | `tasks/prd-arch-20260912-05-settings-stores/tasks.md` | read; T01 and T02 links, dependency, state, and correction record checked |
| T01 handoff | `tasks/prd-arch-20260912-05-settings-stores/done/task_01.md` | read; work, acceptance, affected files, corrections, and validation evidence checked |
| T02 handoff | `tasks/prd-arch-20260912-05-settings-stores/done/task_02.md` | read; work, acceptance, affected files, corrections, and validation evidence checked |
| Previous review | `tasks/prd-arch-20260912-05-settings-stores/codereview_1/codereview.md` | read; CR-01 through CR-04 traced individually |
| Implementation | `1deb228..633dd21` | delimited; five configuration sources and one provider settings test file reviewed |

The implementation commit changes these reviewable code paths:

- `src/TokenHound.Infrastructure/Configuration/SectionStore.cs`
- `src/TokenHound.Infrastructure/Configuration/HudPositionStore.cs`
- `src/TokenHound.Infrastructure/Configuration/ProviderSettingsStore.cs`
- `src/TokenHound.Infrastructure/Configuration/RateLimitSettingsStore.cs`
- `src/TokenHound.Infrastructure/Configuration/RefreshSettingsStore.cs`
- `tests/TokenHound.Infrastructure.Tests/Configuration/ProviderSettingsStoreTests.cs`

No later source change exists between `633dd21` and the reviewed worktree. The unrelated user modification to `opencode.json` is excluded.

## Coverage matrix

| Source | Obligation | Implementation | Test | State | Evidence |
| --- | --- | --- | --- | --- | --- |
| R-01 | Preserve Hud store operations and `FilePath` | `HudPositionStore` delegates to `SectionStore<HudPositionSettings>` | `HudPositionStoreTests` | conformant | Exact constructors remain; load, save, path, and `Hud` section delegates are present; current suite passes. |
| R-02 | Preserve Provider behavior and converter | `ProviderSettingsStore`; cloned shared options; whole-document `FromJson` | `ProviderSettingsStoreTests` | conformant | Both `Providers` and `providers` regression inputs disable Claude; converter and merge paths remain. |
| R-03 | Preserve RateLimit behavior | `RateLimitSettingsStore`; `Clamp` read/write delegates | `RateLimitSettingsStoreTests` | conformant | Load, parse, save, and async paths retain the minimum-floor clamp; current suite passes. |
| R-04 | Preserve Refresh behavior | `RefreshSettingsStore` delegates to shared store | `RefreshSettingsStoreTests` | conformant | Defaults, parse, persistence, and async operations retain their section contract. |
| R-05 | Preserve absent and whitespace `FromJson` behavior | `SectionStore.FromJson` and facade wrappers | Store tests plus code inspection | conformant | Whitespace and absent sections return new payload instances; Provider retains its all-enabled default. |
| R-06 | Preserve sibling sections through the write coordinator | `SectionStore.Save` and `SaveAsync` call `UserSettingsFile.Update` and `UpdateAsync` | `UserSettingsFileTests`; `UserSettingsFileConcurrencyTests` | conformant | No independent writer was introduced; full suite passes. |
| R-07 | Preserve constructor path resolution | Four exact facade constructor surfaces; `SectionStore.ResolveSettingsFile` | Constructor and path tests | conformant | Source diff matches the former `()`, `(UserSettingsFile)`, `(string? filePath, string? baseDirectory = null)` declarations. |
| PRD writer constraint | Keep `UserSettingsFile` as the only write coordinator | `SectionStore` owns the shared update delegation | Settings and concurrency tests | conformant | Writes still enter through `UserSettingsFile`; its I/O semantics are unchanged. |
| PRD API constraint | Keep all four public APIs unchanged | Four facade declarations | Build and base diff | conformant | Constructors, public methods, return types, defaults, and XML documentation remain. |
| PRD disk constraint | Preserve section names and JSON shape | `Hud`, `Providers`, `RateLimit`, and `Refresh` are supplied to the shared store | Round-trip tests | conformant | Existing section records and `UserSettingsFile` serialization remain the disk boundary. |
| PRD acceptance 1 | Check R-01 through R-07 | This matrix | Current and reused evidence | conformant | Every requirement has implementation and verification evidence. |
| PRD acceptance 2 | Introduce no behavior silently | Bounded source diff and previous-finding trace | Current suite and code inspection | conformant | The prior casing and API regressions are corrected; no new blocking hit was found. |
| PRD acceptance 3 | Settings and concurrency tests pass with a nonzero guard | Infrastructure test project | Current full MTP run | conformant | 678 passed, 0 failed, `--minimum-expected-tests 1`, exit 0. |
| PRD acceptance 4 | Collapse duplicated options and path blocks | `SectionStore<T>` | QA-01 through QA-03 | conformant | Counts are 2/1/1 against baselines 8/4/4 and meet all targets. |
| DEC-01 | Shared store owns section name, settings file, options, and operations | `SectionStore<T>` | Code inspection and QA profile | conformant | `_sectionName`, `_settingsFile`, shared options, parse, load, save, and path resolution are centralized. |
| DEC-02 | Four public facades retain identical signatures | Four settings stores | Build and base diff | conformant | Exact constructor declarations are restored and facades delegate by composition. |
| DEC-03 | Reuse existing path policy and default filename | `SectionStore.CreateSettingsFile` | QA-02 and path tests | conformant | One call reaches `SettingsPathResolver.ResolveOverride` with `appsettings.json`. |
| DEC-04 | Preserve Provider converter and case-insensitive root matching | Provider clones shared options and adds `ProviderSettingsJsonConverter` | Direct Provider `FromJson` regression tests | conformant | Whole-document deserialization preserves root casing behavior without mutating shared options. |
| QA-01 | Reach two `AllowTrailingCommas = true` hits | `SectionStore.cs`; pre-existing `UserSettingsFile.cs` | Exact current and baseline searches | conformant | Baseline 8, current 2, target 2. |
| QA-02 | Reach one path-resolution call | `SectionStore.CreateSettingsFile` | Exact current and baseline searches | conformant | Baseline 4, current 1, target 1. |
| QA-03 | Reach one `CreateSettingsFile` helper | `SectionStore.CreateSettingsFile` | Exact current and baseline searches | conformant | Baseline 4, current 1, target 1. |
| TC-01 | Hud load, save, and override scenarios | Hud facade and shared store | `HudPositionStoreTests` | conformant | Reused focused 10/10 evidence; current full suite passes. |
| TC-02 | Provider converter and settings behavior | Provider facade and whole-document parser | `ProviderSettingsStoreTests` | conformant | Reused focused 22/22 evidence includes three CR-01 regressions; current full suite passes. |
| TC-03 | RateLimit and Refresh scenarios | Both facades and shared store | Both store test classes | conformant | Reused focused 17/17 and 10/10 evidence; current full suite passes. |
| TC-04 | Preserve sibling sections under concurrency | Shared update delegation | User settings and concurrency classes | conformant | Reused focused 9/9 and 3/3 evidence; current full suite passes. |
| T01.1 | Add shared store with section ownership and operations | `SectionStore<T>` | Build and inspection | conformant | Current class implements every named operation and owns `_sectionName`. |
| T01.2 | Migrate Hud with exact public surface | `HudPositionStore` | Hud tests | conformant | Thin facade and exact constructors verified. |
| T01.3 | Migrate Refresh | `RefreshSettingsStore` | Refresh tests | conformant | Thin facade and behavior verified. |
| T01.4 | Remove Hud and Refresh duplication | Both facades | QA profile | conformant | Target patterns are absent from both facades. |
| T02.1 | Migrate RateLimit | `RateLimitSettingsStore` | RateLimit tests | conformant | Thin facade retains clamp behavior. |
| T02.2 | Migrate Provider and preserve converter behavior | `ProviderSettingsStore` | Provider tests | conformant | CR-01 regression coverage and whole-document parse path are present. |
| T02.3 | Remove RateLimit and Provider duplication | Both facades | QA profile | conformant | Target patterns are absent from both facades. |

## Compliance with rules and skills

| Rule or skill | State | Evidence |
| --- | --- | --- |
| Repository architecture and writer boundary | OK | Core is untouched; `UserSettingsFile` remains the sole writer. |
| Repository C# structure and public documentation | OK | One class per file, all changed classes are below 300 lines, methods are at or below 30 lines, and public facade members retain XML documentation. |
| Repository async rules | OK | Infrastructure awaits use `ConfigureAwait(false)`; facade pass-through methods return tasks directly and propagate cancellation. |
| Repository four-argument call formatting | OK | All `SectionStore<T>` calls place one argument per line and the closing parenthesis on its own line; scoped diff check passes. |
| `dotnet-efficient-validation` | OK | Native MTP established by `global.json`, project configuration, SDK 10.0.401, and guarded execution. |
| `repository-cli-efficiency` | OK | Inspection was scoped; diff stat and names preceded the full feature diff; profile patterns were combined by purpose. |
| `no-workarounds` | OK | The implementation restores root deserialization and exact constructor contracts; it introduces no suppression, retry, fallback chain, or duplicated workaround. |
| `sdd-review-code` | OK | Sources, links, tasks, handoffs, base, obligations, profile, previous findings, and validations are recorded here. |
| Desktop E2E policy | N/A | E2E omitted as required; the TechSpec requires no manual acceptance. |

## Quality profile

| ID | Rule | Class | Command | Hits | State |
| --- | --- | --- | --- | --- | --- |
| QA-01 | Shared JSON options literals | blocking | `rtk rg -n "AllowTrailingCommas = true" src/TokenHound.Infrastructure/Configuration` | 0 aggravated of 2 total | OK |
| QA-02 | Shared path-resolution call | blocking | `rtk rg -n "SettingsPathResolver\.ResolveOverride" src/TokenHound.Infrastructure/Configuration` | 0 aggravated of 1 total | OK |
| QA-03 | Shared settings-file helper | blocking | `rtk rg -n "private static UserSettingsFile CreateSettingsFile" src/TokenHound.Infrastructure/Configuration` | 0 aggravated of 1 total | OK |

- Terrain baseline: verified at `1deb228` as 8/4/4 and applied.
- Hits discounted by baseline: one surviving QA-01 hit in the unchanged `UserSettingsFile.cs`; the other current hits are the shared replacements required by DEC-01 and DEC-03.
- Reservations accumulated in the feature: 0.
- Suggested escalation: no trigger fired.

## TechSpec adherence

| Decision or contract | State | Evidence |
| --- | --- | --- |
| DEC-01 shared store structure | YES | `SectionStore<T>` owns the section name, settings file, shared serializer options, and all specified operations. |
| DEC-02 facade compatibility | YES | The base diff proves exact constructor and public method declarations; composition is used. |
| DEC-03 path resolution | YES | One helper calls the unchanged resolver with the existing default filename. |
| DEC-04 Provider converter path | YES | A cloned options instance carries the converter and whole-document parsing preserves root casing. |
| Compatibility: on-disk representation | YES | Section names and `UserSettingsFile` serialization are unchanged. |
| Compatibility: public behavior | YES | The prior casing regression has direct coverage; all named store behavior passes. |

## Verified tasks

| Task | Location | State | Handoff and evidence |
| --- | --- | --- | --- |
| T01 | `done/task_01.md` | COMPLETE | Work and acceptance are checked; correction handoff proves DEC-01, constructor restoration, formatting, focused tests, and intermediate profile. |
| T02 | `done/task_02.md` | COMPLETE | Work and acceptance are checked; correction handoff proves Provider casing, converter behavior, final profile, focused tests, and integration run. |

The manifest marks both tasks done, T02 depends on T01, both links resolve, and no task or obligation is orphaned.

## Executed validations

- Profile and exclusions: .NET 10, native Microsoft.Testing.Platform; desktop E2E omitted by policy.
- Validated state: implementation `1deb228..633dd21`, current PRD and TechSpec, SDK 10.0.401, Windows/PowerShell environment.
- Reused evidence: focused handoff runs are reusable because the reviewed source and project configuration are unchanged since `633dd21`; the current build and full suite independently validate that same state.
- Manual acceptance: none required; on-disk shape and section preservation are automated.

| Command | Result | Obligations covered |
| --- | --- | --- |
| `rtk dotnet --version` | passed; `10.0.401` | validation environment |
| `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-restore --nologo --verbosity:minimal` | passed; 3 projects, 0 errors, 0 warnings | compile and API compatibility |
| `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1` | passed; 678 passed, 0 failed | R-01 through R-07; TC-01 through TC-04 |
| QA-01 exact current command | passed; 2 hits | QA-01 |
| QA-02 exact current command | passed; 1 hit | QA-02 |
| QA-03 exact current command | passed; 1 hit | QA-03 |
| Equivalent `rtk git grep` commands at `1deb228` | passed; baselines 8/4/4 reproduced | Terrain baseline |
| `rtk git diff --check 1deb228 633dd21 -- <reviewed code paths>` | passed | source formatting |
| `rtk rg -n "[ \t]+$" src/TokenHound.Infrastructure/Configuration/SectionStore.cs` | passed by no matches; `rg` exit 1 | new-file whitespace |

## Findings

None.

## Optional improvements

None.

## Previous findings

| Review/ID | State | Current evidence |
| --- | --- | --- |
| `codereview_1/CR-01` | resolved | `ProviderSettingsStore.FromJson` deserializes `UserSettings` with case-insensitive options; exact and lower-case root tests pass. |
| `codereview_1/CR-02` | resolved | All four facades restore `(string? filePath, string? baseDirectory = null)` and remove the extra one-argument overload; QA-02 now measures the shared resolver call. |
| `codereview_1/CR-03` | resolved | `SectionStore<T>` stores `_sectionName` and uses it in instance `FromJson`; DEC-01 now describes the implemented parser contract precisely. |
| `codereview_1/CR-04` | resolved | Four-argument calls follow repository formatting and scoped diff validation passes. |

## Limitations and open items

- The later task-document reorganization is outside the implementation commit, but all current PRD links, task locations, states, IDs, and handoffs were checked as the documentation overlay.
- The unrelated worktree modification to `opencode.json` was not reviewed or changed.
- E2E was omitted under the desktop .NET policy. The TechSpec defines no essential manual acceptance, so no obligation remains unverifiable.

## Conclusion

The settings-store refactor conforms to R-01 through R-07, DEC-01 through DEC-04, TC-01 through TC-04, and QA-01 through QA-03. Both tasks are complete, every current link and state is consistent, all four prior findings are resolved, the implementation is bounded to `1deb228..633dd21`, the build is clean, and the current Infrastructure suite passes 678 tests with nonzero-test enforcement. No new or aggravated blocking hit and no reservation remain. Status: **APPROVED**.
