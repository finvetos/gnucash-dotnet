# Bridge Process

The bridge process is the compatibility boundary for official Windows GnuCash installs.

Responsibilities:

- run as `win-x86`
- locate the official GnuCash installation
- configure native search paths for GnuCash DLLs and backend modules
- expose a narrow protocol to the SDK
- keep native GnuCash pointer ownership out of public .NET APIs

Initial protocol scope:

1. `Ping`
2. `LocateGnuCash`
3. `OpenBook`
4. `ListAccounts`
5. `Shutdown`

Transport is intentionally undecided at the scaffold stage. Named pipes are the preferred default for Windows once the process lifecycle is implemented.