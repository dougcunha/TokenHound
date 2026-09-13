# PRD — Refactoring shared HTTP failure construction

## Context and motivation

The 2026-09-12 analysis (AA-07, AA-14) found that Cursor and Antigravity build identical `ProviderHttpException` failures in two private `CreateException` methods (D1), and that `CopilotRateLimitExtractor.ExtractRetryAfterSeconds` is a pure pass-through wrapper around `HttpRetryAfterParser.ExtractSeconds` (D8). A change to Retry-After parsing or failure construction currently requires edits in several places, and the pass-through hides that four Copilot call sites already use the shared parser. Consolidating removes duplicate decision points without changing any response outcome.

## Scope

- Target: HTTP failure construction for Cursor and Antigravity, and the Retry-After pass-through used by Copilot.
- Allowed structural change: add one shared failure factory; retarget call sites; delete the pass-through wrapper. Message nouns and status/retry metadata stay identical.
- Out of scope: unification of exception hierarchies (`CopilotApiException`, `OpenCode*Exception`, Claude `RateLimitException`) — AA-11 is a pending design decision. Rate-limited **snapshot** builders (D2/D4) are `arch-20260912-04`.

## Behaviors to preserve

| ID | Observable behavior | Source and evidence | Verification |
| --- | --- | --- | --- |
| R-01 | Cursor non-success responses throw `ProviderHttpException` with the same message, status code, and `RetryAfterSeconds`. | `CursorApiClient.cs:108-117`, called at `:75` | `CursorApiClientTests` pass. |
| R-02 | Antigravity non-success responses throw `ProviderHttpException` with the same message, status code, and `RetryAfterSeconds`. | `AntigravityCloudCodeClient.cs:143-152`, called at `:134` | `AntigravityCloudCodeClientTests` pass. |
| R-03 | Retry-After parsing order (delta → HTTP date → raw integer) and ceiling rounding are unchanged. | `HttpRetryAfterParser.cs:10-32` | parser/provider tests pass. |
| R-04 | The four Copilot call sites parse Retry-After identically after the wrapper is deleted. | `CopilotApiClient.cs:176`, `CopilotBillingClient.cs:231`, `CopilotMetricsClient.cs:283`, `CopilotRequestGate.cs:245` | Copilot client/gate/metrics tests pass. |
| R-05 | Only 429 responses trigger Retry-After parsing; other statuses carry `null`. | both `CreateException` bodies | focused tests. |

## Constraints

- The factory must accept a caller-supplied message so per-provider wording is preserved exactly.
- Time source remains `TimeProvider.System` at the current call sites; no clock behavior change.
- `CopilotApiException` and `CopilotRateLimitExtractor.TryExtractRateLimit` stay untouched.

## Acceptance criteria

- [ ] All `R-NN` items were checked after refactoring.
- [ ] No new behavior entered the scope silently.
- [ ] Infrastructure tests pass with `--minimum-expected-tests 1`.
- [ ] The pass-through wrapper and both duplicate `CreateException` bodies are gone.

## Assumptions and open items

- Assumption: `TimeProvider.System` is the intended clock for these provider clients (no injected clock today).
- Open item: AA-11 (exception-hierarchy unification) stays out; revisit separately.
