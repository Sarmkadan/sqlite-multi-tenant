#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.Logging;

namespace SqliteMultiTenant.Api.Interceptors;

/// <summary>
/// Request interceptor for preprocessing and validation before controller execution.
/// Implements cross-cutting concerns like tenant context extraction and authorization.
/// </summary>
public interface IRequestInterceptor
{
    Task<bool> OnRequestAsync(HttpContext context);
    Task OnResponseAsync(HttpContext context);
}

/// <summary>
/// Tenant context interceptor that extracts tenant ID from request.
/// Populates HttpContext.Items with tenant information for downstream use.
/// </summary>
public sealed class TenantContextInterceptor : IRequestInterceptor {
    private const string TenantIdHeader = "X-Tenant-Id";
    private const string TenantContextKey = "TenantContext";
    private readonly ILogger<TenantContextInterceptor> _logger;

    public TenantContextInterceptor(ILogger<TenantContextInterceptor> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Extracts tenant ID from request header and validates it.
    /// Returns false if tenant ID is missing or invalid (can be handled as 400 or 401).
    /// </summary>
    public Task<bool> OnRequestAsync(HttpContext context)
    {
        // Skip interceptor for health check endpoints
        if (context.Request.Path.StartsWithSegments("/health") ||
            context.Request.Path.StartsWithSegments("/status"))
            return Task.FromResult(true);

        var tenantId = context.Request.Headers[TenantIdHeader].ToString();

        if (string.IsNullOrWhiteSpace(tenantId))
        {
            _logger.LogWarning("Request missing tenant ID header");
            // In production, return 400 or 401
            return Task.FromResult(false);
        }

        // Store tenant context in HttpContext for use by controllers
        context.Items[TenantContextKey] = new TenantContext { TenantId = tenantId };

        _logger.LogDebug("Tenant context extracted: {tenantId}", tenantId);

        return Task.FromResult(true);
    }

    /// <summary>
    /// Response interceptor to add tenant ID to response headers (optional).
    /// </summary>
    public Task OnResponseAsync(HttpContext context)
    {
        if (context.Items.TryGetValue(TenantContextKey, out var tenantContextObj) &&
            tenantContextObj is TenantContext tenantContext)
        {
            context.Response.Headers.Add("X-Tenant-Id", tenantContext.TenantId);
        }

        return Task.CompletedTask;
    }
}

/// <summary>
/// Tenant context information extracted from request.
/// </summary>
public sealed class TenantContext {
    public string TenantId { get; set; }
    public DateTime ExtractedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Request validation interceptor that enforces business rules.
/// Examples: rate limiting per tenant, quota enforcement.
/// </summary>
public sealed class RequestValidationInterceptor : IRequestInterceptor {
    private readonly ILogger<RequestValidationInterceptor> _logger;

    public RequestValidationInterceptor(ILogger<RequestValidationInterceptor> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Validates request against business rules.
    /// Returns false if validation fails.
    /// </summary>
    public Task<bool> OnRequestAsync(HttpContext context)
    {
        // Check content-type is valid for POST/PUT requests
        if (context.Request.Method == "POST" || context.Request.Method == "PUT")
        {
            var contentType = context.Request.ContentType?.ToLower();

            if (!string.IsNullOrEmpty(contentType) &&
                !contentType.Contains("application/json") &&
                !contentType.Contains("multipart/form-data"))
            {
                _logger.LogWarning("Invalid content-type: {contentType}", contentType);
                return Task.FromResult(false);
            }
        }

        // Check content length limits
        const long MaxBodySize = 10_000_000; // 10MB
        if (context.Request.ContentLength > MaxBodySize)
        {
            _logger.LogWarning("Request body exceeds size limit: {size}", context.Request.ContentLength);
            return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }

    public Task OnResponseAsync(HttpContext context)
    {
        return Task.CompletedTask;
    }
}

/// <summary>
/// Correlation ID interceptor for request tracing across logs and systems.
/// Adds X-Correlation-Id header to track related operations.
/// </summary>
public sealed class CorrelationIdInterceptor : IRequestInterceptor {
    private const string CorrelationIdHeader = "X-Correlation-Id";
    private const string CorrelationIdKey = "CorrelationId";
    private readonly ILogger<CorrelationIdInterceptor> _logger;

    public CorrelationIdInterceptor(ILogger<CorrelationIdInterceptor> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Extracts or generates correlation ID for request tracing.
    /// </summary>
    public Task<bool> OnRequestAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[CorrelationIdHeader].ToString();

        if (string.IsNullOrWhiteSpace(correlationId))
            correlationId = Guid.NewGuid().ToString();

        context.Items[CorrelationIdKey] = correlationId;
        context.Response.Headers.Add(CorrelationIdHeader, correlationId);

        _logger.LogDebug("Correlation ID: {correlationId}", correlationId);

        return Task.FromResult(true);
    }

    public Task OnResponseAsync(HttpContext context)
    {
        return Task.CompletedTask;
    }
}

/// <summary>
/// Interceptor pipeline for executing multiple interceptors in order.
/// </summary>
public sealed class InterceptorPipeline {
    private readonly List<IRequestInterceptor> _interceptors;
    private readonly ILogger<InterceptorPipeline> _logger;

    public InterceptorPipeline(ILogger<InterceptorPipeline> logger)
    {
        _interceptors = new List<IRequestInterceptor>();
        _logger = logger;
    }

    /// <summary>
    /// Registers interceptor in pipeline.
    /// Interceptors execute in registration order.
    /// </summary>
    public void Register(IRequestInterceptor interceptor)
    {
        if (interceptor is not null)
            _interceptors.Add(interceptor);
    }

    /// <summary>
    /// Executes all request interceptors.
    /// Stops on first failure (returns false).
    /// </summary>
    public async Task<bool> ExecuteRequestInterceptorsAsync(HttpContext context)
    {
        foreach (var interceptor in _interceptors)
        {
            try
            {
                if (!await interceptor.OnRequestAsync(context))
                    return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in request interceptor: {type}", interceptor.GetType().Name);
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Executes all response interceptors (reverse order).
    /// Always executes all interceptors regardless of errors.
    /// </summary>
    public async Task ExecuteResponseInterceptorsAsync(HttpContext context)
    {
        // Execute in reverse order (LIFO for cleanup)
        for (int i = _interceptors.Count - 1; i >= 0; i--)
        {
            try
            {
                await _interceptors[i].OnResponseAsync(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in response interceptor: {type}", _interceptors[i].GetType().Name);
            }
        }
    }
}
