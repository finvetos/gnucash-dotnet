# M12 Maximum Non-UI API Coverage

Date: 2026-05-17

## Intent

Turn the project from feature-by-feature growth into a deliberate coverage program for the GnuCash accounting engine surface, excluding Windows desktop UI and GUI-only behavior.

The target is maximum practical API coverage while preserving the public design rule:

- Raw GnuCash native calls stay inside the win-x86 bridge.
- .NET developers get typed, safe SDK models and workflow APIs.
- Stock Windows GnuCash installs remain the runtime dependency.
- Source-book writes stay blocked until intent, backup, and save/reopen verification are in place.

## Scope

### 1. Capability Atlas

- Maintain a domain map of all non-UI GnuCash capability areas.
- Track each capability by stable id, status, bridge surface, SDK surface, verification evidence, and notes.
- Keep GUI-only and unsafe internal areas classified instead of invisible.

### 2. Capability Classification

Use these statuses:

- `sdk-supported`
- `bridge-supported`
- `read-only`
- `write-supported`
- `import-export`
- `deferred`
- `not-viable-with-stock-install`
- `excluded-ui`
- `not-assessed`

Every status change should be accompanied by a milestone journal note or implementation evidence.

### 3. Stock Installer Export Inventory

- Extend native inspection beyond required exports.
- Inventory useful exports from the official installer runtime.
- Compare discovered exports against the capability atlas and source/header research.
- Keep raw exported symbols out of the public SDK.

### 4. Domain Binding Campaigns

Add bridge-native bindings by domain:

- Book/session/backend.
- Accounts.
- Commodities, currencies, and securities.
- Transactions, splits, lots, and prices.
- Business objects.
- Reconciliation.
- Budgets and scheduled transactions.
- Imports and backend-specific paths.

Each binding group must include a capability atlas update before being lifted into the SDK.

### 5. Clean SDK Surface

- Expose workflow-level SDK APIs rather than C-style function mirrors.
- Use typed request/result models.
- Preserve process isolation from the 32-bit GnuCash runtime.
- Keep bridge/protocol DTOs hidden from consumers.

### 6. Real Native Fixtures

- Add disposable books created by GnuCash itself or copied from known-valid real fixtures.
- Use these fixtures for write smoke tests.
- Retire reliance on hand-authored XML fixtures for native mutation verification.

### 7. Coverage Gate

- Add a machine-readable coverage matrix.
- Add tests that validate matrix shape, status names, unique ids, and evidence for supported/write statuses.
- Make coverage updates part of release readiness.

## Definition Of Done

- `capabilities/non-ui-capability-atlas.md` exists and describes the coverage model.
- `capabilities/capability-coverage.json` exists and is validated by tests.
- All current SDK and bridge capabilities are represented in the matrix.
- A native export inventory command or script can list stock installer exports by library.
- New supported/write-supported capabilities require matrix evidence.
- The atlas identifies what is excluded as UI-only, unsafe/internal, or not viable with the stock installer.
- Native write smoke tests use real writable fixtures.
- Full gate passes: build, tests, ArchUnitNET, Sentrux check, Sentrux gate, package smoke, and optional native smoke when prerequisites exist.

## Working Order

1. Create the capability atlas and matrix.
2. Add a matrix validation test.
3. Add an export inventory command or script.
4. Add real native fixture creation or fixture acquisition.
5. Backfill current implemented capabilities into the matrix.
6. Wire coverage checks into release readiness docs.
7. Use the matrix to guide M11 workflow implementation and future milestone planning.

## Risks

- GnuCash exports include internal functions that should not become public SDK commitments.
- Some engine behavior may be reachable only through Scheme, import modules, or backend-specific paths.
- Write coverage can look complete while missing accounting invariants; save/reopen verification remains mandatory.
- Capability matrix churn can become paperwork unless it is tied to tests and release gates.

## Journal

### 2026-05-17 - Planned

Decision:

- Treat maximum non-UI API coverage as M12.
- Keep M11 as the workflow delivery milestone.
- Use M12 to map, measure, and gate coverage while M11 continues adding SDK capabilities.

### 2026-05-17 - Atlas And Gate Started

Implemented:

- Added `capabilities/non-ui-capability-atlas.md`.
- Added `capabilities/capability-coverage.json` as the first machine-readable matrix.
- Added architecture tests that validate capability ids, known statuses, verification evidence, write-support evidence, and bridge protocol command representation.
- Updated the roadmap, docs index, and quality gate documentation.

Verification:

- `dotnet build .\GnuCash.DotNet.slnx --configuration Debug -p:HeurexUseGitVersion=false -m:1 -nr:false -p:UseSharedCompilation=false -v:minimal` passed.
- `dotnet test .\GnuCash.DotNet.slnx --no-build --configuration Debug -p:HeurexUseGitVersion=false -m:1 -nr:false -v:minimal` passed with 74 tests.
- `.\tools\sentrux\sentrux.exe check . --include-untracked` passed.
- `.\tools\sentrux\sentrux.exe gate .` passed.

Next:

- Add the stock installer export inventory command or script.
- Add real native writable fixtures so write smoke tests no longer depend on hand-authored XML.
