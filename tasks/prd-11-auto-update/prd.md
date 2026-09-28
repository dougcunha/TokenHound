# PRD — Auto-update from GitHub Releases

## Problem and context

TokenHound ships through GitHub Releases (`v*` tags) as an fx-dependent single-file zip (portable) and an Inno Setup installer, each for `win-x64` and `win-arm64` (`.github/workflows/release.yml:55`, `installer/TokenHound.iss:58`). Right now users only learn about a new version by checking GitHub themselves, so bug fixes and provider changes reach them late. This feature lets the app find, offer, and apply its own updates without the user having to know how it was installed.

## Outcomes and metrics

| ID | Expected outcome | Metric or evidence |
| --- | --- | --- |
| OBJ-01 | Users learn about a newer release without leaving the app | A newer stable release is reported within one check interval, or right away after a manual check |
| OBJ-02 | Updating takes one confirmation, whatever the install mode | After the user confirms, the app restarts on the new version with no other manual steps (manual acceptance MA scripts) |
| OBJ-03 | Update checks never break the app's rate-limit invariant | No GitHub call is sent while a persisted 429/403 rate-limit deadline is in force (unit tests) |

## Stories and journeys

| ID | User | Need | Benefit | Flow or edge |
| --- | --- | --- | --- | --- |
| US-01 | Any user | Be told when a new version exists | Stays current without watching GitHub | Periodic check finds `vX.Y.Z` > current → notification |
| US-02 | Any user | Check for updates now from the tray | Gets an immediate answer | Tray "Check for updates" → "up to date", "update available", or an error message |
| US-03 | Portable user | Apply the update in place | No manual unzip or copy | Confirm → download zip → app closes → exe swapped → app restarts |
| US-04 | Installed user | Apply the update through the installer | Keeps uninstall and shortcuts consistent | Confirm → download setup → app closes → silent installer runs → app restarts |
| US-05 | Any user | Ignore one version or turn off checks | Is not nagged | "Skip this version" suppresses that tag only; interval 0 or disabled turns off periodic checks |

## Functional requirements

| ID | Requirement | Acceptance criterion |
| --- | --- | --- |
| FR-01 | Query the latest release of the TokenHound GitHub repository and compare its tag (`vMAJOR.MINOR.PATCH`) with the running version | Unit tests: newer tag → update available; equal, older, or unparseable tag → no update |
| FR-02 | Skip prereleases and drafts | A release marked prerelease or draft never counts as an update (unit test) |
| FR-03 | Run a periodic check at a configurable interval in hours, persisted in the JSON settings; the first check runs shortly after startup | Default 24 h; changing the value takes effect without a restart; 0 or `enabled=false` turns periodic checks off (unit tests on the scheduler/policy) |
| FR-04 | Tray menu item "Check for updates" runs a check right away and always shows a result | Up to date, update available, or error (rate limited, offline) is shown to the user (manual acceptance) |
| FR-05 | When an update is found, notify the user and ask whether to update now, later, or skip this version | "Later" asks again at the next periodic check; "Skip" stores the tag and suppresses it until a newer tag appears (unit test on the skip policy) |
| FR-06 | Detect install mode from an installer-only marker file next to the exe: present = installed, absent = portable | Unit tests for both cases; `TokenHound.iss` installs the marker and the zip does not contain it |
| FR-07 | Select the asset that matches install mode and process architecture (`x64`/`arm64`) by the release naming patterns | Unit tests: correct zip or setup asset for each mode × RID; no matching asset → clear error, no download |
| FR-08 | Download to a temporary folder and verify integrity before applying | Size matches the GitHub asset metadata, and the SHA-256 matches when the release publishes a checksum; on mismatch nothing is applied (unit tests) |
| FR-09 | Portable apply: extract the new exe, close the app, replace the exe, and restart; keep the previous exe until the new one starts | After the swap the new version runs; if the replacement fails, the old exe stays in place and the user sees an error (manual acceptance MA-2) |
| FR-10 | Installed apply: close the app and run the downloaded setup silently in the current user's context, restarting the app afterward | Installer runs without wizard pages and the app restarts on the new version (manual acceptance MA-3) |
| FR-11 | Portable folder not writable: do not start the swap | The user is told the folder is not writable and is pointed to the release page; the app keeps running (unit test on the preflight check) |
| FR-12 | Respect GitHub rate limits under the 429 persistence invariant | 429, or 403 with `X-RateLimit-Remaining: 0`, persists a deadline from `Retry-After`/`X-RateLimit-Reset`; no check is dispatched before it expires; `Retry-After: 0` is never retried immediately (unit tests) |
| FR-13 | Show a manual check's failure without crashing and without stacking duplicate checks | A check already running makes a new manual request reuse it; errors are shown and logged with structured fields |

## Non-functional requirements

| ID | Attribute | Limit or criterion |
| --- | --- | --- |
| NFR-01 | Architecture | Version comparison, asset selection, and schedule/skip policies live in `TokenHound.Core` with no UI or OS dependency |
| NFR-02 | Security | HTTPS to `api.github.com`/`github.com` only; the asset URL must come from the repository's release; downloads are verified (FR-08) before anything runs; no GitHub token is required or stored |
| NFR-03 | Network budget | At most one scheduled check per interval; unauthenticated GitHub limit (60/h per IP) is never approached under default settings |
| NFR-04 | Responsiveness | Checks and downloads run off the UI thread and accept a `CancellationToken`; the HUD stays responsive during a download |
| NFR-05 | Platform | Windows 10/11 x64 and arm64; the installed build stays per-user (`PrivilegesRequired=lowest`), with no elevation prompt |
| NFR-06 | Configuration | Settings persisted as JSON with `System.Text.Json`, matching the existing settings stores |

## User experience

- Tray menu gains "Check for updates"; while a check or download runs, the item shows progress state and cannot be started twice.
- Update-available prompt shows current → new version, a link to the release notes, and actions **Update now**, **Later**, **Skip this version**.
- Download failures, integrity mismatches, rate limits (with time until retry), and write-permission problems each show a specific message; none closes the app.
- The update interval is visible and editable where the other refresh settings are edited.

## Constraints and dependencies

- GitHub REST API `GET /repos/{owner}/{repo}/releases/latest`, unauthenticated (60 requests/h per IP).
- Release assets: `TokenHound-{ver}-{rid}-fxdependent.zip` and `TokenHound-Setup-{ver}-{rid}.exe`, RIDs `win-x64` and `win-arm64`; tags `v*` (`release.yml`, `TokenHound.iss`).
- Inno Setup installer (per-user, `{autopf}` default dir) must install the marker file; this changes `installer/TokenHound.iss`.
- Project invariants: pure Core, persisted rate-limit deadlines, JSON settings.

## Out of scope

- Delta or background silent updates without user confirmation.
- Prerelease or beta channel opt-in.
- Code-signing or Authenticode verification of downloads (no signing pipeline exists yet).
- Rollback to a previous version from the UI.
- Updating the .NET runtime that the fx-dependent build requires.
- Changing the release workflow's asset names or publishing a checksum file (checksum is used only if present).

## Assumptions and sources

- Assumption (product decision proposed for the merged HIL): prereleases are ignored (FR-02); default interval 24 h (FR-03); integrity = size + optional SHA-256 (FR-08). Impact if wrong: FR-02/FR-03/FR-08 change.
- Assumption: the running version comes from the assembly informational version set by the release build; the TechSpec confirms the source. Impact if wrong: FR-01 needs a different version source.
- Assumption: Inno Setup `/VERYSILENT /SUPPRESSMSGBOXES` flags and a helper process for the portable swap are TechSpec choices, not product rules.
- Source: `.github/workflows/release.yml:55`, `installer/TokenHound.iss:52-84`, `CLAUDE.md` invariants; triage open decisions in `workflow.md#DEC-01`.

## PRD acceptance gate

- [x] Every requirement has an ID and an observable criterion.
- [x] Metrics, boundaries, and out-of-scope items are explicit.
- [x] Internal rules came from the user or an identified project source.
- [x] Implementation details remain in the TechSpec.
