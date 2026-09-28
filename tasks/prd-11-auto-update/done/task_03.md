# Stable execution context

Load in this exact order:

1. `tasks/prd-11-auto-update/prd.md`
2. `tasks/prd-11-auto-update/techspec.md`
3. This file

Use the current PRD and TechSpec versions already loaded; recover only missing or changed sources. This order does not guarantee a host cache hit.

---

# T03 — GitHub release client and update check service

## Outcome

`UpdateCheckService.CheckAsync` returns an `UpdateCheckOutcome` for the running version: it asks the gate, calls `releases/latest` with the required headers, maps the JSON, applies `UpdatePolicy`, records `lastCheckUtc`, records rate limits in the gate, and shares one in-flight check between concurrent callers.

## Dependencies and boundaries

- Depends on: T01, T02
- Unblocks: T06, T07
- In scope: CMP-08 (`GitHubReleaseClient`, `UpdateRateLimitedException`), CMP-09 (`UpdateCheckService`), running-version resolution helper (informational version, `+` suffix dropped; DEC-01), tests TC-09..TC-11.
- Out of scope: downloads (T04), scheduling (T07), UI (T06).

## Traceability

| Source | Section | Obligation covered |
| --- | --- | --- |
| FR-01 | `prd.md#functional-requirements` | Query latest release, compare |
| FR-02 | `prd.md#functional-requirements` | Prerelease/draft excluded in mapping + policy |
| FR-12 | `prd.md#functional-requirements` | 429/403 recorded; no call while blocked |
| FR-13 | `prd.md#functional-requirements` | Single in-flight check; structured errors |
| NFR-02 | `prd.md#non-functional-requirements` | HTTPS api.github.com, no token |
| NFR-03 | `prd.md#non-functional-requirements` | One request per check |
| NFR-04 | `prd.md#non-functional-requirements` | Async with `CancellationToken` |
| DEC-01, DEC-02, DEC-04 | `techspec.md#technical-decisions` | Version source, filtering, gate use |
| CMP-08, CMP-09 | `techspec.md#components-and-flow` | Components |
| TC-09..TC-11 | `techspec.md#test-approach` | Tests |

## Context to recover on demand

- Applicable skills and rules: `AGENTS.md` (structured logging, `ConfigureAwait(false)`, `CancellationToken`), 429 invariant.
- Existing code: `src/TokenHound.Infrastructure/Providers/Antigravity/AntigravityCloudCodeClient.cs:24-49` and its tests (injected `HttpClient`, mocked `HttpMessageHandler`); `src/TokenHound.App/Presentation/ApplicationInfo.cs:67-95` (informational version).
- Contract or integration: `techspec.md#integrations-and-interfaces` (headers, status mapping, 15 s timeout).

## Work

- [x] T03.1 `GitHubReleaseClient.GetLatestAsync(ct)`: `GET https://api.github.com/repos/dougcunha/TokenHound/releases/latest` with `User-Agent: TokenHound/<version>`, `Accept: application/vnd.github+json`, `X-GitHub-Api-Version: 2022-11-28`; map `tag_name`, `prerelease`, `draft`, `html_url`, `assets[]`; 404 → null; 429 / 403 with `X-RateLimit-Remaining: 0` → `UpdateRateLimitedException(retryAfterSeconds, resetUtc)`; other failures → typed exception.
- [x] T03.2 `UpdateCheckService.CheckAsync(trigger, ct)`: gate check (blocked → outcome with deadline, no call), client call, `UpdatePolicy.Evaluate`, `lastCheckUtc` update on any completed call, gate success/limit recording, in-flight task sharing, structured logs `UpdateCheckStarted`/`UpdateCheckCompleted`.
- [x] T03.3 Running version resolver: strip `+metadata`; unparseable → outcome `Unavailable` with reason, logged once.
- [x] T03.4 Tests TC-09..TC-11.

## Acceptance criteria

- A 200 response with a newer stable tag → `Available` with the mapped release; `prerelease: true` → not available.
- Requests carry the three headers; no `Authorization` header is ever sent.
- 404 → up to date; 500 or timeout → error outcome, no gate deadline.
- While the gate is blocked, `CheckAsync` returns the rate-limited outcome with the resume time and the handler sees zero requests.
- Two concurrent `CheckAsync` calls produce one HTTP request and the same outcome.
- `lastCheckUtc` is updated after a completed call and unchanged after a rate-limited refusal.

## Verification

- Unit: TC-09..TC-11 with a scripted `HttpMessageHandler`.
- Integration: none (no live GitHub in tests; real API semantics covered by MA-1).
- E2E: omitted by .NET desktop policy.
- Manual: covered later by MA-1 (T06).
- Commands: `rtk dotnet build TokenHound.slnx --no-restore`; `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests --no-build --no-restore -- --minimum-expected-tests 1`.
- Environment dependency: none.
- Expected evidence: passing `GitHubReleaseClientTests`, `UpdateCheckServiceTests`.

## Affected files

- Create: `src/TokenHound.Infrastructure/Updates/{GitHubReleaseClient,UpdateRateLimitedException,UpdateCheckService,RunningVersion}.cs`, `tests/TokenHound.Infrastructure.Tests/Updates/{GitHubReleaseClientTests,UpdateCheckServiceTests}.cs`.

## Observability and recovery

- Operational signal: `UpdateCheckStarted {Trigger}`, `UpdateCheckCompleted {Outcome} {CurrentVersion} {LatestVersion}`.
- Recovery: none needed; the service is not wired yet.

## Handoff

> Updated by `sdd-execute-task` during implementation.

- Produced result: `GitHubReleaseClient.GetLatestAsync` (GET `releases/latest` with `User-Agent: TokenHound/<version>`, `Accept: application/vnd.github+json`, `X-GitHub-Api-Version: 2022-11-28`, no credentials; 15 s default timeout; maps tag, prerelease, draft, html_url, assets incl. `digest`; 404 → null; 429 or 403 with `X-RateLimit-Remaining: 0` → `UpdateRateLimitedException(retryAfter, reset)`; other statuses → `HttpRequestException`). `UpdateCheckService.CheckAsync(trigger, ct)` (unparseable running version → `Unavailable` without a request; blocked gate → `RateLimited` with resume time and no request; success → gate `RecordSuccessAsync` + `UpdatePolicy.Evaluate` with the settings' skipped version; 429/403 → gate deadline + `RateLimited`; HTTP/JSON/timeout failures → `Failed`, no deadline; one shared in-flight task, callers wait with their own token; logs `UpdateCheckStarted {Trigger}` / `UpdateCheckCompleted {Outcome} {CurrentVersion} {LatestVersion}`). `RunningVersion.Resolve/FromText`, `UpdateCheckTrigger`.
- Changed files: created `src/TokenHound.Infrastructure/Updates/{GitHubReleaseClient,UpdateRateLimitedException,UpdateCheckService,RunningVersion,UpdateCheckTrigger}.cs`, `tests/TokenHound.Infrastructure.Tests/Updates/{ScriptedHttpHandler,GitHubReleaseClientTests,UpdateCheckServiceTests}.cs`.
- Checks: `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-restore` → 0 errors, 0 warnings; filtered `GitHubReleaseClientTests` + `UpdateCheckServiceTests` → 15 passed (TC-09, TC-10, TC-11); full Infrastructure suite → 864 passed. Quality profile: QA-01..QA-05, QA-07 empty; largest file 173 lines.
- Validated state: base `a8bd1bf` + T01 + T02 + the files above; Debug, net10.0.
- Open items: reservation QA-08 `src/TokenHound.Infrastructure/Updates/UpdateCheckService.cs:36` — constructor takes 4 dependencies (client, gate, settings store, current version), as CMP-09 specifies; the single-line regex does not catch multi-line signatures, so it was checked by reading.

### ADR candidates

None - direct TechSpec implementation or local decision.
