# M10 Native Workflow Completion

Date: 2026-05-17

## Intent

Bundle the next five planned phases into one larger M10 milestone:

- Native SDK backend.
- Native transaction write foundation.
- Import apply.
- Reconciliation write.
- Business object expansion.

The point is to move the project from useful previews and copied-book validators to a coherent write-capable SDK that still works with the stock Windows GnuCash installer.

## Scope

### 1. Native SDK Backend

- Move the normal SDK read path toward the native engine instead of the XML bootstrap reader.
- Keep XML as a regression oracle during the transition.
- Preserve AnyCPU SDK behavior by routing native work through the packaged win-x86 bridge.
- Expand parity checks to realistic user-book shapes where possible.

### 2. Transaction Write Foundation

- Add native copied-book transaction creation.
- Create balanced transactions with splits, accounts, commodity/currency, posted date, description, number, and memo/action fields.
- Save and reopen the copied book through the native runtime.
- Verify transaction, split, balance, and reconciliation-state values after reopen.

### 3. Import Apply

- Turn CSV preview and duplicate analysis into a copied-book apply workflow.
- Add explicit apply results with created transaction ids, skipped duplicate rows, invalid rows, and diagnostics.
- Add source-book apply only after backup/write-intent controls are explicit and tested.
- Keep OFX/QFX/QIF deferred unless the transaction write spine is stable early.

### 4. Reconciliation Write

- Add native split-state updates for cleared and reconciled workflows.
- Model a reconciliation session with statement date, expected ending balance, selected splits, variance, and save/reopen verification.
- Block reconciliation commits when the variance is non-zero unless the caller explicitly chooses a supported exception path.

### 5. Business Object Expansion

- Add customer list/read support after the first customer write.
- Add vendors, employees, jobs, invoices, bills, entries, payments, tax tables, and terms in small bridge commands.
- Prefer read/list surfaces before write surfaces for each business object.
- Keep copied-book write validators until source-book writes have backup and intent controls.

## Definition Of Done

- SDK reads can be backed by the native engine for core book data without exposing native or protocol DTOs.
- Copied-book transaction writes pass save/reopen verification.
- CSV import apply creates balanced transactions and reports duplicates/skips.
- Reconciliation write can mark selected splits and verify ending balance after reopen.
- At least customers, vendors, and invoices have read/list SDK surfaces; one additional business object write is verified.
- Full gate passes: build, tests, ArchUnitNET, Sentrux check, and Sentrux gate.
- Local native smoke passes against the stock GnuCash installer without a custom GnuCash build.

## Working Order

1. Add native-backed bridge commands for SDK read replacement and parity.
2. Add transaction/split copied-book write validator.
3. Lift transaction write into SDK request/result models.
4. Add CSV import apply over the transaction write surface.
5. Add reconciliation split-state write validator and SDK preview/apply flow.
6. Add customer read/list, then vendor and invoice read/list.
7. Add the next safest business-object copied-book write.

## Risks

- Native engine write order may require exact begin-edit, set-field, commit-edit sequencing.
- Split balancing must preserve GnuCash rational values and commodity fractions exactly.
- Import apply can create accounting damage if duplicate detection is too optimistic.
- Reconciliation writes need clear invariants because changing split state is easy to do and hard to notice later.
- Business object APIs may expose owner, lot, tax, and terms relationships that deserve explicit domain models rather than generic bags.

## Journal

### 2026-05-17 - Planned

Decision:

- Treat the proposed M10 through M14 work as one larger M10 milestone.
- Keep the milestone internally sliced so implementation can proceed safely without losing the full workflow goal.
- Continue using copied-book write validators before enabling source-book writes.

### 2026-05-17 - Native Read Routing And Transaction Spine

Delivered:

- Added explicit SDK read modes: XML, native, and native-then-XML fallback.
- Added protocol support for selecting the book read backend per request.
- Routed SDK book reads through the bridge-native `GnuCashNativeBookReader` when requested.
- Kept XML fallback available so non-native developer and CI environments remain usable.
- Added copied-book native transaction creation request/result models in the SDK.
- Added a native transaction write validator that creates balanced transactions in a copied book, saves, reopens, and verifies the transaction id.
- Added headless bridge protocol support for native transaction write validation.
- Added native customer list/read support as the first business-object read expansion.

Deferred within M10:

- CSV import apply should use the transaction write spine next, preferably as a batch copied-book operation.
- Reconciliation write should use a split-state native validator after transaction write has native smoke coverage.
- Vendor, invoice, and additional business-object reads remain the next business expansion slices.

Verification:

- `dotnet build .\GnuCash.DotNet.slnx --configuration Debug -p:HeurexUseGitVersion=false -m:1 -nr:false -p:UseSharedCompilation=false -v:minimal` passed.
- `dotnet test .\GnuCash.DotNet.slnx --no-build --configuration Debug -p:HeurexUseGitVersion=false -m:1 -nr:false -v:minimal` passed with 68 tests.
- `.\tools\sentrux\sentrux.exe check . --include-untracked` passed.
- `.\tools\sentrux\sentrux.exe gate .` passed.
- Published the updated bridge to `releases\publish\bridge-win-x86`.
- Native smoke against stock GnuCash 5.13 passed for `ValidateNativeTransactionWrite`: before transaction count 1, after transaction count 2, found after reopen.
- Native smoke against stock GnuCash 5.13 passed for native `OpenBook`: file format `GnuCashNative`, 3 accounts, 2 commodities, 1 transaction, 2 splits, 1 price.

Observed GnuCash behavior:

- The stock runtime still emits locale, missing MySQL DBD driver, and hand-authored XML parent diagnostics to stderr during smoke tests. The protocol status returned success independently from that diagnostic chatter.
