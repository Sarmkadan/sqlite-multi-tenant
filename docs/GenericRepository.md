# GenericRepository
The `GenericRepository` class is a fundamental component of the `sqlite-multi-tenant` project, providing a standardized interface for interacting with data storage. It serves as a base class for implementing repository patterns, allowing for the encapsulation of data access and manipulation logic. By leveraging this class, developers can create type-safe and reusable data access layers, streamlining their application's architecture and promoting maintainability.

## API
The `GenericRepository` class exposes several public members that enable various data operations:
* `GetAllAsync`: Retrieves a list of all entities of type `T` from the data storage. Returns a `Task` containing a `List<T>`.
* `GetByIdAsync`: Retrieves an entity of type `T` by its identifier. Returns a `Task` containing the entity, or `null` if not found.
* `CreateAsync`: Creates a new entity of type `T` in the data storage. Returns a `Task` containing the created entity.
* `UpdateAsync`: Updates an existing entity of type `T` in the data storage. Returns a `Task` containing a boolean indicating success.
* `DeleteAsync`: Deletes an entity of type `T` from the data storage. Returns a `Task` containing a boolean indicating success.
* `FindAsync`: Retrieves a list of entities of type `T` that match the specified criteria. Returns a `Task` containing a `List<T>`.
* `GetCountAsync`: Retrieves the total count of entities of type `T` in the data storage. Returns a `Task` containing an integer count.
* `ExistsAsync`: Checks if an entity of type `T` with the specified identifier exists in the data storage. Returns a `Task` containing a boolean indicating existence.
* `GetPagedAsync`: Retrieves a paginated list of entities of type `T` from the data storage. Returns a `Task` containing a `PaginatedResult<T>`.
* `BulkCreateAsync`: Creates multiple new entities of type `T` in the data storage. Returns a `Task` containing the number of entities created.
* `BulkUpdateAsync`: Updates multiple existing entities of type `T` in the data storage. Returns a `Task` containing the number of entities updated.
* `BulkDeleteAsync`: Deletes multiple entities of type `T` from the data storage. Returns a `Task` containing the number of entities deleted.

## Usage
The following examples demonstrate how to utilize the `GenericRepository` class:
```csharp
// Example 1: Retrieving all entities
var repository = new MyRepository();
var entities = await repository.GetAllAsync();
foreach (var entity in entities)
{
    Console.WriteLine(entity);
}

// Example 2: Creating a new entity
var newEntity = new MyEntity { Name = "John Doe" };
var createdEntity = await repository.CreateAsync(newEntity);
Console.WriteLine($"Created entity with ID {createdEntity.Id}");
```

## Notes
When using the `GenericRepository` class, consider the following edge cases and thread-safety remarks:
* The `GetAllAsync` and `FindAsync` methods may return large datasets, which can impact performance. Implement pagination or filtering to mitigate this.
* The `CreateAsync`, `UpdateAsync`, and `DeleteAsync` methods may throw exceptions if the data storage is unavailable or if the operation fails. Handle these exceptions accordingly to ensure data consistency.
* The `BulkCreateAsync`, `BulkUpdateAsync`, and `BulkDeleteAsync` methods may have performance implications due to the bulk nature of the operations. Monitor and optimize these operations as needed.
* The `GenericRepository` class is designed to be thread-safe, allowing for concurrent access and operations. However, the underlying data storage may impose its own thread-safety constraints. Ensure that the data storage is properly configured and accessed to avoid concurrency issues.
