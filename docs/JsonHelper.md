# JsonHelper

The `JsonHelper` class provides a centralized set of static utility methods for performing essential JSON serialization, deserialization, and manipulation tasks within the `sqlite-multi-tenant` project. By abstracting the underlying JSON processing implementation, this helper ensures consistent behavior, standard formatting, and unified error handling across the application when managing JSON data.

## API

### Serialize&lt;T&gt;
Converts a strongly-typed object into its JSON string representation.
*   **Parameters:** `T value` - The object to serialize.
*   **Returns:** A `string` containing the JSON representation.
*   **Throws:** Throws an exception if serialization fails due to invalid object structure or type limitations.

### Deserialize&lt;T&gt;
Converts a JSON string into a strongly-typed object of type `T`.
*   **Parameters:** `string json` - The JSON string to deserialize.
*   **Returns:** An instance of `T` populated with the data from the JSON string.
*   **Throws:** Throws an exception if the JSON string is malformed or incompatible with the structure of `T`.

### DeserializeDynamic
Converts a JSON string into a `dynamic` object, allowing for property access without a predefined class structure.
*   **Parameters:** `string json` - The JSON string to deserialize.
*   **Returns:** A `dynamic` object representing the JSON data.
*   **Throws:** Throws an exception if the JSON string is malformed.

### MergeJson
Merges two JSON strings into a single JSON object. If duplicate keys exist, the values from the second JSON string typically overwrite the values from the first.
*   **Parameters:** `string json1` - The base JSON string. `string json2` - The JSON string to merge into the base.
*   **Returns:** A new JSON `string` containing the merged properties.
*   **Throws:** Throws an exception if either input string is not valid JSON or if merging is not possible.

### GetProperty&lt;T&gt;
Extracts the value of a specific property from a JSON string.
*   **Parameters:** `string json` - The JSON string. `string propertyName` - The name of the property to extract.
*   **Returns:** The value of the property cast to type `T`.
*   **Throws:** Throws an exception if the property is not found, if the path is invalid, or if the value cannot be cast to type `T`.

### IsValidJson
Validates whether a provided string is a properly formatted JSON string.
*   **Parameters:** `string json` - The string to validate.
*   **Returns:** `bool` - `true` if the string is valid JSON; otherwise, `false`.

### DeepClone&lt;T&gt;
Creates a complete deep copy of an object by serializing it to JSON and then deserializing it back into a new instance.
*   **Parameters:** `T obj` - The object to clone.
*   **Returns:** A new instance of `T` that is a deep copy of the original object.
*   **Throws:** Throws an exception if the object cannot be serialized or deserialized.

### PrettyPrint
Formats a JSON string by adding indentation and newlines to improve human readability.
*   **Parameters:** `string json` - The JSON string to format.
*   **Returns:** A `string` containing the formatted JSON.
*   **Throws:** Throws an exception if the input string is not valid JSON.

### Minify
Removes all unnecessary whitespace, including newlines, from a JSON string to create a compact representation.
*   **Parameters:** `string json` - The JSON string to minify.
*   **Returns:** A minified JSON `string`.
*   **Throws:** Throws an exception if the input string is not valid JSON.

## Usage

```csharp
// Example 1: Basic Serialization and Deserialization
var user = new { Name = "John Doe", Id = 123 };
string jsonString = JsonHelper.Serialize(user);

// Deserialize back to a specific type
var deserializedUser = JsonHelper.Deserialize<dynamic>(jsonString);
Console.WriteLine(deserializedUser.Name); // Outputs: John Doe
```

```csharp
// Example 2: Pretty printing and merging
string json1 = "{\"Name\": \"App\"}";
string json2 = "{\"Version\": \"1.0\"}";

string merged = JsonHelper.MergeJson(json1, json2);
string prettyMerged = JsonHelper.PrettyPrint(merged);

Console.WriteLine(prettyMerged);
/* 
Outputs:
{
  "Name": "App",
  "Version": "1.0"
}
*/
```

## Notes

*   **Thread Safety:** The methods in `JsonHelper` are static and stateless. They are inherently thread-safe, provided the underlying JSON library being wrapped is thread-safe (as is common with modern C# JSON libraries).
*   **Error Handling:** All methods assume valid input strings where appropriate. Passing `null` or invalid JSON strings to methods expecting valid JSON will typically result in an exception. It is recommended to use `IsValidJson` before performing operations on untrusted input strings.
*   **Performance:** While `DeepClone<T>` is convenient for creating copies, it is not the most performant method for cloning complex objects due to the overhead of string serialization and deserialization. Use it only when necessary.
