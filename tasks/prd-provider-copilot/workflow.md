# SDD workflow: Provider Copilot

## Reconciliation

- Feature slug: `provider-copilot`.
- User request: initiate the SDD flow from the Copilot provider specification with a PRD, TechSpec, and executable tasks.
- Authoritative specification: `docs/specs/11-PROVIDER-COPILOT.md`.
- External source requested by the user: `D:\\MyProjects\\TokenHound\\docs\\specs\\11-PROVIDER-COPILOT.md`.
- Source verification: the external source and the worktree copy have identical SHA-256 `BEA084E9F8EFAC85FD0F6DDC88F3E841A0D3C8F35995193BC78B75B2DED29C5F`.
- Git base: `5b7622a2586794c193aa140c2064c47faa52fa55` on branch `copilot-provider`.
- Pre-existing worktree changes: none at reconciliation time.
- Existing feature artifacts: none. No checkpoint, PRD, TechSpec, task plan, manifest, handoff, review, or acceptance report exists for this feature.
- Reusable references inspected: `AGENTS.md`, `ARCHITECTURE.md`, `CONTEXT.md`, the Copilot specification, and the Codex and Cursor provider planning artifacts.

## Decisions

### DEC-0001: Feature scope and source

- Status: recorded from the explicit user request, not a HIL approval.
- Scope: one provider-adapter feature identified by the slug `provider-copilot`.
- Source: `docs/specs/11-PROVIDER-COPILOT.md` is the authoritative product input for the PRD stage.
- Constraints: preserve the repository invariants, keep provider code in Infrastructure, keep Core pure, borrow credentials read-only, preserve rate-limit deadlines, and omit E2E for the desktop .NET validation plan.
- Next authorization required: HIL 1 approval of the written PRD before TechSpec and task planning.

## Initial current stage

- Phase: product.
- Next action: delegate PRD creation to `sdd-create-prd`.
- HIL 1 is not yet requested because the PRD does not exist.

## Product stage handoff

- Agent: Hubble (01a07ca0-4798-7a60-a1cc-6c73c6b17364).
- Result: drafted, no product-stage blocker.
- Artifact: prd.md.
- Artifact SHA-256: A95EB2C2AC1A648DB2AB09F12857A284F9F0260415FF72ACAF9E9DCA20483235.
- Coverage: 12 functional requirements, 7 non-functional requirements, 10 acceptance scenarios, credential safety, official fidelity, open-map quota parsing, overage, stale/rate-limit behavior, activity monitoring, HUD constraints, scope, and out-of-scope boundaries.
- Pending decisions: HIL1-01 through HIL1-06 in prd.md, covering status presentation, fractional formatting, optional metadata, identity-only behavior, stale retention/expiry, and launch metrics.
- Authorization boundary: no TechSpec, task plan, implementation, or correction is authorized until HIL 1 is recorded.

## Pending HIL 1

The coordinator must present prd.md for product approval. The decision is whether to approve the PRD as written, request named corrections, or reject the product scope. Silence does not authorize downstream stages.

## HIL 1 decision

### DEC-0002: PRD approved for technical planning

- Human response: Pode continuar.
- Decision: approve prd.md as the product contract for provider-copilot.
- Scope effect: no new product scope is added.
- Deferred items: HIL1-01 through HIL1-06 remain explicit deferrals. The TechSpec must preserve existing Notch conventions, omit unspecified metadata and launch KPIs, use existing archive policy, and avoid inventing a new identity-only or presentation contract. Any change beyond those boundaries requires an exception HIL.
- Approved source: prd.md, SHA-256 A95EB2C2AC1A648DB2AB09F12857A284F9F0260415FF72ACAF9E9DCA20483235.
- Next authorization: HIL 2 approval of the TechSpec, DAG, validations, and concrete tasks before implementation.

## Technical planning handoff

- Agent: Lovelace (01a07cc1-69ce-7852-a7ba-fe742e829807).
- Result: drafted, no unresolved technical blocker; the first attempt was stopped before writing and the same executor then completed the artifact.
- Artifact: techspec.md.
- Original artifact SHA-256: 93EE4CE18CDB3370856496F4A9896C9BE314FFAEB246AE4679D7EC575EA9C5B9.
- Revised artifact SHA-256 after external authentication research: 0295635F191B1165696818835E7FF4255EC3919D2613861D3F8887897641C093.
- Coverage: all PRD FR-01 through FR-12, NFR-01 through NFR-07, and AC-01 through AC-10 mapped to decisions, components, contracts, errors, security, concurrency, sequencing, and validation evidence.
- Main technical consequences: Copilot Infrastructure adapter, open-map quota parser, fractional remainder preservation, read-only credential discovery, durable archive/deadline gating, activity heuristics, generic Notch integration, and Core purity.
- Validation profile: .NET SDK 10.0.400 with native Microsoft.Testing.Platform; Core and Infrastructure tests use project-scoped commands with minimum expected tests; desktop E2E is explicitly omitted.
- Open implementation prerequisite: the installed official Copilot CLI 1.0.82 keychain target is now evidenced as host/login-qualified, while the plaintext config token property remains undocumented and requires an isolated official-CLI fixture before coding. No target or property was invented in the TechSpec.
- Authorization boundary: task planning and implementation remain blocked until HIL 2 approval.

## External authentication research

- GitHub's official authentication documentation confirms the credential precedence `COPILOT_GITHUB_TOKEN`, `GH_TOKEN`, `GITHUB_TOKEN`, system keychain, then GitHub CLI; it identifies the service name `copilot-cli` and Windows Credential Manager.
- GitHub's official configuration-directory documentation confirms `COPILOT_HOME` or the default `.copilot` directory, the automatically managed `config.json`, and retained `loggedInUsers` state. It does not publish a stable plaintext token property or Windows target-name contract.
- The current upstream [Copilot CLI issue #4527](https://github.com/github/copilot-cli/issues/4527) reports the Windows target shape `https://<host>:<login>.copilot-cli` for a data-residency host.
- A read-only local inspection of the installed 1.0.82 profile found JSONC `config.json` with `loggedInUsers` and `lastLoggedInUser` entries containing `host` and `login`, no top-level token property, and the corresponding host/login-qualified Credential Manager target. Secret values were not recorded.
- TechSpec consequence: derive target candidates from validated host/login metadata and treat the plaintext token property as an implementation prerequisite. Do not add a recursive token search or a hard-coded account-specific target.
- Sources: `tasks/prd-provider-copilot/techspec.md` external-authentication evidence and DEC-0003 technical decision rationale.

### DEC-0003: Credential format evidence constrains implementation

- Status: technical evidence recorded from the user's explicit request to research the real format; this is not HIL 2 approval.
- Scope: Copilot CLI credential discovery only.
- Decision: use the official precedence and `copilot-cli` service name; resolve Windows targets from validated host/login state; require a sanitized official plaintext-fallback fixture before implementing config token extraction; never guess a token property or write to Copilot-owned state.
- Evidence: official GitHub authentication/configuration documentation, upstream issue #4527, and the read-only local Copilot CLI 1.0.82 inspection described above.
- Authorization effect: HIL 2 remains pending. Task planning and implementation are still unauthorized.

## Pending HIL 2

The coordinator must present techspec.md for technical approval. The decision covers the proposed components and shared-policy changes, the task sequencing and validation commands, the desktop manual acceptance route, and authorization to create the executable task plan and implement within this contract. Any change to deferred HIL1 items or new scope requires an exception HIL.

## HIL 2 decision

### DEC-0004: TechSpec approved for task planning and implementation

- Human response: Pode continuar.
- Decision: approve the revised techspec.md and authorize executable task planning and implementation within its contract.
- Scope effect: authorize the shared archive/rate-limit changes, Copilot adapter, activity integration, validation route, and desktop manual acceptance already specified; no new product scope is added.
- Conditions: preserve HIL1-01 through HIL1-06 deferrals, keep Core pure, borrow credentials read-only, and resolve implementation prerequisite IP-01 before the credential fallback branch is coded. Any contract change or unresolved prerequisite that affects acceptance requires an exception HIL.
- Approved source: techspec.md, SHA-256 0295635F191B1165696818835E7FF4255EC3919D2613861D3F8887897641C093.
- Next authorization: task planning is authorized now; implementation tasks may execute only after the plan is persisted and task-level ownership is assigned.

## Current stage

- Phase: plan-project.
- Next action: delegate task planning to `sdd-plan-tasks`.
- HIL 2 is approved; the task DAG and concrete task artifacts must be created before implementation scheduling.

## Task planning reconciliation

- The first task-planning handle, Lovelace (`01a07cc1-69ce-7852-a7ba-fe742e829807`), remained active without producing task artifacts and was explicitly stopped after status redirection.
- Reconciliation found no `tasks.md`, no `task_*.md`, no partial task diff, and no source or test changes from that handle.
- The task-planning scope is safe to reassign as a fresh attempt with the same approved PRD, TechSpec, IP-01 condition, and exclusive artifact ownership.
- The reassigned Epicurus handle (`01a07d09-bf7c-7663-a09e-a82d487b1ce4`) was also stopped after extended inactivity and produced no task artifacts or partial diff. A completed planner may be reused for the next attempt.
- The reused Hubble handle (`01a07ca0-4798-7a60-a1cc-6c73c6b17364`) was stopped after the same no-output condition and produced no task artifacts or partial diff.
- Three delegated planning attempts produced no writer output. The coordinator therefore assumes the authorized task-planning stage locally, using the same PRD, TechSpec, templates, scope, IP-01 condition, and validation policy; this changes execution ownership only, not the approved contract.
- The first delegated implementation attempt for T01, Lorentz (`01a07d1a-54b3-7943-815d-0fed228b5e9a`), also produced no source/test diff or handoff and was stopped after reconciliation. The coordinator assumes T01 locally under the existing exclusive task scope; no other implementation task is active.

## Task planning handoff

- Agent: coordinator, after delegated planner reconciliation.
- Result: executable plan ready; no product, architecture, or validation scope was added.
- Manifest: `tasks.md`.
- Task cards: `task_01.md` through `task_06.md`.
- Coverage: FR-01 through FR-12, NFR-01 through NFR-07, AC-01 through AC-10, DEC-01 through DEC-10, CMP-01 through CMP-10, and TC-01 through TC-12 are mapped to tasks and evidence.
- DAG: T01 -> T02 -> {T03, T04} -> T05 -> T06. T03 additionally requires IP-01. File and test ownership is disjoint; T06 is serialized integrated validation.
- Validation: project-scoped native MTP commands use `--minimum-expected-tests 1`; the WPF app is built separately; desktop E2E is omitted.
- Open items: IP-01 and the six explicit HIL1 deferrals remain unchanged. T03 must stop with a concrete gap if the sanitized plaintext fixture cannot be obtained.
- Next authorization: DEC-0004 already authorizes implementation within this plan; task-level execution must use the manifest and one exclusive executor per eligible task.

## T01 implementation handoff

- Agent: coordinator, after reconciling the inactive delegated T01 attempt.
- Result: complete; no product, architecture, or validation scope was added.
- Delivery: Core model and policy seams, pure snapshot retention, and focused Core tests are implemented under T01 ownership.
- Validation: .NET SDK 10.0.400 native Microsoft.Testing.Platform; Core build exit 0; 43 Core tests passed with `--minimum-expected-tests 1`; `git diff --check` passed; Core dependency inspection found no external references.
- Changed files: the nine T01-owned source/test files listed in `task_01.md`; no T02 or later implementation files were changed.
- Decision: `None - direct TechSpec implementation or local decision`; the retention result preserves a visible NeedsAuth/Unsupported current snapshot while marking archive history for clearing.
- Next action: execute T02 with exclusive ownership of its listed archive, UsageStore, and Infrastructure engine test files.

## T02 implementation handoff

- Agent: coordinator, after local execution under the T02 exclusive scope.
- Result: complete; no product, architecture, or validation scope was added.
- Delivery: TokenHound-owned JSON archive, serialized atomic writes, startup stale recovery, durable deadline gate, independent activity timer, and `ActivityUpdated` event args.
- Validation: Core 43 tests passed; Infrastructure build exit 0; all 283 Infrastructure tests passed with the native MTP minimum-test guard; `git diff --check` passed. The restore/build output retains the pre-existing NU1903 SQLite package advisory.
- Changed files: the T02-owned source/test files listed in `task_02.md`, including `UsageArchive.Persistence.cs`, `UsageStore.Refresh.cs`, and `UsageStore.Activity.cs`; no provider, activity-monitor heuristic, App, or WPF files were changed.
- Decision: `None - direct TechSpec implementation or local decision`; an optional archive injection keeps existing in-memory UsageStore callers side-effect free while the App composition root can provide the TokenHound archive. Partial files keep each implementation file within the repository's 300-line limit without changing the class contract.
- Next action: execute T04 now; execute T03 only after IP-01 fixture evidence is resolved or its acceptance gap is explicitly recorded.

## T04 implementation handoff

- Agent: coordinator, after local execution under the T04 exclusive scope.
- Result: complete; no product, architecture, or validation scope was added.
- Delivery: Copilot activity monitor, shared timestamp reads, 120 ms watcher debounce, qualifying host detector, and injected deterministic tests.
- Validation: focused T04 tests passed (4 activity, 5 process-host); complete Infrastructure validation passed with 292 tests; build exit 0; `git diff --check` passed. The only build warnings are the pre-existing NU1903 SQLite advisory.
- Changed files: the four T04-owned source/test files listed in `task_04.md`; no provider HTTP, credential, UsageStore, App, or WPF files were changed.
- Decision: `None - direct TechSpec implementation or local decision`; Code.exe is accepted only with a supported GitHub Copilot extension directory, while copilot and gh qualify directly.
- Next action: resolve IP-01 in an isolated Copilot CLI home before executing T03; if no sanitized official plaintext fixture can be obtained, record the concrete acceptance gap and leave that fallback unavailable.

## T03 implementation handoff

- Agent: coordinator, after local execution with the explicit IP-01 gap recorded.
- Result: complete with one scoped acceptance gap; no product or architecture scope was added.
- Delivery: Copilot credential precedence, validated keychain target derivation, disabled-by-default unverified config fallback, bounded HTTP client, open-map parser, status/overage mapping, and provider tests.
- Validation: Infrastructure build exit 0; Copilot-focused tests passed with 37 tests; the then-current full Infrastructure suite passed with 320 tests; current integrated validation passes with 324 Infrastructure tests; `git diff --check` passed. No live endpoint or real token was used.
- Changed files: the ten T03-owned provider source files, including `CopilotConfigReader.Models.cs`, and the seven Copilot test files listed in `task_03.md`.
- Decision: `None - direct TechSpec implementation`; IP-01 is an explicit prerequisite gap, not an architectural decision.
- Open item: the official CLI plaintext token property remains unverified. `CopilotConfigReader` accepts a token only when a caller supplies an explicit verified property path, so the default fallback remains unavailable and must not be enabled by guessing.
- Next action: execute T05, then T06 integrated validation.

## T05 implementation handoff

- Agent: coordinator, after local execution under the T05 exclusive scope.
- Result: complete; no provider-specific HUD or deferred product decision was added.
- Delivery: App composition registration for Copilot provider/monitor and TokenHound archive, ActivityUpdated routing through the existing dispatcher/ring, generic Unsupported/ActiveBlock presentation, and disposal ownership.
- Validation: Core build exit 0 and 43 tests passed; Infrastructure build exit 0 with pre-existing NU1903 advisory and 324 tests passed; App build exit 0 with the same advisory; Copilot and UsageStore filters each passed with 24 tests; `git diff --check` passed. Desktop E2E was omitted by policy.
- Changed files: `src/TokenHound.App/App.xaml.cs`; `src/TokenHound.App/ViewModels/NotchViewModel.cs`; `src/TokenHound.App/ViewModels/ProviderRingViewModel.cs`; `tests/TokenHound.Infrastructure.Tests/ViewModels/NotchViewModelTests.cs`; `tests/TokenHound.Infrastructure.Tests/ViewModels/ProviderRingViewModelTests.cs`.
- Validated state: `NotchWindow.xaml.cs` and `WindowStyles.cs` have no diff, preserving WM_MOUSEACTIVATE/MA_NOACTIVATE, WS_EX_NOACTIVATE, click-through geometry, and SWP_NOZORDER behavior. Existing provider paths remain source-compatible.
- Open items: T06 manual Windows MCP evidence is not available in this session and remains essential for completion. IP-01 and HIL1-01 through HIL1-06 remain open as documented.
- Next action: execute T06 automated integrated checks and record the manual-validation limitation without claiming completion.

## T06 integrated validation handoff

- Agent: coordinator, after local integrated validation and code review preparation.
- Result: automated validation and static integration review complete; T06 remains incomplete because the essential Windows MCP/manual acceptance route is unavailable in this session.
- Validation: Core build exit 0 and 43 tests passed; Infrastructure build exit 0 with the pre-existing NU1903 SQLite and xUnit1051 advisories; App build exit 0 with the pre-existing NU1903 advisory; Copilot filter passed with 37 tests; UsageStore filter passed with 24 tests; full Infrastructure tests passed with 324 tests; `git diff --check` passed. No E2E target or aggregate desktop automation was run.
- Coverage: Core purity, credential ownership, project scope, App registration/disposal, ActivityUpdated routing, generic presentation, and unchanged WM_MOUSEACTIVATE/SWP_NOZORDER code were statically reviewed. The code review is `codereview_001/codereview.md`.
- Open items: T06.5 App launch/Screenshot display `[2]`, T06.6 approved-session Busy/Idle transition, and T06.7 foreground focus/drag confirmation are not verifiable without Windows MCP tools. IP-01 remains the explicitly reported plaintext-config fallback gap. These essential items prevent approval.
- Next action: provide the Windows MCP route and an approved existing Copilot session, then rerun only T06.5 through T06.7 and the review status decision.

## T06 review handoff

- Review: `codereview_001/codereview.md`.
- Status: REJECTED, based only on essential unverified acceptance, not on an automated build or test failure.
- Findings: `CR-01` covers unavailable Windows MCP manual evidence; `CR-02` covers the explicitly documented IP-01 plaintext-config fallback gap.
- Automated evidence: Core 43 tests, Copilot 37 tests, UsageStore 24 tests, and full Infrastructure 324 tests passed; Core, Infrastructure, and App builds passed; final `git diff HEAD --check` is clean.
- Required next step: execute T06.5 through T06.7 through the required Windows MCP App/Screenshot route and resolve IP-01 with sanitized official fixture evidence before requesting re-review.
