# Early Roadmap Journal

This document keeps the M1-M7 working notes that originally lived in
`capability-roadmap.md`. The roadmap stays shorter and easier to scan, while
the implementation history remains available.

## 2026-05-17 - M1 Started

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

## 2026-05-17 - M1 Completed

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

## 2026-05-17 - M2 Started

Decision:

- Build the first query and balance layer in the SDK on top of the M1 read-only book methods.
- Keep bridge protocol churn low until filtering or balances need native engine behavior for performance or backend parity.
- Preserve rational values for balances instead of converting accounting values to floating point.

Target:

- Account filtering by id, name, type, parent id, and commodity.
- Transaction filtering by account, posted date, number, description, currency, and split reconciled state.
- Account balance snapshots grouped by account.

## 2026-05-17 - M2 Completed

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

## 2026-05-17 - M3 Started

Decision:

- Start reports as structured SDK DTOs instead of GUI-style rendered reports.
- Build account summary and transaction report from the M1/M2 read model.
- Include CSV/JSON helpers so consumers can quickly move report data into files, pipelines, or tests.

## 2026-05-17 - M3 Completed

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

## 2026-05-17 - M4 Started

Decision:

- Begin with read-only reconciliation state summaries.
- Defer marking splits cleared or reconciled until the project has an explicit write path and book-save strategy.
- Treat GnuCash split states `n`, `c`, and `y` as unreconciled, cleared, and reconciled.

## 2026-05-17 - M4 Read-State Completed

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

## 2026-05-17 - M5 Started

Decision:

- Start imports with CSV transaction preview only.
- Validate row shape, dates, amounts, and target account existence.
- Do not write transactions into a book until the mutation/save path is designed.

## 2026-05-17 - M5 CSV Preview Completed

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

## 2026-05-17 - M6 Started

Decision:

- Start with read-only commodity/security and price database support.
- Parse GnuCash XML price entries through the bridge.
- Expose securities and price queries through the SDK before adding buy/sell/dividend helpers.

## 2026-05-17 - M6 Read Foundation Completed

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

## 2026-05-17 - M7 Native API Direction Accepted

Decision:

- Use the installed GnuCash native engine API for business objects and all write-capable operations.
- Keep the public SDK pointer-free and AnyCPU/x64 friendly.
- Keep native calls inside the packaged win-x86 bridge process so the wrapper works with the official Windows installer.
- Treat direct schema/XML mutation as out of scope for write support.

Discovery:

- The stock Windows installation exposes the required business and session symbols from `bin\libgnc-engine.dll`.
- Export checks found `qof_session_*`, `gncCustomer*`, `gncVendor*`, `gncEmployee*`, `gncJob*`, `gncInvoice*`, `gncEntry*`, `gncOwner*`, `gncTaxTable*`, `gncBillTerm*`, `xaccTrans*`, `xaccSplit*`, `xaccAccount*`, and `gnc_commodity*`.

Delivered:

- Added `ValidateNativeApi` as the next bridge protocol capability.
- Added a bridge-side native API probe that inspects `libgnc-engine.dll` for the required write/business API exports.
- Added a human-facing `validate-api` CLI command.
- Added tests for the new protocol value, CLI command behavior, and headless validation request.

Next work: continue through the native session spine, then start customer creation as the first disposable write workflow.

## 2026-05-17 - M7 Native Session Spine Delivered

Added `ValidateNativeSession`, `validate-session`, native session lifecycle P/Invokes, runtime bootstrap, file URI normalization, and regression tests. The published `win-x86` bridge opened a disposable book read-only through the stock Windows GnuCash install. Detailed journal: [M7 Native Session Spine](m7-native-session-spine.md).
