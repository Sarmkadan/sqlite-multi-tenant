# StringExtensions

`StringExtensions` provides a collection of utility methods for string manipulation, validation, and conversion within the `sqlite-multi-tenant` project. These methods address common needs such as sanitizing identifiers for database operations, truncating strings safely, validating tenant identifiers and file paths, normalizing whitespace, escaping strings for JSON output, and performing lightweight enum parsing.

## API

### ToSafeDatabaseIdentifier
```csharp
public static string ToSafeDatabaseIdentifier(this string input)
```
Transforms an arbitrary string into a safe identifier suitable for use as a database object name (e.g., table or column name). It removes or replaces characters that are invalid in SQLite identifiers, collapses whitespace, and ensures the result conforms to identifier rules.  
**Parameters:** `input` — the source string to sanitize.  
**Returns:** A sanitized string containing only characters valid in a database identifier.  
**Throws:** `ArgumentNullException` if `input` is `null`.

### SafeTruncate
```csharp
public static string SafeTruncate(this string value, int maxLength)
```
Truncates a string to the specified maximum length without breaking Unicode surrogate pairs or combining character sequences. If truncation is necessary, the result ends at the last complete grapheme cluster boundary within the limit.  
**Parameters:** `value` — the string to truncate; `maxLength` — the maximum allowed length in characters.  
**Returns:** The original string if its length does not exceed `maxLength`; otherwise, a truncated string ending on a safe boundary.  
**Throws:** `ArgumentNullException` if `value` is `null`. `ArgumentOutOfRangeException` if `maxLength` is negative.

### IsValidTenantIdentifier
```csharp
public static bool IsValidTenantIdentifier(this string identifier)
```
Determines whether a string qualifies as a valid tenant identifier according to the multi-tenancy rules of the system. Typically enforces length limits, allowed character sets, and disallows leading/trailing whitespace or reserved words.  
**Parameters:** `identifier` — the candidate tenant identifier.  
**Returns:** `true` if the string is a valid tenant identifier; otherwise `false`.  
**Throws:** Does not throw (returns `false` for `null` input).

### ToEnum\<T\>
```csharp
public static T ToEnum<T>(this string value, bool ignoreCase = true) where T : struct, Enum
```
Parses a string into an enum value of type `T`. Supports case-insensitive matching by default and falls back to numeric parsing if the string represents a valid underlying integer value.  
**Parameters:** `value` — the string to parse; `ignoreCase` — whether to perform case-insensitive name matching (default `true`).  
**Returns:** The enum constant corresponding to the parsed value.  
**Throws:** `ArgumentNullException` if `value` is `null`. `ArgumentException` if `value` does not match any enum name or numeric representation.

### EscapeForJson
```csharp
public static string EscapeForJson(this string raw)
```
Escapes a string for safe inclusion in a JSON value by backslash-escaping control characters, quotation marks, and reverse solidus characters. Does not wrap the result in quotes; it produces the inner literal content.  
**Parameters:** `raw` — the unescaped string.  
**Returns:** The escaped string suitable for embedding in a JSON string literal.  
**Throws:** `ArgumentNullException` if `raw` is `null`.

### ContainsForbiddenCharacters
```csharp
public static bool ContainsForbiddenCharacters(this string input, char[] forbiddenCharacters)
```
Checks whether a string contains any character from a specified set of forbidden characters. The comparison is ordinal and case-sensitive.  
**Parameters:** `input` — the string to inspect; `forbiddenCharacters` — an array of prohibited characters.  
**Returns:** `true` if any forbidden character is present; otherwise `false`.  
**Throws:** `ArgumentNullException` if `input` or `forbiddenCharacters` is `null`.

### NormalizeWhitespace
```csharp
public static string NormalizeWhitespace(this string input)
```
Replaces all sequences of whitespace characters (spaces, tabs, line breaks) with a single space, and trims leading and trailing whitespace. Preserves non-breaking spaces and other Unicode whitespace variants by collapsing them into a standard space.  
**Parameters:** `input` — the string to normalize.  
**Returns:** A whitespace-normalized string with no leading/trailing whitespace and no consecutive whitespace runs.  
**Throws:** `ArgumentNullException` if `input` is `null`.

### IsValidFilePath
```csharp
public static bool IsValidFilePath(this string path)
```
Validates whether a string represents a well-formed file path for the current operating system. Checks for invalid characters, reserved device names, and proper root or drive specification where applicable. Does not verify that the path actually exists on disk.  
**Parameters:** `path` — the candidate file path.  
**Returns:** `true` if the path is syntactically valid; otherwise `false`.  
**Throws:** Does not throw (returns `false` for `null` input).

### Reverse
```csharp
public static string Reverse(this string s)
```
Reverses the order of characters in a string, preserving Unicode surrogate pairs and combining character sequences so that grapheme clusters remain intact.  
**Parameters:** `s` — the string to reverse.  
**Returns:** A new string with characters in reverse order.  
**Throws:** `ArgumentNullException` if `s` is `null`.

## Usage

### Example 1: Sanitizing a Tenant-Provided Name for a Database Table
```csharp
string rawTenantName = "Acme Corp! (2025)";
string safeName = rawTenantName.ToSafeDatabaseIdentifier();
// safeName might be "Acme_Corp_2025"

if (safeName.IsValidTenantIdentifier())
{
    string createTableSql = $"CREATE TABLE tenant_{safeName} (id INTEGER PRIMARY KEY);";
    // Execute against the tenant's database connection
}
```

### Example 2: Truncating and Escaping User Input for a JSON Log Entry
```csharp
string userComment = "This is a very long comment that exceeds the storage limit...";
string truncated = userComment.SafeTruncate(200);
string normalized = truncated.NormalizeWhitespace();
string escaped = normalized.EscapeForJson();

string jsonLog = $"{{\"comment\": \"{escaped}\"}}";
// Write jsonLog to audit file
```

## Notes

- All methods treat `null` input consistently: validation predicates (`IsValidTenantIdentifier`, `IsValidFilePath`) return `false` without throwing; transformation methods throw `ArgumentNullException`.
- `SafeTruncate` and `Reverse` are grapheme-cluster-aware, making them safe for strings containing emoji, accented characters, or scripts that use combining marks. They do not split surrogate pairs.
- `ToEnum<T>` uses `Enum.TryParse` internally with the specified case sensitivity. When `ignoreCase` is `true`, input like `"active"` matches `Status.Active`. Numeric strings (e.g., `"2"`) parse to the underlying enum integer value if defined.
- `EscapeForJson` escapes only the minimal set of characters required by the JSON specification (U+0022, U+005C, and control characters U+0000–U+001F). It does not perform HTML encoding or any other escaping.
- `NormalizeWhitespace` collapses all Unicode whitespace categories (including non-breaking spaces) into a single U+0020 space. If preservation of non-breaking spaces is required, pre-process the string before calling this method.
- `IsValidFilePath` performs syntactic validation only. It does not access the file system, so paths exceeding the OS maximum length or containing valid-but-nonexistent directory components still return `true`.
- All methods are stateless and thread-safe. They operate purely on their input arguments without shared mutable state.
