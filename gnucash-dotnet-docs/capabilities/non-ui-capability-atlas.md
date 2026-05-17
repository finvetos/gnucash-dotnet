# Non-UI Capability Atlas

This atlas is the working control surface for maximum GnuCash engine coverage. It excludes desktop-window behavior and GUI-only workflows, but it does not hide them; excluded areas should still be classified so the project can explain its boundaries.

Machine-readable status lives in [capability-coverage.json](capability-coverage.json). Update both files when a capability meaningfully changes.

## Coverage Statuses

| Status | Meaning |
| --- | --- |
| `sdk-supported` | Stable public .NET API exists. |
| `bridge-supported` | Bridge protocol or CLI can perform the operation, but the SDK surface is not polished yet. |
| `read-only` | Safe inspection/query support exists or is planned before mutation. |
| `write-supported` | Mutation is supported with accounting invariants and save/reopen verification. |
| `import-export` | Capability is handled through import/export flows rather than direct object mutation. |
| `deferred` | Valuable but intentionally later. |
| `not-viable-with-stock-install` | Blocked by the official installer/runtime surface. |
| `excluded-ui` | Belongs to GUI behavior and is outside the wrapper goal. |
| `not-assessed` | Known area that still needs source/runtime research. |

## Domain Map

| Domain | Current Shape | Maximum Coverage Target |
| --- | --- | --- |
| Installation and bridge | Installer discovery, API validation, x86 bridge isolation. | Export inventory, runtime diagnostics, and clear compatibility reporting. |
| Book/session/backend | Open/read existing XML/native books; copied-book write validation. | Create, save, backup, restore, validate, and classify every stock backend path. |
| Accounts | Read/list/query/balances. | Create/update/delete accounts with hierarchy and commodity validation. |
| Commodities and prices | Read currencies, securities, latest prices. | Create commodities/securities and write verified prices. |
| Transactions and splits | Read/filter plus copied-book transaction write and batch foundation. | Full split editing, transfer helpers, delete/void support, lots, and verified source-book writes. |
| Reconciliation | Read state and preview ending balance. | Mark cleared/reconciled splits with statement sessions and reopen verification. |
| Imports | CSV preview, duplicate analysis, copied-book apply foundation. | OFX/QFX, QIF, CSV prices, duplicate strategy, apply paths, and import profiles. |
| Reports | Structured account/transaction/trial/income/balance/cash-flow reports. | Portfolio, multi-currency, JSON/CSV exports, and stable renderer decisions. |
| Business objects | Customer create/list foundation. | Vendors, employees, jobs, invoices, bills, entries, posting, payments, terms, tax tables. |
| Investments and currency | Securities and prices are readable. | Buy/sell, dividends, splits, price writes, currency conversions, gain/loss classification. |
| Scheduled and budgets | Not yet assessed. | Read/write where native APIs are stable; otherwise classify. |
| Slots, metadata, preferences | Not yet assessed. | Typed slot access for safe domains and explicit exclusions for GUI preferences. |

## Coverage Rules

- Every public SDK method should have a matrix entry.
- Every bridge protocol command should have at least one matrix entry.
- Every `write-supported` entry must name save/reopen verification evidence.
- Every `sdk-supported` or `bridge-supported` entry must name the API, protocol command, CLI command, test, or doc evidence.
- Every `not-viable-with-stock-install` entry must name the blocking runtime fact.
- Every `excluded-ui` entry must explain why it belongs to UI behavior instead of engine workflows.

## Release Use

Before a release:

1. Run the capability coverage tests.
2. Review all `not-assessed` entries touched by the release.
3. Confirm new SDK or bridge APIs appear in the matrix.
4. Confirm source-book write entries include backup, intent, and verification notes.
5. Include meaningful coverage changes in the milestone journal.
