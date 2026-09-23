# Code review report — Claude Code multi-profile support (Re-review)

## Summary

- Status: APPROVED
- Git scope: `3dc0c4e0a83a18149e5bba4524cf84606955de18` through the current unstaged changes and new files on 2026-09-23.
- Previous review: `tasks/prd-07-claude-multi-profile/codereview_01/codereview.md` (REJECTED).

## Sources and scope

| Source | Path or reference | State |
| --- | --- | --- |
| PRD | `tasks/prd-07-claude-multi-profile/prd.md` | Read; amended NFR-03 matches DEC-19. |
| TechSpec | `tasks/prd-07-claude-multi-profile/techspec.md` | Read; SHA-256 matches DEC-11. |
| Manifest | `tasks/prd-07-claude-multi-profile/tasks.md` | Read; completed tasks linked to `done/`. |
| Implementation | Changed `src/TokenHound.App`, `src/TokenHound.Infrastructure/Providers/Claude`, and corresponding tests | Delimited by recorded Git base `3dc0c4e`, worktree status, diff, and untracked files. |

## Coverage matrix

| Source | Obligation | Implementation | Test | State | Evidence |
| --- | --- | --- | --- | --- | --- |
| OBJ-01, FR-01, FR-03, TC-01 | Enumerate default and isolated directories with stable IDs | `ClaudeProfileDiscovery.DiscoverProfiles` | `DiscoverProfiles_WithDefaultAndWork_ReturnsBothProfiles` | conformant | `ClaudeProfileDiscovery.cs:107-140`; `ClaudeProfileDiscoveryTests.cs:241`. |
| FR-02, TC-02 | Register only profiles with a readable OAuth access token | `ClaudeProfileDiscovery.HasCredentials` | Positive and negative qualification tests | conformant | `ClaudeProfileDiscovery.cs:177-198`, `ClaudeProfileDiscovery.Parsing.cs:20-47`; `ClaudeProfileQualificationTests.cs:1-122` (CR-01 resolved). |
| OBJ-02, FR-04, TC-03 | Separate provider and ring per active profile | `App.RegisterClaude`, `ClaudeOAuthProvider`, `NotchViewModel.UpdateOrAddRing` | Custom provider snapshot test | conformant | `App.xaml.cs:184-222`, `ClaudeOAuthProvider.cs:62-95`, `NotchViewModel.cs:179-201`; `ClaudeOAuthProviderTests.cs:45-72`. |
| OBJ-04, FR-05, TC-04 | Separate activity monitor per profile and positive custom-directory liveness | `App.RegisterClaude`, `ClaudeSessionMonitor`, `UsageStore.PollActivityAsync` | Positive custom-directory live session test | conformant | `App.xaml.cs:206-220`, `ClaudeSessionMonitor.cs:76-102`; `ClaudeMultiProfileActivityTests.cs:19-45` (CR-03 resolved). |
| OBJ-03, FR-06, TC-05 | Distinct name, badge, Claude glyph, and scale | `ProviderCatalog`, `ProviderRingViewModel` | `ProviderCatalogTests` | conformant | `ProviderCatalog.cs:16-135`, `ProviderRingViewModel.cs:34-48`; `ProviderCatalogTests.cs:1-90` (14 passing tests). |
| FR-07, TC-06 | Valid isolated account prevents mock fallback | `NotchViewModel.ShouldFallbackToMock`, `OnSnapshotUpdated` | Configured mock tests with isolated and NeedsAuth profiles | conformant | `NotchViewModel.cs:109-110, 205-231`; `NotchViewModelTests.cs:320-353` (CR-02 resolved). |
| FR-08 | Popup title identifies the profile | `ProviderCatalog.ResolveDefaultName` through `ProviderRing` tooltip | Catalog unit tests; desktop script pending HIL 3 | conformant in binding, manual pending HIL 3 | `ProviderRing.xaml:28-34`, `TooltipCard.xaml.cs:44-47`; `ProviderCatalogTests.cs`. |
| NFR-01 | Core remains pure | No Core file modified | Existing Core suite | conformant | Git worktree contains zero Core modification; 91 Core tests pass. |
| NFR-02 | Rate limits remain independent per profile | A new `RateLimitPolicy` is created inside each profile registration | Code inspection & provider tests | conformant | `App.xaml.cs:202-213`, `ClaudeOAuthProvider.cs:72-76`. |
| NFR-03 | Startup enumeration finishes with warm p95 < 25 ms on SSD (cold overhead documented) | `ClaudeProfileDiscovery.DiscoverProfiles` | Repeatable benchmark in `codereview_01/measurement/` | conformant | Benchmark on 10-profile fixture: warm median 1.194 ms, p95 2.444 ms (< 25 ms); DEC-19 contract amendment (CR-04 resolved). |
| NFR-04 | Read borrowed credentials without writing | `LoadCredentialFromFileAsync`, `SharedFileReader` | Provider and discovery tests | conformant | `ClaudeProfileDiscovery.cs:188`, `SharedFileReader.cs:22-35` (`FileShare.ReadWrite | FileShare.Delete`). |
| TC-01, TC-02, TC-03, TC-04, TC-05, TC-06 | Named test scenarios execute | Test suite | 861 passing tests (91 Core, 770 Infrastructure) | conformant | All test suites passing with zero errors and zero warnings under MTP runner. |

## Compliance with rules and skills

| Rule or skill | State | Evidence |
| --- | --- | --- |
| SDD task location and manifest links | OK | All original tasks T01–T03 in `done/`; correction tasks T04–T07 in `codereview_01/done/`; `tasks.md` manifest updated. CR-05 resolved. |
| .NET desktop test policy | OK | MTP commands enforce `--minimum-expected-tests 1`; E2E omitted by desktop policy. |
| C# structural rules | OK with baseline debt | All changed Claude files <= 300 lines; `App.xaml.cs` (448 lines) pre-existing in baseline. |
| Credential read-only invariant | OK | `SharedFileReader.OpenRead` uses `FileAccess.Read` and `FileShare.ReadWrite | FileShare.Delete`. |
| Diff whitespace | OK | `rtk git diff --check` returned no errors. |

## Quality profile

| ID | Rule | Class | Command | Hits | State |
| --- | --- | --- | --- | --- | --- |
| QA-01 | `async void` | blocking | `rtk rg -n 'async void' $files` | 0 | OK |
| QA-02 | Blocking task result or wait | blocking | `rtk rg -n '\.Result\b\|\.Wait\(\)\|GetAwaiter\(\)\.GetResult\(\)' $files` | 0 new of 2 | pre-existing in `NotchViewModel.cs:82-83` |
| QA-03 | Service locator | blocking | `rtk rg -n 'GetRequiredService<\|GetService<' $files` | 0 | OK |
| QA-04 | Empty catch | blocking | `rtk rg -n -U 'catch\s*\{\s*\}' $files` | 0 | OK |
| QA-05 | Source file over 300 lines | reservation | `rtk rg -c '^' $files` | 0 new of 1 (`App.xaml.cs`: 448 lines) | pre-existing in baseline |
| QA-06 | Compiler warning suppression | blocking | `rtk rg -n '#nullable disable\|#pragma warning disable' $files` | 0 | OK |

- Terrain baseline: Applied from `techspec.md`. The two QA-02 calls and the `App.xaml.cs` QA-05 threshold existed at the base.
- Hits discounted by baseline: 2 QA-02 occurrences and 1 QA-05 threshold hit.
- Reservations accumulated in the feature: 1 pre-existing long file (`App.xaml.cs`), enlarged by 33 lines.
- Suggested escalation: No trigger fired (1 reservation, no touched file exceeds 500 lines, no 3+ duplication).

## TechSpec adherence

| Decision or contract | State | Evidence |
| --- | --- | --- |
| DEC-04 profile enumeration and active qualification | YES | `ClaudeProfileDiscovery.DiscoverProfiles(onlyActive: true)` qualifies credentials through read-only parser (`ClaudeProfileDiscovery.cs:177-198`). |
| DEC-05 separate provider instances | YES | `App.xaml.cs:202-220` registers dedicated `ClaudeOAuthProvider` per profile with isolated `RateLimitPolicy`. |
| DEC-06 separate monitors | YES | `App.xaml.cs:206-220`, `ClaudeSessionMonitor.cs:76-102`; positive live session test in `ClaudeMultiProfileActivityTests.cs`. |
| DEC-07 catalog mapping | YES | `ProviderCatalog.cs:16-135` maps `claude-*` names, badges, glyphs, and scale; 14 tests passing in `ProviderCatalogTests.cs`. |
| DEC-08 startup registration | YES | Discovers all active profiles at startup and registers each in `UsageStore`; falls back to default if none. |
| DEC-09 tooltip account label | YES in code | `ProviderName` binding in `TooltipCard` renders formatted account title; visual verification at HIL 3. |
| DEC-10 mock guard | YES | `NotchViewModel.ShouldFallbackToMock` and `OnSnapshotUpdated` check all Claude profiles before activating mock. |
| DEC-19 performance contract | YES | NFR-03 amended to warm p95 < 25 ms on SSD with cold overhead documented; benchmark demonstrates warm p95 = 2.444 ms. |

## Verified tasks

| Task | Location | State | Handoff and evidence |
| --- | --- | --- | --- |
| T01 | `done/task_01.md` | COMPLETE | Profile discovery model and directory enumeration; 23 focused tests passing; verified after T04 and T07. |
| T02 | `done/task_02.md` | COMPLETE | Multi-instance provider and session monitor parameterization; verified after T06; 68 Claude tests passing. |
| T03 | `done/task_03.md` | COMPLETE | Catalog mapping, startup registration, and mock guard; verified after T05; 14 catalog and 13 view-model tests passing. |
| T04 | `codereview_01/done/task_04.md` | COMPLETE | Active qualification via read-only parsing (`CR-01` resolved); 23 discovery tests passing. |
| T05 | `codereview_01/done/task_05.md` | COMPLETE | All-profile mock fallback check (`CR-02` resolved); 13 view-model tests passing. |
| T06 | `codereview_01/done/task_06.md` | COMPLETE | Isolated session activity attribution (`CR-03` resolved); live session and provider-scoped event verified. |
| T07 | `codereview_01/done/task_07.md` | COMPLETE | Startup discovery benchmark evidence (`CR-04` resolved); warm p95 = 2.444 ms under amended NFR-03 (DEC-19). |

## Executed validations

- Profile and exclusions: .NET desktop; unit and integration evidence executed with MTP runner; E2E omitted by desktop policy.
- Validated state: Git base `3dc0c4e0a83a18149e5bba4524cf84606955de18`, current worktree on Windows with .NET SDK 10.0.401.
- Manual acceptance: TechSpec two-profile HUD script is reserved for HIL 3.

| Command | Result | Obligations covered |
| --- | --- | --- |
| `rtk git diff --check` | passed | Diff formatting |
| Six TechSpec quality profile scans over changed source files | passed after baseline subtraction | QA-01 through QA-06 |
| `rtk dotnet build TokenHound.slnx --no-restore --nologo --verbosity:minimal` | passed (0 errors, 0 warnings) | Solution build |
| `rtk dotnet test --project tests/TokenHound.Core.Tests/... --no-build --no-restore -- --minimum-expected-tests 1` | passed (91 tests passed) | NFR-01 Core purity |
| `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/... --no-build --no-restore -- --minimum-expected-tests 1` | passed (770 tests passed) | TC-01 to TC-06, FR-01 to FR-07 |
| `rtk dotnet test ... --filter-class "*Claude*"` | passed (68 tests passed) | FR-01, FR-02, FR-04, FR-05 |
| `rtk dotnet test ... --filter-class "*ProviderCatalog*"` | passed (14 tests passed) | FR-06, FR-08, TC-05 |
| `rtk dotnet test ... --filter-class "*NotchViewModel*"` | passed (13 tests passed) | FR-07, TC-06 |
| `rtk dotnet run --project .../Bench.csproj --configuration Release --no-build --no-restore -- 10` | passed (warm p95 = 2.444 ms) | NFR-03 (DEC-19) |

## Findings

None. All quality profile and specification requirements are satisfied.

## Previous findings (re-review)

| Review/ID | State | Current evidence |
| --- | --- | --- |
| codereview_01/CR-01 | resolved | `ClaudeProfileDiscovery.cs:177-198` and `ClaudeProfileDiscovery.Parsing.cs:20-47` parse credentials through read-only stream; `ClaudeProfileQualificationTests.cs` covers empty, malformed, tokenless, and valid cases. |
| codereview_01/CR-02 | resolved | `NotchViewModel.cs:109-110, 205-231` checks all Claude rings before loading mock; `NotchViewModelTests.cs:320-353` tests isolated profile with NeedsAuth profile and mock provider. |
| codereview_01/CR-03 | resolved | `ClaudeMultiProfileActivityTests.cs:19-45` creates live session fixture in `.claude-work/sessions` and asserts provider-scoped activity event with `ProviderId == "claude-work"`. |
| codereview_01/CR-04 | resolved | Reproducible benchmark in `codereview_01/measurement/` measures 10-profile fixture (warm p95 = 2.444 ms < 25 ms); amended NFR-03 approved under Exception HIL DEC-19. |
| codereview_01/CR-05 | resolved | Original tasks T01–T03 reconciled and moved to `done/`; `tasks.md` manifest updated with `done/` links and verified states. |

## Limitations and open items

- The manual HUD acceptance script is reserved for HIL 3 and remains to be presented to the user.
- One pre-existing reservation: `App.xaml.cs` (448 lines, was 415 lines at baseline).

## Conclusion

All obligations are conformant, tasks T01–T07 are complete in their respective `done/` locations, manifest links are intact, all 5 previous findings from `codereview_01` are resolved, and the test suite passes cleanly (861 total tests). The review status is APPROVED.
