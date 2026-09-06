"""Create a relative instruction symlink without modifying its previous target."""
import argparse
import os
import sys
import tempfile
from pathlib import Path


def create_link(link: Path, target: Path, root: Path, force: bool = False) -> str:
    root = root.resolve(strict=True)
    if not root.is_dir():
        raise ValueError(f"{root} is not a directory")
    # Resolve parents only: resolving the final component would mutate its target.
    link = link.parent.resolve() / link.name
    target = target.resolve(strict=True)
    if not link.is_relative_to(root) or not target.is_relative_to(root):
        raise ValueError("link parent and target must remain inside repo-root")
    if not target.is_file():
        raise ValueError(f"target {target} is not a regular file")
    if link == target:
        raise ValueError("link and target must be different paths")
    if link.is_symlink() and link.resolve() == target:
        return f"OK: {link} already points to {target}"
    if link.is_dir():
        raise ValueError(f"refusing to replace directory {link}")
    if os.path.lexists(link) and not force:
        raise FileExistsError(f"{link} exists; preserve unique content before using --force")

    relative_target = os.path.relpath(target, start=link.parent)
    link.parent.mkdir(parents=True, exist_ok=True)
    if force:
        # Stage on the same filesystem so a failed creation preserves the old entry.
        with tempfile.TemporaryDirectory(prefix=".ai-ready-link-", dir=link.parent) as staging:
            pending = Path(staging) / "link"
            os.symlink(relative_target, pending)
            os.replace(pending, link)
    else:
        # Direct creation is exclusive; rename can overwrite a racing file on POSIX.
        os.symlink(relative_target, link)
    return f"OK: created {link} -> {relative_target}"


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("link_path")
    parser.add_argument("target_path")
    parser.add_argument("--repo-root", required=True)
    parser.add_argument("--force", action="store_true")
    args = parser.parse_args()
    try:
        print(create_link(Path(args.link_path), Path(args.target_path),
                          Path(args.repo_root), args.force))
    except (OSError, RuntimeError, ValueError) as exc:
        print(f"ERROR: {exc}", file=sys.stderr)
        if getattr(exc, "winerror", None) == 1314:
            print("Symlink privilege unavailable; use a verified supported import or "
                  "ask the user to enable symlinks.", file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
