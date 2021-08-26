namespace SqliteMultiTenant.Exceptions;

public static class BackupExceptionExtensions
{
    public static bool IsCreationFailure(this BackupException ex) => ex.Message.Contains("creation", StringComparison.OrdinalIgnoreCase);
    public static bool IsVerificationFailure(this BackupException ex) => ex.Message.Contains("verification", StringComparison.OrdinalIgnoreCase);
    public static bool IsRestoreFailure(this BackupException ex) => ex.Message.Contains("restore", StringComparison.OrdinalIgnoreCase);
    public static string GetErrorDetails(this BackupException ex) => $"BackupId: {ex.BackupId}, DatabaseId: {ex.DatabaseId}, Message: {ex.Message}";
}
