# ValidationRuleBuilder

`ValidationRuleBuilder<T>` provides a fluent API for constructing and executing validation rules for properties within a data object of type `T`. It enables the creation of composable constraints—including mandatory field checks, string length restrictions, email format validation, numerical range bounds, regular expression pattern matching, and custom predicate-based logic—encapsulating validation execution and outcomes into a structured `RuleValidationResult`.

## API

### ValidationRuleBuilder<T>

A sealed class that acts as the entry point for defining validation rules for a specific field in a model of type `T`.

*   **`public ValidationRuleBuilder()`**: Initializes a new instance of the `ValidationRuleBuilder<T>` class.
*   **`public ValidationRuleBuilder<T> Required()`**: Adds a rule ensuring the field is not null or empty.
*   **`public ValidationRuleBuilder<T> StringLength(...)`**: Adds a rule to validate the string length of the field within specified bounds.
*   **`public ValidationRuleBuilder<T> Email()`**: Adds a rule validating that the field value conforms to standard email format requirements.
*   **`public ValidationRuleBuilder<T> Range(...)`**: Adds a rule to validate that the field value falls within a specific numerical range.
*   **`public ValidationRuleBuilder<T> Pattern(...)`**: Adds a rule to validate the field against a specified regular expression pattern.
*   **`public ValidationRuleBuilder<T> Custom(...)`**: Adds a rule based on a user-provided predicate function.
*   **`public ValidationRuleBuilder<T> MustMatch(...)`**: Adds a rule to ensure the field matches another specified value or condition.
*   **`public RuleValidationResult Validate(T instance)`**: Executes all configured rules against the provided `instance`. Returns a `RuleValidationResult` detailing the outcome.
*   **`public string FieldName`**: Gets or sets the name of the field being validated.
*   **`public Func<object, bool> Predicate`**: Gets or sets the predicate function used for rule evaluation.
*   **`public string ErrorMessage`**: Gets or sets the message to be returned if validation fails.

### RuleValidationResult

A sealed class representing the outcome of a validation operation.

*   **`public bool IsValid`**: Indicates whether the validation passed (`true`) or failed (`false`).
*   **`public List<RuleValidationError> Errors`**: A collection of `RuleValidationError` objects containing details for each failed rule.

### RuleValidationError

A sealed class representing a specific failure within a validation operation.

*   **`public string FieldName`**: The name of the field that failed validation.
*   **`public string Message`**: The error message associated with the failure.

## Usage

### Fluent Validation Configuration
```csharp
var builder = new ValidationRuleBuilder<User>()
    .Required()
    .StringLength(min: 3, max: 50)
    .Email();

var result = builder.Validate(userInstance);

if (!result.IsValid)
{
    foreach (var error in result.Errors)
    {
        Console.WriteLine($"{error.FieldName}: {error.Message}");
    }
}
```

### Custom Predicate Validation
```csharp
var ageBuilder = new ValidationRuleBuilder<User>()
    .Custom(user => ((User)user).Age >= 18);

var result = ageBuilder.Validate(userInstance);
```

## Notes

*   **Thread Safety**: The `ValidationRuleBuilder` is designed as a builder pattern for configuring rules. While the configuration phase (`Required`, `StringLength`, etc.) is typically intended for single-threaded initialization, the `Validate` method is intended to be side-effect free and thread-safe when called on a configured builder instance.
*   **Null Handling**: Passing a `null` instance to the `Validate` method may result in an `ArgumentNullException` depending on the underlying implementation of the specific rules added to the builder. Ensure the model instance is instantiated before validation.
*   **Rule Ordering**: Rules are evaluated in the order they are added to the builder. The validation process may short-circuit depending on the implementation; do not rely on side-effects occurring within custom predicate functions if validation fails early.
