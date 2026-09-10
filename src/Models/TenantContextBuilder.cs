#nullable enable

namespace SqliteMultiTenant.Models;

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

    public TenantContextBuilder WithTenantName(string? tenantName)
    {
        _context.TenantName = tenantName;
        return this;
    }

    public TenantContextBuilder WithUserId(string? userId)
    {
        _context.UserId = userId;
        return this;
    }

    public TenantContextBuilder WithUserEmail(string? userEmail)
    {
        _context.UserEmail = userEmail;
        return this;
    }

    public TenantContextBuilder WithEstablishedAt(DateTime establishedAt)
    {
        _context.EstablishedAt = establishedAt;
        return this;
    }

    public TenantContextBuilder WithCreatedAt(DateTime createdAt)
    {
        _context.CreatedAt = createdAt;
        return this;
    }

    public TenantContextBuilder WithRequestId(string? requestId)
    {
        _context.RequestId = requestId;
        return this;
    }

    public TenantContextBuilder WithConnectionId(string? connectionId)
    {
        _context.ConnectionId = connectionId;
        return this;
    }

    public TenantContextBuilder WithDatabasePath(string? databasePath)
    {
        _context.DatabasePath = databasePath;
        return this;
    }

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

    public TenantContextBuilder AsInvalid()
    {
        _context.Invalidate();
        return this;
    }

    public TenantContext Build()
    {
        if (!_context.Validate(out var errorMessage))
        {
            throw new InvalidOperationException($"Failed to build TenantContext: {errorMessage}");
        }
        return _context;
    }
}
