# DataMapper

`DataMapper` is the convention-based object mapper implemented in `src/Utilities/DataMapper.cs`. It creates a new target object and copies values between public instance properties whose names match, making it suitable for simple entity-to-DTO and DTO-to-entity transformations.

## Relationship to `IDataMapper`

`DataMapper` is a sealed implementation of [`IDataMapper`](IDataMapper.md). The interface exposes the single-object and list-mapping operations so consumers can depend on the abstraction and register or replace the implementation through dependency injection. The concrete class requires an `ILogger<DataMapper>` in its constructor for property-level conversion warnings and operation-level errors.

`MappingProfile` is also declared in `DataMapper.cs`, but it is a separate type: `DataMapper` does not accept or consult a `MappingProfile`. Registering a custom mapping therefore has no effect on `DataMapper.Map` or `DataMapper.MapList` in the current implementation.

## Public API

### `DataMapper`

```csharp
public DataMapper(ILogger<DataMapper> logger)
```

Creates a mapper using the supplied logger and initializes its property metadata cache.

```csharp
public TTarget Map<TSource, TTarget>(TSource source)
    where TTarget : class, new()
```

Creates a new `TTarget` and maps readable source properties to writable target properties. `TTarget` must be a reference type with a public parameterless constructor. In the current implementation, a null `source` returns a new, unmapped target instance.

```csharp
public List<TTarget> MapList<TSource, TTarget>(List<TSource> sources)
    where TTarget : class, new()
```

Maps every item by calling `Map<TSource, TTarget>` and returns a new list. In the current implementation, a null or empty list produces an empty list.

### `MappingProfile`

```csharp
public MappingProfile()
```

Creates an empty registry of custom property mapping delegates.

```csharp
public void AddCustomMapping<TSource, TTarget>(
    string propertyName,
    Func<TSource, object> mappingFunc)
    where TSource : class
```

Registers a delegate under the key formed from `typeof(TSource).Name` and `propertyName`. The `TTarget` type parameter is part of the public signature but is not used to construct the key or invoke the delegate.

```csharp
public bool TryGetCustomMapping(
    string typeName,
    string propertyName,
    out Func<object, object>? mapping)
```

Looks up a registered delegate using the key formed from `typeName` and `propertyName`.

## Mapping conventions

- Only public instance properties are considered. Property metadata is cached by the type's full name for reuse by the mapper instance.
- Property names match case-insensitively using ordinal comparison. Properties do not need to appear in the same order.
- The source property must be readable and the target property must be writable.
- A target property with no matching source property keeps the value assigned by the target's constructor or initializer.
- Equal declared property types are assigned directly. When declared types differ and the source value is non-null, the mapper attempts a conversion using invariant culture.
- Explicit conversions cover `string`, `int`, `long`, `double`, `bool`, `DateTime`, and `Guid`; other target types are passed to `Convert.ChangeType`.
- A null source property value is assigned directly to the matching target property. If that assignment is invalid, the property-level exception is logged and mapping continues.
- Failed type conversion is logged and produces `null`; assigning that result may leave a nullable/reference target property null or may itself fail for a non-nullable value type. Property-level failures do not stop other properties from being mapped.
- Lists are mapped item by item with the same conventions. There is no special collection, nested-object, flattening, or recursive mapping behavior.
- The implementation has no `IDataReader`, `DbDataReader`, row-to-object, or dictionary-to-object overload. A reader row or dictionary is therefore not mapped by column/key; it is treated like any other source object, using its public properties.

## Usage example

```csharp
using SqliteMultiTenant.Utilities;

public sealed class TenantRecord
{
    public string Id { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public int UserCount { get; init; }
}

public sealed class TenantDto
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public long UserCount { get; set; }
}

// Resolve IDataMapper from dependency injection. Its registered implementation
// can be DataMapper, constructed with an ILogger<DataMapper>.
IDataMapper mapper = serviceProvider.GetRequiredService<IDataMapper>();

var source = new TenantRecord
{
    Id = "2f1f36aa-9061-4ca8-bf9b-f312f3cba855",
    DisplayName = "Northwind",
    UserCount = 12
};

TenantDto dto = mapper.Map<TenantRecord, TenantDto>(source);

// Id is parsed from string to Guid, DisplayName is copied directly,
// and UserCount is converted from int to long.
Console.WriteLine($"{dto.Id}: {dto.DisplayName} ({dto.UserCount})");

var dtos = mapper.MapList<TenantRecord, TenantDto>(new List<TenantRecord> { source });
```

Mapping errors that escape the per-property handling are logged and rethrown. Callers should validate mapped values when conversion failure is possible, because an individual failed property conversion does not necessarily fail the entire mapping operation.
