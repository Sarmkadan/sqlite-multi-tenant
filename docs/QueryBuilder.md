# QueryBuilder
The `QueryBuilder` class is designed to facilitate the construction of SQL queries in a multi-tenant SQLite environment. It provides a fluent interface for specifying various query components, such as selection, filtering, joining, ordering, and pagination, allowing developers to build complex queries in a structured and readable manner.

## API
* `public QueryBuilder()`: Initializes a new instance of the `QueryBuilder` class.
* `public QueryBuilder Select`: Specifies the selection component of the query.
* `public QueryBuilder Where(string condition, params object[] parameters)`: Adds a filtering condition to the query. The `condition` parameter specifies the filter expression, and the `parameters` parameter provides values for any placeholders in the expression.
* `public QueryBuilder And(string condition, params object[] parameters)`: Adds an additional filtering condition to the query using a logical AND operator.
* `public QueryBuilder Or(string condition, params object[] parameters)`: Adds an additional filtering condition to the query using a logical OR operator.
* `public QueryBuilder InnerJoin`: Specifies an inner join operation for the query.
* `public QueryBuilder LeftJoin`: Specifies a left join operation for the query.
* `public QueryBuilder OrderBy`: Specifies the ordering component of the query.
* `public QueryBuilder Limit`: Specifies the pagination limit for the query.
* `public QueryBuilder Offset`: Specifies the pagination offset for the query.
* `public string Build()`: Constructs the final SQL query string based on the specified components.
* `public void ApplyParameters(Dictionary<string, object> parameters)`: Applies parameter values to the query.
* `public QueryBuilder Reset()`: Resets the query builder to its initial state.
* `public override string ToString()`: Returns a string representation of the query builder.

## Usage
The following examples demonstrate how to use the `QueryBuilder` class to construct SQL queries:
```csharp
// Example 1: Simple selection query
var queryBuilder = new QueryBuilder();
queryBuilder.Select("id", "name");
queryBuilder.Where("age > @age", new { age = 18 });
var query = queryBuilder.Build();
Console.WriteLine(query);

// Example 2: Query with joining and pagination
var queryBuilder2 = new QueryBuilder();
queryBuilder2.Select("orders.id", "customers.name");
queryBuilder2.InnerJoin("orders", "customers", "orders.customer_id = customers.id");
queryBuilder2.Where("orders.total > @total", new { total = 100 });
queryBuilder2.OrderBy("orders.id");
queryBuilder2.Limit(10);
queryBuilder2.Offset(5);
var query2 = queryBuilder2.Build();
Console.WriteLine(query2);
```

## Notes
When using the `QueryBuilder` class, note that the `Build` method will throw an exception if the query is not properly constructed (e.g., missing selection or filtering components). Additionally, the `ApplyParameters` method should be used to provide parameter values to prevent SQL injection attacks. The `Reset` method can be used to reuse a query builder instance, but it will discard any previously specified components. The `QueryBuilder` class is not thread-safe, so it should not be shared across multiple threads. The `InsertBuilder` and `UpdateBuilder` classes are related but separate classes, and their usage is not covered in this documentation.
