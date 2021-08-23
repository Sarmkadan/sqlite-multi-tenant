# QueryBuilderBenchmarks
The `QueryBuilderBenchmarks` class is designed to provide a set of benchmarking tools for evaluating the performance of query building operations in the context of SQLite multi-tenancy. It offers a range of pre-defined queries and setup methods to facilitate thorough performance analysis.

## API
* `public void Setup()`: Initializes the benchmarking environment. This method does not take any parameters and does not return a value. It should be called before running any benchmarks to ensure a consistent setup.
* `public string SimpleSelect`: Returns a query string representing a simple SELECT operation. This property does not take any parameters and does not throw any exceptions.
* `public string SelectWithOrderAndLimit`: Returns a query string representing a SELECT operation with ORDER BY and LIMIT clauses. This property does not take any parameters and does not throw any exceptions.
* `public string SelectWithJoin`: Returns a query string representing a SELECT operation with a JOIN clause. This property does not take any parameters and does not throw any exceptions.
* `public string InsertBuild`: Returns a query string representing an INSERT operation built using the query builder. This property does not take any parameters and does not throw any exceptions.

## Usage
The following examples demonstrate how to use the `QueryBuilderBenchmarks` class to evaluate the performance of query building operations:
```csharp
// Example 1: Simple benchmarking
var benchmarks = new QueryBuilderBenchmarks();
benchmarks.Setup();
var simpleSelectQuery = benchmarks.SimpleSelect;
Console.WriteLine(simpleSelectQuery);

// Example 2: Benchmarking with more complex queries
var benchmarks = new QueryBuilderBenchmarks();
benchmarks.Setup();
var selectWithJoinQuery = benchmarks.SelectWithJoin;
var insertBuildQuery = benchmarks.InsertBuild;
Console.WriteLine(selectWithJoinQuery);
Console.WriteLine(insertBuildQuery);
```

## Notes
When using the `QueryBuilderBenchmarks` class, consider the following edge cases and thread-safety remarks:
* The `Setup` method should be called only once before running any benchmarks to avoid re-initializing the environment.
* The query strings returned by the properties are pre-defined and do not depend on any external state, making them thread-safe.
* However, the `Setup` method may have side effects or depend on external resources, so it should be called from a single thread to avoid concurrency issues.
* The benchmarking results may vary depending on the system configuration, database schema, and data distribution, so it is essential to run the benchmarks in a controlled environment to ensure reliable results.
