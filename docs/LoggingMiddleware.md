# LoggingMiddleware

`LoggingMiddleware` records structured request and response events for ASP.NET Core requests. It also measures downstream execution time, flags slow or unsuccessful responses, and records exceptions before rethrowing them.

## Constructor

```csharp
public LoggingMiddleware(
    RequestDelegate next,
    ILogger<LoggingMiddleware> logger)
```

- `next` is the next delegate in the HTTP request pipeline.
- `logger` receives the structured log events produced by the middleware.
- The constructor throws `ArgumentNullException` when either argument is `null`.

## `InvokeAsync`

```csharp
public Task InvokeAsync(HttpContext context)
```

For each request, `InvokeAsync`:

1. Starts a `Stopwatch` and reads the request trace identifier.
2. Logs the incoming HTTP method, path, request identifier, and remote IP address.
3. Replaces the response body stream with a temporary in-memory stream.
4. Invokes the next middleware in the pipeline.
5. Stops the timer, logs the response status and elapsed time, and copies the buffered response to the original response stream.
6. Restores the original response body stream in a `finally` block.

If downstream processing throws, the middleware stops the timer, logs the exception with request details, and rethrows the original exception. The original response stream is still restored.

## Log levels and fields

| Situation | Level | Logged values |
| --- | --- | --- |
| Every incoming request | `Information` | HTTP method, path, request ID, and remote IP address |
| Completed response taking more than 5000 ms | `Warning` | HTTP method, path, status code, and duration in milliseconds |
| Completed response with status code 400 or greater | `Warning` | HTTP method, path, status code, and duration in milliseconds |
| Other completed response | `Information` | HTTP method, path, status code, and duration in milliseconds |
| Exception from downstream processing | `Error` | Exception, HTTP method, path, request ID, and duration in milliseconds |

The slow-request comparison is strictly greater than `5000` milliseconds. A request that takes exactly `5000` ms is evaluated by its status code instead. Because the slow-response check occurs first, a response that is both slow and has a status code of 400 or greater produces the slow-response warning rather than the status-code warning.

The messages use structured logging placeholders, so logging providers can capture the values as searchable properties.

## Registration

Add the middleware to the ASP.NET Core request pipeline with `UseMiddleware`:

```csharp
using SqliteMultiTenant.Middleware;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseMiddleware<LoggingMiddleware>();

app.MapControllers();
app.Run();
```

Place it according to the scope that should be observed: it records requests and responses produced by middleware registered after it in the pipeline.
