# BackupVerificationResult

Represents the outcome of a SQLite backup file verification operation, capturing integrity check results, file metadata, and any error details for audit and diagnostic purposes.

## API

### `public bool IsValid`
Gets a value indicating whether the backup file passed all verification checks. Returns `true` when the integrity check succeeded and no errors were encountered; otherwise `false`.

### `public string IntegrityCheckResult`
Gets the raw output or summary from the SQLite integrity check (e.g., `PRAGMA integrity_check`). Contains `"ok"` on success or an error description on failure.

### `public long FileSizeBytes`
Gets the size of the verified backup file in bytes. Useful for quota enforcement and storage reporting.

### `public int PageCount`
Gets the number of pages in the backup database file as reported by SQLite.

### `public int PageSizeBytes`
Gets the page size in bytes of the backup database file as reported by SQLite.

### `public DateTime VerifiedAt`
Gets the UTC timestamp when the verification was performed.

### `public string? ErrorMessage`
Gets the error message if verification failed; `null` when `IsValid` is `true`.

### `public static BackupVerificationResult Success`
Gets a pre-configured successful verification result with `IsValid = true`, `IntegrityCheckResult = "ok"`, `ErrorMessage = null`, and `VerifiedAt` set to the current UTC time. Other properties default to zero.

### `public static BackupVerificationResult Failed`
Gets a pre-configured failed verification result with `IsValid = false`, `IntegrityCheckResult = "error"`, `ErrorMessage = "Verification failed"`, and `VerifiedAt` set to the current UTC time. Other properties default to zero.

## Usage

### Verifying a backup after creation
```csharp
var backupPath = Path.Combine(tenantDirectory, "backup.sqlite");
var result = await BackupVerifier.VerifyAsync(backupPath, cancellationToken);

if (!result.IsValid)
{
    _logger.LogWarning("Backup verification failed for tenant {TenantId}: {Error}",
        tenantId, result.ErrorMessage);
    await AlertOnCallAsync($"Backup corrupt: {result.IntegrityCheckResult}");
    return;
}

_logger.LogInformation("Backup verified: {Size} bytes, {Pages} pages at {VerifiedAt}",
    result.FileSizeBytes, result.PageCount, result.VerifiedAt);
```

### Using static factory results in tests
```csharp
[Fact]
public void Restore_Throws_When_Verification_Failed()
{
    var failedResult = BackupVerificationResult.Failed;
    failedResult.ErrorMessage = "Checksum mismatch";

    var ex = await Assert.ThrowsAsync<BackupCorruptException>(() =>
        _restoreService.RestoreAsync(failedResult, targetDbPath));

    Assert.Contains("Checksum mismatch", ex.Message);
}
```

## Notes

- The static `Success` and `Failed` properties return new instances on each access; they are not singletons. Mutating the returned instance does not affect subsequent accesses.
- `ErrorMessage` is only populated when `IsValid` is `false`; however, callers should not rely on this convention for logic—always check `IsValid` first.
- `FileSizeBytes`, `PageCount`, and `PageSizeBytes` default to zero in the static factory results. Production verification populates these from `PRAGMA page_count` and `PRAGMA page_size`.
- `VerifiedAt` uses `DateTime.UtcNow` at the moment the factory property is accessed. For deterministic testing, inject a time provider or set the property after creation.
- This type is immutable by convention (all properties have public getters only). Thread safety is inherent for read-only usage; no synchronization is required when sharing instances across threads.
