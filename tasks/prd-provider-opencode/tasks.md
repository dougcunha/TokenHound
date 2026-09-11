# Implementation plan — OpenCode Provider Integration

## Stable sources

- PRD: [prd.md](file:///D:/MyProjects/TokenHound/tasks/prd-provider-opencode/prd.md)
- TechSpec: [techspec.md](file:///D:/MyProjects/TokenHound/tasks/prd-provider-opencode/techspec.md)

---

## Dependency graph

| ID | Delivery | Depends on | Unblocks |
| --- | --- | --- | --- |
| T01 | Credential discovery and auth DTO | — | T02, T03 |
| T02 | HTTP API client and quota response DTOs | T01 | T03 |
| T03 | OpenCode usage provider adapter and snapshot mapping | T01, T02 | T04 |
| T04 | Process activity monitor and configuration registration | T03 | — |

---

## Traceability matrix

| Source ID | Source section | Obligation | Tasks | Evidence or test |
| --- | --- | --- | --- | --- |
| OBJ-02 | `prd.md#outcomes-and-metrics` | Zero-configuration onboarding through automatic API key extraction (`FR-01`) | T01 | `OpenCodeCredentialDiscoveryTests` (`TC-01`) |
| FR-01 | `prd.md#functional-requirements` | Borrow API key from `auth.json` or env variables | T01 | `OpenCodeCredentialDiscoveryTests` |
| FR-02 | `prd.md#functional-requirements` | Read-only file sharing (`FileShare.ReadWrite \| FileShare.Delete`) | T01 | `OpenCodeCredentialDiscoveryTests` |
| FR-03 | `prd.md#functional-requirements` | Telemetry query to `https://opencode.ai/zen/go/v1/usage` | T02 | `OpenCodeApiClientTests` |
| FR-04 | `prd.md#functional-requirements` | Parse 3 windows (5-Hour Rolling, Weekly, Monthly) | T02, T03 | `OpenCodeUsageProviderTests` |
| FR-05 | `prd.md#functional-requirements` | Handle HTTP 429 and `Retry-After` with `RateLimitPolicy` | T02, T03 | `OpenCodeApiClientTests`, `OpenCodeUsageProviderTests` |
| FR-06 | `prd.md#functional-requirements` | Process liveness monitoring for `opencode` / `OpenCode` | T04 | `OpenCodeActivityMonitorTests` |
| FR-07 | `prd.md#functional-requirements` | Provider status state machine (`ProviderStatus.Ok`, `ProviderStatus.NeedsAuth`, `ProviderStatus.AccessDenied`, `ProviderStatus.RateLimited`, `ProviderStatus.Stale`) | T03 | `OpenCodeUsageProviderTests` |
| FR-08 | `prd.md#functional-requirements` | Default provider configuration in `appsettings.json` | T04 | Config load verification |
| NFR-01 | `prd.md#non-functional-requirements` | Pure `TokenHound.Core`, infrastructure isolation | T01, T02, T03, T04 | Solution build check |
| NFR-02 | `prd.md#non-functional-requirements` | Zero fake data (`UsedFraction = percent / 100.0`) | T03 | `OpenCodeUsageProviderTests` |
| NFR-03 | `prd.md#non-functional-requirements` | Borrow-Don't-Own credential invariant | T01 | Code review, unit tests |
| NFR-04 | `prd.md#non-functional-requirements` | Network resilience and timeout <= 10s | T02 | `OpenCodeApiClientTests` |
| NFR-05 | `prd.md#non-functional-requirements` | Testability via in-memory mock HTTP handlers | T01, T02, T03, T04 | MTP test runner |
| TC-01 | `techspec.md#test-approach` | Credential discovery unit tests | T01 | `OpenCodeCredentialDiscoveryTests` |
| TC-02 | `techspec.md#test-approach` | API client usage parsing unit tests | T02 | `OpenCodeApiClientTests` |
| TC-03 | `techspec.md#test-approach` | API client 429 rate limit unit tests | T02 | `OpenCodeApiClientTests` |
| TC-04 | `techspec.md#test-approach` | Usage provider snapshot mapping unit tests | T03 | `OpenCodeUsageProviderTests` |
| TC-05 | `techspec.md#test-approach` | Activity monitor liveness unit tests | T04 | `OpenCodeActivityMonitorTests` |

---

## Tasks

- [T01 — Credential discovery and auth DTO](done/task_01.md): Discovers OpenCode Go API key from environment variables or `%USERPROFILE%\.local\share\opencode\auth.json` with read-only file sharing.
- [T02 — HTTP API client and quota response DTOs](done/task_02.md): Implements `OpenCodeApiClient` targeting `zen/go/v1/usage` with defensive JSON deserialization and HTTP 429 rate-limit parsing.
- [T03 — OpenCode usage provider adapter and snapshot mapping](done/task_03.md): Implements `OpenCodeUsageProvider : IUsageProvider` generating authoritative snapshots across 5-hour rolling, weekly, and monthly limit windows.
- [T04 — Process activity monitor and configuration registration](done/task_04.md): Implements `OpenCodeActivityMonitor : IActivityMonitor` and registers the provider in `appsettings.json` defaults.

---

## Coverage gate

- Coverage: Pass — All functional (`FR-01` to `FR-08`) and non-functional (`NFR-01` to `NFR-05`) requirements mapped.
- Traceability: Pass — Traceability matrix links all PRD requirements and TechSpec decisions to exact tasks.
- Dependencies: Pass — Acyclic linear sequence: T01 -> T02 -> T03 -> T04.
- Atomicity: Pass — Each task delivers a cohesive vertical slice with full unit test coverage.
- Executability: Pass — Uses project standard MTP command `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests --no-build --no-restore -- --minimum-expected-tests 1`.
- Validation profile: Pass — Desktop C#/.NET, E2E omitted by desktop .NET policy, unit/integration testing on `TokenHound.Infrastructure.Tests`.
- Idempotency: Pass — Clean additive implementations without side effects on existing providers.

---

## Assumptions and open items

- Assumption: The endpoint `https://opencode.ai/zen/go/v1/usage` retains the JSON schema validated during discovery.
- Open item: None.
- Required environment: None (all tests execute against in-memory mock handlers and temporary files).

---

## State

- [x] T01 — done
- [x] T02 — done
- [x] T03 — done
- [x] T04 — done

---

## Problems and solutions

- None.
