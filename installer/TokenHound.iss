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

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon
Name: "{userstartup}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: startupicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch {#MyAppName}"; Flags: nowait postinstall skipifsilent
