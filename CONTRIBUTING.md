# Contributing to SqliteMultiTenant

Thanks for your interest in contributing! This guide covers the basics.

## Prerequisites

- .NET SDK 10.0 (`global.json` pins `10.0.100` with `rollForward: latestMinor`; projects target `net10.0`)
- Git

## Build and test

```bash
dotnet restore
dotnet build SqliteMultiTenant.sln -c Release
dotnet test tests/sqlite-multi-tenant.Tests -c Release
```

Some tests are known to fail on `main`. Run the tests relevant to your change before and after it, and make sure the failing count does not grow.

## Coding conventions

- **Naming:** PascalCase for types and members, `_camelCase` for private fields, `I` prefix for interfaces, `Async` suffix for `Task`-returning methods, `CancellationToken cancellationToken = default` as the last parameter.
- **Nullable reference types** are enabled. Every `.cs` file starts with `#nullable enable`.
- **Warnings:** the build is not configured with `TreatWarningsAsErrors`, so warnings do not fail the build. Do not add new errors, and avoid adding new warnings.
- **Public APIs** get XML doc comments (`<summary>`).
- **Formatting:** `.editorconfig` is the source of truth (4-space indent, Allman braces, `var` preferred). Run `dotnet format src/SqliteMultiTenant.csproj` before committing. Follow the style of the file you are editing rather than reformatting unrelated code.
- See [CLAUDE.md](CLAUDE.md) for the full set of architecture and layout conventions.

## Branches and commits

- Branch names: `feature/<short-name>`, `fix/<short-name>`, or `docs/<short-name>`, branched from `main`.
- Commit messages use conventional prefixes: `feat:`, `fix:`, `docs:`, `test:`, `refactor:`, `chore:`.
- Write the subject in the imperative mood, lowercase after the prefix, for example `fix: handle empty tenant id in backup service`.

## Pull requests

1. For large or design-changing work, **open an issue first** to discuss the approach.
2. Keep each PR focused on a single concern. Split unrelated changes into separate PRs.
3. **Add or update tests** for any behavior change, following the naming and layout in the test project.
4. Make sure the build and tests pass locally. **CI must pass** (`.github/workflows/ci.yml`: restore, build, test in Release).
5. Fill in the PR template, and update documentation (`README.md`, `docs/`) when public behavior changes.

Report bugs and request features through GitHub Issues.
