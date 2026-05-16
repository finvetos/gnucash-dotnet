#!/usr/bin/env pwsh
[CmdletBinding(SupportsShouldProcess = $true)]
param(
  [Parameter(Mandatory = $true)]
  [string] $ProjectRoot,

  [switch] $NoGitIgnore,

  [switch] $NoReadmes
)

$ErrorActionPreference = "Stop"
$PSNativeCommandUseErrorActionPreference = $true
$script:HxUtf8NoBomEncoding = New-Object -TypeName System.Text.UTF8Encoding -ArgumentList $false

function Resolve-HxProjectRoot {
  param([string] $Path)

  $expandedPath = [Environment]::ExpandEnvironmentVariables($Path)
  if (-not (Test-Path $expandedPath)) {
    New-Item -ItemType Directory -Force -Path $expandedPath | Out-Null
  }

  return (Resolve-Path $expandedPath).Path
}

function New-HxDirectory {
  param(
    [string] $Root,
    [string] $RelativePath
  )

  $path = Join-Path $Root $RelativePath
  if ($PSCmdlet.ShouldProcess($path, "Create directory")) {
    New-Item -ItemType Directory -Force -Path $path | Out-Null
  }
}

function New-HxReadme {
  param(
    [string] $Root,
    [string] $RelativePath,
    [string] $Content
  )

  $path = Join-Path $Root $RelativePath
  if (-not (Test-Path $path) -and $PSCmdlet.ShouldProcess($path, "Create README")) {
    [System.IO.File]::WriteAllText($path, $Content, $script:HxUtf8NoBomEncoding)
  }
}

function Update-HxGitIgnore {
  param([string] $Root)

  $gitIgnorePath = Join-Path $Root ".gitignore"
  $block = @"

# Heurex generated output
bin/
obj/
artifacts/
publish/
packages/
TestResults/
*.binlog
*.trx
*.coverage
*.coveragexml
coverage.cobertura.xml
*.nupkg
*.snupkg
*.symbols.nupkg

# Python local output
.venv/
__pycache__/
*.py[cod]
.pytest_cache/
.ruff_cache/
.mypy_cache/
.pyright/
.coverage
.coverage.*
htmlcov/
dist/
build/
*.egg-info/
*.whl

# JavaScript and TypeScript local output
node_modules/
.pnpm-store/
.npm/
.turbo/
.vite/
.vitest/
coverage/
*.tsbuildinfo

# Heurex release artifacts. Keep releases/channels, releases/manifests, and releases/release.run.yml tracked.
releases/artifacts/
releases/packages/
releases/publish/
releases/tmp/
releases/**/*.nupkg
releases/**/*.snupkg
releases/**/*.zip
releases/**/*.tar.gz
"@

  $existing = if (Test-Path $gitIgnorePath) { Get-Content -Raw -Path $gitIgnorePath } else { "" }
  if ($existing.Contains("# Heurex generated output")) {
    Write-Host ".gitignore already contains the Heurex generated-output block."
    return
  }

  if ($PSCmdlet.ShouldProcess($gitIgnorePath, "Append Heurex generated-output ignore rules")) {
    [System.IO.File]::AppendAllText($gitIgnorePath, $block, $script:HxUtf8NoBomEncoding)
  }
}

$root = Resolve-HxProjectRoot -Path $ProjectRoot

$directories = @(
  "src",
  "test",
  "gnucash-dotnet-docs",
  "gnucash-dotnet-docs\architecture",
  "gnucash-dotnet-docs\architecture\\decisions",
  "gnucash-dotnet-docs\architecture\\diagrams",
  "gnucash-dotnet-docs\operations",
  "releases",
  "releases\channels",
  "releases\manifests",
  "releases\artifacts",
  "releases\packages",
  "releases\publish",
  "releases\tmp",
  "assets",
  "assets\icons",
  "assets\logos",
  "assets\fonts",
  "tools"
)

foreach ($directory in $directories) {
  New-HxDirectory -Root $root -RelativePath $directory
}

if (-not $NoReadmes) {
  New-HxReadme -Root $root -RelativePath "gnucash-dotnet-docs\README.md" -Content "# Documentation`n`nUse this directory for architecture, operating notes, decisions, and Mermaid-enabled Obsidian content.`n"
  New-HxReadme -Root $root -RelativePath "assets\README.md" -Content "# Assets`n`nPlace icons, logos, fonts, and other product-owned static assets under this directory.`n"
  New-HxReadme -Root $root -RelativePath "releases\README.md" -Content "# Releases`n`nKeep release manifests and channel definitions tracked here. Generated release artifacts belong under ignored subdirectories such as packages, publish, artifacts, and tmp.`n"
}

if (-not $NoGitIgnore) {
  Update-HxGitIgnore -Root $root
}

if ($WhatIfPreference) {
  Write-Host "Heurex project layout would be ready at $root"
}
else {
  Write-Host "Heurex project layout is ready at $root"
}
