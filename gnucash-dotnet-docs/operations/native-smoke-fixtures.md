# Native Smoke Fixtures

Native write smoke tests must use books created by GnuCash itself or real disposable books whose licensing and provenance are clear. Hand-authored XML fixtures are useful for parser and SDK regression tests, but they are not enough for native mutation smokes because the GnuCash engine expects full book metadata and backend invariants.

## Local Smoke Script

Use:

```powershell
.\tools\native-smoke.ps1
```

That runs install validation, required API validation, and export inventory against the stock installer. To run book and write checks, pass a real disposable book:

```powershell
.\tools\native-smoke.ps1 -BookPath "D:\temp\gnucash-smoke\sample.gnucash"
```

The script copies source books before native write checks. It writes disposable outputs under `artifacts/native-smoke` by default.

## Fixture Rules

- Do not use private personal finance files as committed fixtures.
- Prefer fixtures created from an empty GnuCash profile with synthetic accounts and transactions.
- Keep committed fixtures small and license-cleared.
- For source-book write features, verify against a copied book first.
- Every write smoke should save, reopen, and verify accounting state through the bridge.

## Next Fixture Work

The project still needs an automated way to create a valid synthetic book through the native engine. Until then, local write smokes should use a manually created disposable GnuCash book supplied through `-BookPath`.
