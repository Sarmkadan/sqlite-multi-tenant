# IDataMapper

The `IDataMapper` interface provides a standardized contract for object-to-object mapping within the `sqlite-multi-tenant` framework, facilitating the transformation of source objects into target types, including the mapping of collections. It serves as the abstraction for the `DataMapper` implementation, enabling dependency injection and decoupled design when converting data transfer objects or database entities.

## API

### IDataMapper (Interface)

*   `Map<TSource, TTarget>(TSource source)`
    *   **Purpose**: Maps a single instance of `TSource` to a new instance of `TTarget`.
    *   **Parameters**: `source` (the object to map).
    *   **Return Value**: A new instance of `TTarget`.
    *   **Throws**: Throws if `TTarget` does not have a parameterless constructor or if mapping fails.
*   `MapList<TSource, TTarget>(List<TSource> sources)`
    *   **Purpose**: Maps a list of `TSource` instances to a new `List` of `TTarget` instances.
    *   **Parameters**: `sources` (the list of objects to map).
    *   **Return Value**: A new `List<TTarget>`.
    *   **Throws**: Throws if `TTarget` does not have a parameterless constructor.

### DataMapper (Class)

*   `DataMapper()`
    *   **Purpose**: Initializes a new instance of the `DataMapper` class.
*   `Map<TSource, TTarget>(TSource source)`
    *   **Purpose**: Implementation of the `IDataMapper.Map` method.
*   `MapList<TSource, TTarget>(List<TSource> sources)`
    *   **Purpose**: Implementation of the `IDataMapper.MapList` method.

### MappingProfile (Class)

*   `MappingProfile()`
    *   **Purpose**: Initializes a new instance of the `MappingProfile` class, used to configure custom mapping behaviors.
*   `AddCustomMapping<TSource, TTarget>()`
    *   **Purpose**: Registers custom mapping logic between `TSource` and `TTarget` within the profile.
*   `TryGetCustomMapping()`
    *   **Purpose**: Attempts to retrieve a registered custom mapping configuration for the specified types.
    *   **Return Value**: Returns `true` if a custom mapping exists, `false` otherwise.

## Usage

### Example 1: Basic Object Mapping
```csharp
public class UserEntity { public string Name { get; set; } }
public class UserDto { public string Name { get; set; } }

var mapper = new DataMapper();
var entity = new UserEntity { Name = "John Doe" };

// Automatically maps properties with matching names
UserDto dto = mapper.Map<UserEntity, UserDto>(entity);
```

### Example 2: Mapping a Collection
```csharp
var entities = new List<UserEntity> 
{ 
    new UserEntity { Name = "Alice" }, 
    new UserEntity { Name = "Bob" } 
};

var mapper = new DataMapper();

// Maps the entire collection
List<UserDto> dtos = mapper.MapList<UserEntity, UserDto>(entities);
```

## Notes

*   **Type Constraints**: Both `Map` and `MapList` require `TTarget` to have a public parameterless constructor (`where TTarget : class, new()`).
*   **Property Mapping**: Mapping is primarily convention-based, relying on property name matching between source and target types.
*   **Custom Configurations**: For scenarios where convention-based mapping is insufficient, use `MappingProfile` and `AddCustomMapping` to define explicit transformation logic.
*   **Thread Safety**: The `DataMapper` instance is generally intended to be thread-safe once initialized, assuming the underlying mapping configurations (defined in `MappingProfile`) are not modified concurrently. If configurations are modified at runtime, thread synchronization must be managed externally.
