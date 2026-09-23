# Implementation plan — Claude Code Multi-Profile Support

## Stable sources

- PRD: `tasks/prd-07-claude-multi-profile/prd.md`
- TechSpec: `tasks/prd-07-claude-multi-profile/techspec.md`

> Common sources come before the task and mutable state; read order does not guarantee a cache hit.

## Dependency graph

| ID | Delivery | Depends on | Unblocks |
| --- | --- | --- | --- |
| T01 | Profile discovery model and directory enumeration | — | T02, T03 |
| T02 | Multi-instance provider and session monitor parameterization | T01 | T03 |
| T03 | Catalog mapping, app startup registration, and mock guard | T01, T02 | — |

## Traceability matrix

| Source ID | Source section | Obligation | Tasks | Evidence or test |
| --- | --- | --- | --- | --- |
| FR-01 | `prd.md#functional-requirements` | Multi-profile enumeration | T01 | `TC-01` in `ClaudeProfileDiscoveryTests.cs` |
| FR-02 | `prd.md#functional-requirements` | Active profile qualification | T01 | `TC-02` in `ClaudeProfileDiscoveryTests.cs` |
| FR-03 | `prd.md#functional-requirements` | Unique provider identification | T01 | `TC-01` in `ClaudeProfileDiscoveryTests.cs` |
| FR-04 | `prd.md#functional-requirements` | Dedicated provider instances | T02, T03 | `TC-03` in `ClaudeOAuthProviderTests.cs` |
| FR-05 | `prd.md#functional-requirements` | Dedicated activity monitors | T02, T03 | `TC-04` in `ClaudeSessionMonitorTests.cs` |
| FR-06 | `prd.md#functional-requirements` | Provider catalog mapping | T03 | `TC-05` in `ProviderCatalogTests.cs` |
| FR-07 | `prd.md#functional-requirements` | Mock fallback awareness | T03 | `TC-06` in `NotchViewModelTests.cs` |
| FR-08 | `prd.md#functional-requirements` | Account identification in HUD popup | T03 | `TC-05` in `ProviderCatalogTests.cs` & manual acceptance |
| TC-01 | `techspec.md#test-approach` | Discover profiles with default and work dirs | T01 | `DiscoverProfiles_WithDefaultAndWork_ReturnsBothProfiles` |
| TC-02 | `techspec.md#test-approach` | Inactive directory exclusion | T01 | `DiscoverProfiles_WhenDirectoryLacksCredentials_ExcludesInactive` |
| TC-03 | `techspec.md#test-approach` | Custom providerId snapshot | T02 | `GetSnapshotAsync_WithCustomProviderId_ReturnsMatchingSnapshot` |
| TC-04 | `techspec.md#test-approach` | Custom sessions directory monitor | T02 | `CheckLivenessAsync_WithCustomDirectory_ScansSpecifiedSessions` |
| TC-05 | `techspec.md#test-approach` | Catalog name/badge/glyph for claude-* | T03 | `ResolveDefaultName_WithClaudeSlug_ReturnsFormattedName` |
| TC-06 | `techspec.md#test-approach` | Mock fallback with isolated profile | T03 | `ShouldFallbackToMock_WhenIsolatedProfileHasCredentials_ReturnsFalse` |

## Tasks

- [T01 — Profile discovery model and directory enumeration](done/task_01.md): Introduce `ClaudeProfile` record and implement `ClaudeProfileDiscovery.DiscoverProfiles` with unit tests.
- [T02 — Multi-instance provider and session monitor parameterization](done/task_02.md): Parameterize `ClaudeOAuthProvider` and `ClaudeSessionMonitor` by `providerId` and directory path.
- [T03 — Catalog mapping, app startup registration, and mock guard](done/task_03.md): Extend `ProviderCatalog`, register all active profiles in `App.xaml.cs`, and adapt `NotchViewModel` mock fallback.

## Coverage gate

- Coverage: Pass. Every FR and TC maps to an executable task.
- Traceability: Pass. PRD obligations trace through TechSpec to task files.
- Dependencies: Pass. Acyclic chain T01 -> T02 -> T03.
- Atomicity: Pass. Each task delivers a cohesive, verified vertical slice.
- Executability: Pass. Builds and tests execute using standard project tooling.
- Validation profile: Pass. E2E omitted by .NET desktop policy; unit and integration tests enforce contracts.
- Idempotency: Pass. Re-running tests and builds produces deterministic results.

## Assumptions and open items

- Assumption: Claude profiles follow `.claude` and `.claude-<slug>` under `%USERPROFILE%`.
- Open item: None.
- Required environment: None (local file system and mock responses in unit tests).

## State

- [x] T01 — verified and reconciled in done/task_01.md after T04 & T07
- [x] T02 — verified and reconciled in done/task_02.md after T06
- [x] T03 — verified and reconciled in done/task_03.md after T05

## Problems and solutions

- `codereview_01` rejected the integrated result on findings CR-01 through CR-05.
- All correction tasks are complete in `codereview_01/done/`:
  - `CR-01` -> resolved by `codereview_01/done/task_04.md` (read-only credential validation).
  - `CR-02` -> resolved by `codereview_01/done/task_05.md` (all-profile mock fallback check).
  - `CR-03` -> resolved by `codereview_01/done/task_06.md` (live isolated session fixture and activity event).
  - `CR-04` -> resolved by `codereview_01/done/task_07.md` and Exception HIL DEC-19 (amended NFR-03 benchmark evidence).
  - `CR-05` -> resolved by reconciling original T01–T03 handoffs, moving tasks to `done/`, and updating manifest links and state through the DAG owner.
- The correction round is complete; next step is an independent re-review.
