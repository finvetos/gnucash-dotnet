# M8 Native Capability Spine

Date: 2026-05-17

## Intent

Bundle the next four phases into one milestone: native read parity, native write foundation, SDK business objects, and import/reconciliation workflows.

This is intentionally larger than M7. The goal is to move from "the bridge can open GnuCash" to "a .NET developer can safely perform useful accounting workflows through the installed GnuCash runtime."

## Scope

### 1. Native Read Parity

- Read accounts, commodities, transactions, splits, prices, and book metadata through the native engine.
- Keep the XML reader as a regression oracle during this milestone, not as the long-term source of truth.
- Add parity checks that compare native counts and core fields against the XML bootstrap reader for fixture books.
- Keep native pointers fully bridge-owned.

### 2. Native Write Foundation

- Add writable native session lifecycle support using the reusable session handle pattern from M7.
- Create disposable write workflows first, then verify by reopening through native API.
- Start with low-risk business object creation, likely customer creation.
- Add transaction/split creation once session save and edit lifecycle are proven.

### 3. SDK Business Objects

- Expose .NET-friendly models and services for customers, vendors, employees, jobs, invoices, bills, entries, payments, tax tables, terms, accounts, commodities, and transactions.
- Keep protocol DTOs internal to the SDK boundary.
- Keep all native identity, pointer, and lifecycle details behind bridge contracts.
- Prefer explicit request/response records over generic object bags.

### 4. Import And Reconciliation

- Add bank statement import preview/apply flows.
- Add matching and duplicate-detection primitives.
- Add reconciliation status updates with native write verification.
- Keep workflow APIs ergonomic for .NET developers while preserving GnuCash accounting invariants.

## Definition Of Done

- The bridge can read core book data natively from an official Windows GnuCash install.
- The SDK can expose core read models without requiring callers to understand GnuCash internals.
- At least one business object write round-trips through native save/reopen verification.
- At least one import/reconciliation workflow has a preview path and a verified apply path.
- Full gate passes: build, tests, ArchUnitNET, Sentrux check, and Sentrux gate.
- Local native smoke passes against the stock installer without a custom GnuCash build.

## Working Order

1. Native account and commodity list parity. Completed 2026-05-17.
2. Native transaction, split, price, and metadata parity. Completed 2026-05-17.
3. Writable session save/reopen harness. Completed 2026-05-17.
4. First business object write. Completed 2026-05-17 with customer creation.
5. SDK business object surface over the proven bridge workflow.
6. Import preview/apply and reconciliation verification.

## Risks

- GLib collection traversal may require small bridge-owned helpers to stay safe.
- Some GnuCash business APIs may require exact edit/commit ordering to avoid silent no-op writes.
- Fixture books created by hand may trigger native XML warnings. Prefer GnuCash-authored fixtures for CI-grade native tests.
- Public SDK models may need iteration once real workflows expose awkward accounting details.

## Journal

### 2026-05-17 - Native Account And Commodity Parity

- Added bridge-owned native account traversal through `gnc_book_get_root_account`, `gnc_account_n_children`, and `gnc_account_nth_child`.
- Added native account field reads for GUID, name, type, parent, commodity, code, description, and placeholder status.
- Added native commodity table traversal through `gnc_commodity_table_foreach_commodity`.
- Added `validate-read-parity` as both a human CLI command and a headless protocol request.
- Added protocol status contracts and regression tests for CLI/headless failure paths.
- Moved the test install fixture out of `CliApplicationTests` to keep Sentrux file-length gates clean.

Native smoke against stock GnuCash 5.13 on Windows passed:

- `validate-api --json`: ready, callable from X86, no missing exports.
- `validate-read-parity --book-path D:\temp\gnucash-dotnet-native-smoke\sample.gnucash --json`: ready.
- Native/XML parity: 3 accounts, 2 commodities, no mismatches.

Observed GnuCash behavior:

- `gnc_commodity_table_foreach_commodity` enumerates built-in ISO currencies and the template commodity.
- `gnc_commodity_table_get_size` intentionally filters default commodities.
- The bridge reader now treats book commodities as referenced account currencies plus non-default custom commodities for this first read slice. Transaction and price parity should add their referenced commodities next.

Gate:

- `pwsh -File .\tools\check.ps1 -Configuration Debug -DisableGitVersion`
- Result: build passed, 56 tests passed, Sentrux check passed, Sentrux gate passed.

### 2026-05-17 - Native Core Read Parity

- Expanded native reads to cover book id, transactions, splits, prices, and core counts.
- Added account-tree transaction traversal through `xaccAccountTreeForEachTransaction`.
- Added split reads for account, memo, action, reconciled state, value, quantity, and reconcile date.
- Added price database traversal through `gnc_pricedb_foreach_price`.
- Added `gnc_numeric` marshaling for split and price amounts.
- Extended `validate-read-parity` status to report book, transaction, split, and price mismatches.

Native smoke against stock GnuCash 5.13 on Windows passed:

- Book id matched XML.
- Native/XML parity: 3 accounts, 2 commodities, 1 transaction, 2 splits, 1 price, no mismatches.

### 2026-05-17 - Writable Save/Reopen Harness

- Added writable session support to the native session handle.
- Added `validate-write-roundtrip` as both CLI and headless protocol request.
- The validator copies the source book, opens the copy writable, saves it, reopens read-only, and compares native summary counts.

Native smoke against stock GnuCash 5.13 on Windows passed:

- Before/after counts matched: 3 accounts, 2 commodities, 1 transaction.
- Backend error code was 0.

### 2026-05-17 - First Business Object Write

- Added `validate-customer-write` as both CLI and headless protocol request.
- Added QOF collection traversal for customer verification.
- Added native customer creation using `gncCustomerCreate`, `gncCustomerSetID`, `gncCustomerSetName`, and `gncCustomerSetCurrency`.
- The validator copies the source book, creates a customer in the copy, saves, reopens, and verifies the customer by id and name.

Native smoke against stock GnuCash 5.13 on Windows passed:

- Customer count changed from 0 to 1.
- The created customer GUID was returned.
- Reopen verification found the customer.

Gate:

- `pwsh -File .\tools\check.ps1 -Configuration Debug -DisableGitVersion`
- Result: build passed, 60 tests passed, Sentrux check passed, Sentrux gate passed.

Known stderr behavior:

- Stock GnuCash 5.13 still emits locale, DBD MySQL driver, and hand-authored XML parent warnings to stderr during native smoke tests.
- The bridge status contracts report success/failure independently from that native diagnostic chatter.
