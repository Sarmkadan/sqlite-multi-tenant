# CLAUDE.md

## Project overview

SqliteMultiTenant - a .NET 10 class library (NuGet package `SqliteMultiTenant`) providing per-tenant SQLite database isolation, migrations, backups, connection pooling and a demo ASP.NET/console host.

## Build

Requires .NET SDK 10.0 (`global.json` pins 10.0.100, rollForward latestMinor).

```bash
dotnet restore
dotnet build SqliteMultiTenant.sln -c Release          # whole solution (lib + tests + benchmarks)
dotnet build src/SqliteMultiTenant.csproj -c Release   # library only (same as `make build`)
dotnet pack src/SqliteMultiTenant.csproj -c Release -o ./nupkg
```

Build currently produces ~1200 warnings and 0 errors; `TreatWarningsAsErrors=false` in `Directory.Build.props`. Do not introduce new errors; reducing warnings is welcome but not required.

## Test

```bash
dotnet test tests/sqlite-multi-tenant.Tests -c Release
dotnet test tests/sqlite-multi-tenant.Tests -c Release --filter "FullyQualifiedName~TenantServiceTests"
```

- Framework: xUnit 2.9, FluentAssertions 7, NSubstitute 5.3.
- Single test project: `tests/sqlite-multi-tenant.Tests/` (flat layout, plus `Operations/`, `Security/`, `Utilities/`, `Validation/` subfolders).
- File naming: `<ClassUnderTest>Tests.cs`; integration tests use `<Class>IntegrationTests.cs` and hit real temp SQLite files.
- Method naming: `Method_Scenario_ExpectedResult` (e.g. `GetTenantAsync_WithBlankId_ThrowsArgumentException`), Arrange/Act/Assert comments.
- Test classes are `public sealed`, dependencies mocked with `Substitute.For<T>()` in the constructor.
- Known state: not all tests pass (as of 2026-09: ~174 failing of ~1692). Run the relevant subset before and after a change and do not make the failing count grow. `make test` swallows failures with `|| true`; prefer plain `dotnet test`.
- CI (`.github/workflows/ci.yml`, `build.yml`): `dotnet restore && dotnet build -c Release && dotnet test -c Release`.

## Lint / Format

```bash
dotnet format src/SqliteMultiTenant.csproj                                   # apply .editorconfig
dotnet build src/SqliteMultiTenant.csproj -c Release /p:EnforceCodeStyleInBuild=true
```

`.editorconfig` is the source of truth: 4-space indent, UTF-8, final newline, trim trailing whitespace, Allman braces (`csharp_new_line_before_open_brace = all`), `var` preferred everywhere. Note that many existing files (especially tests) use K&R braces; follow the style of the file you are editing rather than reformatting whole files.

## Architecture

- `src/SqliteMultiTenant.csproj` - the library. Root namespace `SqliteMultiTenant`, `GenerateDocumentationFile=true`, `Nullable` and `ImplicitUsings` enabled.
- `src/GlobalUsings.cs` - global usings for ASP.NET MVC, Logging, Configuration, Hosting, MemoryCache.
- `src/Program.cs` - demo entry point; `--web` or `ASPNETCORE_ENVIRONMENT` switches to a WebApplication host (used by Docker health checks), otherwise runs a console demo.
- `src/Configuration/` - `MultiTenantOptions`, `OptionsValidator`, and the DI entry point `ServiceConfiguration.AddSqliteMultiTenant(connectionString, Action<SqliteMultiTenantOptions>)`.
- `src/Services/` - `ITenantService`/`TenantService`, `IBackupService`, `IMigrationService`, `IIntegrityCheckService`, `ITenantDatabaseMaintenanceService`, `ITenantSizeReportService`. Interface + implementation pairs in the same folder.
- `src/Repositories/` - data access over System.Data.SQLite (`ITenantRepository`, `BackupRepository`, `MigrationRepository`, `GenericRepository`).
- `src/Database/` - `ConnectionManager`, `ConnectionPoolManager`, `SchemaManager`.
- `src/Tenants/` - provisioning, quota enforcement, isolation verification, recovery.
- `src/Models/` - `Tenant`, `TenantContext`, `Migration`, `Backup`, result records.
- `src/Exceptions/` - `MultiTenantException` (abstract base, carries `TenantId`) and subclasses: `TenantNotFoundException`, `BackupException`, `MigrationException`, `DatabaseAccessException`, `QuotaExceededException`, `BatchTooLargeException`.
- `src/Api/`, `src/Middleware/` - controllers, `ApiResponseBuilder`, `Result<T>` wrapper, error-handling/correlation-id/rate-limiting middleware.
- Other feature folders: `BackgroundWorkers`, `BulkOperations`, `Caching`, `Cli`, `Events`, `Health`, `Integration`, `Logging`, `Monitoring`, `Security` (SQLCipher, encryption keys), `Validation`, `Utilities`.
- `benchmarks/sqlite-multi-tenant.Benchmarks/` - BenchmarkDotNet project.
- `examples/*.cs` - standalone usage samples, not compiled by the solution.
- `docs/*.md` - one page per class; `README.md` is also the NuGet readme.

Files in the repo root that are NOT part of any project (leftovers, ignore for builds): `Program.cs`, `TenantContextHelper.cs`, `test_hashes.cs`, `test_relative.cs`, `}`, `tools/*.py`, `aider_buildcmd.py`, `*_SUMMARY.md`, `IMPLEMENTATION_COMPLETE.txt`, `.aider*`. Do not add new loose `.cs` files to the root.

## Conventions

- Every `.cs` file starts with `#nullable enable` and most carry the author banner comment (`// Author: Vladyslav Zaiets | https://sarmkadan.com`). Keep both in new files.
- File-scoped namespaces (`namespace SqliteMultiTenant.X;`), one public type per file, filename equals type name.
- Public APIs get XML doc comments (`<summary>`); the doc file is generated, so missing docs surface as CS1591 warnings.
- Naming: PascalCase types/members, `_camelCase` private fields, `I`-prefixed interfaces, `Async` suffix on Task-returning methods, `CancellationToken cancellationToken = default` as last parameter.
- Helper logic lives in static `*Extensions.cs` / `*JsonExtensions.cs` / `*Validation.cs` partner files next to the type (e.g. `TenantService.cs`, `TenantServiceJsonExtensions.cs`, `TenantServiceValidation.cs`).
- Error handling: validate arguments up front and throw `ArgumentException`/`ArgumentNullException`; domain failures throw a `MultiTenantException` subclass with `TenantId`; HTTP layer converts via `ErrorHandlingMiddleware` into `Result<T>` / `ApiResponse`. Log with structured templates (`logger.LogError(ex, "... {TenantId}", id)`), not string interpolation.
- Dependency injection: constructor injection only, `ILogger<T>` injected everywhere; registrations are split across three files in `src/Configuration/`: `ServiceConfiguration.cs` (`AddSqliteMultiTenant`: repositories as singletons, services as scoped, `SqliteMultiTenantOptions` instance as singleton), `ServiceCollectionExtensions.cs` (`AddSqliteMultiTenantServices` with `ServiceOptions`: caching, event bus, HttpClient), and `DependencyInjectionSetup.cs` (`AddApiControllers`, `AddMiddlewareServices`, `AddCachingServices`, `AddEventServices`, `AddHealthCheckServices`, `AddBackgroundWorkers`, ...). Options are passed as plain configured instances, not `IOptions<T>` (only one class uses it).
- JSON: `System.Text.Json`; each `*JsonExtensions.cs` file defines its own `JsonSerializerOptions` (no shared defaults inside `src/`; the stray `SqliteMultiTenant/Utilities/JsonDefaults.cs` at repo root is not compiled).
- Commit messages: conventional prefixes (`feat:`, `fix:`, `docs:`, `chore:`, `test:`), imperative, lowercase after the colon. No `Co-Authored-By` lines.
- `CHANGELOG.md` exists but is stale; do not update it unless asked.
