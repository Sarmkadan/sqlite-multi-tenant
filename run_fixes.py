import os
import re

def process_file(filepath):
    with open(filepath, 'r', encoding='utf-8') as f:
        content = f.read()

    original = content

    # a) Add '#nullable enable' to top
    if '#nullable enable' not in content:
        content = '#nullable enable\n' + content

    # Tokenise string literals and comments
    token_pattern = re.compile(r'(/\*[\s\S]*?\*/|//.*?$|"""[\s\S]*?"""|@"(?:[^"]|"")*"|"[^"\\]*(?:\\.[^"\\]*)*")', re.MULTILINE)
    
    parts = token_pattern.split(content)
    
    for i in range(0, len(parts), 2):
        code = parts[i]
        
        # b) null checks
        code = code.replace(' == null', ' is null')
        code = code.replace(' != null', ' is not null')
        
        # c) Replace 'Array.Empty<' with '[]'
        code = re.sub(r'\bArray\.Empty\s*<\s*[A-Za-z0-9_.[\]]+\s*>\s*\(\s*\)', '[]', code)
        
        parts[i] = code

    content = ''.join(parts)
    
    # d) Add sealed
    def seal_classes(text):
        def class_replacer(match):
            access = match.group(1)
            modifiers = match.group(2)
            rest = match.group(3)
            
            if 'static' in modifiers or 'abstract' in modifiers or 'sealed' in modifiers:
                return match.group(0)
            
            return f"{access} sealed {modifiers}class {rest} {{"
            
        return re.sub(r'\b(public|internal)\s+((?:(?:partial|unsafe|new|abstract|sealed|static)\s+)*)class\s+([^{;]+?)\s*\{', class_replacer, text)

    parts = token_pattern.split(content)
    for i in range(0, len(parts), 2):
        parts[i] = seal_classes(parts[i])
        
    content = ''.join(parts)

    if original != content:
        with open(filepath, 'w', encoding='utf-8') as f:
            f.write(content)

for root, dirs, files in os.walk('.'):
    if any(ignore in root.split(os.sep) for ignore in ['.git', 'bin', 'obj']):
        continue
    for f in files:
        if f.endswith('.cs'):
            filepath = os.path.join(root, f)
            try:
                process_file(filepath)
            except Exception as e:
                print(f"Error processing {filepath}: {e}")
