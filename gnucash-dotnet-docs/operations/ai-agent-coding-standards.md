# AI Agent Coding Standards

This repo uses Heurex defaults for AI-assisted coding.

## Loop

1. Inspect `.sentrux/rules.toml`.
2. Inspect `test/**/ArchitectureRulesTests.cs`.
3. Make the smallest coherent change.
4. Run `tools/check.ps1`.
5. Run `tools/release.ps1` only for release work.

## Sentrux

- Prefer `tools/sentrux/sentrux.exe`.
- Build it with `tools/build-sentrux.ps1` when missing.
- Use the custom build with `--include-untracked` so new files are scanned before commit.
- Save a new baseline only after the operator accepts the changed architecture.

## ArchUnitNET

- Boundary rules belong in `ArchitectureRulesTests.cs`.
- Keep rules simple and readable.
- Add or update a rule when a new architectural boundary is introduced.
- Do not remove a failing rule to make tests pass; fix the code or get operator approval.
- Include smell rules for test-tooling leakage, layer inversions, namespace drift, and other exact boundaries when the rule is clear.
- Let Sentrux own broad graph smells such as cycles unless a local ArchUnitNET rule is clearer.

## Test Stack

- xUnit v3 is the default test engine.
- Use FluentAssertions 7.x for readable assertions.
- Use AutoFixture and AutoFixture.Xunit3 for generated test data.
- Use NSubstitute and AutoFixture.AutoNSubstitute for mocks.
- Use xunit.analyzers as a default analyzer gate.
- Use `Xunit.v3.Priority` only through the `--withTestOrdering true` template switch, and only when order is part of an integration behavior.
- Do not add `Dangl.Xunit.Extensions.Ordering` to xUnit v3 projects by default; the available package pulls in xUnit v2.

## Code Shape

- Keep command parsing, rendering, and host composition separate.
- Use Microsoft DI, options, and logging as the default host stack.
- Configure Serilog at the executable boundary.
- Avoid cycles, god files, broad helper buckets, and hidden test dependencies.

## Release

Read `releases/release.run.yml` before release work. Use `tools/release.ps1` for the release gate.
