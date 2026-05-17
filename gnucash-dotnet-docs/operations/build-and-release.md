# Build And Release

## Verify

```powershell
.\tools\verify.ps1
```

Run helper scripts with `pwsh` when launching them explicitly:

```powershell
pwsh -File .\tools\verify.ps1
pwsh -File .\tools\check.ps1
```

Fresh clones and freshly generated repositories need `dotnet restore` before a direct `dotnet build --no-restore`. The helper scripts already run restore first.

For the full coding gate, including Sentrux when available:

```powershell
.\tools\check.ps1
```

For the release gate, Sentrux is required unless `-SkipSentrux` is passed:

```powershell
.\tools\release.ps1
```

## Pack

```powershell
.\tools\pack.ps1
```

Packages are written to the generated repository by default:

```text
releases/packages
```

When `HEUREX_TEMPLATE_BUILD_ROOT` is configured, packages are written under `<external-build-root>/dotnet/packages/<repo-name>` instead.

`tools/pack.ps1` publishes the bridge as `win-x86` before packing. The SDK package includes the bridge under `tools/bridge/win-x86` plus a `buildTransitive` target that copies it to the consuming app output under:

```text
GnuCash.DotNet.Bridge/win-x86
```

## Package Smoke Regression

Run the lightweight consumer smoke test after packing:

```powershell
.\tools\package-smoke.ps1
```

The smoke test creates a temporary console app, installs the local `GnuCash.DotNet` package, verifies the packaged bridge is copied to the consumer output, and calls `ValidateInstallationAsync()` against a fake GnuCash installation shape.

## Release Signing

Release builds require strong-name signing. Store the private key as a GitHub Actions secret:

```text
GNUCASH_DOTNET_STRONG_NAME_KEY_BASE64
```

The release workflow decodes that secret into a temporary key file, passes it to MSBuild through `GnuCashDotNetStrongNameKeyFile`, and sets `GnuCashDotNetRequireStrongName=true`. Architecture tests assert that release assemblies are strong-name signed when `GNUCASH_DOTNET_EXPECT_SIGNED=true`.

## Publish

Publish only from a deliberate release or preview gate. Avoid publishing every local AI-assisted edit.

Private NuGet package promotion:

1. Local shared directory for the first development loop and high-frequency AI-assisted package cycles.
2. Local LAN Synology-hosted NuGet store once packages need to be consumed from multiple machines on the network.
3. Azure Artifacts once the package is release-ready and should be available away from the LAN without VPN friction.

Public libraries use GitHub and GitHub Releases as the public release surface.

Release intent is captured in `releases/release.run.yml`.
