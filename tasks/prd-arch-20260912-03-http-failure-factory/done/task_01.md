# Stable execution context

Load in this exact order:

1. `tasks/prd-arch-20260912-03-http-failure-factory/prd.md`
2. `tasks/prd-arch-20260912-03-http-failure-factory/techspec.md`
3. This file

Use the current PRD and TechSpec versions already loaded; recover only missing or changed sources. This order does not guarantee a host cache hit.

---

# T01 — Add shared failure factory; migrate Cursor and Antigravity

## Outcome

`ProviderHttpException.FromResponse` exists and both Cursor and Antigravity build failures through it; their private `CreateException` copies are gone and responses produce identical exceptions.

## Dependencies and boundaries

- Depends on: —
- Unblocks: T02
- In scope: add the factory; delete `CursorApiClient.CreateException` and `AntigravityCloudCodeClient.CreateException`; retarget their call sites.
- Out of scope: Copilot Retry-After call sites (T02); exception hierarchy (AA-11).

## Traceability

| Source | Section | Obligation covered |
| --- | --- | --- |
| R-01, R-02, R-05 | `prd.md#behaviors-to-preserve` | Identical failure outcomes |
| DEC-01..DEC-03 | `techspec.md#technical-decisions` | Factory + migrations |
| QA-01 | `techspec.md#quality-profile` | Duplicate constructor count 2 → 0 |

## Context to recover on demand

- Applicable skills: `dotnet-efficient-validation`, `no-workarounds`.
- Existing code: `ProviderHttpException.cs`, `HttpRetryAfterParser.cs:10-32`, `CursorApiClient.cs:75,108-117`, `AntigravityCloudCodeClient.cs:134,143-152`.
- Contract or integration: TechSpec `#technical-decisions`.

## Work

- [x] T01.1 Add `internal static ProviderHttpException FromResponse(HttpResponseMessage response, string message)` parsing Retry-After only for 429 via `HttpRetryAfterParser.ExtractSeconds(response, TimeProvider.System)`.
- [x] T01.2 Replace `CursorApiClient.CreateException` with a factory call, passing the existing message text verbatim.
- [x] T01.3 Replace `AntigravityCloudCodeClient.CreateException` with a factory call, passing the existing message text verbatim.
- [x] T01.4 Remove now-unused members/usings.

## Acceptance criteria

- [x] Cursor and Antigravity produce the same exception message, `StatusCode`, and `RetryAfterSeconds` as before for non-429 and 429 responses.
- [x] `rtk rg -n "private static ProviderHttpException CreateException\(HttpResponseMessage response\)" src` returns 0 hits.
- [x] No new behavior surfaces.

## Verification

- Unit: Cursor 429 with and without Retry-After; non-429 status.
- Integration: Antigravity non-success incl. 429.
- E2E: omitted by .NET desktop policy.
- Manual: none.
- Commands: `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*CursorApiClientTests*"` (and `*AntigravityCloudCodeClientTests*`).
- Environment dependency: none.
- Expected evidence: pass counts plus the zero-hit `rg`.

## Affected files

- Modify: `src/TokenHound.Infrastructure/Providers/ProviderHttpException.cs`
- Modify: `src/TokenHound.Infrastructure/Providers/Cursor/CursorApiClient.cs`
- Modify: `src/TokenHound.Infrastructure/Providers/Antigravity/AntigravityCloudCodeClient.cs`

## Observability and recovery

- Operational signal: none beyond existing exception metadata.
- Recovery: `git revert`.

## Handoff

> Updated by `sdd-execute-task` during implementation.

- Produced result: `ProviderHttpException.FromResponse(HttpResponseMessage, string)` added and both Cursor and Antigravity now build failures through it. Their private `CreateException` methods are deleted; caller-supplied message text is unchanged (`Cursor usage request failed with HTTP ...`, `Google Cloud Code quota request failed with HTTP ...`). Retry-After parses only for 429 via `HttpRetryAfterParser.ExtractSeconds(response, TimeProvider.System)`.
- Changed files: `src/TokenHound.Infrastructure/Providers/ProviderHttpException.cs` (+13), `src/TokenHound.Infrastructure/Providers/Cursor/CursorApiClient.cs` (18 lines changed; `using System.Net` and `CreateException` removed), `src/TokenHound.Infrastructure/Providers/Antigravity/AntigravityCloudCodeClient.cs` (18 lines changed; same removals). Pre-existing uncommitted changes in the 4 provider files were left untouched.
- Checks:
  - `rtk dotnet build --nologo --verbosity:minimal` → ok, 7 projects, 0 errors, 0 warnings; `$LASTEXITCODE=0`.
  - `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*CursorApiClientTests*"` → 4 tests passed, 0 warnings; `$LASTEXITCODE=0`.
  - `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*AntigravityCloudCodeClientTests*"` → 6 tests passed, 0 warnings; `$LASTEXITCODE=0`.
  - QA-01 `rtk rg -n "private static ProviderHttpException CreateException\(HttpResponseMessage response\)" src` → 0 hits (`rg` exit 1, no matches). Baseline 2 → target 0 met.
- Validated state: Code/diff at working-tree HEAD with the three files above; .NET 10 `net10.0`, MTP runner, `--no-build`/`--no-restore` on a fresh green build; tests 4/4 and 6/6 executed (no skips reported); quality profile QA-01 = 0 hits. Message text preserved verbatim by construction; focused tests assert `StatusCode` and `RetryAfterSeconds` for 401/429 (Cursor) and 403 (Antigravity).
- Open items: None. T02 (Copilot Retry-After call sites) remains out of scope; exception hierarchy AA-11 untouched.

### ADR candidates

None - direct TechSpec implementation or local decision.
