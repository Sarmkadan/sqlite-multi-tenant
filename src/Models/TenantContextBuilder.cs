#nullable enable

namespace SqliteMultiTenant.Models;

/// <summary>
/// Fluent builder for creating <see cref="TenantContext"/> instances.
/// </summary>
public sealed class TenantContextBuilder
{
    private readonly TenantContext _context = new();

    /// <summary>
    /// Sets the tenant ID.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="tenantId"/> is null.</exception>
    /// <returns>This builder instance.</returns>
    public TenantContextBuilder WithTenantId(string tenantId)
    {
        ArgumentNullException.ThrowIfNull(tenantId);
        _context.TenantId = tenantId;
        return this;
    }

    /// <summary>
    /// Sets the tenant name.
    /// </summary>
    /// <param name="tenantName">The tenant name, or null to clear it.</param>
    /// <returns>This builder instance.</returns>
    public TenantContextBuilder WithTenantName(string? tenantName)
    {
        _context.TenantName = tenantName;
        return this;
    }

    /// <summary>
    /// Sets the user ID.
    /// </summary>
    /// <param name="userId">The user ID, or null to clear it.</param>
    /// <returns>This builder instance.</returns>
    public TenantContextBuilder WithUserId(string? userId)
    {
        _context.UserId = userId;
        return this;
    }

    /// <summary>
    /// Sets the user email address.
    /// </summary>
    /// <param name="userEmail">The user email address, or null to clear it.</param>
    /// <returns>This builder instance.</returns>
    public TenantContextBuilder WithUserEmail(string? userEmail)
    {
        _context.UserEmail = userEmail;
        return this;
    }

    /// <summary>
    /// Sets the time at which the tenant was established.
    /// </summary>
    /// <param name="establishedAt">The establishment timestamp.</param>
    /// <returns>This builder instance.</returns>
    public TenantContextBuilder WithEstablishedAt(DateTime establishedAt)
    {
        _context.EstablishedAt = establishedAt;
        return this;
    }

    /// <summary>
    /// Sets the creation timestamp.
    /// </summary>
    /// <param name="createdAt">The creation timestamp.</param>
    /// <returns>This builder instance.</returns>
    public TenantContextBuilder WithCreatedAt(DateTime createdAt)
    {
        _context.CreatedAt = createdAt;
        return this;
    }

    /// <summary>
    /// Sets the request ID.
    /// </summary>
    /// <param name="requestId">The request ID, or null to clear it.</param>
    /// <returns>This builder instance.</returns>
    public TenantContextBuilder WithRequestId(string? requestId)
    {
        _context.RequestId = requestId;
        return this;
    }

    /// <summary>
    /// Sets the connection ID.
    /// </summary>
    /// <param name="connectionId">The connection ID, or null to clear it.</param>
    /// <returns>This builder instance.</returns>
    public TenantContextBuilder WithConnectionId(string? connectionId)
    {
        _context.ConnectionId = connectionId;
        return this;
    }

    /// <summary>
    /// Sets the path to the tenant database file.
    /// </summary>
    /// <param name="databasePath">The database file path, or null to clear it.</param>
    /// <returns>This builder instance.</returns>
    public TenantContextBuilder WithDatabasePath(string? databasePath)
    {
        _context.DatabasePath = databasePath;
        return this;
    }

    /// <summary>
    /// Sets the additional context data.
    /// </summary>
    /// <param name="contextData">The context data dictionary, or null to clear it.</param>
    /// <returns>This builder instance.</returns>
    public TenantContextBuilder WithContextData(Dictionary<string, object>? contextData)
    {
        _context.ContextData = contextData;
        return this;
    }

    /// <summary>
    /// Sets the allowed tenants.
    /// </summary>
    /// <param name="allowedTenants">The allowed tenants.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="allowedTenants"/> is null.</exception>
    /// <returns>This builder instance.</returns>
    public TenantContextBuilder WithAllowedTenants(IEnumerable<string> allowedTenants)
    {
        ArgumentNullException.ThrowIfNull(allowedTenants);
        foreach (var tenant in allowedTenants)
        {
            _context.AllowedTenants.Add(tenant);
        }
        return this;
    }

    /// <summary>
    /// Marks the context as invalid so that <see cref="Build"/> fails validation.
    /// </summary>
    /// <returns>This builder instance.</returns>
    public TenantContextBuilder AsInvalid()
    {
        _context.Invalidate();
        return this;
    }

    /// <summary>
    /// Validates and returns the configured <see cref="TenantContext"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the context fails validation.</exception>
    /// <returns>The validated tenant context.</returns>
    public TenantContext Build()
    {
        if (!_context.Validate(out var errorMessage))
        {
            throw new InvalidOperationException($"Failed to build TenantContext: {errorMessage}");
        }
        return _context;
    }
}
