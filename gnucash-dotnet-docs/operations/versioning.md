# Versioning

This repository uses SemVer with GitVersion.

GitVersion.MsBuild is referenced centrally, so normal `dotnet build`, `dotnet pack`, and release workflow runs stamp assembly and NuGet versions automatically from the Git history. GitHub Actions uses full checkout history (`fetch-depth: 0`) so tags and commit-message increments are visible during CI/CD.

## Stable Releases

Tag stable releases with a `v` prefix:

```powershell
git tag v0.1.0
git push origin v0.1.0
```

## Commit Messages

GitVersion increments versions from Conventional Commits:

```text
feat: add list command
fix: handle redirected output
perf: reduce allocations
feat!: change public command behavior
```

It also accepts explicit directives:

```text
+semver: major
+semver: minor
+semver: patch
+semver: breaking
+semver: feature
+semver: fix
+semver: none
```

## Policy

- `MAJOR`: breaking public command/API behavior
- `MINOR`: backward-compatible functionality
- `PATCH`: backward-compatible fixes
- `alpha`: internal or development preview
- `beta`: feature-complete preview
- `rc`: release candidate

## Install Commit Hook

```powershell
.\tools\install-git-hooks.ps1
```

## Show Computed Version

```powershell
.\tools\version.ps1
```

Release workflow tags still use `v` prefixes, but the computed SemVer is owned by GitVersion. Do not hard-code package versions in CI scripts.
