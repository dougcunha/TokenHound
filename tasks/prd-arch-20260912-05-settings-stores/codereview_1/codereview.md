# Code review report: Refactoring settings stores onto one section store

## Summary

- Status: REJECTED
- Git scope: Not delimited. No `--base` was supplied. The review uses handoffs plus the current worktree over `312a0b32c8862321464f2c506a73d1e155f9b04b`.
- Previous review: none
- Review result: 4 findings, 0 optional improvements

## Sources and scope

| Source | Path or reference | State |
| --- | --- | --- |
| PRD | `tasks/prd-arch-20260912-05-settings-stores/prd.md` | read; links and IDs checked |
| TechSpec | `tasks/prd-arch-20260912-05-settings-stores/techspec.md` | read; links, decisions, tests, profile, and baseline checked |
| Manifest | `tasks/prd-arch-20260912-05-settings-stores/tasks.md` | read; T01 and T02 point to existing handoffs and are marked done |
| T01 handoff | `tasks/prd-arch-20260912-05-settings-stores/done/task_01.md` | read; implementation and validation claims checked |
| T02 handoff | `tasks/prd-arch-20260912-05-settings-stores/done/task_02.md` | read; implementation and validation claims checked |
| Implementation | Four modified facades plus new `SectionStore.cs` | limited but reviewable from handoffs and current worktree |

The implementation set is:

- `src/TokenHound.Infrastructure/Configuration/SectionStore.cs`
- `src/TokenHound.Infrastructure/Configuration/HudPositionStore.cs`
- `src/TokenHound.Infrastructure/Configuration/ProviderSettingsStore.cs`
- `src/TokenHound.Infrastructure/Configuration/RateLimitSettingsStore.cs`
- `src/TokenHound.Infrastructure/Configuration/RefreshSettingsStore.cs`

The task documents are split between staged, unstaged, and untracked states. Their current worktree overlay is internally linked. Unrelated staged provider refactors and other PRD moves are outside this review.

## Coverage matrix

| Source | Obligation | Implementation | Test | State | Evidence |
| --- | --- | --- | --- | --- | --- |
| R-01 | Preserve Hud store behavior and `FilePath` | `HudPositionStore`; `SectionStore` | `HudPositionStoreTests` | conformant | Delegation preserves read, write, path, and section name; full project run passed. |
| R-02 | Preserve Provider store behavior, including converter behavior | `ProviderSettingsStore.FromJson`; `SectionStore.FromJson` | `ProviderSettingsStoreTests` | non-conformant | CR-01: root property matching changed from case-insensitive binding to exact lookup, and no provider test calls the public `FromJson`. |
| R-03 | Preserve RateLimit store behavior | `RateLimitSettingsStore`; clamped read/write delegates | `RateLimitSettingsStoreTests` | conformant | Clamp remains on read, parse, and write paths; full project run passed. |
| R-04 | Preserve Refresh store behavior | `RefreshSettingsStore`; `SectionStore` | `RefreshSettingsStoreTests` | conformant | Delegation preserves defaults, read, write, and section name; full project run passed. |
| R-05 | Preserve empty results for absent or whitespace `FromJson` input | `SectionStore.FromJson` and facade wrappers | Store tests plus code inspection | conformant | `string.IsNullOrWhiteSpace` and missing-section returns both construct `new T()`. |
| R-06 | Preserve sibling sections through `UserSettingsFile.Update` | `SectionStore.Save` and `SaveAsync` | `UserSettingsFileTests`; `UserSettingsFileConcurrencyTests` | conformant | The shared store remains an `Update`/`UpdateAsync` client; 675 tests passed. |
| R-07 | Preserve constructor path resolution | `SectionStore.CreateSettingsFile` | Store constructor/path tests | conformant | All path overloads reach `SettingsPathResolver.ResolveOverride` with `appsettings.json`. Public API shape is assessed separately in CR-02. |
| PRD constraint: writer | Keep `UserSettingsFile` as the sole write coordinator | `SectionStore.Save` and `SaveAsync` | Section-preservation tests | conformant | No independent writer was introduced. |
| PRD constraint: API | Keep the four public APIs unchanged | All four facade constructor sets | Build and diff inspection | non-conformant | CR-02: each facade adds a one-argument public constructor and removes optional metadata from the two-argument constructor. |
| PRD constraint: disk | Preserve section names and on-disk JSON shape | Facade constants and `UserSettingsFile` writes | Round-trip tests | conformant | `Hud`, `Providers`, `RateLimit`, and `Refresh` remain unchanged; serialization still flows through `UserSettingsFile`. |
| PRD acceptance 1 | Check every R-NN item after refactoring | Manifest and handoffs | Review matrix | non-conformant | R-02 is not preserved although both tasks are marked done. |
| PRD acceptance 2 | Introduce no behavior silently | Facade constructors and provider parse path | Diff inspection | non-conformant | CR-01 and CR-02 introduce behavior/API changes not approved by the PRD. |
| PRD acceptance 3 | Settings and concurrency tests pass | Infrastructure test project | Full MTP project run | conformant | 675 passed, 0 failed. |
| PRD acceptance 4 | Collapse duplicated constructor, options, and path blocks | `SectionStore` | QA-01 through QA-03 | conformant | Current counts are 2, 1, and 1, matching the profile targets. |
| DEC-01 | Shared generic store owns persistence, section name, options, and operations | `SectionStore<T>` | Code review | non-conformant | CR-03: the class does not own a section name; facades retain constants and pass them to a static parser. |
| DEC-02 | Keep four public facades with identical signatures | All four facades | Build and diff inspection | non-conformant | CR-02: call sites compile, but the public constructor set and optional metadata changed. |
| DEC-03 | Reuse path resolution and existing filename | `SectionStore.CreateSettingsFile` | Constructor/path tests | conformant | Current code calls `SettingsPathResolver.ResolveOverride` with the prior filename. |
| DEC-04 | Inject the provider converter into shared options | `ProviderSettingsStore.JSON_OPTIONS` | Provider store tests and code inspection | conformant | The facade clones `SharedOptions` and adds `ProviderSettingsJsonConverter`. |
| QA-01 | Reduce `AllowTrailingCommas = true` to 2 hits | `SectionStore.cs`; `UserSettingsFile.cs` | Exact profile command | conformant | 2 hits, target 2. |
| QA-02 | Reduce optional `baseDirectory` pattern to 1 hit | `SectionStore` constructor | Exact profile command | conformant | 1 hit, target 1. The target conflicts with exact preservation of the facade optional metadata; see CR-02. |
| QA-03 | Reduce `CreateSettingsFile` helpers to 1 hit | `SectionStore.CreateSettingsFile` | Exact profile command | conformant | 1 hit, target 1. |
| TC-01 | Hud load/save/override scenarios | `HudPositionStore` | `HudPositionStoreTests` | conformant | Included in the passing full project run. |
| TC-02 | Provider settings and converter remain unchanged | `ProviderSettingsStore` | `ProviderSettingsStoreTests` | non-conformant | CR-01 is outside the current provider test coverage. |
| TC-03 | RateLimit and Refresh load/save/missing-section scenarios | Both facades | Their store tests | conformant | Included in the passing full project run. |
| TC-04 | Preserve other sections under concurrency | `SectionStore` through `UserSettingsFile` | User settings and concurrency tests | conformant | Included in the passing full project run. |
| T01.1 | Add a shared store with the section name and shared plumbing | `SectionStore<T>` | Code review | non-conformant | CR-03: shared plumbing exists, but section-name ownership does not match DEC-01 or T01.1. |
| T01.2 | Migrate Hud to a thin facade | `HudPositionStore` | Hud tests | conformant | Delegation is present and tests pass. |
| T01.3 | Migrate Refresh to a thin facade | `RefreshSettingsStore` | Refresh tests | conformant | Delegation is present and tests pass. |
| T01.4 | Remove Hud/Refresh duplication | Both facades | QA profile | conformant | Target patterns are absent from the facades. |
| T02.1 | Migrate RateLimit to a thin facade | `RateLimitSettingsStore` | RateLimit tests | conformant | Delegation and clamp behavior are present; tests pass. |
| T02.2 | Migrate Provider and preserve converter behavior | `ProviderSettingsStore` | Provider tests | non-conformant | CR-01: public `FromJson` behavior regressed. |
| T02.3 | Remove RateLimit/Provider duplication | Both facades | QA profile | conformant | Target patterns are absent from the facades. |

## Compliance with rules and skills

| Rule or skill | State | Evidence |
| --- | --- | --- |
| Repository architecture and writer boundary | OK | Core is untouched and `UserSettingsFile` remains the writer. |
| Repository public API requirement | NOT OK | CR-02; facade constructor declarations at `HudPositionStore.cs:40,53`, `ProviderSettingsStore.cs:44,57`, `RateLimitSettingsStore.cs:37,50`, and `RefreshSettingsStore.cs:40,53`. |
| Repository C# size, docs, async, and null guards | OK | All five classes are below 300 lines; shared methods are below 30 lines; public members are documented; Infrastructure awaits use `ConfigureAwait(false)`. |
| Repository four-argument call formatting | NOT OK | CR-04; four nested constructor calls end with `baseDirectory))`. |
| `dotnet-efficient-validation` | OK | Native MTP confirmed from `global.json`, evaluated project properties, SDK 10.0.401, and test execution with a minimum count. |
| `repository-cli-efficiency` | OK | Inspection stayed within the feature, configuration sources, and named tests; diff statistics preceded the full scoped diff. |
| `no-workarounds` | OK | CR-01 identifies the changed lookup contract and missing regression coverage rather than treating the passing suite as proof. |
| Desktop E2E policy | N/A | E2E omitted as required for this desktop .NET project. |

## Quality profile

| ID | Rule | Class | Command | Hits | State |
| --- | --- | --- | --- | --- | --- |
| QA-01 | Single shared JSON options block | blocking | `rtk rg -n "AllowTrailingCommas = true" src/TokenHound.Infrastructure/Configuration` | 0 new/aggravated of 2 total | OK |
| QA-02 | Single triple-constructor pattern | blocking | `rtk rg -n "string\? baseDirectory = null" src/TokenHound.Infrastructure/Configuration` | 0 aggravated of 1 total | OK |
| QA-03 | Single `CreateSettingsFile` helper | blocking | `rtk rg -n "private static UserSettingsFile CreateSettingsFile" src/TokenHound.Infrastructure/Configuration` | 0 aggravated of 1 total | OK |

- Terrain baseline: applied from the TechSpec, with starting counts 8, 4, and 4.
- Hits discounted by baseline: 1 surviving pre-feature QA-01 hit in `UserSettingsFile.cs`; the shared-store hits replace facade duplication and meet all targets.
- Reservations accumulated in the feature: 0.
- Suggested escalation: no profile trigger fired.

## TechSpec adherence

| Decision or contract | State | Evidence |
| --- | --- | --- |
| DEC-01 shared store structure | PARTIAL | Persistence and options moved, but the section-name state did not; see CR-03. |
| DEC-02 public facade compatibility | NO | Existing source calls compile, but the public overload set and optional parameter metadata changed; see CR-02. |
| DEC-03 path resolution | YES | `SectionStore.CreateSettingsFile` uses the existing resolver and filename. |
| DEC-04 provider converter injection | YES | Provider options clone the shared options and add the converter. |
| Compatibility: on-disk representation | YES | Section names and `UserSettingsFile` serialization are unchanged. |
| Compatibility: public behavior | NO | Provider `FromJson` no longer accepts case variants of the root property; see CR-01. |

## Verified tasks

| Task | Location | State | Handoff and evidence |
| --- | --- | --- | --- |
| T01 | `done/task_01.md` | INCOMPLETE | The implementation and checks exist, but T01.1 does not match DEC-01 and the Hud/Refresh public constructor sets changed. |
| T02 | `done/task_02.md` | INCOMPLETE | The implementation and checks exist, but provider `FromJson` regressed and the RateLimit/Provider public constructor sets changed. |

The manifest marks both tasks done. The findings make those states inconsistent with their outcomes and acceptance claims.

## Executed validations

- Profile and exclusions: .NET 10, native Microsoft.Testing.Platform; desktop E2E omitted by policy.
- Validated state: the five implementation files and the current PRD, TechSpec, manifest, and handoffs on Windows/PowerShell.
- Reused evidence: the T02 handoff build passed with 0 errors and 0 warnings. The current Debug Infrastructure assembly is newer than all five reviewed source files, so the no-build test run used the current implementation.
- Manual acceptance: none required by the TechSpec; on-disk shape is covered by tests.

| Command | Result | Obligations covered |
| --- | --- | --- |
| `rtk dotnet --version` plus evaluated test properties | passed; SDK 10.0.401, native MTP, `net10.0` | validation environment |
| T02 handoff: `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-restore --nologo --verbosity:minimal` | reused; passed, 0 errors, 0 warnings | compile compatibility |
| `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1` | passed; 675 passed, 0 failed | R-01 through R-07, TC-01 through TC-04, excluding the missing CR-01 case |
| QA-01 exact `rtk rg` command | passed; 2 hits | QA-01 |
| QA-02 exact `rtk rg` command | passed; 1 hit | QA-02 |
| QA-03 exact `rtk rg` command | passed; 1 hit | QA-03 |
| `rtk git diff HEAD --check -- <reviewed tracked files and task directory>` | passed | scoped whitespace validation |
| `rtk rg -n "[ \t]+$" src/TokenHound.Infrastructure/Configuration/SectionStore.cs` | passed by no matches; `rg` exit 1 | new-file whitespace validation |
| Strict PowerShell reflection load of the .NET 10 Infrastructure assembly | blocked; Windows PowerShell could not load `System.Runtime, Version=10.0.0.0` | direct runtime probe for R-02 |

## Findings

| ID | Severity | Source | Evidence | Impact | Proven recommendation |
| --- | --- | --- | --- | --- | --- |
| CR-01 | Medium | R-02, TC-02, T02.2 | `ProviderSettingsStore.cs:82-94` delegates to `SectionStore.cs:134-149`, whose `JsonElement.TryGetProperty(sectionName, ...)` is case-sensitive. The prior code deserialized `UserSettings` with `PropertyNameCaseInsensitive = true`. `ProviderSettingsStoreTests.cs:55-252` contains no call to `ProviderSettingsStore.FromJson`. | JSON such as `{"providers":{"claude":{"Enabled":false}}}` previously produced a disabled Claude state. The new public method treats the section as absent and returns the all-enabled default. There are no current in-repository callers, but the specified public behavior regressed silently. | Preserve case-insensitive root-section matching for the Provider facade, or use root deserialization that retains the former serializer contract. Add a direct regression test with a casing variant and keep the exact-name case. |
| CR-02 | Low | PRD public API constraint, DEC-02, PRD acceptance 2 | The scoped diff shows every prior `(string? filePath, string? baseDirectory = null)` constructor replaced by a non-optional two-argument constructor plus a new one-argument public overload. Current declarations are at `HudPositionStore.cs:40,53`, `ProviderSettingsStore.cs:44,57`, `RateLimitSettingsStore.cs:37,50`, and `RefreshSettingsStore.cs:40,53`. | Existing source and binary calls remain usable, but reflection metadata and the public overload set are not identical. This conflicts with the PRD and DEC-02. QA-02 currently rewards removal of the optional declarations, so the profile and compatibility requirement also disagree. | Reconcile QA-02 with the compatibility requirement through an approved specification decision. Then either restore the exact prior constructor surface or explicitly approve and document the API change. |
| CR-03 | Low | DEC-01, T01.1 | `SectionStore.cs:12-162` stores delegates and `UserSettingsFile`, while each facade retains its section-name constant and passes it to static `FromJson`. T01 proposes `T01-ADR-01`, but no approved DEC replaces DEC-01. | The implementation and TechSpec assign section-name ownership to different components, and T01 is marked complete against a decision it did not implement. Future changes cannot rely on the recorded design. | Either implement section-name ownership as DEC-01 specifies or approve the proposed ADR and update the TechSpec and task state before marking T01 complete. |
| CR-04 | Low | Repository C# call-formatting rule | `HudPositionStore.cs:58`, `ProviderSettingsStore.cs:62`, `RateLimitSettingsStore.cs:55`, and `RefreshSettingsStore.cs:58` close four-argument `SectionStore` calls as `baseDirectory))`. | The changed C# does not meet the repository rule that calls with at least four arguments place the closing parenthesis on its own line. | Reformat the four nested constructor initializers so the inner call closes on its own line. |

## Optional improvements

None.

## Previous findings

Not applicable. This is the first review under this PRD.

## Limitations and open items

- No base reference was supplied. The review cannot prove that the handoff-defined files form the complete change relative to a feature-start commit.
- The worktree contains extensive unrelated staged changes. This review excludes them and does not issue findings for the global staged diff.
- A direct reflection probe was not executable in Windows PowerShell because that host could not load the .NET 10 runtime assembly. This limits runtime reproduction of CR-01, but it does not make the code-path change or missing test unverifiable.
- E2E was omitted under the desktop .NET policy. The TechSpec requires no manual acceptance.

## Conclusion

The shared store reaches all three blocking quality targets, the current MTP project passes 675 tests, and the save/path behaviors remain intact. Approval is blocked because provider `FromJson` loses a specified case-insensitive behavior, the public constructor surface changes without approval, DEC-01 is only partially implemented, and four changed calls violate the repository formatting rule. T01 and T02 therefore cannot remain complete in this reviewed state.
