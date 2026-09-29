; TokenHound Inno Setup 6 script.
; Builds a per-user installer from a framework-dependent single-file publish.
;
; Default locations assume ISCC runs from the repository root:
;   iscc "installer\TokenHound.iss"
; The build script (installer/build-installer.ps1) and CI pass absolute
; directories explicitly, so the defaults below are only a fallback.
;
; Defines (override with /DName=Value):
;   MyAppVersion - installer and display version (default 0.0.0-dev)
;   PublishDir   - dotnet publish output consumed by [Files] (default ..\publish\win-x64)
;   OutputDir    - directory receiving the Setup exe (default ..\dist)
;   MyAppArch    - x64 or arm64 (default x64)
;   MyAppExeName - published executable name (default TokenHound.App.exe)

#ifndef MyAppVersion
  #define MyAppVersion "0.0.0-dev"
#endif
#ifndef PublishDir
  #define PublishDir "..\\publish\\win-x64"
#endif
#ifndef OutputDir
  #define OutputDir "..\\dist"
#endif
#ifndef MyAppArch
  #define MyAppArch "x64"
#endif
#ifndef MyAppExeName
  #define MyAppExeName "TokenHound.App.exe"
#endif

#define MyAppName "TokenHound"
#define MyAppPublisher "Douglas Cunha"
#define MyAppURL "https://github.com/dougcunha/TokenHound"

#if MyAppArch == "arm64"
  #define MyAppRid "win-arm64"
#else
  #define MyAppArch "x64"
  #define MyAppRid "win-x64"
#endif

[Setup]
AppId={{0BAB47B0-2890-4438-A8AF-372B10F3311B}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
LicenseFile=..\LICENSE
SetupIconFile=..\logo.ico
PrivilegesRequired=lowest
OutputDir={#OutputDir}
OutputBaseFilename=TokenHound-Setup-{#MyAppVersion}-{#MyAppRid}
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
MinVersion=10.0.22000
#if MyAppArch == "arm64"
ArchitecturesAllowed=arm64
ArchitecturesInstallIn64BitMode=arm64
#else
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
#endif
UninstallDisplayIcon={app}\{#MyAppExeName}
; VersionInfoVersion intentionally omitted: AppVersion may carry a prerelease
; suffix (e.g. 0.0.0-ci.42), and Inno derives the numeric version info itself.
DisableProgramGroupPage=yes
AllowNoIcons=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "startupicon"; Description: "Start automatically with Windows"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Excludes: "*.pdb"
; Installer-only marker: the app updates itself through a newer installer when this file is present.
Source: "TokenHound.installed"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon
; Same file as StartupLaunchService.SHORTCUT_FILE_NAME in the app; silent setups keep the user's current choice.
Name: "{userstartup}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: startupicon; Check: ShouldCreateStartupShortcut

[UninstallDelete]
; Also removes a startup shortcut the app created from Settings, which the uninstall log does not track.
Type: files; Name: "{userstartup}\{#MyAppName}.lnk"

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch {#MyAppName}"; Flags: nowait postinstall skipifsilent
; Relaunch after a silent self-update started by the app with /RELAUNCH=1.
Filename: "{app}\{#MyAppExeName}"; Flags: nowait; Check: ShouldRelaunch

[Code]
const
  AppInstanceMutex = 'TokenHound.App.Instance';
  AppExitTimeoutMs = 30000;
  AppExitPollMs = 500;
  StartupTaskName = 'startupicon';

var
  StartupShortcutExisted: Boolean;
  StartupTaskPreselected: Boolean;

function StartupShortcutPath: String;
begin
  Result := ExpandConstant('{userstartup}\{#MyAppName}.lnk');
end;

function ShouldRelaunch: Boolean;
begin
  Result := ExpandConstant('{param:RELAUNCH|0}') = '1';
end;

{ Silent setups without an explicit /TASKS (e.g. the app's self-update) keep the shortcut as the user left it. }
function ShouldCreateStartupShortcut: Boolean;
begin
  Result := (not WizardSilent) or (ExpandConstant('{param:TASKS|}') <> '') or StartupShortcutExisted;
end;

{ The wizard shows the current state of the shortcut, which the app's Settings may have changed. }
procedure CurPageChanged(CurPageID: Integer);
begin
  if (CurPageID = wpSelectTasks) and not StartupTaskPreselected then
  begin
    if StartupShortcutExisted then
      WizardSelectTasks(StartupTaskName)
    else
      WizardSelectTasks('!' + StartupTaskName);
    StartupTaskPreselected := True;
  end;
end;

{ Inno never removes an icon for an unticked task; an interactive untick means "do not start with Windows". }
procedure CurStepChanged(CurStep: TSetupStep);
begin
  if (CurStep = ssPostInstall) and not WizardSilent and not WizardIsTaskSelected(StartupTaskName) then
    DeleteFile(StartupShortcutPath);
end;

function InitializeSetup: Boolean;
var
  Waited: Integer;
begin
  StartupShortcutExisted := FileExists(StartupShortcutPath);
  { A self-update starts this installer and then exits; wait for its instance mutex before copying files. }
  Waited := 0;
  while ShouldRelaunch and CheckForMutexes(AppInstanceMutex) and (Waited < AppExitTimeoutMs) do
  begin
    Sleep(AppExitPollMs);
    Waited := Waited + AppExitPollMs;
  end;
  Result := True;
end;
