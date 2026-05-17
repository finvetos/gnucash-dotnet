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
5. `ListCommodities`
6. `ListTransactions`
7. `ListPrices`
8. `Shutdown`

Native API protocol scope starts with:

1. `ValidateNativeApi`
2. `ValidateNativeSession`
3. Native read parity for accounts, commodities, transactions, prices, and business objects
4. Native write workflows for customers, vendors, employees, jobs, invoices, bills, entries, posting, payments, tax tables, and terms

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

The `validate-api` command checks that the installed `libgnc-engine.dll` exposes the native symbols needed by write-capable bridge features:

```powershell
GnuCash.DotNet.Bridge validate-api
GnuCash.DotNet.Bridge validate-api --install-path "C:\Program Files (x86)\gnucash"
GnuCash.DotNet.Bridge validate-api --json
```

The API check validates the export surface without mutating a book. It checks engine exports from `libgnc-engine.dll`, module initialization from `libgnc-module.dll`, and runtime/binreloc setup exports from `libgnc-core-utils.dll`. The SDK still calls native GnuCash from the packaged win-x86 bridge process; the public .NET library never exposes GnuCash pointers or requires consuming applications to run as x86.

The `validate-session` command opens a specific book read-only through the native engine:

```powershell
GnuCash.DotNet.Bridge validate-session --book-path "D:\books\sample.gnucash"
GnuCash.DotNet.Bridge validate-session --install-path "C:\Program Files (x86)\gnucash" --book-path "D:\books\sample.gnucash"
GnuCash.DotNet.Bridge validate-session --book-path "D:\books\sample.gnucash" --json
```

The bridge configures process-local GnuCash runtime state before opening a native session. In particular it points GnuCash binreloc at the installed prefix, runs GnuCash environment setup, initializes the module system, initializes the engine, normalizes Windows paths to `file:` URIs, and then calls `qof_session_begin` with `SESSION_READ_ONLY`.

The first transport is newline-delimited JSON over stdio because it is simple to launch, test, and package with the SDK. Named pipes can still be added later if the protocol needs long-running multiplexed sessions.

SDK startup path:

1. `GnuCashClient.ValidateInstallationAsync()` creates a `LocateGnuCash` protocol request.
2. The SDK starts the configured bridge path, or a packaged bridge found next to the SDK, as `headless --stdio`.
3. The SDK sends one JSON `BridgeRequest` line to stdin and reads one JSON `BridgeResponse` line from stdout.
4. The bridge returns a `GnuCashInstallationStatus` payload that callers can show directly or use as a readiness gate before opening a book.

The SDK can launch either a published `GnuCash.DotNet.Bridge.exe` or a development `GnuCash.DotNet.Bridge.dll` through `dotnet`. Production packages should prefer the `win-x86` executable so the bridge can load the official Windows GnuCash runtime.

The first read-only book milestone uses a bridge-side GnuCash XML reader for plain and compressed XML files. That keeps the SDK shape moving while native engine integration is added. SQLite and database-backed books are intentionally deferred until native session support is in place.

Business objects, imports, reconciliation writes, invoice posting, payments, and any operation that saves a book must use the native GnuCash engine API. Direct XML or SQL access is read-only bootstrap support and must not become the authoritative mutation path.

NuGet packages copy the published bridge into consuming app outputs at:

```text
GnuCash.DotNet.Bridge/win-x86/GnuCash.DotNet.Bridge.exe
```

The SDK probes that packaged location before falling back to legacy/dev locations or an explicit `GnuCash:BridgeExecutablePath`.
