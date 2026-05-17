# M7 Native Session Spine

Date: 2026-05-17

## Delivered

- Added `ValidateNativeSession` as a bridge protocol capability.
- Added the human-facing `validate-session` CLI command.
- Added bridge-owned P/Invoke bindings for GnuCash session lifecycle calls.
- Promoted native read-only session ownership into a reusable disposable handle.
- Added runtime bootstrap for official Windows installs:
  - DLL search path points at the installed `bin` directory.
  - GnuCash binreloc is pointed at the installed prefix for this bridge process.
  - GnuCash environment setup runs before module and engine initialization.
  - GnuCash module system and engine initialization run in the packaged `win-x86` bridge.
- Added file URI normalization through `gnc_uri_normalize_uri` before `qof_session_begin`.
- Added native summary counts for root-inclusive accounts, commodities, and transactions.
- Extended native API validation to include `libgnc-module.dll` and `libgnc-core-utils.dll`.
- Added CLI and headless regression coverage for native session validation failure paths.

## Local Smoke

- Published the bridge as `win-x86`.
- Ran `validate-session` against the stock install at `C:\Program Files (x86)\gnucash`.
- Opened a disposable sample book read-only through the native engine.
- Result: ready, process architecture `X86`, root account found, account count `3`, commodity count `2`, transaction count `1`, backend error code `0`.

## Verification

- `pwsh -File .\tools\check.ps1 -Configuration Debug -DisableGitVersion` passed.
- Test count after this milestone: 54 passing tests.
- Sentrux check passed.
- Sentrux gate passed.

## Notes

- Native GnuCash and supporting libraries can still write diagnostic messages to stderr. The bridge protocol keeps JSON responses on stdout.
- The local sample book opened successfully, but native XML validation emitted warnings about fixture account parent handling. Use a cleaner GnuCash-authored fixture before making native session smoke part of CI.

## Next Work

- Execute [M8 Native Capability Spine](m8-native-capability-spine.md), bundling native read parity, native write foundation, SDK business objects, and import/reconciliation workflows.
