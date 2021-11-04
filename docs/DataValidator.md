# DataValidator

The `DataValidator` class provides a fluent API for enforcing data integrity constraints on object properties and values. It allows developers to chain multiple validation rules, aggregating failures into a `ValidationResult` which indicates whether the data satisfies all defined requirements. This utility is intended for use in scenarios requiring consistent, declarative validation logic across an application.

## API

### DataValidator
- `public DataValidator()`: Initializes a new instance of the `DataValidator` class.

#### Validation Methods
All validation methods return the `DataValidator` instance to support fluent chaining.

- `public DataValidator RequireString(string fieldName, string value, int? minLength = null, int? maxLength = null)`: Validates that the provided string meets optional length constraints.
- `public DataValidator RequireRange<T>(string fieldName, T value, T min, T max) where T : IComparable<T>`: Validates that a numeric or comparable value falls within the specified range, inclusive.
- `public DataValidator RequireValidEmail(string fieldName, string value)`: Validates that the provided string is a correctly formatted email address.
- `public DataValidator RequireValidUrl(string fieldName, string value)`: Validates that the provided string is a correctly formatted URL.
- `public DataValidator RequireValidGuid(string fieldName, string value)`: Validates that the provided string can be parsed as a valid GUID.
- `public DataValidator RequirePattern(string fieldName, string value, string pattern, string message)`: Validates that the string matches the specified regular expression pattern.
- `public DataValidator Require<T>(string fieldName, T value, Func<T, bool> predicate, string message)`: Validates the value against a custom predicate function.
- `public DataValidator RequireNotEmpty<T>(string fieldName, T value)`: Validates that the value is not null, or for collections/strings, not empty.
- `public DataValidator RequireEqual<T>(string fieldName, T value, T expected)`: Validates that the value equals the expected value.
- `public DataValidator RequireNotEqual<T>(string fieldName, T value, T forbidden)`: Validates that the value does not equal the forbidden value.

#### Result Extraction
- `public ValidationResult GetResult()`: Returns the final `ValidationResult` containing all collected `ValidationError` instances.

### ValidationError
Represents an individual validation failure.
- `public string FieldName`: Gets the name of the field that failed validation.
- `public string Message`: Gets the descriptive message explaining the validation failure.
- `public ValidationError(string fieldName, string message)`: Initializes a new instance of the `ValidationError` class.
- `public override string ToString()`: Returns a string representation of the validation error.

### ValidationResult
Represents the outcome of a validation operation.
- `public bool IsValid`: Gets a value indicating whether all validation rules passed.

## Usage

### Basic Property Validation
```csharp
var validator = new DataValidator();
var result = validator
    .RequireNotEmpty("Username", user.Username)
    .RequireString("Username", user.Username, minLength: 3, maxLength: 20)
    .RequireValidEmail("Email", user.Email)
    .GetResult();

if (!result.IsValid)
{
    // Handle validation failures
}
```

### Complex Conditional Validation
```csharp
public ValidationResult ValidateOrder(Order order)
{
    return new DataValidator()
        .RequireRange("Quantity", order.Quantity, 1, 100)
        .Require<string>("Category", order.Category, cat => cat == "Retail" || cat == "Wholesale", "Invalid Category.")
        .RequireNotEmpty("OrderId", order.OrderId)
        .GetResult();
}
```

## Notes

- **Thread Safety**: `DataValidator` instances maintain internal state (collected errors). They are not thread-safe and should not be shared across threads. Create a new `DataValidator` instance for each validation operation.
- **Validation Order**: Rules are executed in the order they are chained. If multiple rules are applied to the same field, all applicable rules will be evaluated and failures aggregated.
- **Null Handling**: Many `Require` methods will implicitly fail if a required value is null; check specific method documentation regarding nullability requirements.
