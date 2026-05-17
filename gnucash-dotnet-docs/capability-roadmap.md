# GnuCash.DotNet Capability Roadmap

This document is the working map for the wrapper. Keep it updated as milestones land so future coding sessions can resume from the same context.

## Goal

Build a .NET 10 SDK that lets a normal Windows .NET application work with the official GnuCash installation without requiring a forked or custom GnuCash build.

The SDK should expose a developer-friendly API while the bridge process absorbs GnuCash runtime details, process architecture differences, and native integration complexity.

## Capability Classification

Every GnuCash capability should eventually be classified as one of:

- `SDK-supported`: exposed as a stable .NET API.
- `Bridge-supported`: available through the bridge protocol but not yet lifted into a polished SDK surface.
- `Read-only`: safe for inspection/query workflows.
- `Write-supported`: can change a book and is protected by accounting invariants and regression tests.
- `Import-export`: available through import/export operations rather than direct model mutation.
- `GUI-only`: belongs to the desktop UI and is not suitable for this wrapper.
- `Deferred`: valuable, but intentionally outside the current milestone.
- `Not viable with stock install`: blocked unless the official installer exposes more runtime surface.

## Target Capability Areas

### Core Accounting Model

- Books and book metadata.
- Account trees.
- Commodities and currencies.
- Transactions and splits.
- Prices.
- Lots.
- Balances.
- Opening balances.
- Query/search operations.

### Persistence And Sessions

- Locate and validate an official GnuCash install.
- Open an existing book.
- Create a new book.
- Save changes.
- Backup/copy a book.
- Validate book structure.
- Detect file/backend type.
- Surface friendly SDK errors.

### Imports

- QIF.
- OFX/QFX.
- CSV transactions.
- CSV prices.
- MT940 when available from the installed runtime.
- Import preview.
- Import execution.
- Duplicate detection results.

### Reconciliation

- Read cleared/reconciled state.
- Mark splits cleared.
- Mark splits reconciled.
- Model a reconciliation session.
- Validate ending date and balance.
- Regression coverage for statement import plus reconciliation state.

### Foreign Currency And Investments

- Multi-currency accounts.
- Exchange rates.
- Price database.
- Security and stock commodities.
- Buy/sell transactions.
- Dividends.
- Capital gains and lots where available.
- Portfolio valuation snapshots.

### Reports

Reports should start as structured data APIs before attempting GUI-style rendering.

- Balance sheet.
- Income statement.
- Trial balance.
- Account summary.
- Transaction report.
- Cash flow.
- Portfolio/security valuation.
- Export to DTO, JSON, and CSV first.
- HTML/PDF can come later if there is a stable renderer path.

### Business Features

- Customers.
- Vendors.
- Employees.
- Jobs.
- Invoices.
- Bills.
- Payments.
- Tax tables.
- Terms.
- Accounts receivable and accounts payable flows.

### Scheduled And Advanced Features

- Scheduled transactions.
- Budgets.
- Engine-relevant preferences.
- Tax metadata.
- Notes, attachments, and custom slots.

## Bridge Protocol Direction

The bridge protocol stays versioned and narrow. Each supported operation should include:

- Request DTO.
- Response DTO.
- Error behavior.
- Bridge unit test.
- SDK wrapper.
- SDK regression test.
- Capability matrix entry.

Near-term protocol commands:

- `Ping`
- `LocateGnuCash`
- `OpenBook`
- `ListCommodities`
- `ListAccounts`
- `ListTransactions`
- `ListPrices`
- `Shutdown`

Later protocol commands:

- `GetCapabilities`
- `GetBridgeInfo`
- `GetGnuCashInfo`
- `QueryPrices`
- `QueryBalances`
- `ImportPreview`
- `ImportExecute`
- `Reconcile`
- `RunReport`
- Business object commands.

## Milestones

### M0 - Skeleton And Release Gates

Status: complete.

The repo has the project skeleton, public docs, Apache-2.0 license, GitHub workflow foundation, GitVersion SemVer stamping, release signing gates, ArchUnitNET tests, Sentrux checks, package smoke tests, and a human/headless bridge shell.

### M1 - Read-Only Book Foundation

Status: complete.

Target:

- Open a sample GnuCash book through the bridge.
- Return book summary metadata.
- List commodities and currencies.
- List accounts.
- List transactions and splits.
- Expose the same flow through the public SDK.
- Add regression tests that prove the SDK launches the bridge and reads a sample book.

Initial implementation note:

The first parser can read GnuCash XML files directly in the bridge as a bootstrap capability. Native engine integration and non-XML backends can replace or augment the parser later without changing the public SDK shape.

### M2 - Query And Balance Layer

Status: complete.

Target:

- Account lookup by id/name/path.
- Transaction filtering by account, date, description, number, commodity, and reconciled state.
- Balance snapshots by account.
- Balance snapshots by commodity/currency.
- Basic price lookup support.

### M3 - Structured Reports

Status: complete.

Target:

- Account summary.
- Transaction report.
- Trial balance.
- Income statement.
- Balance sheet.
- Cash flow.
- Export DTOs to JSON and CSV.

### M4 - Reconciliation

Status: read-state complete; write support deferred.

Target:

- Read cleared and reconciled split state.
- Model reconciliation sessions.
- Mark splits cleared/reconciled after invariants are in place.
- Regression tests around imported statement data.

### M5 - Imports

Status: CSV preview complete; execution deferred.

Target:

- CSV transaction import preview.
- OFX/QFX preview where supported by the installed runtime.
- Duplicate detection result DTOs.
- Execution commands once preview is stable.

### M6 - Investments And Foreign Currency

Status: read foundation complete.

Target:

- Security commodities.
- Price database reads.
- Buy/sell/dividend transaction helpers.
- Portfolio valuation snapshots.
- Foreign currency report support.

### M7 - Business Objects

Status: pending.

Target:

- Customers, vendors, employees, jobs.
- Invoices and bills.
- Payments.
- Tax tables and terms.

### M8 - Native Runtime Parity Review

Status: pending.

Target:

- Compare implemented behavior against the official GnuCash runtime APIs and UI workflows.
- Mark every known capability as supported, deferred, GUI-only, or not viable with the stock installer.
- Decide whether a native shim, Scheme invocation, or file/backend access is needed for each remaining area.

## Journal

### 2026-05-17 - M1 Started

Decision:

- Use the existing bridge process and protocol boundaries.
- Keep the SDK x64/AnyCPU friendly.
- Make M1 stateless by passing the book path in each bridge request.
- Start with GnuCash XML parsing inside the bridge so the first usable read-only SDK does not depend on compiling GnuCash from source.

Next work:

- Add protocol DTOs for book summaries, commodities, accounts, transactions, and splits.
- Add bridge XML reader.
- Add SDK `OpenBookAsync` and `GnuCashBook` read methods.
- Add sample-book regression tests.

### 2026-05-17 - M1 Completed

Delivered:

- Added protocol DTOs for book requests, book summaries, commodities, accounts, rational amounts, splits, and transactions.
- Added bridge support for `OpenBook`, `ListCommodities`, `ListAccounts`, and `ListTransactions`.
- Added a bridge-side GnuCash XML reader that supports plain XML and compressed XML book files.
- Added SDK `OpenBookAsync`, `GnuCashBook`, and read-only methods for commodities, accounts, transactions, and splits.
- Added regression tests at the parser, headless bridge, protocol, SDK, and architecture levels.
- Added an ArchUnitNET smell test to keep raw book protocol DTOs out of the public SDK book surface.

Verification:

- `dotnet test -c Debug -p:HeurexTemplateBuildRoot=D:\temp\gnucash-dotnet-codex-build -v:minimal` passed.
- `pwsh -File .\tools\check.ps1 -Configuration Debug` passed with `HEUREX_TEMPLATE_BUILD_ROOT=D:\temp\gnucash-dotnet-codex-check`.
- Sentrux check and gate both passed.

Known limitation:

- M1 reads GnuCash XML files as a bootstrap path. SQLite and database-backed books are explicitly not supported yet by the reader.

Next work:

- Start M2 by adding account lookup, transaction filtering, and balance snapshots.

### 2026-05-17 - M2 Started

Decision:

- Build the first query and balance layer in the SDK on top of the M1 read-only book methods.
- Keep bridge protocol churn low until filtering or balances need native engine behavior for performance or backend parity.
- Preserve rational values for balances instead of converting accounting values to floating point.

Target:

- Account filtering by id, name, type, parent id, and commodity.
- Transaction filtering by account, posted date, number, description, currency, and split reconciled state.
- Account balance snapshots grouped by account.

### 2026-05-17 - M2 Completed

Delivered:

- Added SDK query DTOs for accounts and transactions.
- Added account filtering by id, partial name, type, parent id, commodity space, and commodity id.
- Added `GetAccountByIdAsync`.
- Added transaction filtering by account id, posted date range, number, description text, currency, and split reconciled state.
- Added account balance snapshots with rational amount preservation and decimal projection.
- Added single-account balance lookup.
- Added SDK regression tests for account filters, transaction filters, and balances.

Verification:

- `dotnet test -c Debug -p:HeurexTemplateBuildRoot=D:\temp\gnucash-dotnet-codex-build-m2 -v:minimal` passed with 41 tests.

Known limitation:

- Balance snapshots are computed in the SDK from transaction split values. This is correct for the XML bootstrap reader but should be compared against native GnuCash engine balances once native/backend integration is added.

Next work:

- Start M3 by defining structured report DTOs and implementing account summary plus transaction report over the M1/M2 read model.

### 2026-05-17 - M3 Started

Decision:

- Start reports as structured SDK DTOs instead of GUI-style rendered reports.
- Build account summary and transaction report from the M1/M2 read model.
- Include CSV/JSON helpers so consumers can quickly move report data into files, pipelines, or tests.

### 2026-05-17 - M3 Completed

Delivered:

- Added structured account summary report DTOs.
- Added structured split-level transaction report DTOs.
- Added `CreateAccountSummaryReportAsync`.
- Added `CreateTransactionReportAsync` with transaction query support.
- Added CSV and JSON report serialization helpers.
- Added regression tests for account summary rows, grouped totals, transaction report rows, CSV output, and JSON output.

Verification:

- `dotnet test -c Debug -p:HeurexTemplateBuildRoot=D:\temp\gnucash-dotnet-codex-build-m3 -v:minimal` passed with 43 tests.

Known limitation:

- Reports are currently SDK-computed structured data reports, not native GnuCash GUI report renderer output.

Next work:

- Start M4 with reconciliation read-state APIs first, then add write operations only after the native/backend mutation path is designed.

### 2026-05-17 - M4 Started

Decision:

- Begin with read-only reconciliation state summaries.
- Defer marking splits cleared or reconciled until the project has an explicit write path and book-save strategy.
- Treat GnuCash split states `n`, `c`, and `y` as unreconciled, cleared, and reconciled.

### 2026-05-17 - M4 Read-State Completed

Delivered:

- Added reconciliation summary DTOs.
- Added per-account summaries for unreconciled, cleared, reconciled, and other split states.
- Added state-specific balances using rational split values.
- Added `ListReconciliationSummariesAsync`.
- Added `GetReconciliationSummaryAsync`.
- Added regression tests for account reconciliation summaries and state-filtered summaries.

Verification:

- `dotnet test -c Debug -p:HeurexTemplateBuildRoot=D:\temp\gnucash-dotnet-codex-build-m4 -v:minimal` passed with 44 tests.

Deferred:

- Marking splits cleared or reconciled remains deferred until write support has a native/backend-safe design.

Next work:

- Start M5 with import capability design and preview DTOs before executing any import mutations.

### 2026-05-17 - M5 Started

Decision:

- Start imports with CSV transaction preview only.
- Validate row shape, dates, amounts, and target account existence.
- Do not write transactions into a book until the mutation/save path is designed.

### 2026-05-17 - M5 CSV Preview Completed

Delivered:

- Added CSV transaction import option DTOs.
- Added import preview row and issue DTOs.
- Added `PreviewCsvTransactionImportAsync`.
- Added CSV parsing for headered and fixed-position rows.
- Added validation for target account existence, required columns, date shape, description, and decimal amount.
- Added amount scaling using the target account commodity fraction.
- Added regression tests for valid and invalid preview rows.

Verification:

- `dotnet test -c Debug -p:HeurexTemplateBuildRoot=D:\temp\gnucash-dotnet-codex-build-m5 -v:minimal` passed with 45 tests.

Deferred:

- Import execution remains deferred until the project has a native/backend-safe write and save strategy.
- OFX/QFX, QIF, CSV prices, and duplicate detection remain future import work.

Next work:

- Start M6 with commodity/security-oriented read models, price database strategy, and investment transaction helpers.

### 2026-05-17 - M6 Started

Decision:

- Start with read-only commodity/security and price database support.
- Parse GnuCash XML price entries through the bridge.
- Expose securities and price queries through the SDK before adding buy/sell/dividend helpers.

### 2026-05-17 - M6 Read Foundation Completed

Delivered:

- Added price database protocol DTOs.
- Added `ListPrices` bridge protocol command.
- Added GnuCash XML `pricedb` parsing in the bridge.
- Added SDK price and price query models.
- Added `ListSecuritiesAsync`.
- Added `ListPricesAsync`.
- Added `ListLatestPricesAsync`.
- Added regression tests for securities, price database entries, price filtering, and latest price selection.

Verification:

- `dotnet test -c Debug -p:HeurexTemplateBuildRoot=D:\temp\gnucash-dotnet-codex-build-m6 -v:minimal` passed with 47 tests.

Deferred:

- Price writes, online quote retrieval, investment transaction helpers, and portfolio valuation reports remain future work.

Next work:

- Start M7 only after deciding whether business objects should come from XML parsing, native engine APIs, or a database/backend adapter.
