#!/usr/bin/env pwsh
[CmdletBinding()]
param(
  [string] $Branch = "codex/include-untracked-scan-files",
  [string] $Repo = "",
  [string] $OutputPath = ""
)

$ErrorActionPreference = "Stop"
$PSNativeCommandUseErrorActionPreference = $true

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$siblingFork = Join-Path (Split-Path -Parent $repoRoot) "sentrux"
if ([string]::IsNullOrWhiteSpace($Repo)) {
  $Repo = if (Test-Path (Join-Path $siblingFork ".git")) { $siblingFork } else { "https://github.com/dbchandra23/sentrux.git" }
}

$srcDir = Join-Path $repoRoot "tools/sentrux-src"
$outDir = if ([string]::IsNullOrWhiteSpace($OutputPath)) { Join-Path $repoRoot "tools/sentrux" } else { $OutputPath }
$exeName = if ($IsWindows -or -not (Test-Path Variable:IsWindows)) { "sentrux.exe" } else { "sentrux" }

if (-not (Test-Path $srcDir)) {
  git clone --branch $Branch $Repo $srcDir
} else {
  Push-Location $srcDir
  try {
    git fetch origin $Branch
    git checkout $Branch
    git reset --hard "origin/$Branch"
  } finally {
    Pop-Location
  }
}

if ($IsWindows -or -not (Test-Path Variable:IsWindows)) {
  $devShell = $env:VSDEVSHELL_PATH
  if ([string]::IsNullOrWhiteSpace($devShell) -and -not [string]::IsNullOrWhiteSpace($env:VSINSTALLDIR)) {
    $candidate = Join-Path $env:VSINSTALLDIR "Common7\Tools\Launch-VsDevShell.ps1"
    if (Test-Path $candidate) {
      $devShell = $candidate
    }
  }

  if ([string]::IsNullOrWhiteSpace($devShell) -and (Get-Command "vswhere" -ErrorAction SilentlyContinue)) {
    $devShell = & vswhere -latest -products * -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -find "Common7\Tools\Launch-VsDevShell.ps1" | Select-Object -First 1
  }

  if ($devShell -and (Test-Path $devShell)) {
    & $devShell -Arch amd64 -HostArch amd64 -SkipAutomaticLocation 2>&1 | Out-Null
  } else {
    Write-Warning "Launch-VsDevShell.ps1 not found. cargo build may fail to locate link.exe."
  }
}

Push-Location $srcDir
try {
  $env:CARGO_TERM_COLOR = "never"
  cargo build --release -p sentrux
} finally {
  Pop-Location
}

$built = Join-Path $srcDir "target/release/$exeName"
if (-not (Test-Path $built)) {
  throw "Expected built binary at $built but it is missing."
}

New-Item -ItemType Directory -Force -Path $outDir | Out-Null
Copy-Item $built (Join-Path $outDir $exeName) -Force
& (Join-Path $outDir $exeName) --version
