# T04 — Qualify Claude profiles by readable OAuth token

## Outcome

`DiscoverProfiles(onlyActive: true)` excludes empty, malformed, unreadable, and tokenless credential files while retaining profiles with a readable access token.

## Dependencies and boundaries

- Depends on: none.
- Unblocks: T07.
- In scope: discovery qualification and focused tests.
- Out of scope: authentication, token refresh, and arbitrary profile paths.

## Traceability

| Source | Section | Finding covered |
| --- | --- | --- |
| `codereview_01/CR-01` | `codereview.md#findings` | FR-02 and TC-02 accept file existence without a readable OAuth token. |

## Requirements

- Keep credential access read-only with `FileShare.ReadWrite | FileShare.Delete`.
- An expired but readable token may still register so its own ring can show `NeedsAuth`.
- Preserve default and isolated profile ordering and deterministic IDs.

## Context to recover on demand

- TechSpec: `techspec.md#technical-decisions`, `techspec.md#test-approach`.
- Rules and skills: `AGENTS.md`, `no-workarounds`, `dotnet-efficient-validation`.
- Code: `ClaudeProfileDiscovery.DiscoverProfiles`, `LoadCredentialFromFileAsync`, `SharedFileReader`.

## Work

- [x] T04.1 Use the existing read-only parser to qualify candidate profiles by readable access token.
- [x] T04.2 Cover malformed, empty, tokenless, and valid credentials for default and isolated directories.
- [x] T04.3 Verify the filtered Claude discovery tests and the TechSpec quality profile.

## Acceptance criteria

- `onlyActive: true` returns exactly the profiles whose credential files contain a readable OAuth access token.
- `onlyActive: false` still enumerates candidate directories without requiring credentials.
- No credential file is written or refreshed.

## Verification

- Unit: positive and negative credential fixtures around `DiscoverProfiles`.
- Integration: none.
- E2E: omitted by .NET desktop policy.
- Manual: none.
- Environment dependency: none.
- Commands: `rtk dotnet build TokenHound.slnx --no-restore`; `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*ClaudeProfile*"`.
- Expected evidence: nonzero passing test count and no new blocking quality-profile hit.

## Affected files

- Modify: `src/TokenHound.Infrastructure/Providers/Claude/ClaudeProfileDiscovery.cs`.
- Modify: `tests/TokenHound.Infrastructure.Tests/Providers/ClaudeProfileDiscoveryTests.cs`.
- Create: `tests/TokenHound.Infrastructure.Tests/Providers/ClaudeProfileQualificationTests.cs` to keep each test file below 300 lines.

## Observability and recovery

- Operational signal: only qualified profile IDs reach registration.
- Recovery: restore the prior discovery implementation if qualification regresses.

## Handoff

> Updated by `sdd-execute-corrections` during implementation.

- Produced result: `HasCredentials` now opens each candidate through `SharedFileReader.OpenRead`, parses it with the existing credential parser, and excludes unreadable or tokenless files. The prior positive discovery tests were moved to `ClaudeProfileQualificationTests.cs` so both test files remain below 300 lines. An initial assertion incorrectly expected unfiltered discovery to omit the default descriptor when its directory was absent; it was corrected to assert the tested candidate profile. A later build caught a range expression inside an assertion expression tree; its expected ID is now calculated before the assertion.
- Changed files: `src/TokenHound.Infrastructure/Providers/Claude/ClaudeProfileDiscovery.cs`, `tests/TokenHound.Infrastructure.Tests/Providers/ClaudeProfileDiscoveryTests.cs`, and new `tests/TokenHound.Infrastructure.Tests/Providers/ClaudeProfileQualificationTests.cs`.
- Checks: solution build passed with 0 errors and 0 warnings; focused profile test run passed 23 tests; broader Claude test run passed 68 tests; TechSpec blocking quality scan found no hit in the touched source; `rtk git diff --check` passed. Source and test file lengths are 223, 238, and 122 lines respectively.
- Validated state: Windows, .NET SDK 10.0.401, Debug net10.0, current worktree after T04; MTP runner with `--minimum-expected-tests 1`.
- Open items: NFR-03 timing evidence remains in T07. The desktop HUD script remains for HIL 3.
