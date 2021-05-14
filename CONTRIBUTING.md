# Contributing to sqlite-multi-tenant

Thank you for considering contributing to sqlite-multi-tenant! This guide will help you get started.

## How to Contribute

1. **Fork the repository** on GitHub.
2. **Clone your fork** locally:
   ```bash
   git clone https://github.com/YOUR_USERNAME/sqlite-multi-tenant.git
   cd sqlite-multi-tenant
   ```
3. **Create a new branch** for your feature or bug fix:
   ```bash
   git checkout -b feature/your-feature-name
   ```
4. **Make your changes** and commit them with descriptive messages.
5. **Run the tests** locally to ensure everything works as expected:
   ```bash
   dotnet test
   ```
6. **Push to your fork** on GitHub.
7. **Submit a Pull Request** to the main repository.

## Development Requirements

- **.NET 10.0 SDK** or later
- SQLite (bundled via NuGet, no separate install needed)

## Building the Project

```bash
# Restore dependencies
dotnet restore

# Build the solution (Release configuration)
dotnet build --configuration Release

# Run all tests
dotnet test --configuration Release --verbosity normal --logger "trx;LogFileName=test-results.trx"

# Run benchmarks
dotnet run --project benchmarks/sqlite-multi-tenant.Benchmarks -c Release

# Run with Docker
docker-compose up --build
```

## CI / CD

All pull requests run the CI workflow automatically (`ci.yml`). Ensure your branch passes before requesting review:

- `ci.yml` — build and test on every push/PR to `main`
- `release.yml` — triggered on `v*` tags; packs and publishes the NuGet package and creates a GitHub release
- `docker.yml` — builds and pushes the container image to `ghcr.io/sarmkadan/sqlite-multi-tenant`

## Project Structure

```
src/
  Api/              - REST controllers and request/response models
  BackgroundWorkers/ - Scheduled tasks (backup rotation, maintenance)
  Caching/          - In-memory and distributed cache services
  Cli/              - Command-line interface
  Configuration/    - DI setup and options
  Database/         - Connection pooling and schema management
  DataOperations/   - Query builders and data import/export
  Events/           - Domain event bus and handlers
  Middleware/       - ASP.NET middleware (rate limiting, logging, etc.)
  Models/           - Domain entities (Tenant, Backup, Migration)
  Monitoring/       - Metrics, audit logging, performance monitoring
  Repositories/     - Data access layer
  Security/         - Encryption key management, rate limiting
  Services/         - Business logic layer
  Tenants/          - Tenant provisioning, isolation, recovery
  Utilities/        - Extension methods and helpers
  Validation/       - Fluent validation rules
tests/              - Unit and integration tests
```

## Code Style

- Follow the existing coding conventions found in the repository.
- Use XML documentation (`/// <summary>`) for all public classes, methods, and properties.
- **Keep all author headers intact.** Do not remove existing author headers from source files.
- Use `sealed` on classes that are not designed for inheritance.
- Prefer `async/await` with `CancellationToken` support for I/O operations.
- Use structured logging with `ILogger` and named placeholders (not string interpolation).

## Pull Request Guidelines

- Keep PRs focused on a single concern.
- Include tests for new functionality.
- Update XML documentation for any changed public APIs.
- Ensure `dotnet build` completes without warnings.

## Reporting Issues

If you find a bug or have a feature request, please use GitHub Issues.
When reporting an issue, please include:
- A clear and descriptive title.
- Steps to reproduce the problem.
- Expected behavior vs. actual behavior.
- .NET version and operating system.
- Any relevant logs or error messages.

## License

By contributing, you agree that your contributions will be licensed under the MIT License.
