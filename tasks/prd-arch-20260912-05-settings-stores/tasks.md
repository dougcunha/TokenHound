# Implementation plan — Refactoring settings stores onto one section store

## Stable sources

- PRD: `tasks/prd-arch-20260912-05-settings-stores/prd.md`
- TechSpec: `tasks/prd-arch-20260912-05-settings-stores/techspec.md`

> Common sources before the task and mutable state; read order does not guarantee a cache hit.

## Dependency graph

| ID | Delivery | Depends on | Unblocks |
| --- | --- | --- | --- |
| T01 | `SectionStore<T>` added; Hud and Refresh stores delegate | — | T02 |
| T02 | RateLimit and Provider stores delegate (including the extra converter) | T01 | — |

## Traceability matrix

| Source ID | Source section | Obligation | Tasks | Evidence or test |
| --- | --- | --- | --- | --- |
| R-01 | `prd.md#behaviors-to-preserve` | Hud store unchanged | T01 | `HudPositionStoreTests` |
| R-02 | `prd.md#behaviors-to-preserve` | Provider store unchanged incl. converter | T02 | `ProviderSettingsStoreTests` |
| R-03 | `prd.md#behaviors-to-preserve` | Rate-limit store unchanged | T02 | `RateLimitSettingsStoreTests` |
| R-04 | `prd.md#behaviors-to-preserve` | Refresh store unchanged | T01 | `RefreshSettingsStoreTests` |
| R-05 | `prd.md#behaviors-to-preserve` | Missing-section `FromJson` behavior | T01, T02 | store tests |
| R-06 | `prd.md#behaviors-to-preserve` | Other sections preserved on save | T01, T02 | `UserSettingsFileTests` |
| R-07 | `prd.md#behaviors-to-preserve` | Constructor path resolution | T01, T02 | store tests |
| DEC-01..DEC-04 | `techspec.md#technical-decisions` | Section store + facades | T01, T02 | code review |
| QA-01..QA-03 | `techspec.md#quality-profile` | Duplicate counts reduce | T01, T02 | scoped `rtk rg` |
| TC-01..TC-04 | `techspec.md#safety-net` | Store/concurrency tests | T01, T02 | MTP commands |

## Tasks

- [T01 — Add `SectionStore<T>` and migrate Hud/Refresh](done/task_01.md): the shared primitive exists and two simple stores delegate to it.
- [T02 — Migrate RateLimit and Provider stores](done/task_02.md): the remaining two stores delegate, including the provider converter.

## Coverage gate

- Coverage: pass — AA-09 maps to T01 and T02.
- Traceability: pass — R-01..R-07, DEC-01..04, QA-01..03, TC-01..04 mapped.
- Dependencies: pass — T01 → T02; no cycles.
- Atomicity: pass — each task one reviewable result.
- Executability: pass — MTP commands recorded.
- Validation profile: pass — E2E omitted for .NET desktop.
- Idempotency: pass.

## Assumptions and open items

- Assumption: the four public facades remain the call-site contract.
- Open item: none blocking.
- Required environment: none.

## State

- [x] T01 — done
- [x] T02 — done

## Problems and solutions

- T01 quality-profile targets (QA-01=2, QA-02=1, QA-03=1) are feature end-state measures that also require migrating `RateLimitSettingsStore` and `ProviderSettingsStore` (T02). After T01 the folder counts are 5/3/3; `SectionStore<T>` adds exactly one hit per pattern and the migrated Hud/Refresh facades remove their four option blocks and two path/helper blocks. Residual hits belong to the untouched T02 files; no blocking hit is attributable to T01. T02 closes the targets.
- T02 needed the provider converter injected into the shared serializer options (DEC-04). `SectionStore<T>` was extended (still one `AllowTrailingCommas = true` literal) with an internal `SharedOptions` plus an optional `JsonSerializerOptions? options` parameter on `FromJson`; `ProviderSettingsStore` clones the shared options and adds `UserSettings.ProviderSettingsJsonConverter`. T02 also edited `SectionStore.cs`, which was not in its "Affected files" list but is the shared primitive created by T01 and the parameterization DEC-04 requires.
- Feature quality profile reached after T02: QA-01 = 2 (`SectionStore.cs:19`, pre-existing `UserSettingsFile.cs:26`), QA-02 = 1 (`SectionStore.cs:61`), QA-03 = 1 (`SectionStore.cs:151`).

### Reopen after code review (`codereview_1/codereview.md`, status REJECTED)

- Reason: `sdd-review-code` rejected the feature. T01 and T02 were moved back from `done/` to the feature root and their manifest state reset to pending; contracts and IDs preserved. T02 depends on T01, so both must be revalidated after T01 changes.
- Previous handoffs are preserved in the reopened `task_01.md` and `task_02.md` (`## Handoff`).
- Findings:
  - `CR-01` (Medium, T02): `ProviderSettingsStore.FromJson` delegates to `SectionStore.FromJson`, whose `JsonElement.TryGetProperty` is case-sensitive, so a lower-case root `providers` section now returns the all-enabled default instead of the stored state. Prior behavior deserialized `UserSettings` with `PropertyNameCaseInsensitive = true`.
  - `CR-02` (Low, T01+T02): every `(string? filePath, string? baseDirectory = null)` facade constructor was replaced by a non-optional two-argument constructor plus a new one-argument overload, changing public constructor metadata and the overload set. Conflicts with the PRD "public API unchanged" constraint and DEC-02; QA-02 currently rewards removing the optional declarations. Needs an approved specification decision (HIL).
  - `CR-03` (Low, T01): `SectionStore<T>` stores delegates instead of owning the section name, so DEC-01 is only partially implemented; `T01-ADR-01` is unapproved.
  - `CR-04` (Low, T01+T02): four nested `SectionStore` constructor calls close as `baseDirectory))` instead of placing the closing parenthesis on its own line.
- Verification of the rejected work is retained in the task handoffs; the 675-test project run, QA counts 2/1/1, and the integrated build still reflect this state but do not clear CR-01..CR-04.
- Caller decisions after review:
  - CR-02: restore the exact public constructor surface (`(string? filePath, string? baseDirectory = null)`) in all four facades and revise QA-02. QA-02 now measures `SettingsPathResolver.ResolveOverride` (baseline 4, target 1) so it guards shared path resolution without conflicting with the PRD API constraint.
  - CR-03: implement DEC-01 section-name ownership in `SectionStore<T>` rather than approving `T01-ADR-01`.
- Rework outcome (re-executed T01 then T02 from a clean baseline): `SectionStore<T>` now owns `_sectionName` and exposes an instance `FromJson`; all four facades again expose exactly `()`, `(UserSettingsFile)`, `(string? filePath, string? baseDirectory = null)`; `ProviderSettingsStore.FromJson` restored whole-document case-insensitive root deserialization with the provider converter and gained CR-01 regression tests. Final profile: QA-01 = 2, QA-02 = 1, QA-03 = 1.
