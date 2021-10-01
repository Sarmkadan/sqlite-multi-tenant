# CorrelationIdMiddleware

A middleware component for ASP.NET Core applications that generates and propagates a unique correlation identifier through the request pipeline, enabling end-to-end request tracing and log correlation across services.

## API

### `public CorrelationIdMiddleware`

Initializes a new instance of the `CorrelationIdMiddleware` class.

### `public async Task InvokeAsync(HttpContext context, RequestDelegate next)`

Invokes the middleware to process the HTTP request.

- **Parameters**
  - `context`: The `HttpContext` for the current request.
  - `next`: The delegate representing the next middleware in the pipeline.
- **Return value**: A `Task` representing the asynchronous operation.
- **Exceptions**: May throw if the `HttpContext` or `RequestDelegate` is `null`.

### `public static IApplicationBuilder UseCorrelationId(IApplicationBuilder app)`

Adds the `CorrelationIdMiddleware` to the specified `IApplicationBuilder` pipeline.

- **Parameters**
  - `app`: The `IApplicationBuilder` instance.
- **Return value**: The `IApplicationBuilder` instance for method chaining.
- **Exceptions**: Throws `ArgumentNullException` if `app` is `null`.

### `public static string GetCorrelationId(HttpContext context)`

Retrieves the correlation identifier associated with the current HTTP request.

- **Parameters**
  - `context`: The `HttpContext` for the current request.
- **Return value**: The correlation identifier as a string, or `null` if not set.
- **Exceptions**: None.

## Usage

### Basic Setup in `Startup.cs`
