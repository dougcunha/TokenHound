# PRD: Main window context menu

## Problem and context

TokenHound users need access to application actions directly from the main desktop HUD. The current window has no context menu or Settings/About dialogs. Users need to exit the application, request fresh counters, open the future settings surface, and identify the installed application version.

This PRD records the user's September 7, 2026 request. It defines product behavior only; implementation, visual composition, and task sequencing belong in subsequent artifacts.

## Outcomes and metrics

| ID | Expected outcome | Metric or evidence |
| --- | --- | --- |
| OBJ-01 | Access all four actions from the HUD. | Each exact menu label opens or executes its specified action in an acceptance walkthrough. |
| OBJ-02 | Request current usage without waiting for scheduled polling. | An eligible provider is refreshed before its next scheduled cycle; all monitored providers are considered. |
| OBJ-03 | Identify the app and installed version. | About displays a brief accurate description and a version matching the running build. |

These are release acceptance measures, not new telemetry collection requirements. No adoption or latency target was supplied.

## Stories and journeys

| ID | User | Need | Benefit | Flow or edge |
| --- | --- | --- | --- | --- |
| US-01 | Desktop HUD user | Close TokenHound | End monitoring intentionally | Open menu, select Close, observe process exit. |
| US-02 | Desktop HUD user | Refresh every monitored counter | Obtain current usage on demand | Select Refresh; eligible providers update, blocked or failed providers retain honest status. |
| US-03 | Desktop HUD user | Open Settings | Access the reserved settings surface | Select Settings, inspect the empty dialog, dismiss it, continue using the HUD. |
| US-04 | Desktop HUD user | Read app information | Understand its purpose and report its version | Select About, read description and version, dismiss it. |

## Functional requirements

| ID | Requirement | Acceptance criterion |
| --- | --- | --- |
| FR-01 | Provide a context menu on the visible main HUD containing exactly Close, Refresh, Settings, and About, in that order. | Right-clicking the HUD background or a provider cell opens the same menu; all four English labels appear once. Opening or dismissing the menu executes no action. |
| FR-02 | Close must terminate the application. | Selecting Close closes the HUD and any application dialogs, ends monitoring, and leaves no TokenHound application process running. Closing only a dialog does not exit the app. |
| FR-03 | Refresh must request an immediate update of all currently monitored providers and their available usage counters. | With the next scheduled poll still in the future, selecting Refresh initiates an update for every eligible monitored provider. Returned values appear in the HUD and existing usage details, including when values have not changed. |
| FR-04 | Manual refresh must respect provider restrictions and tolerate partial failure. | A provider under an active rate-limit deadline receives no premature network call. One provider's failure does not prevent other providers from updating. Failed or unavailable data is not replaced with fabricated zeroes or percentages. |
| FR-05 | Make refresh progress and outcome understandable without allowing duplicate refresh bursts. | While refresh is running, visible feedback indicates work in progress. Repeated activation does not launch overlapping duplicate refreshes. Completion clears the feedback; failures or rate-limit deferrals remain distinguishable through provider status/details. |
| FR-06 | Settings must open an empty application-styled dialog. | Selecting Settings opens a dialog titled Settings with an empty content area and a working dismissal control. It contains no configuration fields, save/apply actions, or invented settings. Closing it changes no configuration. |
| FR-07 | About must open a dialog with the app name, brief description, and current application version. | Selecting About shows TokenHound, a concise description of its Windows desktop monitoring purpose, and a readable version matching the running build. Releasing a different version changes the displayed version without editing static About copy. |
| FR-08 | Dialogs must have predictable lifecycle and dismissal. | Settings and About can be dismissed with their close control and Escape. Reopening either after dismissal works. Repeated invocation does not create duplicate instances of the same dialog. Dismissing a dialog leaves monitoring active. |

## Non-functional requirements

| ID | Attribute | Limit or criterion |
| --- | --- | --- |
| NFR-01 | Visual consistency and polish | Menu and dialogs use a coherent dark surface, typography, spacing, and control treatment consistent with the current app. About gives clear hierarchy to the name, description, and version. Visual review confirms legible text, aligned content, and no clipping at Windows scaling of 100%, 150%, and 200%. |
| NFR-02 | Accessibility | Once opened, the menu supports arrow-key navigation, Enter activation, Escape dismissal, and visible selection. Dialog controls support keyboard focus, logical Tab navigation, and accessible names. Text and version remain readable without relying on color alone. |
| NFR-03 | HUD focus and positioning | Ordinary HUD clicks and dragging retain existing non-activating behavior. Explicitly opened dialogs can receive keyboard focus. Dismissing menus/dialogs does not relocate the HUD. Menus and dialogs open within the available desktop work area. |
| NFR-04 | Responsiveness and resilience | During delayed or failed refreshes, the UI continues accepting input, repainting, and allowing Close. Closing during a refresh does not leave the process or a dialog running. |
| NFR-05 | Data and credential integrity | Refresh preserves existing rate-limit deadlines, read-only handling of borrowed credentials, and honest unknown values. Verification with unavailable credentials and a rate-limited provider shows no credential mutation, bypass, or invented counters. |

## User experience

Right-clicking the visible HUD opens the menu near the pointer. Clicking outside or pressing Escape dismisses it. Selecting an action dismisses the menu and performs that action. Right-click must not start the existing left-button drag interaction.

Refresh exposes progress without introducing a new dashboard. Existing provider status/details should communicate unavailable data and rate limits; any last known values remain recognizable as stale when appropriate. If no providers are available, Refresh completes without an exception or invented data and communicates that nothing could be updated.

Settings is intentionally empty apart from its title and dismissal controls. About is a compact, polished information dialog, visually related to Settings and the HUD. Proposed description: "TokenHound monitors LLM usage, rate limits, and agent activity across AI coding tools on your Windows desktop." Exact visual composition remains for the TechSpec/design stage.

## Constraints and dependencies

- Retain the existing Windows WPF application and repository architecture; this feature does not introduce a platform or dependency migration.
- Use the existing monitored provider set and refresh capability. No new provider integration is required.
- Respect the repository's rate-limit, credential, honest-data, and non-activating HUD invariants.
- Obtain the displayed version from the running application's build information. This PRD does not prescribe a metadata API or versioning scheme.
- Repository-facing text and the requested menu labels are English.
- Implementation validation will need a Windows desktop session for interaction, focus, scaling, and shutdown checks.

## Out of scope

- Working settings, persistence of new preferences, provider selection, or authentication flows.
- Hide/snooze actions, a tray menu, extra menu commands, or a global keyboard shortcut.
- Provider adapter redesign, new counters, rate-limit bypass, and credential renewal.
- Update checking, release notes, external links, licensing panels, or diagnostics in About.
- HUD geometry redesign, new branding assets, themes, or animations as independent features.
- Architecture, implementation tasks, and application code changes in this artifact.

## Assumptions and sources

### User decisions

- The user explicitly requested Close, Refresh, Settings, and About; Close exits, Refresh updates all counters, Settings is initially empty and app-styled, and About contains a brief description and current version with a modern, attractive appearance. These decisions support FR-01 through FR-03, FR-06, FR-07, and NFR-01.

### Local evidence

- [Repository instructions](../../AGENTS.md): English repository text, focus preservation, rate-limit deadlines, and credential/data invariants support FR-04 and NFR-03/NFR-05.
- [Current HUD markup](../../src/TokenHound.App/UI/Windows/NotchWindow.xaml) and [window behavior](../../src/TokenHound.App/UI/Windows/NotchWindow.xaml.cs): dark rounded HUD, no existing context menu, non-activating window, and left-button dragging.
- [Application lifecycle](../../src/TokenHound.App/App.xaml.cs): explicit application shutdown, provider registration, startup refresh, and cleanup on exit. Closing a window alone must not be assumed to terminate the application.
- [Refresh entry point](../../src/TokenHound.Infrastructure/Engine/UsageStore.cs): an existing manual refresh capability is available. Its existence is evidence of an integration point, not proof that every acceptance criterion already passes.
- [Application metadata](../../src/TokenHound.App/TokenHound.App.csproj): Windows WPF target, product name, and description. No explicit product version is declared in this file, so no version number is assumed here.
- [Earlier design reference](../../docs/design/2026-08-28-usage-notch-design.md): describes a different menu (Settings, Refresh now, Hide for 1 hour, Quit) and a macOS concept. For this feature, the user's exact four actions supersede that menu, and the existing Windows app supplies the platform and visual baseline. The reference remains unmodified.

### Explicit assumptions

- A-01: The listed order is the desired menu order; Close exits without a confirmation dialog. If revised, update FR-01/FR-02 acceptance.
- A-02: "All counters" means usage counters exposed by the currently monitored providers, not new providers or new activity measurements. "Force" bypasses the normal polling wait, never a provider's rate-limit restriction.
- A-03: Progress feedback, duplicate suppression, keyboard operation after opening, Escape dismissal, single-instance dialogs, and scaling checks are product-quality assumptions supporting FR-05/FR-08 and NFR-01 through NFR-04.
- A-04: No reusable dialog style was found in the current application window files. "Application standard" therefore means visual consistency with the existing HUD, with the shared dialog treatment specified later.
- A-05: The proposed About sentence may be edited without changing its factual scope. No external links or additional metadata are assumed.

No blocking product decisions remain for this draft. These assumptions are visible for product review. No external research was needed; no new public standard or external integration is imposed.

## PRD acceptance gate

- [x] Every requirement has a unique ID and an observable criterion.
- [x] Metrics, boundaries, and out-of-scope items are explicit.
- [x] Internal rules came from the user or an identified repository source.
- [x] Additional product choices are identified as assumptions.
- [x] Implementation details remain in the TechSpec.
- [x] Conflicting historical menu/platform guidance is resolved explicitly.

This gate validates the PRD's completeness, not implementation success. This is a new artifact: FR-01 through FR-08 and NFR-01 through NFR-05 are added; no existing requirement IDs are changed or removed. A future TechSpec and task plan must be validated against this PRD, especially shutdown, refresh restrictions, and dialog focus. No derived artifacts are edited here.
