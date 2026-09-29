# PRD — Start with Windows setting

## Problem and context

The Inno Setup installer can make TokenHound start at logon: its unchecked `startupicon` task creates the shortcut `{userstartup}\TokenHound.lnk` (`installer/TokenHound.iss:81`, `:92`). That choice is only offered while installing. Afterwards the user has to find the Startup folder to change it, and a portable copy has no way to start with Windows at all. The installer also works against a later change of mind. It does not set `UsePreviousTasks`, so it keeps Inno's default `yes`, and a silent self-update (`/RELAUNCH=1`, `:95-97`) recreates the shortcut for a user who removed it. Nothing is added under `[UninstallDelete]` either, so a shortcut the installer did not create stays behind after uninstall.

This feature adds a Settings option that turns start-at-logon on and off. The option and the installer act on the same shortcut, so neither one silently overrides the other.

## Outcomes and metrics

| ID | Expected outcome | Metric or evidence |
| --- | --- | --- |
| OBJ-01 | Users can turn start-at-logon on or off from Settings, in installed and portable copies | Toggling and applying creates or removes the startup entry (unit tests and MA-1, MA-2) |
| OBJ-02 | The Settings option and the installer agree on one startup entry | The toggle shows the state set by the installer task. A later update keeps the user's choice, and uninstall leaves no startup entry (MA-3, MA-4) |

## Stories and journeys

| ID | User | Need | Benefit | Flow or edge |
| --- | --- | --- | --- | --- |
| US-01 | Any user | Turn on start-at-logon after install or in a portable copy | TokenHound is running when they start working | Settings → General → check "Start TokenHound when I sign in to Windows" → Apply → the shortcut exists |
| US-02 | Installed user who ticked the installer task | Turn start-at-logon off | TokenHound stops starting at logon | Settings shows the option as on → uncheck → Apply → the shortcut is removed and later updates do not bring it back |
| US-03 | Installed user | Update or uninstall without surprises | Their choice survives updates, and uninstall cleans up | Silent self-update keeps the current state. Interactive setup pre-checks the task from the current state. Uninstall removes the shortcut |

## Functional requirements

| ID | Requirement | Acceptance criterion |
| --- | --- | --- |
| FR-01 | The startup entry is the per-user shortcut `TokenHound.lnk` in the user's Startup folder (`shell:startup`), the same file the installer's `startupicon` task creates | Unit test: the service resolves `<Startup folder>\TokenHound.lnk`. The installer `[Icons]` entry still names `{userstartup}\TokenHound` |
| FR-02 | Read the current state from the OS every time Settings opens; no JSON setting stores it. The state is on when the shortcut exists and Windows "Startup apps" has not disabled it | Unit tests: no shortcut → off; shortcut present → on; shortcut present with the `StartupApproved\StartupFolder` value marked disabled → off |
| FR-03 | Turning the option on and applying creates or overwrites the shortcut so it targets the running executable, with its folder as working directory, and clears a Windows "Startup apps" disabled flag for that entry | Unit tests on a temporary Startup folder: the shortcut exists afterwards and its target is the running exe path. An existing shortcut with another target is rewritten. After Apply the state reads on |
| FR-04 | Turning the option off and applying deletes the shortcut, including one the installer created | Unit test: the shortcut is gone afterwards and the state reads off. Deleting a shortcut that does not exist is not an error |
| FR-05 | The option lives in a new "General" tab, the first tab in Settings, and uses the Apply pattern of the existing tabs (the Apply button is enabled only when the value differs from the OS state) | Unit tests on the ViewModel: the Apply button is disabled at load and enabled after toggling. Applying calls the service and reloads the state |
| FR-06 | A failed create or delete leaves the toggle on the real OS state and shows an inline error; the app keeps running | Unit test: when the service throws `IOException` or `UnauthorizedAccessException`, `ErrorMessage` is set and `IsEnabled` matches the state read back from the OS |
| FR-07 | Silent installs, including the app's self-update (`/RELAUNCH=1`), keep the startup shortcut exactly as it was before setup ran: never created and never removed | MA-3: turn the option off in the app, run a silent self-update from an install where the task had been ticked → the shortcut is still absent. The reverse case also holds |
| FR-08 | The interactive installer pre-selects the `startupicon` task from the shortcut's current state and applies both choices: ticked creates or keeps the shortcut, unticked removes it | MA-4: with the shortcut present, the setup wizard shows the task ticked. Unticking it and finishing removes the shortcut |
| FR-09 | Uninstall removes `{userstartup}\TokenHound.lnk` even when the app, not the installer, created it | MA-4: after the option is turned on in the app and TokenHound is uninstalled, the shortcut is gone |

## Non-functional requirements

| ID | Attribute | Limit or criterion |
| --- | --- | --- |
| NFR-01 | Architecture | `TokenHound.Core` keeps zero OS dependencies. Shortcut and registry access live in `TokenHound.Infrastructure`, behind a seam. Tests use a temporary folder and a fake "Startup apps" store, and never touch the real Startup folder or the real `StartupApproved` key |
| NFR-02 | Privileges | Per-user only: the Startup folder and `HKCU`. No elevation and no machine-wide locations (the installer runs with `PrivilegesRequired=lowest`) |
| NFR-03 | Responsiveness | Reading the state on Settings open and applying a change each take under 200 ms on a local profile, so the UI thread never shows a visible stall |
| NFR-04 | Accessibility | The toggle has an automation name and help text, like the existing tabs (`AutomationProperties.HelpText`) |

## User experience

Settings opens on a new first tab, "General", with a "Startup" section. It holds a checkbox, "Start TokenHound when I sign in to Windows", with a one-line hint: "Adds a shortcut to your Windows Startup folder." The Apply button is disabled until the checkbox differs from the current state. A failure appears as inline error text under the checkbox, in the same style as the Updates tab errors. There is no HUD or tray change.

## Constraints and dependencies

- Inno Setup 6 script `installer/TokenHound.iss`. The `startupicon` task name and the `{userstartup}\TokenHound` icon stay as they are, for compatibility with existing installs.
- `InstallModeDetector` (`src/TokenHound.Infrastructure/Updates/InstallModeDetector.cs:10`) already tells installed copies from portable ones. This feature behaves the same in both modes.
- Settings stores and ViewModels follow the `UpdateSettingsViewModel` pattern (`src/TokenHound.App/ViewModels/UpdateSettingsViewModel.cs`). This feature has no JSON section because the OS is the source of truth.

## Out of scope

- Other startup mechanisms: `HKCU\...\Run`, Task Scheduler, or MSIX `StartupTask`.
- Command-line arguments or a "start hidden" mode for the logon launch.
- Removing the shortcut when a portable folder is deleted by hand. There is no uninstaller for portable copies, so users turn the option off first.
- Migrating shortcuts that users created by hand under another name.

## Assumptions and sources

- Assumption (product decision proposed for the merged HIL): the OS is the source of truth (FR-02), with no JSON setting. Impact if wrong: a stored setting would drift from the installer and from Windows "Startup apps".
- Assumption (product decision proposed for the merged HIL): the installer changes of FR-07 to FR-09 are in scope, because without them a self-update recreates a shortcut the user removed. Source: `installer/TokenHound.iss:79-97`, where neither `UsePreviousTasks` nor `[UninstallDelete]` is set.
- Assumption (platform knowledge, not fetched): Windows "Startup apps" and Task Manager record a disabled Startup-folder entry under `HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\StartupFolder`, as a binary value named after the `.lnk` file. An odd first byte means disabled. Impact if wrong: FR-02 and FR-03 misread or fail to clear the flag. The TechSpec checks this on a real machine (manual acceptance).
- Assumption (platform knowledge, not fetched): Inno Setup defaults `UsePreviousTasks` to `yes`, and its uninstaller removes only icons it logged. Source: Inno Setup 6 documentation (`[Setup]: UsePreviousTasks`, `[UninstallDelete]`).

## PRD acceptance gate

- [x] Every requirement has an ID and an observable criterion.
- [x] Metrics, boundaries, and out-of-scope items are explicit.
- [x] Internal rules came from the user or an identified project source.
- [x] Implementation details remain in the TechSpec.
