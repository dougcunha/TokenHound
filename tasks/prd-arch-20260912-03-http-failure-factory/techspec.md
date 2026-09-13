# TechSpec — Refactoring shared HTTP failure construction

## Sources and traceability

- PRD: `prd.md`
- Current code and tests: `src/TokenHound.Infrastructure/Providers/ProviderHttpException.cs`, `…/Providers/HttpRetryAfterParser.cs`, `…/Providers/Cursor/CursorApiClient.cs`, `…/Providers/Antigravity/AntigravityCloudCodeClient.cs`, `…/Providers/Copilot/CopilotRateLimitExtractor.cs`, `…/Copilot/CopilotApiClient.cs`, `…/Copilot/CopilotBillingClient.cs`, `…/Copilot/CopilotMetricsClient.cs`, `…/Copilot/CopilotRequestGate.cs`; corresponding provider test classes.
- Applicable instructions and skills: `AGENTS.md`; `dotnet-efficient-validation`; `repository-cli-efficiency`; `no-workarounds`.

## Technical decisions

| ID | Requirements | Decision | Evidence and reason | Alternatives and trade-offs |
| --- | --- | --- | --- | --- |
| DEC-01 | R-01, R-02, R-05 | Add `ProviderHttpException.FromResponse(HttpResponseMessage response, string message)` in `ProviderHttpException.cs`; it parses Retry-After only for 429 via `HttpRetryAfterParser.ExtractSeconds(response, TimeProvider.System)`. | Both `CreateException` bodies are identical except the message noun; verified by reading `CursorApiClient.cs:108-117` and `AntigravityCloudCodeClient.cs:143-152`. | Duplicate an abstract `ProviderHttpClient` base — over-reach for two call sites; rejected. |
| DEC-02 | R-01 | Delete `CursorApiClient.CreateException`; call the factory at `:75`. | Same message text must be passed by the caller. | — |
| DEC-03 | R-02 | Delete `AntigravityCloudCodeClient.CreateException`; call the factory at `:134`. | Same. | — |
| DEC-04 | R-04 | Delete `CopilotRateLimitExtractor.ExtractRetryAfterSeconds`; the four call sites call `HttpRetryAfterParser.ExtractSeconds(response, timeProvider)` directly. | Wrapper body is a single delegation (`CopilotRateLimitExtractor.cs:42-48`); `TryExtractRateLimit` is separate and stays. | Keep wrapper as a seam — rejected, it adds no logic. |

## Affected components

| ID | Component | Current state | Allowed change | Risk and dependencies |
| --- | --- | --- | --- | --- |
| CMP-01 | `ProviderHttpException` | Exception type | Add static factory | Low; additive. |
| CMP-02 | `CursorApiClient` | Cursor transport | Remove private `CreateException`; call factory | Low; shared file with `arch-20260912-01`? No — WS01 edits `CopilotApiClient`, not Cursor. Independent. |
| CMP-03 | `AntigravityCloudCodeClient` | Antigravity transport | Remove private `CreateException`; call factory | Low. |
| CMP-04 | `CopilotRateLimitExtractor` | Rate-limit parsing helper | Delete `ExtractRetryAfterSeconds` | Low; keep `TryExtractRateLimit`. |
| CMP-05 | `CopilotApiClient`, `CopilotBillingClient`, `CopilotMetricsClient`, `CopilotRequestGate` | Retry-After call sites | Call `HttpRetryAfterParser.ExtractSeconds` | Low. `CopilotApiClient` is shared with `arch-20260912-01`, which must land first. |

## Safety net

- Profile: .NET 10 (`net10.0`), MTP test runner. Commands via `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1`.
- E2E: omitted by the .NET desktop policy.
- Command prerequisites and exclusions: `rtk dotnet build` first; run focused provider classes before the full project.
- Manual acceptance: none (transport exceptions are unit-testable).

| ID | Requirement | Level | Scenario | Expected result | Command or script |
| --- | --- | --- | --- | --- | --- |
| TC-01 | R-01, R-03, R-05 | unit | Cursor 429 with and without Retry-After; non-429 | Same exception message/status/retry as today | `CursorApiClientTests` |
| TC-02 | R-02, R-03, R-05 | unit | Antigravity non-success incl. 429 | Same as today | `AntigravityCloudCodeClientTests` |
| TC-03 | R-04 | unit | Copilot delta/date/raw Retry-After via the four call sites | Same parsed value | `CopilotBillingClientTests`, `CopilotMetricsClientTests`, `CopilotRequestGateTests` |

## Dependency sequencing

| Step | Depends on | Change | Evidence to advance | Reversal |
| --- | --- | --- | --- | --- |
| 1 | — | Add `ProviderHttpException.FromResponse` | build green | revert |
| 2 | 1 | Retarget Cursor + Antigravity, delete their `CreateException` | focused tests pass | revert |
| 3 | 2 | Delete wrapper, retarget four Copilot call sites | focused tests pass | revert |
| 4 | 3 | Full Infrastructure test run | all pass | revert |

## Compatibility and rollout

- Preserved contracts: R-01..R-05.
- Migration or coexistence: none; internal helpers.
- Observability: `ProviderHttpException.StatusCode` / `RetryAfterSeconds` preserved.
- Rollback: `git revert`; steps are independently reversible.

## Quality profile

| ID | Rule | Class | Verification command | Baseline | Target |
| --- | --- | --- | --- | --- | --- |
| QA-01 | No duplicate `CreateException(HttpResponseMessage)` bodies | blocking | `rtk rg -n "private static ProviderHttpException CreateException\(HttpResponseMessage response\)" src` | 2 | 0 |
| QA-02 | No Retry-After pass-through | blocking | `rtk rg -n "ExtractRetryAfterSeconds" src` | 5 (1 def + 4 calls) | 0 |
| QA-03 | Single Retry-After parse implementation | blocking | `rtk rg -n "ExtractSeconds\(" src/TokenHound.Infrastructure/Providers/HttpRetryAfterParser.cs` | 1 | 1 |

- Target measures today: 2 duplicate failure constructors; 1 pass-through wrapper with 4 call sites.
- Expected measures at the end: 1 shared factory; 0 pass-through.

## Risks and open items

- Risk: an accidental message-text change during factory extraction. Mitigation: pass the existing message string verbatim; assert in tests.
- Open item: exception-hierarchy unification (AA-11) deferred; `CopilotApiException` stays.
