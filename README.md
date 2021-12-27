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

// existing content ...
