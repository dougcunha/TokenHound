# Stable execution context

Load in this exact order:

1. `tasks/prd-11-auto-update/prd.md`
2. `tasks/prd-11-auto-update/techspec.md`
3. This file

Use the current PRD and TechSpec versions already loaded; recover only missing or changed sources. This order does not guarantee a host cache hit.

---

# T04 — Verified asset download and install-mode detection

## Outcome

`UpdateDownloader` streams a selected release asset into `%LOCALAPPDATA%\TokenHound\updates\<version>\` with progress and cancellation, and accepts it only if the URL is a GitHub release download for this repository, the length matches, and the SHA-256 matches when a digest is published. `InstallModeDetector` reports `Installed` or `Portable` from the marker file and whether the exe folder is writable.

## Dependencies and boundaries

- Depends on: T01
- Unblocks: T08
- In scope: CMP-10, CMP-11, tests TC-12, TC-13.
- Out of scope: applying the update (T05), UI progress rendering (T08).

## Traceability

| Source | Section | Obligation covered |
| --- | --- | --- |
| FR-06 | `prd.md#functional-requirements` | Marker-based mode detection |
| FR-08 | `prd.md#functional-requirements` | Temp download, size + SHA-256 verification, nothing applied on mismatch |
| FR-11 | `prd.md#functional-requirements` | Writability probe |
| NFR-02 | `prd.md#non-functional-requirements` | URL restriction, verified before running |
| NFR-04 | `prd.md#non-functional-requirements` | Async, cancellable, progress |
| DEC-05, DEC-07, DEC-11 | `techspec.md#technical-decisions` | Marker name, integrity rules, probe |
| CMP-10, CMP-11 | `techspec.md#components-and-flow` | Components |
| TC-12, TC-13 | `techspec.md#test-approach` | Tests |

## Context to recover on demand

- Applicable skills and rules: `AGENTS.md` (`ConfigureAwait(false)`, `CancellationToken`).
- Existing code: mocked `HttpMessageHandler` pattern (see T03 context); `src/TokenHound.Infrastructure/Engine/UsageArchive.cs:38-59` (LocalAppData directory resolution).
- Contract or integration: `techspec.md#errors-security-and-recovery`.

## Work

- [x] T04.1 `UpdateDownloader.DownloadAsync(asset, version, IProgress<double>?, ct)`: validate the URL prefix `https://github.com/dougcunha/TokenHound/releases/download/`, stream to a `.partial` file, rename on success, verify length and optional digest, delete on any failure or cancellation; returns the verified path.
- [x] T04.2 `InstallModeDetector`: `DetectMode(appDirectory)` via `TokenHound.installed`; `IsWritable(appDirectory)` via create/delete `~update-probe.tmp`.
- [x] T04.3 Tests TC-12, TC-13 (temp directories; read-only case via a directory ACL or read-only attribute that the test can set on Windows).

## Acceptance criteria

- Size mismatch or digest mismatch → typed failure and no file left in the updates folder.
- Digest absent and size matching → success, with `DigestVerified=false` reported.
- A URL outside the repository's release-download prefix is refused before any request.
- Cancellation deletes the partial file and propagates `OperationCanceledException`.
- Marker present → `Installed`; absent → `Portable`; probe returns false on a folder the test made non-writable.

## Verification

- Unit: TC-12, TC-13.
- Integration: real file system in temp directories.
- E2E: omitted by .NET desktop policy.
- Manual: covered later by MA-2 (read-only folder) in T08.
- Commands: `rtk dotnet build TokenHound.slnx --no-restore`; `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests --no-build --no-restore -- --minimum-expected-tests 1`.
- Environment dependency: none.
- Expected evidence: passing `UpdateDownloaderTests`, `InstallModeDetectorTests`.

## Affected files

- Create: `src/TokenHound.Infrastructure/Updates/{UpdateDownloader,DownloadedUpdate,UpdateDownloadException,InstallModeDetector}.cs`, `tests/TokenHound.Infrastructure.Tests/Updates/{UpdateDownloaderTests,InstallModeDetectorTests}.cs`.

## Observability and recovery

- Operational signal: `UpdateDownloadCompleted {Asset} {Bytes} {DigestVerified}`; failures logged with the reason.
- Recovery: the updates folder only holds verified files; stale version folders may be deleted at the next download.

## Handoff

> Updated by `sdd-execute-task` during implementation.

- Produced result: `UpdateDownloader.DownloadAsync(asset, progress, ct)` — refuses any URL outside `https://github.com/dougcunha/TokenHound/releases/download/` before sending a request; streams to `<updates>\<asset name>.partial` with progress 0..1; checks length against `asset.Size` and, when `digest` is `sha256:<hex>`, the SHA-256; renames to `<updates>\<asset name>` only after verification; deletes the partial file on any failure or cancellation; returns `DownloadedUpdate { FilePath, Asset, DigestVerified }`; logs `UpdateDownloadCompleted {Asset} {Bytes} {DigestVerified}`. Failures: `UpdateDownloadException` with `UpdateDownloadFailure` (`InvalidUrl`, `SizeMismatch`, `DigestMismatch`); HTTP errors surface as `HttpRequestException`. `InstallModeDetector.DetectMode` (marker `TokenHound.installed`) and `IsWritable` (create/delete-on-close `~update-probe.tmp`).
- Changed files: created `src/TokenHound.Infrastructure/Updates/{UpdateDownloader,UpdateDownloadException,UpdateDownloadFailure,DownloadedUpdate,InstallModeDetector}.cs`, `tests/TokenHound.Infrastructure.Tests/Updates/{UpdateDownloaderTests,InstallModeDetectorTests}.cs`.
- Checks: `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-restore` → 0 errors, 0 warnings; filtered `UpdateDownloaderTests` + `InstallModeDetectorTests` → 9 passed (TC-12, TC-13); full Infrastructure suite → 873 passed. Quality profile: QA-01..QA-05 empty; QA-07 fixed (disposals now use `ConfigureAwait(false)`); QA-08 hit at line 76 is a multi-line call the single-line regex misreads; largest file 202 lines.
- Validated state: base `a8bd1bf` + T01..T03 + the files above; Debug, net10.0.
- Open items: (1) the verified file lands in `%LOCALAPPDATA%\TokenHound\updates\<asset name>` (the asset name already carries the version) instead of the per-version subfolder named in CMP-10 — same isolation, one folder less. (2) The non-writable test blocks the probe path with a directory and uses a missing folder; a real ACL-denied folder is covered by MA-2.

### ADR candidates

None - direct TechSpec implementation or local decision.
