# PRD — Provider Adapter: Google Antigravity / Gemini

## 1. Executive Summary

This feature integrates TokenHound with the **Google Antigravity / Gemini** ecosystem on **Windows 11**, implementing a resilient three-tier telemetry pipeline (local Language Server RPC -> Google Cloud Code remote -> local transcript aggregation) and a 45-second reasoning-tolerant activity monitor.

---

## 2. Objectives

- **OBJ-01**: Detect active local `language_server.exe` instances, extracting CSRF tokens from command-line arguments and ephemeral TCP listening ports via `iphlpapi.dll`.
- **OBJ-02**: Execute local RPC calls to `RetrieveUserQuotaSummary` with `forceRefresh: true` and apply the fraction inversion rule ($1.0 - \text{remainingFraction}$).
- **OBJ-03**: Provide a local derived transcript reader aggregating `MODEL` turns for the current local calendar day when quotas are unpublished.
- **OBJ-04**: Implement `IUsageProvider` (`ProviderId => "gemini"`, DisplayName `"Antigravity"`).
- **OBJ-05**: Implement `IActivityMonitor` with a 45-second activity window to accommodate long reasoning pauses.

---

## 3. Requirements

### Functional Requirements
- **FR-01**: Discover `language_server.exe` process arguments containing `--csrf_token` safely on Windows.
- **FR-02**: Discover listening TCP ports bound to the discovered PID via Windows extended TCP table.
- **FR-03**: Query local HTTPS endpoint with `x-codeium-csrf-token` header, bypassing self-signed SSL verification for `127.0.0.1`.
- **FR-04**: Map weekly and daily quota buckets to `LimitWindow` records, inverting `remainingFraction`.
- **FR-05**: Aggregate local `transcript.jsonl` files, counting only `source == "MODEL"` turns for the current calendar day.
- **FR-06**: Detect agent busy state if any transcript file was updated within the last 45 seconds.

### Non-Functional Requirements
- **NFR-01**: Pure domain fidelity: never invent limits when unmeasured (`UsedFraction = null` in derived mode).
- **NFR-02**: Concurrency safety: read transcripts and network responses using async and non-locking file access.
- **NFR-03**: Code standards: sealed classes, file-scoped namespaces, alphabetized usings, XML docs.
