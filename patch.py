import re
import os

def patch_file(path, pattern, repl):
    if not os.path.exists(path): return
    with open(path, 'r') as f: content = f.read()
    content = re.sub(pattern, repl, content, flags=re.MULTILINE | re.DOTALL)
    with open(path, 'w') as f: f.write(content)

# 1. OutputFormatter.cs has duplicate CsvFormatter, JsonFormatter, XmlFormatter
patch_file('src/Formatters/OutputFormatter.cs', r'public class (CsvFormatter|JsonFormatter|XmlFormatter).*?^\}', r'// \g<0>')

# 2. ValidationRuleBuilder.cs has duplicate ValidationResult, ValidationError
patch_file('src/Validation/ValidationRuleBuilder.cs', r'public class (ValidationResult|ValidationError).*?^\}', r'// \g<0>')

# 3. DomainEventHandlers.cs has duplicate TenantCreatedEvent, BackupCompletedEvent
patch_file('src/Events/DomainEventHandlers.cs', r'public class (TenantCreatedEvent|BackupCompletedEvent).*?^\}', r'// \g<0>')

# 4. DistributedCacheService.cs has duplicate CacheStatistics
patch_file('src/Caching/DistributedCacheService.cs', r'public class CacheStatistics.*?^\}', r'// \g<0>')

# 5. CommandParser.cs has duplicate ParsedCommand
patch_file('src/Cli/CommandParser.cs', r'public class ParsedCommand.*?^\}', r'// \g<0>')

# 6. WebhookService.cs has duplicate WebhookSubscription
patch_file('src/Integration/WebhookService.cs', r'public class WebhookSubscription.*?^\}', r'// \g<0>')

# 7. ServiceConfiguration.cs has duplicate MultiTenantOptions
patch_file('src/Configuration/ServiceConfiguration.cs', r'public class MultiTenantOptions.*?^\}', r'// \g<0>')

# 8. ApiResponses.cs ApiResponse<T> already contains a definition for 'Success'
patch_file('src/Api/Responses/ApiResponses.cs', r'public bool Success \{ get; set; \} = true;(.*?public bool Success \{ get; set; \})', r'\1')

# 9. SettingsController.cs ambiguous IConfigurationManager
patch_file('src/Api/Controllers/SettingsController.cs', r'IConfigurationManager _configManager', r'SqliteMultiTenant.Configuration.IConfigurationManager _configManager')

