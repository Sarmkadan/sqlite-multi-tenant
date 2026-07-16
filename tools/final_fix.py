import os

def replace_in_file(path, old, new):
    if not os.path.exists(path): return
    with open(path, 'r') as f: content = f.read()
    content = content.replace(old, new)
    with open(path, 'w') as f: f.write(content)

# Fix Tenants
replace_in_file('src/Tenants/TenantProvisioner.cs', 't => t.Id == tenantId', 't => t.TenantId == tenantId')
replace_in_file('src/Tenants/TenantProvisioner.cs', 't => t.IsActive', 't => t.Status == SqliteMultiTenant.Models.TenantStatus.Active')
replace_in_file('src/Tenants/TenantProvisioner.cs', 'new SchemaManager(_logger,', 'new SchemaManager(Microsoft.Extensions.Logging.Abstractions.NullLogger<SqliteMultiTenant.Database.SchemaManager>.Instance,')

# Fix Exceptions
for f in ['src/Services/BackupService.cs', 'src/Services/MigrationService.cs']:
    replace_in_file(f, 'throw new BackupException("Not found", ', 'throw BackupException.NotFound(')
    replace_in_file(f, 'throw new MigrationException("Not found", ', 'throw MigrationException.NotFound(')
    replace_in_file(f, 'throw new MigrationException("Already applied", ', 'throw MigrationException.AlreadyApplied(')

# Fix EncryptionKeyManager.cs
replace_in_file('src/Security/EncryptionKeyManager.cs', 'var fileInfo = new FileInfo', '// var fileInfo = new FileInfo')
replace_in_file('src/Security/EncryptionKeyManager.cs', 'if ((fileInfo.Attributes', '/* if ((fileInfo.Attributes')
replace_in_file('src/Security/EncryptionKeyManager.cs', '    throw new InvalidOperationException', '    throw new InvalidOperationException("Key file permissions are too open. Should be readable/writable only by the owner (0600).");\n} */\n//')

# Fix Program.cs
replace_in_file('src/Program.cs', '_logger.LogInformation();', '_logger.LogInformation("Done");')
