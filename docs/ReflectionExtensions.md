# ReflectionExtensions

The `ReflectionExtensions` class provides a suite of static utility methods designed to streamline common reflection operations in C#. It facilitates the inspection of types, dynamic property access, instance creation, and custom attribute handling, reducing the boilerplate code typically required when utilizing the `System.Reflection` namespace. This utility is primarily employed for tasks involving dynamic object mapping, data serialization, and generic type handling within the `sqlite-multi-tenant` framework.

## API

### GetPublicProperties
Retrieves an array of all public instance properties for the specified `Type`.
*   **Parameters:** `Type type`
*   **Returns:** `PropertyInfo[]`

### GetPropertyValue
Gets the value of a specific property from a given object instance.
*   **Parameters:** `object obj`, `string propertyName`
*   **Returns:** `object` (the value of the property)
*   **Exceptions:** May throw `ArgumentException` if the property does not exist.

### SetPropertyValue
Attempts to set the value of a specific property on an object instance.
*   **Parameters:** `object obj`, `string propertyName`, `object value`
*   **Returns:** `bool` (`true` if the property was set successfully; `false` otherwise)

### IsCollection
Determines whether the specified `Type` implements `IEnumerable`, excluding the `string` type.
*   **Parameters:** `Type type`
*   **Returns:** `bool`

### GetCollectionElementType
Determines the `Type` of the elements contained within a collection type.
*   **Parameters:** `Type type`
*   **Returns:** `Type`

### IsNullable
Determines whether the specified `Type` is a `Nullable<T>`.
*   **Parameters:** `Type type`
*   **Returns:** `bool`

### GetUnderlyingType
Returns the underlying `Type` of a nullable type, or the provided `Type` itself if it is not nullable.
*   **Parameters:** `Type type`
*   **Returns:** `Type`

### IsScalarType
Determines if the specified `Type` is considered a scalar type (e.g., primitives, `string`, `decimal`, `DateTime`, etc.).
*   **Parameters:** `Type type`
*   **Returns:** `bool`

### GetMethodsByName
Retrieves an array of all methods matching the specified name for the given `Type`.
*   **Parameters:** `Type type`, `string name`
*   **Returns:** `MethodInfo[]`

### CreateInstance
Dynamically creates a new instance of the specified `Type` using its parameterless constructor.
*   **Parameters:** `Type type`
*   **Returns:** `object`
*   **Exceptions:** May throw `MissingMethodException` if no parameterless constructor exists.

### HasAttribute&lt;T&gt;
Checks whether the specified `MemberInfo` or `Type` is decorated with an attribute of type `T`.
*   **Parameters:** `MemberInfo member` (or `Type`)
*   **Returns:** `bool`

### GetAttribute&lt;T&gt;
Retrieves the specified attribute of type `T` from a given `MemberInfo` or `Type`.
*   **Parameters:** `MemberInfo member` (or `Type`)
*   **Returns:** `T` (the attribute instance, or `null` if not found)

### CopyPropertiesTo&lt;T&gt;
Copies the values of all readable public properties from the source object to the target object of the same type.
*   **Parameters:** `T source`, `T target`
*   **Returns:** `void`

## Usage

### Example 1: Dynamic Property Access and Attribute Checking
```csharp
using System.Reflection;
using System.ComponentModel.DataAnnotations;

var user = new User { Name = "John Doe" };
PropertyInfo nameProperty = typeof(User).GetProperty("Name");

if (ReflectionExtensions.HasAttribute<RequiredAttribute>(nameProperty))
{
    var value = ReflectionExtensions.GetPropertyValue(user, "Name");
    Console.WriteLine($"Name property is required and its value is: {value}");
}
```

### Example 2: Copying Object Properties
```csharp
var source = new Configuration { Timeout = 30, Enabled = true };
var target = new Configuration();

// Copies property values from source to target
ReflectionExtensions.CopyPropertiesTo(source, target);
```

## Notes

*   **Performance:** Reflection operations are computationally expensive compared to direct code access. In performance-critical paths, consider caching `PropertyInfo` or `MethodInfo` results.
*   **Thread Safety:** The methods in this class are thread-safe, as they perform read-only operations on metadata or use thread-safe reflection APIs. However, the objects being manipulated by `SetPropertyValue` or `CopyPropertiesTo` are subject to standard C# object thread-safety rules.
*   **Exceptions:** Methods that rely on string-based lookups (e.g., `GetPropertyValue`) may throw exceptions if properties are renamed or do not exist. Always validate the existence of members or use `try-catch` blocks where appropriate.
