# Code review report — Claude Code multi-profile support

## Summary

- Status: REJECTED
- Git scope: `3dc0c4e0a83a18149e5bba4524cf84606955de18` through the current unstaged changes and new files on 2026-09-23.
- Previous review: none.

## Sources and scope

| Source | Path or reference | State |
| --- | --- | --- |
| PRD | `tasks/prd-07-claude-multi-profile/prd.md` | Read; SHA-256 matches DEC-02. |
| TechSpec | `tasks/prd-07-claude-multi-profile/techspec.md` | Read; SHA-256 matches DEC-11. |
| Manifest and tasks | `tasks/prd-07-claude-multi-profile/tasks.md`, `task_01.md` through `task_03.md` | Read; handoffs and checked work present. |
| Implementation | Changed `src/TokenHound.App`, `src/TokenHound.Infrastructure/Providers/Claude`, and corresponding tests | Delimited by the recorded Git base, worktree status, diff, and untracked file inspection. |

## Coverage matrix

| Source | Obligation | Implementation | Test | State | Evidence |
| --- | --- | --- | --- | --- | --- |
| OBJ-01, FR-01, FR-03, TC-01 | Enumerate default and isolated directories with stable IDs | `ClaudeProfileDiscovery.DiscoverProfiles` | `DiscoverProfiles_WithDefaultAndWork_ReturnsBothProfiles` | conformant | `ClaudeProfileDiscovery.cs:107-140`; test at `ClaudeProfileDiscoveryTests.cs:241`. |
| FR-02, TC-02 | Register only profiles with a readable OAuth access token | `ClaudeProfileDiscovery.HasCredentials` | Missing malformed and unreadable file cases | non-conformant | `ClaudeProfileDiscovery.cs:177-182` checks `File.Exists` only; see CR-01. |
| OBJ-02, FR-04, TC-03 | Separate provider and ring per active profile | `App.RegisterClaude`, `ClaudeOAuthProvider`, `NotchViewModel.UpdateOrAddRing` | Custom provider snapshot test | conformant subject to FR-02 | `App.xaml.cs:184-222`, `ClaudeOAuthProvider.cs:62-95`, `NotchViewModel.cs:179-201`. |
| OBJ-04, FR-05, TC-04 | Separate activity monitor per profile and positive custom-directory liveness | `App.RegisterClaude`, `ClaudeSessionMonitor`, `UsageStore.PollActivityAsync` | New custom-directory test observes an empty directory | pending | `App.xaml.cs:206-220`, `ClaudeSessionMonitor.cs:76-102`, `ClaudeSessionMonitorTests.cs:307-324`; see CR-03. |
| OBJ-03, FR-06, TC-05 | Distinct name, badge, Claude glyph, and scale | `ProviderCatalog`, `ProviderRingViewModel` | `ProviderCatalogTests` | conformant | `ProviderCatalog.cs:16-135`, `ProviderRingViewModel.cs:34-48`. |
| FR-07, TC-06 | Valid isolated account prevents mock fallback | `NotchViewModel.ShouldFallbackToMock`, `OnSnapshotUpdated` | New test constructs view model without a mock provider | non-conformant | `NotchViewModel.cs:101-110`, `NotchViewModelTests.cs:320-329`; see CR-02. |
| FR-08 | Popup title identifies the profile | `ProviderCatalog.ResolveDefaultName` through `ProviderRing` tooltip | Catalog unit tests; desktop script pending HIL 3 | conformant in binding, manual pending | `ProviderRing.xaml:28-34`, `TooltipCard.xaml.cs:44-47`. |
| NFR-01 | Core remains pure | No Core file changed | Existing Core suite | conformant | Git worktree scope contains no Core modification. |
| NFR-02 | Rate limits remain independent per profile | A new `RateLimitPolicy` is created inside each profile registration | Code inspection | conformant | `App.xaml.cs:202-213`; provider stores its supplied policy at `ClaudeOAuthProvider.cs:72-76`. |
| NFR-03 | Discovery finishes in less than 25 ms on standard SSD | No measurement supplied | No timing evidence | not verifiable | Neither handoff nor TechSpec test matrix measures the stated limit; see CR-04. |
| NFR-04 | Read borrowed credentials without writing | `LoadCredentialFromFileAsync`, `SharedFileReader` | Existing provider tests | conformant | `ClaudeProfileDiscovery.cs:148-174`, `SharedFileReader.cs:22-35`. |
| TC-01, TC-02, TC-03, TC-05 | Named tests execute | T01 through T03 handoffs | 853 passing tests across Core and Infrastructure | conformant for tested cases | Same code, configuration, and Git base remain in the worktree; handoffs in `task_01.md` through `task_03.md`. |

## Compliance with rules and skills

| Rule or skill | State | Evidence |
| --- | --- | --- |
| SDD task location and manifest links | NOT OK | `tasks.md` checks T01–T03 as complete but links `task_01.md` through `task_03.md` at the feature root; `done/` is absent. See CR-05. |
| .NET desktop test policy | OK | MTP commands in handoffs enforce `--minimum-expected-tests 1`; E2E is omitted by the desktop policy. |
| C# structural rules | OK with baseline debt | Changed Claude source files remain below 300 lines; `App.xaml.cs` was 415 lines at baseline and is now 448. |
| Credential read-only invariant | OK | `SharedFileReader.OpenRead` uses `FileAccess.Read` and `FileShare.ReadWrite | FileShare.Delete`. |
| Diff whitespace | OK | `rtk git diff --check` returned no errors. |

## Quality profile

The eight changed source files were scanned with the six TechSpec rules. `QA-01`, `QA-03`, `QA-04`, and `QA-06` produced no hits. `QA-02` found two calls in `NotchViewModel.cs:82-83`; both exist at the Git base. `QA-05` counted the changed source files.

| ID | Rule | Class | Command | Hits | State |
| --- | --- | --- | --- | --- | --- |
| QA-01 | `async void` | blocking | `rtk rg -n 'async void' $files` | 0 | OK |
| QA-02 | Blocking task result or wait | blocking | `rtk rg -n '\.Result\b|\.Wait\(\)|GetAwaiter\(\)\.GetResult\(\)' $files` | 0 new of 2 | pre-existing |
| QA-03 | Service locator | blocking | `rtk rg -n 'GetRequiredService<|GetService<' $files` | 0 | OK |
| QA-04 | Empty catch | blocking | `rtk rg -n -U 'catch\s*\{\s*\}' $files` | 0 | OK |
| QA-05 | Source file over 300 lines | reservation | `rtk rg -c '^' $files` | 0 new of 1; App 415 to 448 lines | pre-existing, enlarged |
| QA-06 | Compiler warning suppression | blocking | `rtk rg -n '#nullable disable|#pragma warning disable' $files` | 0 | OK |

- Terrain baseline: applied from `techspec.md`; the two QA-02 calls and the App QA-05 threshold existed at the base.
- Hits discounted by baseline: two QA-02 occurrences and one QA-05 threshold hit.
- Reservations accumulated in the feature: one existing long file enlarged by 33 lines; optional extraction can be considered separately.
- Suggested escalation: no trigger fired (one reservation, maximum touched file 448 lines, and no proven duplication in three places).

## TechSpec adherence

| Decision or contract | State | Evidence |
| --- | --- | --- |
| DEC-04 profile enumeration and active qualification | PARTIAL | Enumeration works; qualification only tests file existence (CR-01). |
| DEC-05 separate provider instances | YES | `App.xaml.cs:202-220` and custom provider snapshot test. |
| DEC-06 separate monitors | PARTIAL | Instances have distinct IDs and directories; positive liveness test is absent (CR-03). |
| DEC-07 catalog mapping | YES | `ProviderCatalogTests.cs` covers name, badge, glyph, and scale. |
| DEC-08 startup registration | YES, subject to FR-02 | One provider and monitor are registered per discovered profile. |
| DEC-09 tooltip account label | YES in code | Binding chain reaches `TooltipCard.ProviderNameText`; visual acceptance is pending HIL 3. |
| DEC-10 mock guard | NO | `OnSnapshotUpdated` can activate mock for one `NeedsAuth` profile without checking other valid profiles (CR-02). |
| Observability signal | PARTIAL | `RegisterClaude` has no count/ID log described by the TechSpec; see open items. |

## Verified tasks

| Task | Location | State | Handoff and evidence |
| --- | --- | --- | --- |
| T01 | `task_01.md` at feature root | INCOMPLETE | Discovery implementation and 16 filtered tests recorded; FR-02 remains unmet and the task was not moved to `done/`. |
| T02 | `task_02.md` at feature root | INCOMPLETE | Provider/monitor implementation and filtered tests recorded; TC-04 positive custom-directory evidence is missing. |
| T03 | `task_03.md` at feature root | INCOMPLETE | Catalog and startup implementation recorded; FR-07 mock fallback evidence and behavior remain incomplete. |

## Executed validations

- Profile and exclusions: .NET desktop; unit and integration evidence from the handoffs was reused because the code and build inputs have not changed since those checks. E2E omitted by policy.
- Validated state: Git base `3dc0c4e0a83a18149e5bba4524cf84606955de18`, current uncommitted implementation, and Windows workspace.
- Reused evidence: T01–T03 handoffs report a clean solution build and 853 passing tests (91 Core, 762 Infrastructure). The review did not rerun those unchanged checks.
- Manual acceptance: the TechSpec two-profile HUD script remains for HIL 3. The required Windows MCP App and Screenshot tools are unavailable in this session; no desktop rendering claim is made.

| Command | Result | Obligations covered |
| --- | --- | --- |
| `rtk git diff --check` | passed | Diff formatting |
| Six TechSpec quality profile scans over changed source files | passed after baseline subtraction | QA-01 through QA-06 |
| Handoff solution build and project test commands | passed in authoring session; evidence reused | TC-01 through TC-06 for their implemented test cases |

## Findings

| ID | Severity | Source | Evidence | Impact | Proven recommendation |
| --- | --- | --- | --- | --- | --- |
| CR-01 | High | FR-02, TC-02 | `ClaudeProfileDiscovery.cs:177-182` accepts any existing `.credentials.json`; `DiscoverProfiles` calls it at lines 113 and 135. | Malformed, empty, or unreadable files create rings for profiles without a readable OAuth access token. | Qualify each profile by read-only parsing through the existing credential reader and cover malformed and unreadable cases. |
| CR-02 | Medium | FR-07, TC-06 | `NotchViewModel.cs:109-110` loads mock after any `NeedsAuth` snapshot; the new test at `NotchViewModelTests.cs:320-329` supplies no mock provider. | One expired Claude account can cause unnecessary mock telemetry beside another valid account when mock fallback is configured. | Route snapshot fallback through an all-profile check and test with a configured mock, one valid isolated profile, and one `NeedsAuth` profile. |
| CR-03 | Medium | FR-05, TC-04 | `ClaudeSessionMonitorTests.cs:307-324` creates an empty custom sessions directory and asserts a null result. | The test cannot prove that an active isolated session is read or assigned to the intended provider activity event. | Add a live-session fixture in the custom directory and assert the monitor ID and resulting provider-scoped activity. |
| CR-04 | Medium | NFR-03 | No timing result appears in T01–T03 handoffs or the TechSpec test matrix. | The stated 25 ms startup enumeration limit has no acceptance evidence. | Measure `DiscoverProfiles` on a representative local SSD fixture, record profile count, method, and elapsed result, and address any failure. |
| CR-05 | Medium | SDD task state | `tasks.md` lists three completed tasks at root links; no `done/` directory exists. | The manifest claims completion without the required completed-task location, so resumption and DAG ownership are inconsistent. | Reconcile each original task's evidence, reopen affected work, then move genuinely complete tasks to `done/` and update manifest links and state through the DAG owner. |

## Previous findings

None; this is the first review.

## Limitations and open items

- The manual HUD acceptance script is reserved for HIL 3 and remains unexecuted. The host exposes no Windows MCP App or Screenshot tool.
- The TechSpec calls for a startup log of discovered profile count and IDs; `RegisterClaude` does not emit it. This is a low-effort observability improvement within the approved solution.
- No performance measurement exists for NFR-03, which is a review block under CR-04.
- Task artifact hashes changed after HIL 2 when execution checkboxes and handoffs were appended. PRD and TechSpec hashes still match the approval record; task contracts were read with their handoffs.

## Conclusion

The implementation reaches the main multi-profile path, but active-profile qualification and mock fallback do not yet meet their approved requirements. The custom session test, startup timing evidence, and completed-task state also need correction before review approval.
