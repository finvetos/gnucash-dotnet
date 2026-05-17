[CmdletBinding()]
param(
  [string] $BridgePath = "",
  [string] $InstallPath = "",
  [string] $BookPath = "",
  [string] $WorkingRoot = "",
  [switch] $AllBinDlls
)

$ErrorActionPreference = "Stop"
$PSNativeCommandUseErrorActionPreference = $true

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")

if ([string]::IsNullOrWhiteSpace($BridgePath)) {
  $BridgePath = Join-Path $repoRoot "releases\publish\bridge-win-x86\GnuCash.DotNet.Bridge.exe"
}

if ([string]::IsNullOrWhiteSpace($WorkingRoot)) {
  $WorkingRoot = Join-Path $repoRoot "artifacts\native-smoke"
}

if (-not (Test-Path -LiteralPath $BridgePath)) {
  throw "Bridge executable not found. Publish it first: $BridgePath"
}

New-Item -ItemType Directory -Force -Path $WorkingRoot | Out-Null

function Invoke-SmokeCommand {
  param(
    [string[]] $Arguments,
    [switch] $AllowFailure
  )

  Write-Host ""
  Write-Host ">> $BridgePath $($Arguments -join ' ')"
  & $BridgePath @Arguments
  $exitCode = $LASTEXITCODE
  if ($exitCode -ne 0 -and -not $AllowFailure) {
    throw "Native smoke command failed with exit code $exitCode."
  }
}

$installArgs = @("validate", "--plain")
$apiArgs = @("validate-api", "--plain")
$inventoryArgs = @("inventory-exports", "--plain")
if (-not [string]::IsNullOrWhiteSpace($InstallPath)) {
  $installArgs += @("--install-path", $InstallPath)
  $apiArgs += @("--install-path", $InstallPath)
  $inventoryArgs += @("--install-path", $InstallPath)
}

if ($AllBinDlls) {
  $inventoryArgs += "--all-bin-dlls"
}

Invoke-SmokeCommand $installArgs
Invoke-SmokeCommand $apiArgs
Invoke-SmokeCommand $inventoryArgs

if ([string]::IsNullOrWhiteSpace($BookPath)) {
  Write-Host ""
  Write-Host "Book smoke skipped. Pass -BookPath with a real GnuCash-created disposable book to run read/write checks."
  exit 0
}

if (-not (Test-Path -LiteralPath $BookPath)) {
  throw "BookPath does not exist: $BookPath"
}

$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$workingBookPath = Join-Path $WorkingRoot "roundtrip-$timestamp.gnucash"

$sessionArgs = @("validate-session", "--book-path", $BookPath, "--plain")
$parityArgs = @("validate-read-parity", "--book-path", $BookPath, "--plain")
$roundTripArgs = @(
  "validate-write-roundtrip",
  "--source-book-path",
  $BookPath,
  "--working-book-path",
  $workingBookPath,
  "--plain"
)

if (-not [string]::IsNullOrWhiteSpace($InstallPath)) {
  $sessionArgs += @("--install-path", $InstallPath)
  $parityArgs += @("--install-path", $InstallPath)
  $roundTripArgs += @("--install-path", $InstallPath)
}

Invoke-SmokeCommand $sessionArgs
Invoke-SmokeCommand $parityArgs
Invoke-SmokeCommand $roundTripArgs

Write-Host ""
Write-Host "Native smoke completed. Working book: $workingBookPath"
