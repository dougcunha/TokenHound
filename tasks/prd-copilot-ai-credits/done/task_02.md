# Stable execution context

Load in this exact order:

1. `tasks/prd-copilot-ai-credits/prd.md`
2. `tasks/prd-copilot-ai-credits/techspec.md`
3. This file

Use current source versions already loaded; recover only missing or changed sources. The manifest [tasks.md](tasks.md) is authoritative for dependencies and state.

# T02: Represent and calculate compatible AI-credit values

## Outcome

Pure Core contracts represent billing independently from quota, and tested policies produce balances only from compatible verified inputs. This foundation is separate because direct billing, reports, and presentation all consume it.

## Dependencies and boundaries

- Depends on: None.
- Unblocks: T03, T04, T05, T06.
- In scope: optional snapshot billing property, immutable models/enums, aggregation/compatibility policy, and pure tests.
- Out of scope: network, persistence, UI, real allowance discovery, and changes to operational LimitWindow semantics.

## Traceability

| Source | Section | Obligation covered |
| --- | --- | --- |
| FR-01, FR-04, FR-05, FR-07, FR-08, FR-09; NFR-04, NFR-06 | PRD requirements | Metric separation, decimal dimensions, provenance, and guarded calculations. |
| DEC-01, DEC-04; CMP-01/02; TC-01/03/05/12 | TechSpec | Contract and pure-policy behavior. |

## Context to recover on demand

- Skills: repository-cli-efficiency and dotnet-efficient-validation before validation.
- Existing code: `src/TokenHound.Core/Models/Snapshot.cs`, `LimitWindow.cs`; Core.Tests policy conventions.
- Contract: TechSpec Public domain contract and Policy behavior.

## Work

- [x] T02.1 Add the optional CopilotBilling snapshot property and the named domain records/enums, each in its own file, with required metadata and nullable quantities.
- [x] T02.2 Implement checked decimal aggregation, independent missing-dimension handling, compatible owner/unit/period/coverage checks, and unsupported-data outcomes.
- [x] T02.3 Implement evidence-gated remaining and fraction, including signed overage and null fraction for zero allowance.
- [x] T02.4 Add policy fixtures for valid authoritative evidence, missing/undocumented allowance, mixed dimensions, decimals, overflow, zero/negative totals, promotions/seat changes, and partial/mismatched periods.
- [x] T02.5 Verify old snapshots deserialize with null billing and operational quota fields cannot construct credit values through the policy.

## Acceptance criteria

- The 5,700/725 verified fixture yields 4,975; the same usage without verified allowance yields null total/remaining/fraction.
- Gross, discount, and net are independent; monetary values and internal quota values cannot enter the credit calculation.
- Partial/unknown coverage or incompatible inputs suppress dependent calculations while preserving valid reported dimensions.
- Core stays free of OS, WPF, HTTP, and new dependencies.

## Verification

- Unit: TC-01/03/05/12 pure cases, including serialization compatibility.
- Integration: no external integration; fixture evidence is deliberately synthetic and does not close T01.
- E2E: omitted by .NET desktop policy.
- Manual: inspect public contract documentation and exact decimal assertions.
- Commands: Follow the TechSpec validation profile: effective SDK 10.0.400, xUnit v3/MTP executable route, Release, restore only when needed, then build the affected test project with `--no-restore -c Release --nologo --verbosity:minimal`. Preserve each build exit code. Reuse a valid build for unchanged inputs; inspect actual execution counts and failures. Use `rtk proxy` only for hidden failure details. No publish or full solution validation is required.

```powershell
rtk dotnet run --project tests/TokenHound.Core.Tests/TokenHound.Core.Tests.csproj --no-build --no-restore -c Release -- --minimum-expected-tests 1 --filter-class "*Copilot*"
$taskExit = $LASTEXITCODE
if ($taskExit -ne 0) { throw "Task validation failed: $taskExit" }
```

- Environment dependency: local .NET SDK/package assets only.
- Expected evidence: successful scoped build, nonzero passing Core tests, and pure dependency inspection.

## Affected files

- Modify: `src/TokenHound.Core/Models/Snapshot.cs`.
- Create: CMP-01 billing records/enums under `src/TokenHound.Core/Models/`; `src/TokenHound.Core/Policies/CopilotCreditPolicy.cs`.
- Create: `tests/TokenHound.Core.Tests/Policies/CopilotCreditPolicyTests.cs` and a separate Copilot serialization test class if needed.

## Observability and recovery

- Signal: typed unavailable/invalid outcomes; no logging dependency in Core.
- Recovery: additive model change can be reverted before consumers activate; old archive compatibility remains covered.

## Handoff

> Updated by `sdd-execute-task` during implementation.

- Produced result: Complete for the T02 pure Core contract and policy. Copilot billing is represented independently from operational quota; consumption remains visible when allowance evidence is absent, and derived values are guarded by verified compatible metadata.
- Changed files: Added Copilot billing records and enums under `src/TokenHound.Core/Models/` (`CopilotAllowanceEvidence.cs`, `CopilotBillingContext.cs`, `CopilotBillingPeriod.cs`, `CopilotBillingReason.cs`, `CopilotBillingScope.cs`, `CopilotBillingState.cs`, `CopilotBillingStatus.cs`, `CopilotCreditAggregationRequest.cs`, `CopilotCreditFilter.cs`, `CopilotCreditPolicyOutcome.cs`, `CopilotCreditSource.cs`, `CopilotCreditUsage.cs`, `CopilotCreditUsageItem.cs`, `CopilotPlanType.cs`, `CopilotReportCoverage.cs`); added `CopilotCreditPolicy`, `CopilotCreditPolicyAggregation`, `CopilotCreditPolicyEvaluator`, and `CopilotCreditPolicyMetadata` under `src/TokenHound.Core/Policies/`; added optional `Snapshot.CopilotBilling` property to `src/TokenHound.Core/Models/Snapshot.cs`; added unit test classes `CopilotCreditPolicyTests.cs`, `CopilotCreditPolicyCompatibilityTests.cs`, `CopilotCreditPolicySafetyTests.cs`, and `CopilotSnapshotSerializationTests.cs`. T04 retention edits were excluded from `SnapshotRetentionPolicy.cs` to keep T02 strictly pure.
- Checks: `rtk dotnet build tests/TokenHound.Core.Tests/TokenHound.Core.Tests.csproj --no-restore -c Release --nologo --verbosity:minimal` passed (2 projects, 0 errors, 0 warnings). `rtk dotnet run --project tests/TokenHound.Core.Tests/TokenHound.Core.Tests.csproj --no-build --no-restore -c Release -- --minimum-expected-tests 1 --filter-class "*Copilot*"` passed (27/27). `rtk dotnet run --project tests/TokenHound.Core.Tests/TokenHound.Core.Tests.csproj --no-build --no-restore -c Release -- --minimum-expected-tests 1 --filter-class "*Snapshot*"` passed (6/6). Full Core test suite `rtk dotnet run --project tests/TokenHound.Core.Tests/TokenHound.Core.Tests.csproj --no-build --no-restore -c Release -- --minimum-expected-tests 1` passed (72/72). `rtk git diff --check` passed.
- Validated state: .NET 10 SDK with Microsoft.Testing.Platform executable route; Release build and tests executed on current sources. Pure Core invariants preserved with zero UI, OS, HTTP, or new package dependencies. C# structure rules verified: all files <= 300 lines, methods <= 30 lines, nesting <= 3 levels, alphabetized usings, blank lines inside multi-line blocks and before control flow statements, and multi-line calls for >= 4 arguments.
- Open items: External ownership evidence OI-01 and authoritative production allowance mapping OI-02 remain outside T02; real provider, persistence, HUD/manual, and desktop E2E evidence belong to later tasks (desktop E2E is omitted by the .NET validation profile). No remaining changed-source issues.

### ADR candidates

None - direct TechSpec implementation or local decision.
