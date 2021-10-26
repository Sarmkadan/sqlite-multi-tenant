# StringUtilities

`StringUtilities` is a static utility class within the `sqlite-multi-tenant` project that provides a collection of string manipulation, validation, hashing, and sanitization methods. It serves as a central helper for common text-processing tasks encountered in multi-tenant data pipelines, such as formatting identifiers, cleaning user input, and computing content fingerprints.

## API

### `public static string ComputeSha256Hash(string input)`
Computes the SHA-256 hash of the given input string and returns it as a lowercase hexadecimal string.
- **Parameters**: `input` — the string to hash. Can be `null` or empty.
- **Returns**: a 64-character lowercase hex string, or `string.Empty` if `input` is `null` or empty.
- **Throws**: `ObjectDisposedException` if the underlying cryptographic provider has been disposed (extremely rare in practice for static usage).

### `public static string ComputeMd5Hash(string input)`
Computes the MD5 hash of the given input string and returns it as a lowercase hexadecimal string.
- **Parameters**: `input` — the string to hash. Can be `null` or empty.
- **Returns**: a 32-character lowercase hex string, or `string.Empty` if `input` is `null` or empty.
- **Throws**: `ObjectDisposedException` under the same conditions as `ComputeSha256Hash`.

### `public static string TruncateWithEllipsis(string value, int maxLength)`
Truncates a string to the specified maximum length, appending an ellipsis (`…`) if truncation occurred.
- **Parameters**: `value` — the string to truncate; `maxLength` — the maximum allowed length, must be ≥ 3.
- **Returns**: the original string if its length ≤ `maxLength`; otherwise a truncated string ending with `…` whose total length equals `maxLength`.
- **Throws**: `ArgumentOutOfRangeException` when `maxLength` is less than 3.

### `public static string ToTitleCase(string input)`
Converts the input string to title case using the current culture’s casing rules.
- **Parameters**: `input` — the string to convert.
- **Returns**: a title-cased string. Words entirely in uppercase are treated as acronyms and left unchanged per standard .NET behavior.
- **Throws**: `ArgumentNullException` when `input` is `null`.

### `public static string ToSnakeCase(string input)`
Converts a PascalCase, camelCase, or space-delimited string to snake_case (lowercase with underscores).
- **Parameters**: `input` — the string to convert.
- **Returns**: the snake_case representation. Inserts underscores before uppercase letters and replaces whitespace with underscores.
- **Throws**: `ArgumentNullException` when `input` is `null`.

### `public static string ToCamelCase(string input)`
Converts a PascalCase, snake_case, or space-delimited string to camelCase.
- **Parameters**: `input` — the string to convert.
- **Returns**: the camelCase representation with the first character lowercased.
- **Throws**: `ArgumentNullException` when `input` is `null`.

### `public static string RemoveWhitespace(string input)`
Removes all whitespace characters from the input string.
- **Parameters**: `input` — the string to process.
- **Returns**: a new string with all whitespace characters (spaces, tabs, newlines, etc.) removed. Returns `string.Empty` if `input` is `null` or empty.
- **Throws**: none.

### `public static string SanitizeForFilePath(string input)`
Replaces or removes characters that are invalid in file paths on the current operating system.
- **Parameters**: `input` — the raw string to sanitize.
- **Returns**: a string safe for use as a file or directory name. Invalid characters are replaced with an underscore or removed depending on the platform.
- **Throws**: `ArgumentNullException` when `input` is `null`.

### `public static string SanitizeForHtml(string input)`
Encodes HTML-sensitive characters to their corresponding HTML entities to prevent injection.
- **Parameters**: `input` — the raw string to sanitize.
- **Returns**: an HTML-encoded string where `<`, `>`, `&`, `"`, and `'` are replaced with entities.
- **Throws**: `ArgumentNullException` when `input` is `null`.

### `public static bool IsValidEmail(string email)`
Validates whether a string conforms to a standard email address format.
- **Parameters**: `email` — the string to validate.
- **Returns**: `true` if the string matches a recognized email pattern; otherwise `false`. Returns `false` for `null` or empty input.
- **Throws**: none.

### `public static bool IsValidUrl(string url)`
Validates whether a string represents a well-formed absolute URL.
- **Parameters**: `url` — the string to validate.
- **Returns**: `true` if the string is a valid absolute URI with an HTTP or HTTPS scheme; otherwise `false`. Returns `false` for `null` or empty input.
- **Throws**: none.

### `public static bool IsValidGuid(string input)`
Determines whether a string can be parsed as a GUID.
- **Parameters**: `input` — the string to test.
- **Returns**: `true` if the string represents a valid GUID in any of the standard formats (D, N, B, P, X); otherwise `false`. Returns `false` for `null` or empty input.
- **Throws**: none.

### `public static string GenerateRandomString(int length, string allowedChars = null)`
Generates a cryptographically random string of the specified length using the given character set.
- **Parameters**: `length` — the desired string length; `allowedChars` — optional set of characters to draw from. Defaults to alphanumeric characters (A-Z, a-z, 0-9) if `null`.
- **Returns**: a random string of exactly `length` characters.
- **Throws**: `ArgumentOutOfRangeException` when `length` is negative; `ArgumentException` when `allowedChars` is empty.

### `public static string Repeat(string value, int count)`
Repeats a string a specified number of times.
- **Parameters**: `value` — the string to repeat; `count` — the number of repetitions.
- **Returns**: a concatenated string containing `value` repeated `count` times. Returns `string.Empty` if `count` is 0 or `value` is `null`/empty.
- **Throws**: `ArgumentOutOfRangeException` when `count` is negative.

### `public static IEnumerable<string> SplitPreservingQuotes(string input, char separator = ',')`
Splits a string by a separator while respecting quoted substrings (single or double quotes). Quoted segments are returned with the quotes removed.
- **Parameters**: `input` — the string to split; `separator` — the delimiter character, defaults to comma.
- **Returns**: an enumerable of trimmed segments. Quoted segments have their surrounding quotes stripped.
- **Throws**: `ArgumentNullException` when `input` is `null`.

### `public static double GetStringSimilarity(string first, string second)`
Computes a similarity score between two strings using the Levenshtein distance algorithm, normalized to a 0.0–1.0 range.
- **Parameters**: `first` — the first string; `second` — the second string.
- **Returns**: a value between 0.0 (completely dissimilar) and 1.0 (identical). Returns 1.0 if both strings are `null` or empty; returns 0.0 if one is `null`/empty and the other is not.
- **Throws**: none.

## Usage

### Example 1: Sanitizing and Validating User Input for Tenant Registration
```csharp
string rawName = "  Acme Corp (NYC)  ";
string rawEmail = "contact@acme-corp.com";

// Clean and format the tenant name for a directory folder
string folderName = StringUtilities.SanitizeForFilePath(rawName);
folderName = StringUtilities.ToSnakeCase(folderName);
// Result: "acme_corp_nyc"

// Validate the contact email before storing
if (!StringUtilities.IsValidEmail(rawEmail))
{
    throw new ArgumentException("Invalid email address provided.");
}

// Generate a unique tenant identifier
string tenantId = StringUtilities.GenerateRandomString(12);
// Example output: "aB3xK9mQ2wL7"
```

### Example 2: Processing CSV Data with Quoted Fields and Deduplication
```csharp
string csvLine = "\"Doe, John\", jane.doe@example.com, \"123 Main St, Apt 4\"";
IEnumerable<string> fields = StringUtilities.SplitPreservingQuotes(csvLine);

foreach (string field in fields)
{
    string sanitized = StringUtilities.SanitizeForHtml(field);
    Console.WriteLine(sanitized);
}

// Compute a content fingerprint for deduplication
string content = "Lorem ipsum dolor sit amet";
string hash = StringUtilities.ComputeSha256Hash(content);

// Check similarity between two entries to detect near-duplicates
double similarity = StringUtilities.GetStringSimilarity(
    "Acme Corporation",
    "Acme Corp."
);
if (similarity > 0.85)
{
    Console.WriteLine("Potential duplicate detected.");
}
```

## Notes

- **Thread Safety**: All methods are static and operate on immutable string inputs without shared mutable state. They are safe to call concurrently from multiple threads.
- **Null Handling**: Validation methods (`IsValidEmail`, `IsValidUrl`, `IsValidGuid`) return `false` for `null` input rather than throwing. Transformation methods (`ToTitleCase`, `ToSnakeCase`, `ToCamelCase`, `SanitizeForFilePath`, `SanitizeForHtml`, `SplitPreservingQuotes`) throw `ArgumentNullException` on `null` input. `RemoveWhitespace`, `Repeat`, and `GetStringSimilarity` handle `null` gracefully without throwing.
- **Hashing Edge Cases**: `ComputeSha256Hash` and `ComputeMd5Hash` return `string.Empty` for `null` or empty input, distinguishing them from the hash of an empty string (which would be a valid hex digest). Callers should treat empty-string results as a no-input signal.
- **Truncation Constraints**: `TruncateWithEllipsis` requires `maxLength ≥ 3` to accommodate the ellipsis character itself. Shorter limits will throw.
- **Case Conversion Locale**: `ToTitleCase` uses the current culture, which may produce different results across environments (e.g., Turkish İ). For invariant results, callers should set the desired culture explicitly before invoking.
- **Similarity Precision**: `GetStringSimilarity` normalizes Levenshtein distance by the length of the longer string. Very short strings may produce coarse similarity values; the method is best suited for strings of at least a few characters.
- **Random String Security**: `GenerateRandomString` uses `RNGCryptoServiceProvider` or its modern equivalent, making it suitable for tokens and identifiers that must be unpredictable.
