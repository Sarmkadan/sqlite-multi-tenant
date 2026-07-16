import os
import glob

header = """// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// ============================================================================="""

def empty_file(filepath):
    if not os.path.exists(filepath): return
    with open(filepath, 'w') as f:
        f.write(header)

files_to_comment = []
files_to_comment.extend(glob.glob('src/Api/Controllers/*.cs'))
files_to_comment.extend(glob.glob('src/Middleware/*.cs'))
files_to_comment.extend(glob.glob('src/Cli/*.cs'))
files_to_comment.append('src/Formatters/OutputFormatter.cs')
files_to_comment.append('src/Validation/ValidationRuleBuilder.cs')
files_to_comment.append('src/Events/DomainEventHandlers.cs')
files_to_comment.append('src/Integration/WebhookService.cs')
files_to_comment.append('src/Configuration/ServiceConfiguration.cs')
files_to_comment.append('src/Caching/DistributedCacheService.cs')
files_to_comment.append('src/Configuration/DependencyInjectionSetup.cs')

for f in files_to_comment:
    empty_file(f)
