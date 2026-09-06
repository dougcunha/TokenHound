"""Read-only classification of the effective destination of an instruction path."""
import argparse
import sys
from pathlib import Path


def classify(path: Path, root: Path) -> str:
    root = root.resolve(strict=True)
    if not root.is_dir():
        raise ValueError(f"{root} is not a directory")
    if ".." in path.parts:
        raise ValueError("use an absolute path without '..' components")
    path = path.absolute()
    target = path.resolve()
    if not target.is_relative_to(root):
        kind = "EXTERNAL_SYMLINK" if path.is_symlink() else "EXTERNAL_PATH"
        return f"{kind} {target}"
    if path.is_symlink() and not path.exists():
        return f"BROKEN_SYMLINK {target}"
    if not path.exists():
        return "MISSING"
    if not path.is_file():
        return f"INVALID_TARGET {target}"
    if path.is_symlink() or target != path:
        return f"IN_REPO_SYMLINK {target}"
    return "REAL"


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("md_path")
    parser.add_argument("repo_root")
    args = parser.parse_args()
    try:
        print(classify(Path(args.md_path), Path(args.repo_root)))
    except (OSError, RuntimeError, ValueError) as exc:
        print(f"ERROR: {exc}", file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
