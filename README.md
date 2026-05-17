# GnuCash.DotNet

GnuCash.DotNet is a .NET 10 SDK for working with locally installed GnuCash on Windows.

The project is designed around the official GnuCash installer. It does not require users to install a forked GnuCash build. Normal x64 .NET applications talk to a bundled 32-bit bridge process, and that bridge loads the official 32-bit GnuCash libraries from the user's machine.

## Repository Shape

```text
src/
  GnuCash.DotNet/              Public .NET SDK surface
  GnuCash.DotNet.Protocol/     Shared SDK-to-bridge contracts
  GnuCash.DotNet.Bridge/       win-x86 helper process
  GnuCash.DotNet.NativeShim/   Optional native C shim placeholder

test/
  GnuCash.DotNet.Tests/
  GnuCash.DotNet.Protocol.Tests/
  GnuCash.DotNet.Bridge.Tests/
  GnuCash.DotNet.IntegrationTests/

gnucash-dotnet-docs/           Architecture, operations, and decisions
.github/workflows/             CI and release automation
releases/                      Release run model and package output
assets/                        Product-owned assets
samples/                       Consumer examples
```

## Architecture

```text
User .NET app, usually x64
        |
        v
GnuCash.DotNet.dll
        |
        | starts and manages
        v
GnuCash.DotNet.Bridge.exe, win-x86
        |
        v
Official installed GnuCash x86 DLLs
```

The first milestone is a read-only SDK: locate GnuCash, start the bridge, open a GnuCash XML book, and list commodities, accounts, transactions, and splits. Write support should come later behind explicit accounting invariants and compatibility tests.

## Build

```powershell
pwsh -File .\tools\check.ps1
```

The repository follows the Heurex .NET template standards: central package management, GitVersion, xUnit v3, ArchUnitNET, Sentrux structural checks, and release run manifests.

Release builds are SemVer-stamped by GitVersion and require strong-name signing through the CI signing secret. The release pipeline also runs a package smoke regression to make sure a consumer app can install the produced package and launch the bundled bridge.

## Contributing

This project is public from the beginning, but broad community contribution is intentionally paused until the first usable SDK release. Read [How to Contribute.md](How%20to%20Contribute.md) before opening issues or pull requests.

## License

This repository is licensed under Apache-2.0. GnuCash itself is not bundled and remains governed by the GnuCash project's own license terms.

Read [gnucash-dotnet-docs/operations/licensing.md](gnucash-dotnet-docs/operations/licensing.md) before changing native integration or distribution behavior.
