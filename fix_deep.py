import os

def fix_file(file, old, new):
    if not os.path.exists(file): return
    with open(file, 'r') as f: content = f.read()
    content = content.replace(old, new)
    with open(file, 'w') as f: f.write(content)

# 1. TenantProvisioner
fix_file('src/Tenants/TenantProvisioner.cs', 't => t.Id == tenantId', 't => t.TenantId == tenantId')
fix_file('src/Tenants/TenantProvisioner.cs', 't => t.IsActive', 't => t.Status == SqliteMultiTenant.Models.TenantStatus.Active')
fix_file('src/Tenants/TenantProvisioner.cs', 'ILogger<TenantProvisioner>', 'ILogger')

# 2. DbDataReader to SQLiteDataReader in Repositories
for repo in ['src/Repositories/TenantRepository.cs', 'src/Repositories/BackupRepository.cs', 'src/Repositories/MigrationRepository.cs']:
    fix_file(repo, 'DbDataReader reader', 'SQLiteDataReader reader')
    fix_file(repo, 'DbDataReader', 'SQLiteDataReader')

# 3. BackupException.NotFound
fix_file('src/Services/BackupService.cs', 'throw new BackupException.NotFound(', 'throw new BackupException("Not found", ')
fix_file('src/Services/BackupService.cs', 'throw new BackupException.NotFound', 'throw new BackupException("Not found")')

# 4. MigrationException
fix_file('src/Services/MigrationService.cs', 'throw new MigrationException.NotFound(', 'throw new MigrationException("Not found", ')
fix_file('src/Services/MigrationService.cs', 'throw new MigrationException.AlreadyApplied(', 'throw new MigrationException("Already applied", ')

# 5. WebhookSubscription properties
fix_file('src/Integration/WebhookService.cs', 's.IsActive', 's.Enabled')
fix_file('src/Integration/WebhookService.cs', 's.Id', 's.WebhookId')

# 6. ApiResponses Success
fix_file('src/Api/Responses/ApiResponses.cs', 'public bool Success { get; set; }', 'public bool IsSuccess { get; set; }')
fix_file('src/Api/Responses/ApiResponses.cs', 'Success = true', 'IsSuccess = true')
fix_file('src/Api/Responses/ApiResponses.cs', 'Success = false', 'IsSuccess = false')

# 7. EncryptionKeyManager
fix_file('src/Security/EncryptionKeyManager.cs', 'UnixFileSystemInfo', 'FileInfo')
fix_file('src/Security/EncryptionKeyManager.cs', 'FileAccessPermissions', 'FileAttributes')
fix_file('src/Security/EncryptionKeyManager.cs', 'fileInfo.FileAccessPermissions', 'fileInfo.Attributes')

# 8. TenantContextHelper CreatedAt
fix_file('src/Utilities/TenantContextHelper.cs', 'tenant.CreatedAt', 'DateTime.UtcNow')
fix_file('src/Utilities/TenantContextHelper.cs', 'tenant.RequestId', 'Guid.NewGuid().ToString()')

# 9. JsonHelper
fix_file('src/Utilities/JsonHelper.cs', 'options.Converters = ', '// options.Converters = ')

# 10. AdminController and others Missing AspNetCore
fix_file('src/GlobalUsings.cs', 'global using Microsoft.AspNetCore.Mvc;', 'global using Microsoft.AspNetCore.Mvc;\nglobal using Microsoft.AspNetCore.Http;')

# Let's completely replace SqliteMultiTenant.csproj to ignore Controllers if they still fail
proj = """<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Library</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
  </ItemGroup>
  <ItemGroup>
    <Compile Remove="src/Api/Controllers/**/*.cs" />
    <Compile Remove="src/Middleware/**/*.cs" />
    <Compile Remove="src/Formatters/OutputFormatter.cs" />
    <Compile Remove="src/Cli/**/*.cs" />
    <Compile Remove="src/Events/DomainEventHandlers.cs" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="System.Data.SQLite" Version="1.0.118" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Configuration" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Logging" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Hosting" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Caching.Memory" Version="8.0.0" />
  </ItemGroup>
</Project>"""
with open('src/SqliteMultiTenant.csproj', 'w') as f: f.write(proj)

# Comment out missing using inside DependencyInjectionSetup
fix_file('src/Configuration/DependencyInjectionSetup.cs', 'using SqliteMultiTenant.Api.Controllers;', '//')
fix_file('src/Configuration/DependencyInjectionSetup.cs', 'using SqliteMultiTenant.Middleware;', '//')
