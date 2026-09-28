# Stable execution context

Load in this exact order:

1. `tasks/prd-11-auto-update/prd.md`
2. `tasks/prd-11-auto-update/techspec.md`
3. This file

Use the current PRD and TechSpec versions already loaded; recover only missing or changed sources. This order does not guarantee a host cache hit.

---

# T01 — Core update domain: versions, release model, and update policies

## Outcome

`TokenHound.Core` can parse and compare release versions, decide whether a GitHub release is an update (prerelease, draft, and skipped versions excluded), decide whether a periodic check is due, pick the right asset for an install mode and architecture, and parse a `sha256:` digest. All of this is pure and has unit tests.

## Dependencies and boundaries

- Depends on: —
- Unblocks: T03, T04
- In scope: CMP-01..CMP-04 (`ReleaseVersion`, `ReleaseInfo`, `ReleaseAsset`, `InstallMode`, `UpdateCheckOutcome`, `UpdatePolicy`, `UpdateAssetSelector`) and their Core tests.
- Out of scope: HTTP, file system, settings persistence, UI. Architecture detection itself (`RuntimeInformation`) happens in callers; the selector takes the architecture as input.

## Traceability

| Source | Section | Obligation covered |
| --- | --- | --- |
| FR-01 | `prd.md#functional-requirements` | Tag vs running version comparison |
| FR-02 | `prd.md#functional-requirements` | Prerelease/draft ignored |
| FR-03 | `prd.md#functional-requirements` | Check due-ness (interval, disabled, 0) |
| FR-05 | `prd.md#functional-requirements` | Skip-version policy |
| FR-07 | `prd.md#functional-requirements` | Asset per mode × architecture |
| FR-08 | `prd.md#functional-requirements` | `sha256:` digest parsing |
| NFR-01 | `prd.md#non-functional-requirements` | Policies pure in Core |
| DEC-01, DEC-02, DEC-06 | `techspec.md#technical-decisions` | Version source format, comparison, asset patterns |
| CMP-01..CMP-04 | `techspec.md#components-and-flow` | Core components |
| TC-01..TC-06 | `techspec.md#test-approach` | Core unit tests |

## Context to recover on demand

- Applicable skills and rules: `AGENTS.md` C# structure/style, `dotnet-efficient-validation`.
- Existing code: `src/TokenHound.Core/Policies/RateLimitPolicy.cs` (Core policy style, XML docs), `src/TokenHound.Core/Models/Snapshot.cs` (record DTO style), `tests/TokenHound.Core.Tests/Policies/` (test layout, AwesomeAssertions).
- Contract or integration: `techspec.md#contracts-and-data` (GitHub fields mirrored by `ReleaseInfo`/`ReleaseAsset`).

## Work

- [x] T01.1 `ReleaseVersion` record: `TryParse` accepting `v1.2.3`, `1.2.3`, `1.2.3-beta.1`, `1.2.3+meta` (metadata dropped); `IComparable` with a prerelease below its release; `ToString()` without `v`.
- [x] T01.2 `ReleaseAsset`, `ReleaseInfo` (records with `required`/`init`), `InstallMode` (`Portable`, `Installed`), `UpdateCheckOutcome` (kind + optional release + reason).
- [x] T01.3 `UpdatePolicy.Evaluate(current, release, skippedVersion)` and `UpdatePolicy.IsCheckDue(nowUtc, lastCheckUtc, intervalHours, enabled)`.
- [x] T01.4 `UpdateAssetSelector.Select(release, mode, architecture)` using the DEC-06 patterns (case-insensitive), and `TryParseSha256Digest`.
- [x] T01.5 Core tests TC-01..TC-06.

## Acceptance criteria

- Newer stable tag → `Available`; equal, older, unparseable, prerelease, or draft → not available; a tag equal to `SkippedVersion` → `Skipped`; a tag newer than the skipped one → `Available`.
- `IsCheckDue` is true when enabled and (no last check or elapsed ≥ interval); false when disabled or interval 0.
- The selector returns the zip for `Portable` and the setup for `Installed`, for both `win-x64` and `win-arm64`; unsupported architecture or no matching name → no asset.
- `sha256:<64 hex>` parses; any other algorithm or malformed value → null.
- No reference to UI, OS, or IO types in the new Core files.

## Verification

- Unit: TC-01..TC-06 in `tests/TokenHound.Core.Tests/{Models,Policies}/`.
- Integration: none.
- E2E: omitted by .NET desktop policy.
- Manual: none.
- Commands: `rtk dotnet build TokenHound.slnx --no-restore`; `rtk dotnet test --project tests/TokenHound.Core.Tests --no-build --no-restore -- --minimum-expected-tests 1`.
- Environment dependency: none.
- Expected evidence: passing Core test run listing the new test classes.

## Affected files

- Create: `src/TokenHound.Core/Models/{ReleaseVersion,ReleaseInfo,ReleaseAsset,InstallMode,UpdateCheckOutcome}.cs`, `src/TokenHound.Core/Policies/{UpdatePolicy,UpdateAssetSelector}.cs`, `tests/TokenHound.Core.Tests/Models/ReleaseVersionTests.cs`, `tests/TokenHound.Core.Tests/Policies/{UpdatePolicyTests,UpdateAssetSelectorTests}.cs`.

## Observability and recovery

- Operational signal: none (pure code).
- Recovery: revert the new files; nothing else depends on them yet.

## Handoff

> Updated by `sdd-execute-task` during implementation.

- Produced result: `ReleaseVersion` (parse `v`-prefixed tags, informational versions with `+metadata`, prerelease labels; SemVer-style ordering, prerelease below release), `ReleaseInfo`, `ReleaseAsset`, `InstallMode`, `UpdateCheckStatus`, `UpdateCheckOutcome`, `UpdatePolicy.Evaluate`/`IsCheckDue`, `UpdateAssetSelector.Select`/`ResolveRuntimeIdentifier`/`TryParseSha256Digest`. All pure Core (no IO/UI/OS usings). `UpdateCheckStatus` already includes `RateLimited` and `Failed`, which T03 needs; `IsCheckDue` treats a last check in the future (clock moved back) as due.
- Changed files: created `src/TokenHound.Core/Models/{ReleaseVersion,ReleaseAsset,ReleaseInfo,InstallMode,UpdateCheckStatus,UpdateCheckOutcome}.cs`, `src/TokenHound.Core/Policies/{UpdatePolicy,UpdateAssetSelector}.cs`, `tests/TokenHound.Core.Tests/Models/ReleaseVersionTests.cs`, `tests/TokenHound.Core.Tests/Policies/{UpdatePolicyTests,UpdateAssetSelectorTests}.cs`.
- Checks: `rtk dotnet build tests/TokenHound.Core.Tests/TokenHound.Core.Tests.csproj --no-restore` → 0 errors, 0 warnings; `rtk dotnet test --project tests/TokenHound.Core.Tests --no-build --no-restore -- --minimum-expected-tests 1` → 161 passed; filtered runs: `ReleaseVersionTests` 31 (TC-01, TC-02), `UpdatePolicyTests` 23 (TC-03, TC-04), `UpdateAssetSelectorTests` 15 (TC-05, TC-06). Quality profile over the touched files: QA-01..QA-05 empty; QA-06 largest file 192 lines.
- Validated state: working tree at base `a8bd1bf` plus the files above; Core and Core.Tests projects, Debug, net10.0.
- Open items: reservation QA-08 `src/TokenHound.Core/Policies/UpdatePolicy.cs:57` — `IsCheckDue` has 4 parameters, as the TechSpec contract (DEC-03, CMP-03) specifies; left as is.

### ADR candidates

None - direct TechSpec implementation or local decision.
