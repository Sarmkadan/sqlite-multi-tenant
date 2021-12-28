// existing content ...

## QueryBuilderBenchmarks

The `QueryBuilderBenchmarks` class measures SQL generation overhead in the `QueryBuilder` class, which is exercised during tenant resolution, migration execution, and backup operations. It benchmarks common query patterns like simple selects, ordered/limited queries, joins, and inserts to evaluate performance characteristics of SQL generation logic.

### Usage Example

```csharp
using SqliteMultiTenant.DataOperations;
using SqliteMultiTenant.Benchmarks;

var benchmarks = new QueryBuilderBenchmarks();
benchmarks.Setup();

// Generate a simple SELECT query
string simpleSelect = benchmarks.SimpleSelect();

// Generate a SELECT with ORDER BY and LIMIT
string orderedSelect = benchmarks.SelectWithOrderAndLimit();

// Generate a SELECT with JOIN
string joinedSelect = benchmarks.SelectWithJoin();

// Generate an INSERT query
string insertQuery = benchmarks.InsertBuild();
```

## TenantValidationBenchmarks

The `TenantValidationBenchmarks` class measures the performance of tenant validation operations that run on every inbound API request creating or resolving a tenant. It benchmarks validation of tenant IDs (including reserved names and SQL injection attempts) and tenant names, as well as tenant ID generation to evaluate the throughput characteristics of the validation pipeline.

### Usage Example

```csharp
using SqliteMultiTenant.Utilities;
using SqliteMultiTenant.Benchmarks;

var benchmarks = new TenantValidationBenchmarks();

// Validate a valid tenant ID
ValidationResult validIdResult = benchmarks.ValidateTenantId_Valid();

// Validate a reserved tenant ID (e.g., "admin")
ValidationResult reservedIdResult = benchmarks.ValidateTenantId_Reserved();

// Validate a tenant ID with potential SQL injection
ValidationResult sqlInjectionResult = benchmarks.ValidateTenantId_SqlInjection();

// Validate a tenant name
ValidationResult nameResult = benchmarks.ValidateTenantName();

// Generate a tenant ID from a company name
string generatedId = benchmarks.GenerateTenantId();
```

// existing content ...
