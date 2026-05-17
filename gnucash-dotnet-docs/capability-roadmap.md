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

Status: native API path selected; validation foundation started.

Target:

- Customers, vendors, employees, jobs.
- Invoices and bills.
- Payments.
- Tax tables and terms.

Implementation decision:

- Business object reads and writes must use the native GnuCash engine API through the win-x86 bridge process.
- Direct XML or SQL access can remain read-only bootstrap support, but must not be used as the authoritative mutation path.
- See `architecture/decisions/ADR-0002-native-api-for-writes.md`.

### M8 - Native Capability Spine

Status: complete.

Target:

- Compare implemented behavior against the official GnuCash runtime APIs and UI workflows.
- Mark every known capability as supported, deferred, GUI-only, or not viable with the stock installer.
- Decide whether a native shim, Scheme invocation, or file/backend access is needed for each remaining area.

Delivered:

- Native read parity for book metadata, accounts, commodities, transactions, splits, and prices.
- Writable copied-book save/reopen validation.
- First native business-object write with customer creation and reopen verification.
- Detailed journal: [M8 Native Capability Spine](milestones/m8-native-capability-spine.md).

### M9 - SDK Business Import Reconciliation

Status: SDK preview complete; source writes deferred.

Target:

- Lift the first native business-object workflow into a public SDK surface.
- Add import duplicate/match analysis.
- Add reconciliation ending-balance preview and variance checks.

Detailed journal: [M9 SDK Business Import Reconciliation](milestones/m9-sdk-business-import-reconciliation.md).

### M10 - Native Workflow Completion

Status: in progress.

Target:

- Native SDK backend.
- Native transaction write foundation.
- Import apply.
- Reconciliation write.
- Business object expansion.

Detailed journal: [M10 Native Workflow Completion](milestones/m10-native-workflow-completion.md).

### M11 - Full Accounting Workflows

Status: in progress.

Target:

- Import apply.
- Reconciliation write.
- Business reads and full business workflows.
- Safer source-book writes.
- Native smoke automation.
- Investment and multi-currency workflows.
- Book lifecycle.
- Backend coverage.
- Reporting expansion.
- Additional import formats.

Detailed journal: [M11 Full Accounting Workflows](milestones/m11-full-accounting-workflows.md).

### M12 - Maximum Non-UI API Coverage

Status: in progress.

Target:

- Capability atlas for all non-UI GnuCash domains.
- Machine-readable coverage matrix.
- Coverage validation tests.
- Stock installer export inventory.
- Domain binding campaigns guided by coverage status.
- Real native fixtures for write smoke tests.
- Release readiness checks for coverage drift.

Detailed journal: [M12 Maximum Non-UI API Coverage](milestones/m12-maximum-non-ui-api-coverage.md).

Coverage atlas: [Non-UI Capability Atlas](capabilities/non-ui-capability-atlas.md).

## Journal

Early roadmap notes now live in
[milestones/early-roadmap-journal.md](milestones/early-roadmap-journal.md).
Dedicated milestone journals start at M7 and continue under `milestones/`.
