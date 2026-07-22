#!/usr/bin/env python3
"""
Simple build helper script for the sqlite-multi-tenant repository.

Running this script will invoke `dotnet test` to execute all unit tests.
It is a minimal replacement for the missing `aider_buildcmd.py` that was
referenced in previous commands.
"""

import subprocess
import sys
from pathlib import Path

def main() -> None:
    # Ensure we are running from the repository root (where the .sln or .csproj lives)
    repo_root = Path(__file__).parent
    # Change working directory to the repository root
    try:
        repo_root.chdir()
    except Exception:
        # Fallback: change cwd using os.chdir
        import os
        os.chdir(repo_root)

    # Execute `dotnet test` and forward its exit code
    try:
        result = subprocess.run(
            ["dotnet", "test"],
            stdout=subprocess.PIPE,
            stderr=subprocess.STDOUT,
            text=True,
        )
        # Print the output so the caller sees the test results
        print(result.stdout)
        sys.exit(result.returncode)
    except FileNotFoundError:
        print(
            "Error: `dotnet` CLI not found. Please install the .NET SDK and ensure it is on PATH.",
            file=sys.stderr,
        )
        sys.exit(1)
    except Exception as exc:
        print(f"Unexpected error while running tests: {exc}", file=sys.stderr)
        sys.exit(1)


if __name__ == "__main__":
    main()
