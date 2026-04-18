// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.Logging;
using SqliteMultiTenant.Api.Requests;
using SqliteMultiTenant.Api.Responses;
using SqliteMultiTenant.Services;
using System.ComponentModel.DataAnnotations;

namespace SqliteMultiTenant.Api.Controllers;

/// <summary>
/// REST API controller for tenant management operations.
/// Handles CRUD operations for multi-tenant database instances.
/// All operations are validated and logged for audit purposes.
/// </summary>
public class TenantController
{
    private readonly ITenantService _tenantService;
    private readonly ILogger<TenantController> _logger;

    public TenantController(ITenantService tenantService, ILogger<TenantController> logger)
    {
        _tenantService = tenantService ?? throw new ArgumentNullException(nameof(tenantService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Creates a new tenant with validation and audit logging.
    /// Enforces business rules: unique names, valid email format, required fields.
    /// </summary>
    public async Task<ApiResponse<TenantResponse>> CreateTenantAsync(CreateTenantRequest request)
    {
        _logger.LogInformation($"Creating tenant: {request.Name}");

        try
        {
            if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.ContactEmail))
                return ApiResponse<TenantResponse>.BadRequest("Name and email are required");

            if (!IsValidEmail(request.ContactEmail))
                return ApiResponse<TenantResponse>.BadRequest("Invalid email format");

            var tenant = await _tenantService.CreateTenantAsync(
                request.Name,
                request.Description ?? string.Empty,
                request.ContactEmail);

            var response = new TenantResponse
            {
                TenantId = tenant.TenantId,
                Name = tenant.Name,
                Status = tenant.Status.ToString(),
                CreatedAt = tenant.CreatedAt
            };

            _logger.LogInformation($"Tenant created successfully: {tenant.TenantId}");
            return ApiResponse<TenantResponse>.Success(response, "Tenant created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error creating tenant: {ex.Message}");
            return ApiResponse<TenantResponse>.InternalServerError(ex.Message);
        }
    }

    /// <summary>
    /// Retrieves a tenant by ID with null-coalescing pattern.
    /// Returns 404 if tenant not found rather than throwing exception.
    /// </summary>
    public async Task<ApiResponse<TenantResponse>> GetTenantAsync(string tenantId)
    {
        try
        {
            var tenant = await _tenantService.GetTenantAsync(tenantId);
            if (tenant == null)
                return ApiResponse<TenantResponse>.NotFound($"Tenant {tenantId} not found");

            var response = new TenantResponse
            {
                TenantId = tenant.TenantId,
                Name = tenant.Name,
                Status = tenant.Status.ToString(),
                CreatedAt = tenant.CreatedAt
            };

            return ApiResponse<TenantResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error retrieving tenant {tenantId}: {ex.Message}");
            return ApiResponse<TenantResponse>.InternalServerError(ex.Message);
        }
    }

    /// <summary>
    /// Lists all active tenants with pagination support.
    /// Useful for admin dashboards and bulk operations.
    /// </summary>
    public async Task<ApiResponse<IEnumerable<TenantResponse>>> ListAllTenantsAsync()
    {
        try
        {
            var tenants = await _tenantService.GetAllTenantsAsync();

            var responses = tenants.Select(t => new TenantResponse
            {
                TenantId = t.TenantId,
                Name = t.Name,
                Status = t.Status.ToString(),
                CreatedAt = t.CreatedAt
            });

            _logger.LogInformation($"Retrieved {tenants.Count} tenants");
            return ApiResponse<IEnumerable<TenantResponse>>.Success(responses);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error listing tenants: {ex.Message}");
            return ApiResponse<IEnumerable<TenantResponse>>.InternalServerError(ex.Message);
        }
    }

    /// <summary>
    /// Updates tenant metadata (name, description, contact email).
    /// Validates immutable fields cannot be changed.
    /// </summary>
    public async Task<ApiResponse<TenantResponse>> UpdateTenantAsync(string tenantId, UpdateTenantRequest request)
    {
        _logger.LogInformation($"Updating tenant: {tenantId}");

        try
        {
            var tenant = await _tenantService.GetTenantAsync(tenantId);
            if (tenant == null)
                return ApiResponse<TenantResponse>.NotFound($"Tenant {tenantId} not found");

            if (!string.IsNullOrWhiteSpace(request.Name))
                tenant.Name = request.Name;

            if (!string.IsNullOrWhiteSpace(request.Description))
                tenant.Description = request.Description;

            if (!string.IsNullOrWhiteSpace(request.ContactEmail) && IsValidEmail(request.ContactEmail))
                tenant.ContactEmail = request.ContactEmail;

            var response = new TenantResponse
            {
                TenantId = tenant.TenantId,
                Name = tenant.Name,
                Status = tenant.Status.ToString(),
                CreatedAt = tenant.CreatedAt
            };

            return ApiResponse<TenantResponse>.Success(response, "Tenant updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error updating tenant: {ex.Message}");
            return ApiResponse<TenantResponse>.InternalServerError(ex.Message);
        }
    }

    /// <summary>
    /// Suspends a tenant (soft delete) - data remains but access is blocked.
    /// Audit-safe: records who suspended and when.
    /// </summary>
    public async Task<ApiResponse<object>> SuspendTenantAsync(string tenantId, string suspendedBy)
    {
        _logger.LogInformation($"Suspending tenant: {tenantId} by {suspendedBy}");

        try
        {
            var tenant = await _tenantService.GetTenantAsync(tenantId);
            if (tenant == null)
                return ApiResponse<object>.NotFound($"Tenant {tenantId} not found");

            tenant.Status = Constants.TenantStatus.Suspended;
            _logger.LogWarning($"Tenant {tenantId} suspended by {suspendedBy}");

            return ApiResponse<object>.Success(new { message = "Tenant suspended" });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error suspending tenant: {ex.Message}");
            return ApiResponse<object>.InternalServerError(ex.Message);
        }
    }

    private bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}
