# ApiResponseBuilder

A builder class for constructing standardized API responses in .NET applications. It provides a fluent interface to compose HTTP-compliant responses with data payloads, status codes, messages, error collections, and metadata. Designed for consistency in multi-tenant applications where consistent response formatting is critical.

## API

### Constructors

#### `public ApiResponseBuilder()`
Initializes a new instance of the `ApiResponseBuilder<T>` class with default values:
- Status code: `200 OK`
- Message: `null`
- Data: `default(T)`
- Errors: empty collection
- Metadata: empty dictionary

No parameters. Always succeeds.

---

### Fluent Configuration Methods

#### `public ApiResponseBuilder<T> WithData(T data)`
Sets the response payload.

- **Parameters**:
  - `data`: The payload to include in the response.
- **Return value**: The current builder instance for method chaining.
- **Throws**: `ArgumentNullException` if `data` is `null` and the type `T` is a reference type.

---

#### `public ApiResponseBuilder<T> WithStatusCode(HttpStatusCode statusCode)`
Sets the HTTP status code of the response.

- **Parameters**:
  - `statusCode`: The desired HTTP status code.
- **Return value**: The current builder instance for method chaining.
- **Throws**: `ArgumentOutOfRangeException` if `statusCode` is not a valid HTTP status code.

---

#### `public ApiResponseBuilder<T> WithMessage(string message)`
Sets a human-readable message describing the response.

- **Parameters**:
  - `message`: The message text.
- **Return value**: The current builder instance for method chaining.
- **Throws**: `ArgumentException` if `message` is empty or whitespace.

---

#### `public ApiResponseBuilder<T> AddError(string error)`
Adds a single error to the error collection.

- **Parameters**:
  - `error`: The error message to add.
- **Return value**: The current builder instance for method chaining.
- **Throws**: `ArgumentException` if `error` is empty or whitespace.

---

#### `public ApiResponseBuilder<T> AddErrors(IEnumerable<string> errors)`
Adds multiple errors to the error collection.

- **Parameters**:
  - `errors`: Collection of error messages to add.
- **Return value**: The current builder instance for method chaining.
- **Throws**: `ArgumentNullException` if `errors` is `null`.

---

#### `public ApiResponseBuilder<T> AddMetadata(string key, object value)`
Adds a key-value pair to the metadata dictionary.

- **Parameters**:
  - `key`: The metadata key.
  - `value`: The metadata value.
- **Return value**: The current builder instance for method chaining.
- **Throws**:
  - `ArgumentException` if `key` is empty or whitespace.
  - `ArgumentNullException` if `key` is `null`.

---

### State Mutators

#### `public ApiResponseBuilder<T> Success()`
Marks the response as successful. Equivalent to setting status code to `200 OK`.

- **Return value**: The current builder instance for method chaining.

---

#### `public ApiResponseBuilder<T> Failure()`
Marks the response as a failure. Equivalent to setting status code to `500 Internal Server Error`.

- **Return value**: The current builder instance for method chaining.

---

### Convenience HTTP Status Methods

Each method sets the status code and returns the builder for chaining:

- `public ApiResponseBuilder<T> Created()` → `201 Created`
- `public ApiResponseBuilder<T> Accepted()` → `202 Accepted`
- `public ApiResponseBuilder<T> NotFound()` → `404 Not Found`
- `public ApiResponseBuilder<T> Conflict()` → `409 Conflict`
- `public ApiResponseBuilder<T> Unauthorized()` → `401 Unauthorized`
- `public ApiResponseBuilder<T> Forbidden()` → `403 Forbidden`
- `public ApiResponseBuilder<T> ValidationError()` → `422 Unprocessable Entity`
- `public ApiResponseBuilder<T> ServerError()` → `500 Internal Server Error`
- `public ApiResponseBuilder<T> TooManyRequests()` → `429 Too Many Requests`

All methods return the current builder instance. No exceptions are thrown.

---

### Finalization

#### `public ApiResponse<T> Build()`
Constructs and returns a new `ApiResponse<T>` instance based on the current builder state.

- **Return value**: A new `ApiResponse<T>` instance.
- **Throws**: `InvalidOperationException` if no data has been set and the response is not an error state (i.e., status code ≥ 400).

## Usage

### Example 1: Successful Response with Data
