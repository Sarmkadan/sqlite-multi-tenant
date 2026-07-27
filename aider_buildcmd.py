#!/usr/bin/env python3
"""
Inner build helper script for the `sqlite-multi-tenant` repository.

The outer wrapper (`/home/redrocket/task-factory/aider_buildcmd.py`) changes the
working directory to the repository root and then invokes this script.  The
previous version attempted to run a `build.sh` located in a different project
(`sql-index-advisor`), which caused the “No such file or directory” error.

This script now correctly locates and executes the `build.sh` that lives in the
same directory as this file (`workdir/sqlite-multi-tenant/build.sh`).  It forwards
the output of the build script directly to the console and returns the same
exit code, so `dotnet test` will run as expected.
"""

import subprocess
import sys
from pathlib import Path

def main() -> None:
    # The build script is expected to be in the same directory as this file.
    repo_root = Path(__file__).parent
    build_sh = repo_root / "build.sh"

    if not build_sh.is_file():
        print(
            f"Error: build.sh not found at expected location: {build_sh}",
            file=sys.stderr,
        )
        sys.exit(1)

    try:
        # Run the build script using bash.  Capture both stdout and stderr so
        # they are displayed exactly as the script produces them.
        result = subprocess.run(
            ["bash", str(build_sh)],
            cwd=repo_root,
            stdout=subprocess.PIPE,
            stderr=subprocess.STDOUT,
            text=True,
        )
        # Forward the script's output to the console.
        print(result.stdout, end="")
        sys.exit(result.returncode)
    except Exception as exc:
        print(f"Unexpected error while executing build.sh: {exc}", file=sys.stderr)
        sys.exit(1)


if __name__ == "__main__":
    main()
