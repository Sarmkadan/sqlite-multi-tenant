# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.2.0] - 2026-05-03

### Added
- Comprehensive documentation suite (getting-started, architecture, deployment guides)
- 5 example applications demonstrating various features
- Docker support with Dockerfile and docker-compose.yml
- GitHub Actions CI/CD pipeline with build, test, and publish workflow
- Health check endpoints for monitoring
- Batch operations API for high-volume processing
- Caching service with TTL and LRU eviction
- Event bus for pub-sub messaging
- Rate limiting middleware
- Audit logging with retention policies
- Metadata storage for custom tenant attributes
- Search functionality for tenants
- Backup tagging system for organization
- Performance metrics collection
- Correlation ID tracking for distributed tracing

### Changed
- Enhanced error handling with detailed exception types
- Improved logging with structured format
- Optimized database queries with pagination
- Connection pooling strategy updated

### Fixed
- Database lock timeout handling
- Migration rollback error recovery
- Backup verification process

## [1.1.0] - 2026-04-15

### Added
- REST API controllers for all core operations
- CLI interface for automation
- Request/response logging middleware
- Error handling middleware with standardized responses
- Correlation ID middleware for distributed tracing
- Multiple output formatters (JSON, CSV, XML)
- Integration with external HTTP services
- Webhook support for event notifications
- Generic repository pattern for data access
- Batch processing support
- Encryption service for sensitive data
- Validation framework with fluent API

### Changed
- Refactored service layer for better testability
- Improved async/await implementation
- Enhanced error messages with actionable guidance

### Fixed
- Race condition in connection management
- Null reference exception in migration tracking

## [1.0.0] - 2026-03-20

### Added
- Initial release of SQLite Multi-Tenant
- Core multi-tenant database management
  - Tenant creation, update, delete, lifecycle management
  - Per-tenant SQLite database isolation
  - Tenant metadata storage
  - Tenant search and filtering
- Database migration system
  - Migration creation and tracking
  - Up/down script execution
  - Migration history and rollback support
  - Pending migration queries
- Backup management
  - Multiple backup types (Full, Incremental, Differential)
  - Backup creation and verification
  - Backup expiration policies
  - Backup statistics and queries
- Connection management
  - Per-tenant connection pooling
  - Configurable connection limits
  - Connection timeout handling
- Service layer architecture
  - ITenantService interface and implementation
  - IMigrationService interface and implementation
  - IBackupService interface and implementation
- Repository pattern
  - ITenantRepository interface
  - IMigrationRepository interface
  - IBackupRepository interface
  - SQLite-specific implementations
- Exception handling
  - TenantNotFoundException
  - DatabaseAccessException
  - MigrationException
  - BackupException
- Dependency injection integration
  - Extension methods for service registration
  - Configuration options builder pattern
- Logging support
  - ILogger integration
  - Structured logging
- Validation framework
  - Entity validation
  - Custom validation rules
- Documentation
  - README with quick start guide
  - API reference documentation
  - Configuration reference

## [0.9.0] - 2026-03-01 (Beta)

### Added
- Beta release for community feedback
- Core functionality implementation
- Basic documentation

---

## Semantic Versioning

- **MAJOR** version for incompatible API changes
- **MINOR** version for backwards-compatible feature additions
- **PATCH** version for backwards-compatible bug fixes

## Version Support

| Version | Status | .NET | Support Until |
|---------|--------|------|---------------|
| 1.2.0   | Active | 8.0+ | 2027-05-03   |
| 1.1.0   | Active | 8.0+ | 2026-10-15   |
| 1.0.0   | Active | 8.0+ | 2026-09-20   |
| 0.9.0   | Deprecated | 8.0+ | 2026-06-01 |

## Migration Guide

### From 1.1.0 to 1.2.0

No breaking changes. Update via NuGet:

```bash
dotnet add package SqliteMultiTenant --version 1.2.0
```

### From 1.0.0 to 1.1.0

No breaking changes. All APIs remain compatible.

### From 0.9.0 to 1.0.0

Minor API adjustments:
- `Tenant.Status` property now uses enum
- `Backup.BackupType` property now uses enum
- Service registration method signatures simplified

```csharp
// Old (0.9.0)
services.RegisterMultiTenantServices(connectionString);

// New (1.0.0+)
services.AddSqliteMultiTenant(connectionString, options => {});
```

## Future Roadmap

### 1.3.0 (Q3 2026)
- [ ] Sharding support for very large deployments
- [ ] Distributed caching (Redis integration)
- [ ] Advanced analytics and reporting
- [ ] GDPR compliance features

### 2.0.0 (Q4 2026)
- [ ] PostgreSQL backend support
- [ ] MySQL backend support
- [ ] Kubernetes operators
- [ ] OpenTelemetry integration
- [ ] GraphQL API

## Known Issues

- SQLite has 30-second file lock timeout (see troubleshooting)
- Network storage performance degrades with high concurrency
- Large batch operations (>10000 items) may consume significant memory

## Getting Help

- **Issues**: [GitHub Issues](https://github.com/Sarmkadan/sqlite-multi-tenant/issues)
- **Discussions**: [GitHub Discussions](https://github.com/Sarmkadan/sqlite-multi-tenant/discussions)
- **Email**: rutova2@gmail.com

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

---

**Last Updated**: 2026-05-03  
**Maintained by**: Vladyslav Zaiets ([https://sarmkadan.com](https://sarmkadan.com))
