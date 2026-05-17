# M11 Full Accounting Workflows

Date: 2026-05-17

## Intent

Bundle the next twelve capability areas into one large milestone after the M10 native workflow foundation:

- Import apply.
- Reconciliation write.
- More business reads.
- Safer source-book writes.
- Native smoke automation.
- Investment writes.
- Multi-currency workflows.
- Full business workflow.
- Book lifecycle.
- Backend coverage.
- Reporting expansion.
- Import formats.

The goal is to turn the SDK from a native-capable wrapper into a broad accounting workflow toolkit that remains compatible with the official Windows GnuCash installer.

## Scope

### 1. Import Apply

- Apply CSV preview rows into copied books through the native transaction write spine.
- Return created transaction ids, skipped duplicate rows, invalid rows, and diagnostics.
- Support batch apply with all-or-nothing behavior for copied books before source-book writes.

### 2. Reconciliation Write

- Mark selected splits cleared or reconciled through native APIs.
- Save, reopen, and verify statement ending balance.
- Model reconciliation sessions with statement date, expected ending balance, selected splits, variance, and result diagnostics.

### 3. More Business Reads

- Add native list/read APIs for vendors, invoices, bills, employees, jobs, tax tables, and terms.
- Prefer read/list surfaces before mutation surfaces for each object.
- Keep SDK business models explicit and typed.

### 4. Safer Source-Book Writes

- Add explicit write-intent controls.
- Require backup creation or caller-supplied backup strategy before source-book mutation.
- Return pre/post verification snapshots for every source-book write.

### 5. Native Smoke Automation

- Turn manual installed-GnuCash smoke commands into repeatable scripts or optional integration tests.
- Keep normal CI green without GnuCash installed.
- Add clear skip/report behavior when native smoke prerequisites are absent.

### 6. Investment Writes

- Add buy/sell share helpers.
- Add dividend workflows.
- Add stock split support where feasible.
- Add price writes and price verification.
- Explore lots/cost-basis APIs and mark unsupported pieces explicitly.

### 7. Multi-Currency Workflows

- Add exchange-rate helpers.
- Add currency conversion transactions.
- Add realized/unrealized gain support where native APIs make it safe.
- Expand reports for multi-currency balances and valuations.

### 8. Full Business Workflow

- Add invoice, bill, entry, payment, posting, AR/AP, tax table, terms, job, and employee expense flows.
- Verify each write with save/reopen tests.
- Preserve owner relationships with typed SDK models.

### 9. Book Lifecycle

- Create new books.
- Create account trees.
- Add commodities and securities.
- Backup and restore books.
- Validate book structure and backend type.
- Surface migration/version compatibility diagnostics.

### 10. Backend Coverage

- Validate behavior against XML and compressed XML.
- Validate SQLite-backed books through the stock runtime.
- Investigate database-backed books and classify them as supported, deferred, or not viable.
- Add backend-specific regression fixtures where licensing and size allow.

### 11. Reporting Expansion

- Add trial balance, balance sheet, income statement, cash flow, and portfolio reports.
- Export report DTOs to JSON and CSV first.
- Add HTML/PDF only when a stable renderer path is available.

### 12. Import Formats

- Add OFX/QFX preview and apply where supported by the installed runtime.
- Add QIF preview and apply.
- Add CSV price imports.
- Add MT940 only if the stock runtime exposes a reliable path.
- Add import profiles/templates for repeatable statement layouts.

## Definition Of Done

- CSV import apply and reconciliation write are supported at least for copied books.
- Source-book writes have explicit intent, backup, and verification controls.
- Native smoke automation covers core reads, transaction writes, import apply, reconciliation, and at least one business workflow.
- Investment and multi-currency helpers cover common personal-finance workflows.
- Business workflows include invoices, bills, payments, AR/AP posting, tax tables, terms, and jobs.
- Book lifecycle APIs can create and validate books, accounts, commodities, and securities.
- Backend support is documented and regression-tested for every supported backend.
- Expanded reports expose structured DTOs and JSON/CSV export.
- Import formats beyond CSV are classified and at least OFX/QFX or QIF has a preview path.
- Full gate passes: build, tests, ArchUnitNET, Sentrux check, Sentrux gate, package smoke, and optional native smoke when prerequisites exist.

## Working Order

1. CSV copied-book apply over the native transaction writer.
2. Reconciliation copied-book split-state write.
3. Native smoke automation scripts.
4. Source-book write-intent and backup controls.
5. Vendor and invoice reads.
6. Invoice/bill/payment write validators.
7. Book lifecycle creation and validation.
8. Investment and multi-currency writes.
9. Backend coverage expansion.
10. Report expansion.
11. OFX/QFX and QIF import previews.
12. Source-book apply promotion once copied-book flows are proven.

## Risks

- This milestone is intentionally broad; implementation should still land in small commits with independent verification.
- Source-book writes must not be enabled without backup and reopen verification.
- Investment and business APIs may depend on GnuCash invariants that are not obvious from exported symbols alone.
- Backend support may vary by stock installer configuration and installed database drivers.
- Import duplicate detection must be conservative to avoid silently creating accounting duplicates.

## Journal

### 2026-05-17 - Planned

Decision:

- Treat the next twelve capability areas as one umbrella milestone, M11.
- Keep M10 focused as the native workflow foundation and continue using it as the base for M11.
- Execute M11 in small verified slices rather than attempting a single large merge.

### 2026-05-17 - Slice 1 CSV Apply Started

Implemented:

- Added a native copied-book batch transaction write protocol command.
- Added SDK batch transaction create models and `CreateTransactionsInCopiedBookAsync`.
- Added `ApplyCsvTransactionImportToCopiedBookAsync` to turn valid, non-duplicate CSV preview rows into a copied-book native batch write.
- CSV apply now reports skipped invalid rows, skipped duplicates, native batch diagnostics, and created transaction ids when verification succeeds.

Safety notes:

- Source books are still untouched.
- Duplicate rows are skipped by default; callers can choose to block the apply if duplicates are present.
- The native batch copies the source book once, creates all requested transactions, saves once, and verifies the created transaction ids after reopen.

Verification:

- `dotnet build .\GnuCash.DotNet.slnx --configuration Debug -p:HeurexUseGitVersion=false -m:1 -nr:false -p:UseSharedCompilation=false -v:minimal` passed.
- `dotnet test .\GnuCash.DotNet.slnx --no-build --configuration Debug -p:HeurexUseGitVersion=false -m:1 -nr:false -v:minimal` passed with 70 tests.
- `.\tools\sentrux\sentrux.exe check . --include-untracked` passed.
- `.\tools\sentrux\sentrux.exe gate .` passed.
- Published the current bridge to `releases\publish\bridge-win-x86` for local smoke use.
- `validate-api --json` passed against stock GnuCash 5.13 from the win-x86 bridge with no missing exports, including `xaccTransSetNum`, `xaccSplitSetMemo`, and `xaccSplitSetAction`.

Observed:

- The tiny hand-authored XML unit-test fixture is still useful for SDK/bridge regression tests, but GnuCash rejects it for native writable smoke because it is not a fully valid user book. Native write smokes need either a real disposable book fixture or a book-lifecycle command that creates one through the engine.
