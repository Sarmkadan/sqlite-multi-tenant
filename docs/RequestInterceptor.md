# RequestInterceptor

Documentation for the request interception pipeline in `src/Api/Interceptors/RequestInterceptor.cs`.

## Overview

The request interception pipeline provides a mechanism to cross-cut concerns such as tenant context extraction, request validation, and correlation ID generation before controller execution. The pipeline consists of multiple interceptors that implement the `IRequestInterceptor` interface.

## Pipeline Position

Interceptors are registered via the `InterceptorPipeline` class, which is typically configured in the application's startup pipeline (e.g., `Program.cs` or `Startup.cs`). The pipeline executes request interceptors in registration order and response interceptors in reverse order.

## Relation to IRequestInterceptor

All interceptors implement the `IRequestInterceptor` interface, which is documented in [IRequestInterceptor.md](./IRequestInterceptor.md). This interface defines the contract for request and response interception.

## Public Types and Methods

### IRequestInterceptor

```csharp
public interface IRequestInterceptor
{
    Task<bool> OnRequestAsync(HttpContext context);
    Task OnResponseAsync(HttpContext context);
}
```

Defines the contract for request interceptors:
- `OnRequestAsync`: Processes the request before controller execution. Returns `false` to short-circuit the pipeline.
- `OnResponseAsync`: Processes the response after controller execution.

### TenantContextInterceptor

```csharp
public sealed class TenantContextInterceptor : IRequestInterceptor
{
    public TenantContextInterceptor(ILogger<TenantContextInterceptor> logger);
    public Task<bool> OnRequestAsync(HttpContext context);
    public Task OnResponseAsync(HttpContext context);
}
```

Extracts tenant ID from the `X-Tenant-Id` header and stores it in `HttpContext.Items`. Adds the tenant ID to response headers.

#### TenantContext

```csharp
public sealed class TenantContext
{
    public string TenantId { get; set; }
    public DateTime ExtractedAt { get; set; } = DateTime.UtcNow;
}
```

Holds tenant context information extracted from the request.

### RequestValidationInterceptor

```csharp
public sealed class RequestValidationInterceptor : IRequestInterceptor
{
    public RequestValidationInterceptor(ILogger<RequestValidationInterceptor> logger);
    public Task<bool> OnRequestAsync(HttpContext context);
    public Task OnResponseAsync(HttpContext context);
}
```

Validates requests against business rules:
- Checks content-type for POST/PUT requests (must be `application/json` or `multipart/form-data`).
- Enforces maximum body size (10MB).

### CorrelationIdInterceptor

```csharp
public sealed class CorrelationIdInterceptor : IRequestInterceptor
{
    public CorrelationIdInterceptor(ILogger<CorrelationIdInterceptor> logger);
    public Task<bool> OnRequestAsync(HttpContext context);
    public Task OnResponseAsync(HttpContext context);
}
```

Extracts or generates a correlation ID for request tracing. Stores the ID in `HttpContext.Items` and adds it to response headers as `X-Correlation-Id`.

### InterceptorPipeline

```csharp
public sealed class InterceptorPipeline
{
    public InterceptorPipeline(ILogger<InterceptorPipeline> logger);
    public void Register(IRequestInterceptor interceptor);
    public Task<bool> ExecuteRequestInterceptorsAsync(HttpContext context);
    public Task ExecuteResponseInterceptorsAsync(HttpContext context);
}
```

Manages the execution order of interceptors:
- `Register`: Adds an interceptor to the pipeline.
- `ExecuteRequestInterceptorsAsync`: Executes all request interceptors in registration order. Stops on first failure.
- `ExecuteResponseInterceptorsAsync`: Executes all response interceptors in reverse registration order (LIFO for cleanup).

## Registration Example

```csharp
// In Program.cs or Startup.cs
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Register interceptors
builder.Services.AddSingleton<ILogger<TenantContextInterceptor>>();
builder.Services.AddSingleton<ILogger<RequestValidationInterceptor>>();
builder.Services.AddSingleton<ILogger<CorrelationIdInterceptor>>();
builder.Services.AddSingleton<ILogger<InterceptorPipeline>>();

// Build the app
var app = builder.Build();

// Get the interceptor pipeline from DI
var pipeline = app.Services.GetRequiredService<InterceptorPipeline>();

// Register interceptors in desired execution order
pipeline.Register(app.Services.GetRequiredService<TenantContextInterceptor>());
pipeline.Register(app.Services.GetRequiredService<RequestValidationInterceptor>());
pipeline.Register(app.Services.GetRequiredService<CorrelationIdInterceptor>);

// Use the pipeline as middleware
app.Use(async (context, next) =>
{
    if (await pipeline.ExecuteRequestInterceptorsAsync(context))
    {
        await next();
        await pipeline.ExecuteResponseInterceptorsAsync(context);
    }
    else
    {
        context.Response.StatusCode = 400; // or 401, etc.
        await context.Response.WriteAsync("Request validation failed");
    }
});

app.MapControllers();
app.Run();
```

## Notes

- Interceptors should be registered in the order they should execute for request processing.
- Response processing executes in reverse order (last registered interceptor runs first for response).
- Each interceptor should handle exceptions internally or allow them to be caught by the pipeline's error handling.
- The pipeline short-circuits on request interception failure (returns `false` from `OnRequestAsync`).