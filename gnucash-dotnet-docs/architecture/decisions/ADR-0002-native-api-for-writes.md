# ADR-0002: Use The Native GnuCash API For Writes

Date: 2026-05-17

## Status

Accepted.

## Context

The wrapper must work with the official Windows GnuCash installer. Users should not need a forked or custom GnuCash build.

Early milestones used a bridge-side XML reader to prove the .NET SDK shape for read-only book operations. That bootstrap reader is useful for simple inspection, but it is not an acceptable authority for business objects, reconciliation changes, imports, or any operation that mutates accounting data.

The Python ecosystem shows two practical patterns:

- The official GnuCash Python bindings use the GnuCash engine API through SWIG.
- `piecash` maps the SQL schema directly.

Direct schema writes are attractive for implementation speed, but they make this project depend on storage details instead of GnuCash accounting invariants. Schema drift, KVP semantics, generated IDs, counters, invoice posting logic, lots, tax tables, and payment application could break silently.

## Decision

All write-capable features must go through the installed GnuCash runtime API.

The x64/AnyCPU .NET SDK continues to talk to the packaged win-x86 bridge process. The bridge owns native integration with the official GnuCash installation and exposes stable DTOs over the bridge protocol.

Direct XML or SQL parsing may remain as a bootstrap/read-only optimization, but it must not be the authoritative write path.

## Current Discovery

The stock Windows install at `C:\Program Files (x86)\gnucash` exposes the required engine and business API symbols from `bin\libgnc-engine.dll`, including:

- `qof_session_new`, `qof_session_begin`, `qof_session_load`, `qof_session_save`, `qof_session_end`
- `gncCustomer*`, `gncVendor*`, `gncEmployee*`, `gncJob*`
- `gncInvoice*`, `gncEntry*`, `gncOwner*`
- `gncTaxTable*`, `gncBillTerm*`
- account, transaction, split, commodity, and book APIs such as `xaccTrans*`, `xaccSplit*`, `xaccAccount*`, and `gnc_commodity*`

The bridge now has a native API validation probe so CI and local diagnostics can detect whether a runtime exposes the expected symbol set before write-capable features run.

The bridge also has a native session validation command that has successfully opened a disposable book read-only against the stock Windows install. The working bootstrap sequence is:

1. Configure process-local DLL and GnuCash environment paths for the official install.
2. Point GnuCash binreloc at the installed prefix for this bridge process.
3. Run GnuCash environment setup.
4. Initialize the GnuCash module system.
5. Initialize the GnuCash engine.
6. Normalize the book path to a `file:` URI.
7. Open the session with `SESSION_READ_ONLY`.

## Consequences

- Business objects will be wrapped as API workflows, not as table/XML projections.
- The public .NET SDK must expose stable business DTOs and command-style operations, not native pointers.
- The bridge protocol must carry enough data for deterministic tests and friendly errors.
- Regression tests for writes must use disposable/copy books and verify results by reopening through the GnuCash API.
- The XML reader remains read-only until native read parity replaces it.
- A native shim remains optional. It should be added only if direct P/Invoke becomes too fragile or pointer-heavy inside managed bridge code.

## Near-Term Implementation Order

1. Validate the installed native API surface.
2. Promote the bridge-owned native session abstraction into reusable open/load/save/close primitives.
3. Wrap read-only native account, commodity, transaction, split, and business object queries.
4. Add customer/vendor/employee/job creation workflows.
5. Add invoice and bill workflows, including entries, posting, and payments.
6. Reconcile native read results with the existing XML bootstrap reader, then move authoritative reads to the native path.
