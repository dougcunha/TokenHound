<#
.SYNOPSIS
  Publishes TokenHound and builds the Inno Setup installer.

.DESCRIPTION
  Runs a framework-dependent single-file publish of TokenHound.App and then
  compiles installer/TokenHound.iss with ISCC.exe. ISCC is located via PATH,
  the Inno Setup 6 uninstall registry keys, and well-known install paths.

.PARAMETER Version
  Version stamped into the assembly (-p:Version) and the installer
  (/DMyAppVersion). Defaults to 0.0.0-dev for local builds.

.PARAMETER Rid
  Runtime identifier to publish (win-x64 or win-arm64). Defaults to win-x64.

.PARAMETER Configuration
  Build configuration. Defaults to Release.
#>
[CmdletBinding()]
param(
  [string]$Version = "0.0.0-dev",
  [string]$Rid = "win-x64",
  [string]$Configuration = "Release"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Find-Iscc {
  $fromPath = Get-Command "iscc.exe" -ErrorAction SilentlyContinue
  if ($null -ne $fromPath) {
    return $fromPath.Source
  }

  $registryKeys = @(
    "HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Inno Setup 6_is1",
    "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Inno Setup 6_is1",
    "HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Inno Setup 6_is1"
  )
  foreach ($key in $registryKeys) {
    $location = Get-ItemPropertyValue -Path $key -Name "InstallLocation" -ErrorAction SilentlyContinue
    if ([string]::IsNullOrWhiteSpace($location)) {
      continue
    }

    $candidate = Join-Path $location "ISCC.exe"
    if (Test-Path -LiteralPath $candidate) {
      return $candidate
    }
  }

  $wellKnown = @(
    (Join-Path $env:LOCALAPPDATA "Programs\Inno Setup 6\ISCC.exe"),
    "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
    "C:\Program Files\Inno Setup 6\ISCC.exe"
  )
  foreach ($candidate in $wellKnown) {
    if (Test-Path -LiteralPath $candidate) {
      return $candidate
    }
  }

  throw "ISCC.exe not found. Install Inno Setup 6 (winget install JRSoftware.InnoSetup) or add it to PATH."
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$publishDir = Join-Path $repoRoot "publish\$Rid"
$outputDir = Join-Path $repoRoot "dist"
$issPath = Join-Path $PSScriptRoot "TokenHound.iss"

if (-not (Test-Path -LiteralPath $issPath)) {
  throw "Installer script not found: $issPath"
}

$arch = "x64"
if ($Rid -eq "win-arm64") {
  $arch = "arm64"
}

Write-Host "Publishing TokenHound.App ($Rid, $Configuration, version $Version)..."
& dotnet publish (Join-Path $repoRoot "src\TokenHound.App\TokenHound.App.csproj") `
  -c $Configuration `
  -r $Rid `
  --self-contained false `
  -p:PublishSingleFile=true `
  -p:Version=$Version `
  -o $publishDir `
  --nologo
if ($LASTEXITCODE -ne 0) {
  throw "dotnet publish failed with exit code $LASTEXITCODE."
}

$exeName = "TokenHound.App.exe"
if (-not (Test-Path -LiteralPath (Join-Path $publishDir $exeName))) {
  throw "Publish output missing expected executable: $(Join-Path $publishDir $exeName)"
}

$iscc = Find-Iscc
Write-Host "Compiling installer with $iscc ..."

New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
& $iscc "/DMyAppVersion=$Version" "/DPublishDir=$publishDir" "/DOutputDir=$outputDir" "/DMyAppArch=$arch" $issPath
if ($LASTEXITCODE -ne 0) {
  throw "ISCC failed with exit code $LASTEXITCODE."
}

$setupExe = Join-Path $outputDir "TokenHound-Setup-$Version-$Rid.exe"
if (-not (Test-Path -LiteralPath $setupExe)) {
  throw "Expected installer not found: $setupExe"
}

Write-Host "Installer built: $setupExe"
