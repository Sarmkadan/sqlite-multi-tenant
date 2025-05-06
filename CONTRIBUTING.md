# Contributing to SQLite Multi-Tenant

Thank you for your interest in contributing! This document provides guidelines for contributing to the project.

## Table of Contents

1. [Code of Conduct](#code-of-conduct)
2. [Getting Started](#getting-started)
3. [Development Setup](#development-setup)
4. [Making Changes](#making-changes)
5. [Code Standards](#code-standards)
6. [Commit Messages](#commit-messages)
7. [Pull Request Process](#pull-request-process)
8. [Testing Guidelines](#testing-guidelines)
9. [Documentation](#documentation)
10. [Reporting Issues](#reporting-issues)

## Code of Conduct

Be respectful and inclusive. We value contributions from everyone regardless of background or experience level.

## Getting Started

### Prerequisites

- .NET 8.0 SDK or later
- Git
- GitHub account
- Basic familiarity with C# and .NET

### Fork and Clone

```bash
# Fork the repository on GitHub
# Clone your fork
git clone https://github.com/YOUR_USERNAME/sqlite-multi-tenant.git
cd sqlite-multi-tenant

# Add upstream remote
git remote add upstream https://github.com/Sarmkadan/sqlite-multi-tenant.git

# Create a feature branch
git checkout -b feature/your-feature-name
```

## Development Setup

```bash
# Install dependencies
dotnet restore

# Build the project
dotnet build

# Run tests
dotnet test

# Run linting
dotnet format --verify-no-changes

# Run the application
dotnet run
```

Use the Makefile for common tasks:

```bash
make help          # View all available commands
make dev-setup     # Setup development environment
make build         # Build project
make test          # Run tests
make lint          # Run code analysis
make docker-up     # Start in Docker
```

## Making Changes

### Branches

- `main` - Production-ready code
- `develop` - Development branch
- `feature/*` - Feature branches
- `bugfix/*` - Bug fix branches
- `docs/*` - Documentation branches

### Branch Naming

Use descriptive branch names:

```bash
# Features
git checkout -b feature/add-caching-service
git checkout -b feature/improve-error-messages

# Bugfixes
git checkout -b bugfix/fix-null-reference-error
git checkout -b bugfix/connection-pool-leak

# Documentation
git checkout -b docs/update-readme
git checkout -b docs/add-deployment-guide
```

## Code Standards

### C# Style Guidelines

Follow Microsoft's C# coding conventions with these specific rules:

```csharp
// Always use file-scoped namespaces
namespace SqliteMultiTenant.Services;

// PascalCase for classes, interfaces, methods, properties
public class TenantService
{
    // camelCase for private fields (with underscore prefix)
    private readonly ITenantRepository _repository;
    
    // PascalCase for parameters and local variables where possible
    public async Task<Tenant> GetTenantAsync(string tenantId)
    {
        var tenant = await _repository.GetByIdAsync(tenantId);
        return tenant;
    }
}

// Use explicit access modifiers
public class Tenant { }      // Not internal by default
private string _field;       // Explicitly private

// Use var only when type is obvious
var tenant = new Tenant();   // OK
var name = GetName();        // OK if GetName() return type is clear
Tenant tenant = GetTenant(); // Better when type isn't obvious

// Use expression-bodied members when appropriate
public string Name => _name;
public string GetId() => _id;

// Use null coalescing and null-conditional operators
var name = tenant?.Name ?? "Unknown";

// Use pattern matching
if (response is { Status: 200, Data: not null })
{
    // Handle success
}
```

### File Headers

Every C# file must start with:

```csharp
// =============================================================================
// Author: Your Name | https://your-website.com
// Description: Brief description of what this file does
// =============================================================================
```

For contributed files:

```csharp
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// Contributors: Your Name | https://your-website.com
// =============================================================================
```

### XML Documentation

Document public APIs with XML comments:

```csharp
/// <summary>
/// Creates a new tenant in the system.
/// </summary>
/// <param name="name">The name of the tenant. Must not be null or empty.</param>
/// <param name="email">The contact email. Must be a valid email format.</param>
/// <returns>The created tenant with assigned ID.</returns>
/// <exception cref="ArgumentNullException">Thrown when name or email is null.</exception>
/// <exception cref="ArgumentException">Thrown when email format is invalid.</exception>
/// <example>
/// <code>
/// var tenant = await tenantService.CreateTenantAsync("Acme Corp", "admin@acme.com");
/// </code>
/// </example>
public async Task<Tenant> CreateTenantAsync(string name, string email)
{
    // Implementation
}
```

### Method Comments

Comment "why", not "what":

```csharp
// Bad - Says what the code does
// Split the string by comma
var parts = input.Split(',');

// Good - Explains the reason
// Split by comma to parse comma-separated values; email is optional
var parts = input.Split(',');

// Only add comments for non-obvious logic
var retryCount = 0;
// Exponential backoff: 100ms, 200ms, 400ms, etc.
while (retryCount < maxRetries)
{
    var delay = (int)Math.Pow(2, retryCount) * 100;
    await Task.Delay(delay);
}
```

### Error Handling

Always handle errors gracefully:

```csharp
// Log errors with context
try
{
    await database.SaveChangesAsync();
}
catch (DatabaseException ex)
{
    _logger.LogError($"Failed to save tenant {tenant.Id}: {ex.Message}");
    throw new DataAccessException($"Could not save tenant", ex);
}

// Use specific exception types, not generic Exception
catch (OperationCanceledException)
{
    // Operation was cancelled, possibly timeout
}
catch (InvalidOperationException ex)
{
    // Invalid state, not a data problem
}
```

### Async/Await

Always use async methods for I/O:

```csharp
// Use async for database, network, file operations
public async Task<Tenant> GetTenantAsync(string id)
{
    return await _repository.GetByIdAsync(id);
}

// Don't block on async operations
var tenant = GetTenantAsync(id).Result;  // DON'T do this

// Use ConfigureAwait for library code
await Task.Delay(100).ConfigureAwait(false);
```

### SOLID Principles

- **S**ingle Responsibility: One reason to change
- **O**pen/Closed: Open for extension, closed for modification
- **L**iskov Substitution: Subtypes must be substitutable
- **I**nterface Segregation: Specific interfaces over general ones
- **D**ependency Inversion: Depend on abstractions, not concretions

## Commit Messages

Write clear, descriptive commit messages:

```
[TYPE] Brief description (50 chars or less)

Longer explanation if needed, wrapped at 72 characters. Explain what
changed and why, not how (the code shows how).

Fixes #123
```

### Types

- `feat:` - New feature
- `fix:` - Bug fix
- `docs:` - Documentation
- `style:` - Code style (formatting, missing semicolons, etc.)
- `refactor:` - Code refactoring without functional change
- `perf:` - Performance improvement
- `test:` - Adding or updating tests
- `chore:` - Build, CI/CD, dependency updates

### Examples

```
feat: Add caching service with TTL support

Implements in-memory LRU cache with configurable TTL and eviction.
Reduces database load for frequently accessed entities.

Fixes #456

---

fix: Fix null reference in migration rollback

Handle case where DownScript is null or empty string.
Previously caused NullReferenceException, now skips safely.

Fixes #789

---

docs: Add deployment guide for Docker

Covers Docker setup, docker-compose configuration, and Kubernetes
deployment patterns for production environments.
```

## Pull Request Process

### Before Submitting

1. Update from upstream:
   ```bash
   git fetch upstream
   git rebase upstream/develop
   ```

2. Run tests locally:
   ```bash
   make test
   make lint
   ```

3. Format code:
   ```bash
   dotnet format
   ```

4. Push to your fork:
   ```bash
   git push origin feature/your-feature-name
   ```

### Creating a PR

1. Go to GitHub and create a Pull Request
2. Use descriptive title and description
3. Link related issues: `Fixes #123`
4. Describe what changed and why
5. Include testing instructions if needed

### PR Template

```markdown
## Description
Brief description of changes

## Type of Change
- [ ] Bug fix
- [ ] New feature
- [ ] Breaking change
- [ ] Documentation update

## Related Issues
Fixes #123

## Testing
Describe how to test this change

## Checklist
- [ ] Code follows style guidelines
- [ ] Self-review completed
- [ ] Comments added for complex logic
- [ ] Documentation updated
- [ ] Tests added/updated
- [ ] All tests pass
- [ ] No new warnings generated
```

### CI/CD

- GitHub Actions will automatically run build, test, and lint
- All checks must pass before merging
- Require at least 1 approval from maintainers

## Testing Guidelines

### Unit Tests

- Test public APIs and methods
- Cover happy path and error cases
- Use descriptive test names: `TestCreatingTenantWithValidDataSucceeds`

```csharp
[Fact]
public async Task CreateTenant_WithValidData_ReturnsTenantWithId()
{
    // Arrange
    var service = new TenantService(_mockRepository.Object);
    
    // Act
    var result = await service.CreateTenantAsync("Test", "test@example.com");
    
    // Assert
    Assert.NotNull(result.TenantId);
    Assert.Equal("Test", result.Name);
}
```

### Integration Tests

- Test database interactions
- Use test databases (in-memory SQLite)
- Clean up after tests

```csharp
[Fact]
public async Task TenantRepository_CreateAndRetrieve_RoundTrip()
{
    // Arrange - Setup in-memory database
    using var connection = new SqliteConnection("Data Source=:memory:");
    await connection.OpenAsync();
    
    var repository = new TenantRepository(connection);
    var tenant = new Tenant { Name = "Test" };
    
    // Act
    await repository.CreateAsync(tenant);
    var retrieved = await repository.GetByIdAsync(tenant.TenantId);
    
    // Assert
    Assert.NotNull(retrieved);
    Assert.Equal(tenant.Name, retrieved.Name);
}
```

## Documentation

### When to Document

- Public APIs (methods, classes, properties)
- Complex algorithms or unusual patterns
- Configuration options and settings
- New features with examples

### Types of Documentation

1. **XML Comments** - For code APIs
2. **README Updates** - For feature overviews
3. **Usage Examples** - In examples/ directory
4. **Guides** - In docs/ directory
5. **Inline Comments** - For non-obvious logic

## Reporting Issues

### Bug Reports

Provide:
- Steps to reproduce
- Expected vs actual behavior
- Environment (OS, .NET version, etc.)
- Error messages and stack traces

### Feature Requests

Include:
- Use case and motivation
- Proposed solution (if you have one)
- Alternatives considered
- Potential implementation approach

### Template

```markdown
## Description
Brief description of the issue

## Reproduction Steps
1. ...
2. ...

## Expected Behavior
What should happen

## Actual Behavior
What actually happens

## Environment
- OS: Windows 11 / Linux / macOS
- .NET: 8.0.1
- SQLite: 3.42.0

## Logs
```
Paste error messages or logs here
```
```

## Review Process

1. Code review by maintainers
2. Request changes if needed
3. Approval from at least one maintainer
4. Merge to develop branch
5. Periodic merge to main for release

## Questions?

- **Issues**: [GitHub Issues](https://github.com/Sarmkadan/sqlite-multi-tenant/issues)
- **Email**: vladyslav.zaiets@amdaris.com
- **Website**: https://sarmkadan.com

## License

By contributing, you agree that your contributions will be licensed under the same MIT License as the project.

---

Thank you for contributing to SQLite Multi-Tenant!
