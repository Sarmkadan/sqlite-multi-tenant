# EnumExtensions

The `EnumExtensions` class provides a collection of static utility methods designed to simplify the manipulation of, and metadata retrieval from, C# enumeration types. By abstracting reflection-based operations, these methods enable developers to easily access attributes, validate values, and perform safe parsing of enumeration members, enhancing code readability and maintainability within the sqlite-multi-tenant project.

## API

### GetDisplayName\<T>(T value)
Retrieves the display name associated with an enum value, typically sourced from a `DisplayAttribute`.
*   **Parameters**: `value` (The enum value).
*   **Returns**: The display name as a string, or the string representation of the enum value if no attribute is found.

### ParseSafe\<T>(string value)
Attempts to parse a string into the specified enumeration value of type `T`.
*   **Parameters**: `value` (The string representation to parse).
*   **Returns**: The parsed enumeration value of type `T`.
*   **Throws**: Throws `ArgumentException` if the string cannot be mapped to a defined value in the enum.

### HasAttribute\<T, TAttribute>(T value)
Determines whether the specified enum value is decorated with a particular attribute.
*   **Parameters**: `value` (The enum value), `TAttribute` (The attribute type to check).
*   **Returns**: `true` if the attribute exists; otherwise, `false`.

### GetAttribute\<T, TAttribute>(T value)
Retrieves a specific attribute instance applied to the provided enum value.
*   **Parameters**: `value` (The enum value), `TAttribute` (The attribute type to retrieve).
*   **Returns**: The attribute instance of type `TAttribute`.
*   **Throws**: Throws `InvalidOperationException` if the attribute is not found.

### GetAllValues\<T>()
Retrieves all defined values for the specified enumeration type.
*   **Returns**: An `IEnumerable\<T>` containing every defined value within the enum.

### IsValidEnumValue\<T>(T value)
Validates whether the provided value is a defined member of the enumeration type `T`.
*   **Parameters**: `value` (The value to validate).
*   **Returns**: `true` if the value is defined; otherwise, `false`.

### GetDescription\<T>(T value)
Retrieves the description associated with an enum value, typically sourced from a `DescriptionAttribute`.
*   **Parameters**: `value` (The enum value).
*   **Returns**: The description as a string, or the string representation of the enum value if no attribute is found.

## Usage

```csharp
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

public enum TenantStatus
{
    [Display(Name = "Active Tenant")]
    [Description("The tenant is currently operational.")]
    Active,
    [Display(Name = "Suspended Tenant")]
    Suspended
}

// Example 1: Retrieving Metadata
var status = TenantStatus.Active;
string displayName = EnumExtensions.GetDisplayName(status); // "Active Tenant"
string description = EnumExtensions.GetDescription(status); // "The tenant is currently operational."

// Example 2: Parsing and Validation
string input = "Suspended";
if (EnumExtensions.IsValidEnumValue<TenantStatus>(TenantStatus.Suspended))
{
    TenantStatus parsedStatus = EnumExtensions.ParseSafe<TenantStatus>(input);
    // Proceed with parsedStatus
}
```

## Notes

*   **Reflection Overhead**: These methods rely on .NET reflection to inspect enum members and their attributes. While convenient, frequent calls within performance-critical, tight loops should be avoided to minimize impact.
*   **Thread Safety**: The methods in `EnumExtensions` are stateless and inherently thread-safe, as they do not modify any shared state or fields.
*   **Enum Constraints**: These methods rely on generic constraints, ensuring that the type `T` is treated as an enumeration where applicable.
*   **Attribute Availability**: Methods interacting with attributes (`GetDisplayName`, `GetDescription`, `GetAttribute`) assume the target enum values are decorated with the standard `System.ComponentModel` or `System.ComponentModel.DataAnnotations` attributes to function as expected.
