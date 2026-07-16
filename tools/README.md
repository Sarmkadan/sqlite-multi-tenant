# tools/

One-off maintenance scripts kept out of the repo root so the top level reads
as intentional project structure. None of these run as part of the build,
the test suite, or CI - they are ad-hoc source rewriters that were used during
earlier refactors and are preserved for reference/reuse.

Everything here is a throwaway text-surgery script. Read before running, run
from the repository root, and expect it to edit `.cs` files in place.

| Script | What it does |
| --- | --- |
| `fix_all.py` | Regex sweep applying a batch of source fixes across files. |
| `fix_compile.py` | Literal find/replace pass to clear a set of compile errors. |
| `fix_deep.py` | Deeper literal replacements for multi-line constructs. |
| `final_fix.py` | Final cleanup pass of literal replacements. |
| `patch.py` | Regex-based patcher for a targeted set of paths. |
| `run_fixes.py` | Driver that walks files and applies the standard fix pass. |
| `comment_out_bad.py` | Comments out files that failed to compile so the rest builds. |
| `empty_bad.py` | Replaces broken files with an empty stub carrying the header. |
| `Commit message` | Leftover scratch note (a staged commit subject), kept verbatim. |

## Usage

```bash
# always dry-read the script first
python3 tools/fix_all.py
```

These are not maintained utilities - prefer normal editor/IDE refactoring for
new work. They live here only so their history stays intact.
