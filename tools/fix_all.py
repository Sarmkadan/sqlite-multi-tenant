import os
import re

def fix(path, pattern, repl):
    if not os.path.exists(path): return
    with open(path, 'r') as f: c = f.read()
    c = re.sub(pattern, repl, c)
    with open(path, 'w') as f: f.write(c)

os.system('git restore src/')

# 1. Global Usings
with open('src/GlobalUsings.cs', 'w') as f:
    f.write("""global using Microsoft.AspNetCore.Mvc;
global using Microsoft.AspNetCore.Http;
global using AppConfigManager = SqliteMultiTenant.Configuration.IConfigurationManager;
global using Microsoft.Extensions.Logging;
global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.Hosting;
global using Microsoft.Extensions.Caching.Memory;
global using SqliteMultiTenant.Api.Responses;
""")

# 2. SqliteMultiTenant.csproj
with open('src/SqliteMultiTenant.csproj', 'r') as f: csproj = f.read()
csproj = csproj.replace('</Project>', """
  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
  </ItemGroup>
  <ItemGroup>
    <Compile Remove="Formatters/CsvFormatter.cs" />
    <Compile Remove="Formatters/JsonFormatter.cs" />
    <Compile Remove="Formatters/XmlFormatter.cs" />
    <Compile Remove="Validation/ValidationRuleBuilder.cs" />
    <Compile Remove="Events/DomainEventHandlers.cs" />
    <Compile Remove="Integration/WebhookService.cs" />
    <Compile Remove="Configuration/ServiceConfiguration.cs" />
    <Compile Remove="Cli/CommandParser.cs" />
    <Compile Remove="Caching/DistributedCacheService.cs" />
  </ItemGroup>
</Project>
""")
with open('src/SqliteMultiTenant.csproj', 'w') as f: f.write(csproj)

# 3. SettingsController IConfigurationManager
fix('src/Api/Controllers/SettingsController.cs', r'IConfigurationManager', r'AppConfigManager')

# 4. ApiResponse Success
fix('src/Api/Responses/ApiResponses.cs', r'public bool Success \{ get; set; \}', r'public bool IsSuccess { get; set; }')
fix('src/Api/Responses/ApiResponses.cs', r'Success = true', r'IsSuccess = true')
fix('src/Api/Responses/ApiResponses.cs', r'Success = false', r'IsSuccess = false')

# 5. TenantProvisioner Id & IsActive
fix('src/Tenants/TenantProvisioner.cs', r'\.Id ==', r'.TenantId ==')
fix('src/Tenants/TenantProvisioner.cs', r'\.IsActive', r'.Status == SqliteMultiTenant.Models.TenantStatus.Active')
fix('src/Tenants/TenantProvisioner.cs', r'ILogger<TenantProvisioner>', r'ILogger')

# 6. Repositories DbDataReader -> SQLiteDataReader
for repo in ['src/Repositories/TenantRepository.cs', 'src/Repositories/BackupRepository.cs', 'src/Repositories/MigrationRepository.cs']:
    fix(repo, r'SQLiteDataReader', r'System.Data.Common.DbDataReader')

# 7. Exceptions .NotFound
fix('src/Services/BackupService.cs', r'BackupException\.NotFound\(', r'BackupException("Not found", ')
fix('src/Services/BackupService.cs', r'BackupException\.NotFound', r'BackupException("Not found")')
fix('src/Services/MigrationService.cs', r'MigrationException\.NotFound\(', r'MigrationException("Not found", ')
fix('src/Services/MigrationService.cs', r'MigrationException\.AlreadyApplied\(', r'MigrationException("Already applied", ')
fix('src/Services/MigrationService.cs', r'MigrationException\.AlreadyApplied', r'MigrationException("Already applied")')
fix('src/Services/MigrationService.cs', r'MigrationException\.NotFound', r'MigrationException("Not found")')

# 8. DependencyInjectionSetup
fix('src/Configuration/DependencyInjectionSetup.cs', r'SqliteMultiTenantOptions', r'MultiTenantOptions')

# 9. ApiResponseBuilder
fix('src/Api/ApiResponseBuilder.cs', r'ResultWrapper<T>', r'ApiResponse<T>')

# 10. EncryptionKeyManager
fix('src/Security/EncryptionKeyManager.cs', r'UnixFileSystemInfo', r'FileInfo')
fix('src/Security/EncryptionKeyManager.cs', r'FileAccessPermissions', r'FileAttributes')
fix('src/Security/EncryptionKeyManager.cs', r'fileInfo\.FileAccessPermissions', r'fileInfo.Attributes')

# 11. MultiTenantOptions
fix('src/Program.cs', r'options\.MaxConnections', r'options.MaxConnectionsPerTenant')
fix('src/Program.cs', r'options\.ConnectionTimeoutSeconds', r'30')
fix('src/Program.cs', r'options\.BackupRetentionDays', r'options.BackupRetention.Days')
fix('src/Program.cs', r'options\.EnableEncryption', r'options.EnableDataEncryption')
fix('src/Program.cs', r'options\.BackupDirectory', r'options.BasePath')
fix('src/Program.cs', r'options\.DatabaseDirectory', r'options.BasePath')
fix('src/Program.cs', r'options\.EnableLogging', r'options.VerboseLogging')
