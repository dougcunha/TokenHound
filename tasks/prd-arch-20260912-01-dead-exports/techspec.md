# TechSpec — Refactoring orphaned public members

## Sources and traceability

- PRD: `prd.md`
- Current code and tests: `src/TokenHound.Infrastructure/Providers/Copilot/CopilotPassDispatchBudget.cs`, `…/Copilot/CopilotApiClient.cs`, `…/Copilot/CopilotCredentialDiscovery.cs`, `…/Codex/CodexAuthDiscovery.cs`; `tests/TokenHound.Infrastructure.Tests/Providers/Copilot/CopilotMetricsClientTests.cs`, `…/CopilotCredentialDiscoveryTests.cs`, `…/Codex/CodexAuthDiscoveryTests.cs`.
- Applicable instructions and skills: `AGENTS.md`; `dotnet-efficient-validation` (`references/mtp.md`); `repository-cli-efficiency`; `no-workarounds`.

## Technical decisions

| ID | Requirements | Decision | Evidence and reason | Alternatives and trade-offs |
| --- | --- | --- | --- | --- |
| DEC-01 | R-01 | Delete `RemainingDispatches` (`CopilotPassDispatchBudget.cs:32-36`). | Only repo-wide hit is the declaration; sibling members are wired. | Keep as public API — rejected for an application assembly. |
| DEC-02 | R-02 | Delete the `GetUsageAsync` alias (`CopilotApiClient.cs:148-155`) including its `<see cref>` doc line. | Prod and tests call `GetQuotaAsync`. | `[Obsolete]` shim — rejected, no external consumer. |
| DEC-03 | R-03 | Delete `DiscoverCredentialAsync` (`CopilotCredentialDiscovery.cs:93-95`). | Only the Copilot alias is orphaned; OpenCode/Claude equivalents are used. | Keep for symmetry with other providers — rejected, symmetry is not behavior. |
| DEC-04 | R-04 | Delete `DiscoverAccountAsync` (`CodexAuthDiscovery.cs:66-68`). | Single repo-wide hit is the declaration. | Keep as discovery alias — rejected. |

## Affected components

| ID | Component | Current state | Allowed change | Risk and dependencies |
| --- | --- | --- | --- | --- |
| CMP-01 | `CopilotPassDispatchBudget` | Public budget class | Remove `RemainingDispatches` | Low; sibling members untouched (R-01). Depends on — |
| CMP-02 | `CopilotApiClient` | Copilot quota transport | Remove `GetUsageAsync` alias | Low; also edited by `arch-20260912-03`, which must land after this (shared file). |
| CMP-03 | `CopilotCredentialDiscovery` | Copilot credential discovery | Remove `DiscoverCredentialAsync` alias | Low; also edited by `arch-20260912-02` (deferred). |
| CMP-04 | `CodexAuthDiscovery` | Codex auth discovery | Remove `DiscoverAccountAsync` alias | Low; also edited by `arch-20260912-02` (deferred). |

## Safety net

- Profile: .NET 10 (`net10.0`), C# 13; test projects use Microsoft.Testing.Platform (MTP). Runner commands: `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1` and the Core project equivalent.
- E2E: omitted by the .NET desktop policy; no UI surface is touched.
- Command prerequisites and exclusions: build first (`rtk dotnet build`); no desktop E2E projects.
- Manual acceptance: none required — the change is deletion of unreferenced members.

| ID | Requirement | Level | Scenario | Expected result | Command or script |
| --- | --- | --- | --- | --- | --- |
| TC-01 | R-05 | build | Solution builds | 0 errors, 0 warnings introduced | `rtk dotnet build` |
| TC-02 | R-01, R-02 | unit | Copilot budget/API tests | All pass | focused `CopilotMetricsClientTests`, `CopilotApiClientTests` |
| TC-03 | R-03, R-04 | unit | Credential/Codex discovery tests | All pass | focused `CopilotCredentialDiscoveryTests`, `CodexAuthDiscoveryTests` |

## Dependency sequencing

| Step | Depends on | Change | Evidence to advance | Reversal |
| --- | --- | --- | --- | --- |
| 1 | — | Remove `RemainingDispatches` | `rg` returns 0 hits | `git revert` |
| 2 | — | Remove `GetUsageAsync`, `DiscoverCredentialAsync`, `DiscoverAccountAsync` | `rg` returns 0 hits; build green | `git revert` |
| 3 | 1, 2 | Run focused + project tests | tests pass with `--minimum-expected-tests 1` | `git revert` |

## Compatibility and rollout

- Preserved contracts: R-01..R-05.
- Migration or coexistence: none.
- Observability: build/test pass; no runtime signal expected.
- Rollback: single `git revert` per step; the removals are independent.

## Quality profile

Rules this refactoring must satisfy at the end. Baseline is the target to reduce.

| ID | Rule | Class | Verification command | Baseline | Target |
| --- | --- | --- | --- | --- | --- |
| QA-01 | No `RemainingDispatches` reference | blocking | `rtk rg -n "RemainingDispatches" src` | 1 | 0 |
| QA-02 | No Copilot `GetUsageAsync` alias | blocking | `rtk rg -n "GetUsageAsync" src/TokenHound.Infrastructure/Providers/Copilot` | 1 | 0 |
| QA-03 | No Copilot `DiscoverCredentialAsync` alias | blocking | `rtk rg -n "DiscoverCredentialAsync" src/TokenHound.Infrastructure/Providers/Copilot` | 1 | 0 |
| QA-04 | No `DiscoverAccountAsync` | blocking | `rtk rg -n "DiscoverAccountAsync" src` | 1 | 0 |

- Target measures today: 4 orphaned public members; 4 dead declarations.
- Expected measures at the end: 0 orphaned members; 0 dead declarations.

## Risks and open items

- Risk: a hidden external consumer of an `Infrastructure` public member. Mitigation: confirm no out-of-repo consumer before removal (R-05 assumption).
- Open item: none blocking.
