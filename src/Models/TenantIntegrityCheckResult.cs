#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System.Text;

namespace SqliteMultiTenant.Models;

/// <summary>
/// Represents the result of a SQLite database integrity check for a tenant.
/// Contains the integrity check status (ok/failed) and any error messages.
/// </summary>
public sealed class TenantIntegrityCheckResult
{
    /// <summary>
    /// The tenant identifier.
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// The tenant name.
    /// </summary>
    public string TenantName { get; set; } = string.Empty;

    /// <summary>
    /// The integrity check status: true for OK, false for failed.
    /// </summary>
    public bool IsOk { get; set; }

    /// <summary>
    /// Error message if the integrity check failed.
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Raw integrity check output from SQLite.
    /// </summary>
    public string? IntegrityOutput { get; set; }

    /// <summary>
    /// When the integrity check was performed.
    /// </summary>
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Indicates whether the integrity check passed successfully.
    /// </summary>
    public bool IsSuccess => IsOk && string.IsNullOrEmpty(Error);

    /// <summary>
    /// Gets a human-readable summary of the integrity check result.
    /// </summary>
    public string ResultSummary
    {
        get
        {
            if (IsSuccess)
            {
                return "OK";
            }

            var sb = new StringBuilder();
            sb.Append("FAILED");
            if (!string.IsNullOrEmpty(Error))
            {
                sb.Append($": {Error}");
            }

            if (!string.IsNullOrEmpty(IntegrityOutput))
            {
                sb.Append($"\nIntegrity output:\n{IntegrityOutput}");
            }

            return sb.ToString();
        }
    }

    /// <summary>
    /// Gets a detailed summary of the integrity check result.
    /// </summary>
    public string DetailedResult
    {
        get
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Integrity Check for Tenant: {TenantName} ({TenantId})");
            sb.AppendLine($"Status: {ResultSummary}");
            sb.AppendLine($"Checked At: {CheckedAt:yyyy-MM-dd HH:mm:ss UTC}");

            if (!string.IsNullOrEmpty(Error))
            {
                sb.AppendLine($"Error: {Error}");
            }

            return sb.ToString().Trim();
        }
    }
}
