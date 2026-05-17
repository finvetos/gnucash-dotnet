# M9 SDK Business Import Reconciliation

Date: 2026-05-17

## Intent

Combine the next two phases after the native capability spine:

- Lift the first native business-object write into the public SDK.
- Add import and reconciliation workflow primitives that are safe for .NET developers to run before any book mutation.

The milestone keeps the project aligned with the stock Windows GnuCash installer. The SDK remains AnyCPU-friendly, while native writes stay inside the packaged win-x86 bridge process.

## Scope

### 1. SDK Business Object Surface

- Add .NET request/result models for customer creation.
- Hide bridge protocol DTOs from the SDK public surface.
- Expose the proven native customer workflow as a copied-book operation.
- Return verification fields that let callers know whether save/reopen validation succeeded.

### 2. Import Matching

- Keep CSV import preview read-only.
- Add duplicate/match analysis against transactions already in the opened book.
- Treat duplicate detection as a workflow gate before a future apply command.

### 3. Reconciliation Preview

- Compare expected statement ending balance with the current cleared plus reconciled account balance.
- Return variance and the underlying reconciliation summary.
- Keep reconciliation state writes deferred until transaction/split native mutation APIs are fully guarded.

## Definition Of Done

- SDK can call the native customer write validator through a public business-object method.
- Public business-object models do not expose bridge or protocol types.
- Import analysis can identify a duplicate statement row from existing transaction data.
- Reconciliation preview can report balanced and unbalanced statement ending balances.
- Regression tests cover all M9 public SDK surfaces.
- Full gate passes: build, tests, ArchUnitNET, Sentrux check, and Sentrux gate.

## Journal

### 2026-05-17 - Started

Decision:

- Keep the first customer SDK API honest by naming it `CreateCustomerInCopiedBookAsync`.
- Avoid pretending import apply is safe before native transaction creation, split balancing, and save/reopen verification are implemented.
- Model reconciliation preview before reconciliation writes so the SDK can validate statement balance workflows without changing a book.

### 2026-05-17 - SDK Surface And Preview Primitives Added

Delivered:

- Added `GnuCashCustomerCreateRequest` and `GnuCashCustomerCreateResult`.
- Added `GnuCashBook.CreateCustomerInCopiedBookAsync`.
- Added SDK mapping from `ValidateNativeCustomerWrite` protocol status into SDK business models.
- Added `GnuCashTransactionImportAnalysis` and `GnuCashTransactionImportMatch`.
- Added `GnuCashBook.AnalyzeCsvTransactionImportAsync` with account/date/amount/description duplicate matching.
- Added `GnuCashReconciliationPreview`.
- Added `GnuCashBook.PreviewReconciliationAsync` with expected balance, actual cleared/reconciled balance, and variance.
- Added regression tests for customer write status mapping, import duplicate analysis, and reconciliation preview.

Deferred:

- Real source-book customer mutation remains deferred until the public API has explicit backup and write-intent controls.
- CSV import apply remains deferred until native transaction/split creation has a verified save/reopen workflow.
- Marking splits cleared or reconciled remains deferred until native split-state write support is implemented.

Verification:

- `dotnet build .\GnuCash.DotNet.slnx --configuration Debug -p:HeurexUseGitVersion=false -m:1 -nr:false -p:UseSharedCompilation=false -v:minimal` passed.
- `dotnet test .\GnuCash.DotNet.slnx --no-build --configuration Debug -p:HeurexUseGitVersion=false -m:1 -nr:false -v:minimal` passed with 63 tests.
- `.\tools\sentrux\sentrux.exe check . --include-untracked` passed.
- `.\tools\sentrux\sentrux.exe gate .` passed.

Local note:

- `tools/check.ps1` hit an MSBuild parallel intermediate-directory access issue on this machine. The equivalent single-node build/test commands and Sentrux gates passed.
