"""Read-only, deterministic inventory; file presence does not prove agent loading."""
import argparse
import hashlib
import os
import sys
from pathlib import Path

PRUNE_DIRS = {
    ".git", "node_modules", "bin", "obj", "dist", "build", "out",
    ".venv", "venv", "vendor", ".next", "target", "__pycache__", ".cache",
}
CORE_FILES = {
    "Claude Code": "CLAUDE.md",
    "Codex": "AGENTS.md",
    "OpenCode": "AGENTS.md",
    "Copilot": ".github/copilot-instructions.md",
}
RULE_NAMES = {
    "CLAUDE.md", "CLAUDE.local.md", "AGENTS.md", "AGENTS.override.md", "SKILL.md",
    ".cursorrules", ".clinerules", ".windsurfrules",
}
RULE_DIRS = (".cursor/rules", ".claude/rules", ".clinerules", ".windsurf/rules")


def describe(path: Path, root: Path) -> str:
    rel = path.relative_to(root).as_posix()
    target = path.resolve()
    if not target.is_relative_to(root):
        return f"{rel} -> EXTERNAL {target} (content not read)"
    if path.is_symlink():
        if not path.exists():
            return f"{rel} -> BROKEN symlink -> {os.readlink(path)}"
        return f"{rel} -> symlink -> {os.readlink(path)} -> {describe(target, root)}"
    if path.is_dir():
        return f"{rel} -> DIRECTORY"
    data = path.read_bytes()
    digest = hashlib.sha256(data).hexdigest()
    return f"{rel} -> regular file, {len(data)} bytes, sha256={digest}"


def inventory(root: Path) -> tuple[list[str], list[str]]:
    root = root.resolve(strict=True)
    if not root.is_dir():
        raise ValueError(f"{root} is not a directory")
    lines = [f"# AI-instruction inventory for {root}",
             "Excluded directories: " + ", ".join(sorted(PRUNE_DIRS)),
             "Directory links/junctions are listed but not traversed.",
             "\n## Core entry candidates (verify configuration and loading)"]
    errors = []

    def emit(path: Path) -> None:
        try:
            lines.append("- " + describe(path, root))
        except (OSError, RuntimeError, ValueError) as exc:
            errors.append(f"{path}: {exc}")
            lines.append(f"- {path.relative_to(root).as_posix()} -> ERROR: {exc}")

    for tool, rel in CORE_FILES.items():
        path = root / rel
        lines.append(f"Tool: {tool}")
        if os.path.lexists(path):
            emit(path)
        else:
            lines.append(f"- {rel} -> MISSING")

    lines.append("\n## Additional instructions, skills and directory links")
    for dirpath, dirnames, filenames in os.walk(root, followlinks=False,
                                               onerror=lambda exc: errors.append(str(exc))):
        directory = Path(dirpath)
        descend = []
        for name in sorted(dirnames):
            child = directory / name
            if name in PRUNE_DIRS:
                continue
            try:
                is_junction = getattr(child, "is_junction", lambda: False)()
                linked = child.is_symlink() or is_junction or child.resolve() != child
            except (OSError, RuntimeError, ValueError) as exc:
                errors.append(f"{child}: {exc}")
                lines.append(f"- {child.relative_to(root).as_posix()} -> ERROR: {exc}")
                continue
            if linked:
                emit(child)
            else:
                descend.append(name)
                if name in RULE_NAMES:
                    emit(child)
        dirnames[:] = descend
        for name in sorted(filenames):
            path = directory / name
            rel = path.relative_to(root).as_posix()
            if rel in CORE_FILES.values():
                continue
            in_rule_dir = any(rel.startswith(prefix + "/") for prefix in RULE_DIRS)
            if name in RULE_NAMES or name.endswith(".instructions.md") or (
                in_rule_dir and path.suffix in {".md", ".mdc"}
            ):
                emit(path)
    return lines, errors


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("repo_root", nargs="?", default=".")
    args = parser.parse_args()
    try:
        lines, errors = inventory(Path(args.repo_root))
    except (OSError, RuntimeError, ValueError) as exc:
        print(f"ERROR: {exc}", file=sys.stderr)
        return 1
    print("\n".join(lines))
    for error in errors:
        print(f"ERROR: {error}", file=sys.stderr)
    return 1 if errors else 0


if __name__ == "__main__":
    sys.exit(main())
