# How to Contribute

Thanks for your interest in GnuCash.DotNet.

This project is public from the beginning, but it is not ready for broad external contributions until the first usable release. The early work is focused on proving the bridge architecture, keeping the licensing boundary clean, and establishing reliable compatibility tests against an official Windows GnuCash installation.

## Current Contribution Status

Until the first release:

- Issues and discussions may be disabled or restricted.
- Pull requests may be restricted to collaborators.
- Public support requests may be redirected or closed without review.
- Security-sensitive reports should follow `SECURITY.md`.

After the first release, this page should be updated with the supported contribution workflow, issue templates, roadmap labels, and maintainer response expectations.

## What We Are Building First

The first implementation milestone is read-only access through the bridge process:

1. Locate an official installed GnuCash for Windows.
2. Start the bundled bridge process.
3. Report bridge and GnuCash runtime health.
4. Open a GnuCash book.
5. List accounts.

Write support should wait until the SDK has explicit accounting invariants, file compatibility tests, and clear user-facing failure modes.

## Architecture Rules

Contributions must preserve these boundaries:

- `GnuCash.DotNet.Protocol` contains pure SDK-to-bridge contracts.
- `GnuCash.DotNet` exposes the public developer API and must not depend on bridge implementation details.
- `GnuCash.DotNet.Bridge` hosts the Windows bridge process and must not depend on SDK implementation details.
- Native GnuCash binaries, headers, or copied upstream source must not be committed to this repository.
- GnuCash must remain an external user-installed dependency unless the licensing model is explicitly reviewed.

## Local Checks

Run the full gate before opening a pull request:

```powershell
pwsh -File .\tools\check.ps1 -Configuration Release -RequireSentrux
```

Before the repository has its first commit graph available to GitVersion, use:

```powershell
pwsh -File .\tools\check.ps1 -Configuration Release -RequireSentrux -DisableGitVersion
```

The gate runs restore, build, tests, ArchUnitNET architecture checks, Sentrux structural checks, and the Sentrux regression gate.

## Commit Shape

Use small commits with clear intent:

```text
feat: add bridge install detection
fix: handle missing GnuCash registry key
test: cover bridge handshake parsing
docs: explain release gating
```

Keep unrelated refactors out of feature commits.

## Documentation

Update `gnucash-dotnet-docs/` when you change architecture, release flow, native interop, licensing assumptions, or public SDK behavior.
