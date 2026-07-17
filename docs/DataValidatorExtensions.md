# DataValidatorExtensions

`DataValidatorExtensions` provides a collection of fluent extension methods that augment the `DataValidator` type with common validation rules. These helpers enable concise, readable validation pipelines for strings, collections, numeric values, dates, network identifiers, and credit‑card numbers, returning the original `DataValidator` to allow method chaining.

## API

| Member | Purpose | Parameters | Return Value | Exceptions |
|--------|---------|------------|--------------|------------|
| `RequireString` | Ensures the target value is a non‑null, non‑empty string. | `this DataValidator validator, string value, string fieldName` | `DataValidator` – the same validator instance for chaining. | Throws `ArgumentException` if `value` is `null` or empty. |
| `RequireMinLength` | Validates that a string meets a minimum length requirement. | `this DataValidator validator, string value, int minLength, string fieldName` | `DataValidator` | Throws `ArgumentException` when `value.Length < minLength`. |
| `RequireLengthBetween` | Checks that a string’s length falls within an inclusive range. | `this DataValidator validator, string value, int minLength, int maxLength, string fieldName` | `DataValidator` | Throws `ArgumentException` if the length is outside the specified bounds. |
| `RequireValidPhoneNumber` | Validates that a string conforms to a standard phone‑number pattern. | `this DataValidator validator, string phoneNumber, string fieldName` | `DataValidator` | Throws `FormatException` when the pattern does not match a recognized phone format. |
| `RequireValidDate` | Ensures a string can be parsed as a `DateOnly` (or `DateTime` with no time component). | `this DataValidator validator, string dateString, string fieldName` | `DataValidator` | Throws `FormatException` if parsing fails. |
| `RequireValidDateTime` | Validates that a string can be parsed into a `DateTime`. | `this DataValidator validator, string dateTimeString, string fieldName` | `DataValidator` | Throws `FormatException` on parse failure. |
| `RequireValidIPv4` | Checks that a string represents a syntactically valid IPv4 address. | `this DataValidator validator, string ipAddress, string fieldName` | `DataValidator` | Throws `FormatException` when the address is not a valid IPv4 format. |
| `RequireCollectionCount<T>` | Validates that a collection contains exactly a specified number of items. | `this DataValidator validator, IEnumerable<T> collection, int expectedCount, string fieldName` | `DataValidator` | Throws `ArgumentException` if the collection count differs from `expectedCount`. |
| `RequireMaxItems<T>` | Ensures a collection does not exceed a maximum item count. | `this DataValidator validator, IEnumerable<T> collection, int maxCount, string fieldName` | `DataValidator` | Throws `ArgumentException` when `collection.Count() > maxCount`. |
| `RequireGreaterThan<T>` | Validates that a comparable value is greater than a supplied threshold. | `this DataValidator validator, T value, T threshold, string fieldName` where `T : IComparable<T>` | `DataValidator` | Throws `ArgumentException` if `value.CompareTo(threshold) <= 0`. |
| `RequireLessThan<T>` | Validates that a comparable value is less than a supplied threshold. | `this DataValidator validator, T value, T threshold, string fieldName` where `T : IComparable<T>` | `DataValidator` | Throws `ArgumentException` if `value.CompareTo(threshold) >= 0`. |
| `RequireValidCreditCard` | Checks that a string passes the Luhn algorithm for credit‑card numbers. | `this DataValidator validator, string creditCardNumber, string fieldName` | `DataValidator` | Throws `FormatException` when the number fails the Luhn check or contains invalid characters. |

## Usage

### Example 1 – Validating a user registration payload

