# PRD: Owned CLI processes

## Problem and context

The user supplied `D:/problema-tokenhound.md` and authorized implementation with "Pode implementar". Windows audit evidence in that report attributes recurring 0xc0000142 popups to Git descendants of TokenHound's repeatedly started Codex app-server. Source inspection confirms per-poll startup and immediate termination; the reported Windows popup cause is not yet independently reproduced.

## Outcomes and requirements

| ID | Requirement | Acceptance |
| --- | --- | --- |
| OBJ-01 / FR-01 | Reuse one Codex server across polls | Sequential and concurrent reads start one process and initialize once |
| FR-02 | Recover from exited or unusable servers with bounded backoff | Failures do not cause immediate restart loops; later reads recover |
| FR-03 | Own CLI process trees | Closing TokenHound, including forced termination, removes its CLI descendants |
| FR-04 | Shut down gracefully before forcing termination | Close stdin, wait up to three seconds, then terminate the whole tree |
| FR-05 | Preserve usage and credential behavior | Existing snapshots, rollout fallback, and borrowed credentials remain compatible |
| NFR-01 | No windows or shell for background CLI processes | Redirected stdio and CREATE_NO_WINDOW / UseShellExecute=false |
| NFR-02 | Preserve cancellation and concurrency | No overlapping protocol reads, stale response reuse, or stderr pipe blockage |

## Boundaries and sources

Applies to Codex and `gh auth token`. Browser links, update installers, and the updated app intentionally outlive this application and remain independent. No credential writes, provider API changes, UI changes, dependencies, release, commit, or push. Sources: supplied report; `CodexAppServerClient.cs:97-138`; `CopilotCredentialDiscovery.cs:115-183`; provider specifications 05 and 11.

Manual acceptance: run the built app for one hour; attribute old Git processes to this app (the report's global Git count can include unrelated tools); verify normal and Task Manager shutdown and Application Popup event 26. These observations remain pending until performed.
