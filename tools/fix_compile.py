import os

def fix_file(file, old, new):
    if not os.path.exists(file): return
    with open(file, 'r') as f:
        content = f.read()
    content = content.replace(old, new)
    with open(file, 'w') as f:
        f.write(content)

fix_file('src/Formatters/OutputFormatter.cs', 'class CsvFormatter', 'class CsvFormatterDup')
fix_file('src/Formatters/OutputFormatter.cs', 'class JsonFormatter', 'class JsonFormatterDup')
fix_file('src/Formatters/OutputFormatter.cs', 'class XmlFormatter', 'class XmlFormatterDup')
fix_file('src/Validation/ValidationRuleBuilder.cs', 'class ValidationResult', 'class ValidationResultDup')
fix_file('src/Validation/ValidationRuleBuilder.cs', 'class ValidationError', 'class ValidationErrorDup')
fix_file('src/Events/DomainEventHandlers.cs', 'class TenantCreatedEvent', 'class TenantCreatedEventDup')
fix_file('src/Events/DomainEventHandlers.cs', 'class BackupCompletedEvent', 'class BackupCompletedEventDup')
fix_file('src/Caching/DistributedCacheService.cs', 'class CacheStatistics', 'class CacheStatisticsDup')
fix_file('src/Cli/CommandParser.cs', 'class ParsedCommand', 'class ParsedCommandDup')
fix_file('src/Integration/WebhookService.cs', 'class WebhookSubscription', 'class WebhookSubscriptionDup')
fix_file('src/Configuration/ServiceConfiguration.cs', 'class MultiTenantOptions', 'class MultiTenantOptionsDup')
fix_file('src/Api/Responses/ApiResponses.cs', 'public bool Success { get; set; } = true;', '')

# Fix ambiguous IConfigurationManager
fix_file('src/Api/Controllers/SettingsController.cs', 'IConfigurationManager _configManager', 'SqliteMultiTenant.Configuration.IConfigurationManager _configManager')
fix_file('src/Api/Controllers/SettingsController.cs', 'IConfigurationManager configManager', 'SqliteMultiTenant.Configuration.IConfigurationManager configManager')

# Fix SqliteMultiTenantOptions not found
fix_file('src/Configuration/DependencyInjectionSetup.cs', 'SqliteMultiTenantOptions', 'MultiTenantOptions')

# Fix CsvFormatter.Format signature matching
fix_file('src/Formatters/CsvFormatter.cs', 'public string Format<T>(T? data)', 'public string Format<T>(T data)')
fix_file('src/Formatters/JsonFormatter.cs', 'public string Format<T>(T? data)', 'public string Format<T>(T data)')

# Fix missing ResultWrapper
fix_file('src/Api/ApiResponseBuilder.cs', 'ResultWrapper<T>', 'SqliteMultiTenant.Api.Responses.ResultWrapper<T>')
