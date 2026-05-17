# Bridge Process

The bridge process is the compatibility boundary for official Windows GnuCash installs.

Responsibilities:

- run as `win-x86`
- locate the official GnuCash installation
- configure native search paths for GnuCash DLLs and backend modules
- expose a narrow headless protocol to the SDK
- provide a friendly human-facing console surface for diagnostics and local use
- keep native GnuCash pointer ownership out of public .NET APIs

Initial protocol scope:

1. `Ping`
2. `LocateGnuCash`
3. `OpenBook`
4. `ListAccounts`
5. `Shutdown`

The executable has two modes:

- Human mode: commands such as `help`, `list`, `ping`, and `validate` render through Spectre.Console when attached to an interactive terminal and can fall back to plain or JSON output.
- Headless mode: `GnuCash.DotNet.Bridge headless --stdio` is reserved for the SDK. It reads one `BridgeRequest` JSON envelope per line from stdin and writes one compact `BridgeResponse` JSON envelope per line to stdout. Diagnostic text goes to stderr only.

The `validate` command is the user-facing preflight:

```powershell
GnuCash.DotNet.Bridge validate
GnuCash.DotNet.Bridge validate --install-path "C:\Program Files (x86)\gnucash"
GnuCash.DotNet.Bridge validate --json
```

Validation checks explicit paths, `GNUCASH_HOME`, Windows uninstall registry entries, and common Program Files locations. A ready installation must include the GnuCash GUI/CLI executables, core engine DLLs, and the `etc`, `lib`, and `share` runtime folders. When validation fails, the command tells the user to install GnuCash for Windows first.

The first transport is newline-delimited JSON over stdio because it is simple to launch, test, and package with the SDK. Named pipes can still be added later if the protocol needs long-running multiplexed sessions.

SDK startup path:

1. `GnuCashClient.ValidateInstallationAsync()` creates a `LocateGnuCash` protocol request.
2. The SDK starts the configured bridge path, or a packaged bridge found next to the SDK, as `headless --stdio`.
3. The SDK sends one JSON `BridgeRequest` line to stdin and reads one JSON `BridgeResponse` line from stdout.
4. The bridge returns a `GnuCashInstallationStatus` payload that callers can show directly or use as a readiness gate before opening a book.

The SDK can launch either a published `GnuCash.DotNet.Bridge.exe` or a development `GnuCash.DotNet.Bridge.dll` through `dotnet`. Production packages should prefer the `win-x86` executable so the bridge can load the official Windows GnuCash runtime.
