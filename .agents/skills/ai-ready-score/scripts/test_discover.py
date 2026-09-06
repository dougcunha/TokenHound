"""Regression tests for inventory completeness and read boundaries."""
import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch

from discover import inventory


class DiscoveryTest(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name).resolve() / "repo"
        self.root.mkdir()

    def write(self, rel, text="instructions"):
        path = self.root / rel
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(text, encoding="utf-8")
        return path

    def symlink(self, path, target, directory=False):
        try:
            path.symlink_to(target, target_is_directory=directory)
        except OSError as exc:
            if getattr(exc, "winerror", None) == 1314:
                self.skipTest("Windows symlink privilege unavailable")
            raise

    def test_hidden_skills_monorepo_and_scoped_rules(self):
        paths = [".agents/skills/a/SKILL.md", ".claude/skills/b/SKILL.md",
                 "packages/app/AGENTS.md", ".github/instructions/a.instructions.md",
                 ".claude/rules/team.md", ".cursor/rules/team.mdc", "AGENTS.override.md"]
        for rel in paths:
            self.write(rel)
        lines, errors = inventory(self.root)
        self.assertFalse(errors)
        for rel in paths:
            self.assertIn(rel, "\n".join(lines))

    def test_complete_and_deterministic_over_twenty(self):
        for number in reversed(range(30)):
            self.write(f"module{number:02}/AGENTS.md")
        first, errors = inventory(self.root)
        self.assertFalse(errors)
        matches = [line for line in first if line.startswith("- module")]
        self.assertEqual(len(matches), 30)
        self.assertEqual(matches, sorted(matches))
        self.assertEqual(first, inventory(self.root)[0])

    def test_generated_directories_pruned(self):
        self.write("node_modules/pkg/AGENTS.md")
        self.write(".git/AGENTS.md")
        lines, errors = inventory(self.root)
        self.assertFalse(errors)
        self.assertNotIn("node_modules/pkg/AGENTS.md", "\n".join(lines))
        self.assertNotIn(".git/AGENTS.md", "\n".join(lines))

    def test_directory_named_as_instruction(self):
        (self.root / "AGENTS.md").mkdir()
        lines, errors = inventory(self.root)
        self.assertFalse(errors)
        self.assertIn("AGENTS.md -> DIRECTORY", "\n".join(lines))

    def test_external_content_not_read_and_directory_not_traversed(self):
        external = self.root.parent / "outside"
        external.mkdir()
        (external / "AGENTS.md").write_text("private")
        self.symlink(self.root / "CLAUDE.md", external / "AGENTS.md")
        self.symlink(self.root / ".github", external, True)
        with patch.object(Path, "read_bytes", side_effect=AssertionError("external read")):
            lines, errors = inventory(self.root)
        self.assertFalse(errors)
        self.assertIn("EXTERNAL", "\n".join(lines))
        self.assertNotIn(".github/AGENTS.md", "\n".join(lines))

    def test_broken_link(self):
        self.symlink(self.root / "CLAUDE.md", self.root / "missing")
        lines, errors = inventory(self.root)
        self.assertFalse(errors)
        self.assertIn("BROKEN", "\n".join(lines))

    def test_read_error_is_reported(self):
        self.write("AGENTS.md")
        with patch.object(Path, "read_bytes", side_effect=PermissionError("denied")):
            lines, errors = inventory(self.root)
        self.assertTrue(errors)
        self.assertIn("ERROR", "\n".join(lines))

    def test_invalid_root(self):
        with self.assertRaises(OSError):
            inventory(self.root / "missing")

    def test_directory_resolution_error_preserves_partial_inventory(self):
        self.write("AGENTS.md")
        blocked = self.root / "blocked"
        blocked.mkdir()
        original = Path.resolve

        def resolve(path, *args, **kwargs):
            if path == blocked:
                raise PermissionError("denied")
            return original(path, *args, **kwargs)

        with patch.object(Path, "resolve", resolve):
            lines, errors = inventory(self.root)
        self.assertTrue(errors)
        self.assertIn("AGENTS.md -> regular file", "\n".join(lines))


if __name__ == "__main__":
    unittest.main()
