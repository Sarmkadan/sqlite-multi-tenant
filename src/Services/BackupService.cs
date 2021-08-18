#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using SqliteMultiTenant.Constants;
using SqliteMultiTenant.Exceptions;
using SqliteMultiTenant.Models;
using SqliteMultiTenant.Repositories;

namespace SqliteMultiTenant.Services;

/// <summary>
/// Service implementation for database backup management
/// </summary>
public sealed class BackupService : IBackupService {
    private readonly IBackupRepository _repository;
    private readonly ILogger<BackupService> _logger;

    public BackupService(IBackupRepository repository, ILogger<BackupService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Backup?> GetBackupAsync(string backupId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(backupId))
            throw new ArgumentException("Backup ID cannot be empty", nameof(backupId));

        try
        {
            return await _repository.GetByIdAsync(backupId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error retrieving backup {BackupId}: {Message}", backupId, ex.Message);
            throw;
        }
    }

    public async Task<List<Backup>> GetDatabaseBackupsAsync(string databaseId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(databaseId))
            throw new ArgumentException("Database ID cannot be empty", nameof(databaseId));

        try
        {
            return await _repository.GetByDatabaseAsync(databaseId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error retrieving backups for database {DatabaseId}: {Message}", databaseId, ex.Message);
            throw;
        }
    }

    public async Task<List<Backup>> GetCompletedBackupsAsync(string databaseId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(databaseId))
            throw new ArgumentException("Database ID cannot be empty", nameof(databaseId));

        try
        {
            return await _repository.GetCompletedBackupsAsync(databaseId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error retrieving completed backups: {Message}", ex.Message);
            throw;
        }
    }

    public async Task<Backup?> GetLatestBackupAsync(string databaseId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(databaseId))
            throw new ArgumentException("Database ID cannot be empty", nameof(databaseId));

        try
        {
            return await _repository.GetLatestBackupAsync(databaseId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error retrieving latest backup: {Message}", ex.Message);
            throw;
        }
    }

    public async Task<Backup> CreateBackupAsync(string databaseId, BackupType backupType, string createdBy, string? backupPath = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(databaseId))
            throw new ArgumentException("Database ID cannot be empty", nameof(databaseId));

        if (string.IsNullOrWhiteSpace(createdBy))
            throw new ArgumentException("CreatedBy cannot be empty", nameof(createdBy));

        try
        {
            string finalBackupPath = backupPath ?? GenerateBackupPath(databaseId);

            var backup = new Backup
            {
                BackupId = Guid.NewGuid().ToString(),
                DatabaseId = databaseId,
                BackupPath = finalBackupPath,
                BackupType = backupType,
                Status = BackupStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = createdBy,
                ExpiresAt = DateTime.UtcNow.AddDays(TenantConstants.BackupRetentionDays)
            };

            if (!backup.Validate(out var errors))
                throw new ArgumentException($"Backup validation failed: {string.Join(", ", errors)}");

            var createdBackup = await _repository.AddAsync(backup, cancellationToken);
            _logger.LogInformation("Backup created: {BackupId}", backup.BackupId);
            return createdBackup;
        }
        catch (Exception ex)
        {
            _logger.LogError("Error creating backup: {Message}", ex.Message);
            throw;
        }
    }

    public async Task MarkBackupAsCompletedAsync(string backupId, long sizeBytes, long durationMs, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(backupId))
            throw new ArgumentException("Backup ID cannot be empty", nameof(backupId));

        try
        {
            var backup = await _repository.GetByIdAsync(backupId, cancellationToken);
            if (backup is null)
                throw BackupException.NotFound(backupId);

            backup.MarkAsCompleted(sizeBytes, durationMs);
            await _repository.UpdateAsync(backup, cancellationToken);
            _logger.LogInformation("Backup completed: {BackupId} ({SizeBytes} bytes in {DurationMs}ms)", backupId, sizeBytes, durationMs);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error marking backup as completed: {Message}", ex.Message);
            throw;
        }
    }

    public async Task MarkBackupAsFailedAsync(string backupId, string errorMessage, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(backupId))
            throw new ArgumentException("Backup ID cannot be empty", nameof(backupId));

        try
        {
            var backup = await _repository.GetByIdAsync(backupId, cancellationToken);
            if (backup is null)
                throw BackupException.NotFound(backupId);

            backup.MarkAsFailed(errorMessage);
            await _repository.UpdateAsync(backup, cancellationToken);
            _logger.LogError("Backup failed: {BackupId} - {ErrorMessage}", backupId, errorMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error marking backup as failed: {Message}", ex.Message);
            throw;
        }
    }

    public async Task VerifyBackupAsync(string backupId, string verifiedBy, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(backupId))
            throw new ArgumentException("Backup ID cannot be empty", nameof(backupId));

        if (string.IsNullOrWhiteSpace(verifiedBy))
            throw new ArgumentException("VerifiedBy cannot be empty", nameof(verifiedBy));

        try
        {
            var backup = await _repository.GetByIdAsync(backupId, cancellationToken);
            if (backup is null)
                throw BackupException.NotFound(backupId);

            backup.MarkAsVerified(verifiedBy);
            await _repository.UpdateAsync(backup, cancellationToken);
            _logger.LogInformation("Backup verified: {BackupId}", backupId);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error verifying backup: {Message}", ex.Message);
            throw;
        }
    }

    public async Task SetBackupExpirationAsync(string backupId, DateTime expirationDate, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(backupId))
            throw new ArgumentException("Backup ID cannot be empty", nameof(backupId));

        try
        {
            var backup = await _repository.GetByIdAsync(backupId, cancellationToken);
            if (backup is null)
                throw BackupException.NotFound(backupId);

            backup.SetExpiration(expirationDate);
            await _repository.UpdateAsync(backup, cancellationToken);
            _logger.LogInformation("Backup expiration set: {BackupId} -> {ExpirationDate}", backupId, expirationDate);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error setting backup expiration: {Message}", ex.Message);
            throw;
        }
    }

    public async Task<List<Backup>> GetExpiredBackupsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _repository.GetExpiredBackupsAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error retrieving expired backups: {Message}", ex.Message);
            throw;
        }
    }

    public async Task<int> GetBackupCountAsync(string databaseId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(databaseId))
            throw new ArgumentException("Database ID cannot be empty", nameof(databaseId));

        try
        {
            return await _repository.GetCountByDatabaseAsync(databaseId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error getting backup count: {Message}", ex.Message);
            throw;
        }
    }

    public async Task DeleteBackupAsync(string backupId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(backupId))
            throw new ArgumentException("Backup ID cannot be empty", nameof(backupId));

        try
        {
            var backup = await _repository.GetByIdAsync(backupId, cancellationToken);
            if (backup is null)
                throw BackupException.NotFound(backupId);

            await _repository.DeleteAsync(backupId, cancellationToken);
            _logger.LogInformation("Backup deleted: {BackupId}", backupId);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error deleting backup: {Message}", ex.Message);
            throw;
        }
    }

    public async Task AddBackupTagAsync(string backupId, string tag, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(backupId))
            throw new ArgumentException("Backup ID cannot be empty", nameof(backupId));

        if (string.IsNullOrWhiteSpace(tag))
            throw new ArgumentException("Tag cannot be empty", nameof(tag));

        try
        {
            var backup = await _repository.GetByIdAsync(backupId, cancellationToken);
            if (backup is null)
                throw BackupException.NotFound(backupId);

            backup.AddTag(tag);
            await _repository.UpdateAsync(backup, cancellationToken);
            _logger.LogInformation("Tag added to backup: {BackupId} -> {Tag}", backupId, tag);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error adding backup tag: {Message}", ex.Message);
            throw;
        }
    }

    private string GenerateBackupPath(string databaseId)
    {
        string backupDir = Path.Combine(Directory.GetCurrentDirectory(), TenantConstants.DefaultBackupDirectory);
        Directory.CreateDirectory(backupDir);

        string timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        return Path.Combine(backupDir, $"{databaseId}_{timestamp}{TenantConstants.BackupFileExtension}");
    }
}
