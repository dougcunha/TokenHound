# TechSpec — Refactoring settings stores onto one section store

## Sources and traceability

- PRD: `prd.md`
- Current code and tests: `src/TokenHound.Infrastructure/Configuration/{HudPositionStore,ProviderSettingsStore,RateLimitSettingsStore,RefreshSettingsStore}.cs`, `…/Configuration/SettingsPathResolver.cs`, `…/Configuration/UserSettingsFile.cs`; `tests/TokenHound.Infrastructure.Tests/Configuration/*`.
- Applicable instructions and skills: `AGENTS.md`; `dotnet-efficient-validation`; `repository-cli-efficiency`.

## Technical decisions

| ID | Requirements | Decision | Evidence and reason | Alternatives and trade-offs |
| --- | --- | --- | --- | --- |
| DEC-01 | R-01..R-07 | Add an internal generic `SectionStore<T>` holding `UserSettingsFile`, section-name constant, and the shared `JsonDocumentOptions`/`JsonSerializerOptions`; expose `FromJson`/`Load`/`LoadAsync`/`Save`/`SaveAsync`/`FilePath`. | `RefreshSettingsStore.cs:12-148` and `RateLimitSettingsStore.cs:12-67` read: identical structure. | Four independent stores (status quo) — rejected, change amplification. |
| DEC-02 | R-01..R-04 | Keep the four classes public with identical signatures; each delegates to a `SectionStore<T>` instance (composition), preserving XML docs. | Callers must compile unchanged. | Inheritance from a base — rejected for the provider store's extra converter and differing payloads. |
| DEC-03 | R-07 | Reuse `SettingsPathResolver.ResolveOverride` with the existing default filename per store. | Each store currently calls it from `CreateSettingsFile`. | Change default filename handling — out of scope. |
| DEC-04 | R-02 | Allow the provider store to inject its extra converter into the shared serializer options. | `ProviderSettingsStore` differs only by a converter. | Special-case provider store — rejected; parameterize. |

## Affected components

| ID | Component | Current state | Allowed change | Risk and dependencies |
| --- | --- | --- | --- | --- |
| CMP-01 | new `SectionStore<T>` | absent | Create internal generic | Medium; shared writer path. |
| CMP-02 | `HudPositionStore` | Standalone store | Delegate to `SectionStore<HudPositionSettings>` | Low. |
| CMP-03 | `ProviderSettingsStore` | Store + converter | Delegate, injecting converter | Medium; R-02. |
| CMP-04 | `RateLimitSettingsStore` | Standalone store | Delegate | Low. |
| CMP-05 | `RefreshSettingsStore` | Standalone store | Delegate | Low. |

## Safety net

- Profile: .NET 10 (`net10.0`), MTP runner. `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1`.
- E2E: omitted by the .NET desktop policy.
- Command prerequisites and exclusions: build first; run the four store test classes and `UserSettingsFileTests`/concurrency focused.
- Manual acceptance: none; on-disk JSON shape is asserted in tests.

| ID | Requirement | Level | Scenario | Expected result | Command or script |
| --- | --- | --- | --- | --- | --- |
| TC-01 | R-01, R-07 | unit | Hud position load/save/overrides | Unchanged | `HudPositionStoreTests` |
| TC-02 | R-02 | unit | Provider settings incl. converter | Unchanged | `ProviderSettingsStoreTests` |
| TC-03 | R-03, R-04, R-05 | unit | Rate-limit/refresh load/save/missing section | Unchanged | `RateLimitSettingsStoreTests`, `RefreshSettingsStoreTests` |
| TC-04 | R-06 | unit | Save preserves other sections under concurrency | Unchanged | `UserSettingsFileTests`, `UserSettingsFileConcurrencyTests` |

## Dependency sequencing

| Step | Depends on | Change | Evidence to advance | Reversal |
| --- | --- | --- | --- | --- |
| 1 | — | Add `SectionStore<T>` | build green | revert |
| 2 | 1 | Migrate Hud + Refresh (simplest) | TC-01, TC-03 pass | revert |
| 3 | 2 | Migrate RateLimit + Provider (converter) | TC-02, TC-03 pass | revert |
| 4 | 3 | Full configuration test run | all pass | revert |

## Compatibility and rollout

- Preserved contracts: R-01..R-07.
- Migration or coexistence: none; on-disk format unchanged.
- Observability: `FilePath` and section JSON unchanged.
- Rollback: `git revert` per step.

## Quality profile

| ID | Rule | Class | Verification command | Baseline | Target |
| --- | --- | --- | --- | --- | --- |
| QA-01 | Single shared JSON options block | blocking | `rtk rg -n "AllowTrailingCommas = true" src/TokenHound.Infrastructure/Configuration` | 8 (4 doc + 4 serializer) | 2 |
| QA-02 | Single shared path-resolution call | blocking | `rtk rg -n "SettingsPathResolver\.ResolveOverride" src/TokenHound.Infrastructure/Configuration` | 4 | 1 |
| QA-03 | Single `CreateSettingsFile` helper | blocking | `rtk rg -n "private static UserSettingsFile CreateSettingsFile" src/TokenHound.Infrastructure/Configuration` | 4 | 1 |

- Target measures today: 4 store implementations; 4 duplicated option/ctor/path blocks.
- Expected measures at the end: 1 `SectionStore<T>` + 4 thin facades; 1 shared block each.
- QA-02 revised after review CR-02 (decision: honor the PRD public-API constraint): the four facades keep `(string? filePath, string? baseDirectory = null)` unchanged, so the metric now counts the shared `SettingsPathResolver.ResolveOverride` call instead of the public optional-parameter declarations.

## Risks and open items

- Risk: accidental JSON-shape change on disk. Mitigation: assert round-trip and missing-section behavior in the existing store tests; keep section-name constants byte-identical.
- Open item: none blocking.
