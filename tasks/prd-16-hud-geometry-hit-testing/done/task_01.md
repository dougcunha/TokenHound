# Stable execution context

Load the current versions in this order: `prd.md`, `techspec.md`, then this file. Recover only missing/changed sources.

# T01: Define and verify the shared contour

## Outcome

A recorded pre-change desktop baseline and one deterministic contour definition for all six existing modes, ready for both render surfaces.

## Dependencies and boundaries

- Depends on: recorded HIL 2 approval. Unblocks: T02.
- Scope: baseline evidence, pure geometry, content-safe layout, linked-source tests.
- Excludes: visible HUD integration, provider/data/settings changes, new test frameworks.

## Traceability

| Source | Section | Covered obligation |
| --- | --- | --- |
| FR-01..03, FR-08, FR-11, PD-02..03 | prd.md#functional-requirements; user-experience | Shapes, corner bounds, safe body |
| OBJ-03, NFR-03, NFR-05 | prd.md#non-functional-requirements | Baseline and test/data boundaries |
| DEC-01, DEC-07..08, CMP-01, CMP-07, TC-01 | techspec.md#components-and-flow; test-approach | Canonical definition and tests |

## Context to recover on demand

- Rules/skills: TechSpec sources and Quality profile; dotnet-efficient-validation before commands.
- Code: HudEdgeLayout/HudDockMode, existing NotchPlacementTests, Infrastructure test csproj linked-source pattern.
- Geometry contract and Manual acceptance script in the TechSpec; baseline is unmeasured.

## Work

- [x] T01.1 Capture baseline with the TechSpec desktop script step 1 and record exact environment/results in validation.md before production edits.
- [x] T01.2 Implement immutable frame/point/segment records and canonical top contour with side transforms, Free closure, bounded joins and content-safe body bounds.
- [x] T01.3 Link pure files into the existing test executable and add meaningful boundary/mirror/tangent/minimum/content tests. Test invalid and zero-sized inputs distinctly.
- [x] T01.4 Build affected projects, run the targeted MTP checks, apply the quality profile, and record the validated diff and pending desktop items.

## Acceptance criteria

- Shared closed contours are finite, smooth, bounded, and preserve the required child body for all modes; unavailable top-corner continuations are absent.
- Pure code has no WPF/Win32/Core changes. Each new class/file respects the repository limits.
- Baseline evidence records actual cross-process clicks/focus and existing drag/menu behavior, with no fabricated passes.

## Verification

- Unit: TC-01, known boundary samples, closure/tangent/mirror invariants and invalid-input behavior.
- Integration: no new external contract. Existing App/test compilation verifies linked production sources.
- E2E: omitted by .NET desktop policy.
- Manual: coordinator baseline script step 1 on the user desktop. Missing essential baseline remains pending; independent unit work may proceed.
- Commands: TechSpec Test approach builds, then `rtk dotnet run --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj -c Release --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*HudContour*"`; check `$LASTEXITCODE` and executed count.
- Evidence: baseline table, build/test output summary, exact changed files and quality results.

## Affected files

- Create under `src/TokenHound.App/UI/Placement/`: HudContourLayout.cs, HudContourFrame.cs, HudContourSegment.cs, HudContourPoint.cs.
- Create: `tests/TokenHound.Infrastructure.Tests/Placement/HudContourLayoutTests.cs`, this feature's validation.md.
- Modify: `tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj` only for source links.

## Observability and recovery

No production logging or HWND is added in this task. Reversal removes new pure files/test links while preserving the captured baseline and existing settings.

## Handoff

- Produced result: recorded pre-change desktop baseline; canonical closed contour and immutable point/segment/frame contracts for all six modes. T01 approved by coordinator task review; read-only risk check found no production defect and its test gaps were corrected.
- Changed files: four new `src/TokenHound.App/UI/Placement/HudContour*.cs` files; new `tests/TokenHound.Infrastructure.Tests/Placement/HudContourLayoutTests.cs`; four source links in the existing Infrastructure test csproj; feature validation.md.
- Checks: App and Infrastructure test Release builds, `--no-restore --nologo --verbosity:minimal`: zero errors/warnings. Targeted native MTP command from Verification: 19 passed, 0 failed/skipped, exit 0 with minimum expected tests 1. Geometry checks cover closure, sampled non-intersection, exposed tangent continuity, mirror/rotation boundaries, body containment, minimum/empty/populated layouts, invalid/zero extents, overflow, and mutation rejection. Scoped quality profile and `git diff --check` passed; no new reservations. File sizes: production 12/18/35/290 lines; tests 293 lines.
- Validated state: base `6f2e92b`; code and source links currently in worktree, before visible T02 integration. App build remains valid after the final test-only assertion changes. Diff fingerprint is recorded in validation.md.
- Open items: T02 actual WPF rendering/native alpha/shadow/focus smoke; T03 complete desktop matrix, physical display loss, human visuals, independent review, HIL 3. Baseline's observed corner/shadow input failures are recorded, not treated as final passes. Pure tests do not prove cross-process routing.

### ADR candidates

None. The canonical definition follows the already-approved TechSpec decision; no new durable architecture choice was introduced.
