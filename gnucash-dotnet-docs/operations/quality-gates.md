# Quality Gates

This repository uses two architecture gates by default.

## Sentrux

Sentrux scans repository structure and catches graph-level drift. Heurex templates prefer the local custom build at `tools/sentrux/sentrux.exe`, then fall back to `sentrux` on `PATH`.

The custom build supports `--include-untracked`, so files created by an AI agent are scanned before they are committed.

Sentrux 0.5.x uses normalized structural scores from `0.00` to `1.00`. Keep `.sentrux/rules.toml` thresholds on that scale.

```powershell
.\tools\build-sentrux.ps1
.\tools\check.ps1
```

Before the repository has its first commit, run `.\tools\check.ps1 -DisableGitVersion` because GitVersion needs a commit graph.

## ArchUnitNET

ArchUnitNET/xUnitV3 rules live in `test/*/ArchitectureRulesTests.cs` and in the cross-assembly smell suite at `test/GnuCash.DotNet.Architecture.Tests/BoundarySmellTests.cs`. They run under `dotnet test`.

Use ArchUnitNET for exact boundaries. Use Sentrux for whole-codebase structural health.

Default rules include production-to-test dependency protection, test-tooling leakage checks, and namespace/layer drift checks where the template has enough structure to assert them. Add project-specific rules as the architecture grows.

The release workflow builds Sentrux, then runs `tools/release.ps1`. That script runs restore, build, `dotnet test` with the ArchUnitNET suites, and `tools/check.ps1 -RequireSentrux` before packages or release artifacts are produced.

Release CI also runs `tools/package-smoke.ps1`, a minimal regression test that consumes the produced NuGet package from a temporary console app and verifies the packaged bridge can be started by the SDK.
