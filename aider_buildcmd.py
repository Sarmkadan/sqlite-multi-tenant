#!/usr/bin/env python3
"""
Wrapper script placed at /home/redrocket/task-factory/aider_buildcmd.py.

The original build helper script lives inside the repository at
`sqlite-multi-tenant/aider_buildcmd.py`. This wrapper simply changes the
working directory to the repository root and forwards the call to the
actual script, allowing the command `python3 /home/redrocket/task-factory/aider_buildcmd.py`
to work as expected.
"""

import subprocess
import sys
from pathlib import Path

def main() -> None:
    # Determine the repository root relative to this wrapper script.
    # The repository is assumed to be a sibling directory named
    # `sqlite-multi-tenant`.
    repo_root = Path(__file__).parent / "sqlite-multi-tenant"

    if not repo_root.is_dir():
        print(
            f"Error: repository root not found at expected location: {repo_root}",
            file=sys.stderr,
        )
        sys.exit(1)

    # Path to the actual build helper script inside the repository.
    inner_script = repo_root / "aider_buildcmd.py"

    if not inner_script.is_file():
        print(
            f"Error: inner build script not found at {inner_script}",
            file=sys.stderr,
        )
        sys.exit(1)

    # Execute the inner script with the same arguments.
    try:
        result = subprocess.run(
            [sys.executable, str(inner_script)] + sys.argv[1:],
            cwd=repo_root,
            stdout=subprocess.PIPE,
            stderr=subprocess.STDOUT,
            text=True,
        )
        print(result.stdout)
        sys.exit(result.returncode)
    except FileNotFoundError:
        print(
            "Error: Python interpreter not found.",
            file=sys.stderr,
        )
        sys.exit(1)
    except Exception as exc:
        print(f"Unexpected error while running inner script: {exc}", file=sys.stderr)
        sys.exit(1)


if __name__ == "__main__":
    main()
