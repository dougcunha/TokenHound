"""Regression tests using isolated temporary directories; no repository mutations."""
import os
import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch

from check_target import classify
from symlink import create_link


class HelpersTest(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.base = Path(self.temp.name).resolve()
        self.root = self.base / "repo"
        self.root.mkdir()
        self.target = self.root / "AGENTS.md"
        self.target.write_text("canonical", encoding="utf-8")
        self.link = self.root / "CLAUDE.md"

    def symlink(self, link, target, directory=False):
        try:
            link.symlink_to(target, target_is_directory=directory)
        except OSError as exc:
            if getattr(exc, "winerror", None) == 1314:
                self.skipTest("Windows symlink privilege unavailable")
            raise

    def test_real_missing_directory_and_invalid_root(self):
        self.assertEqual(classify(self.target, self.root), "REAL")
        self.assertEqual(classify(self.link, self.root), "MISSING")
        self.assertTrue(classify(self.root, self.root).startswith("INVALID_TARGET"))
        with self.assertRaises(OSError):
            classify(self.link, self.root / "missing")

    def test_idempotent_and_relative(self):
        self.symlink(self.link, self.target)
        for force in (False, True):
            create_link(self.link, self.target, self.root, force)
            self.assertEqual(self.target.read_text(), "canonical")
        nested = self.root / ".github" / "copilot-instructions.md"
        create_link(nested, self.target, self.root)
        self.assertFalse(os.path.isabs(os.readlink(nested)))
        self.assertEqual(nested.read_text(), "canonical")

    def test_force_preserves_external_target(self):
        external = self.base / "external.md"
        external.write_text("external")
        self.symlink(self.link, external)
        self.assertTrue(classify(self.link, self.root).startswith("EXTERNAL_SYMLINK"))
        create_link(self.link, self.target, self.root, True)
        self.assertEqual(external.read_text(), "external")
        self.assertEqual(self.link.resolve(), self.target)

    def test_same_basename_is_not_same_target(self):
        other = self.root / "other" / "AGENTS.md"
        other.parent.mkdir()
        other.write_text("other")
        self.symlink(self.link, other)
        with self.assertRaises(FileExistsError):
            create_link(self.link, self.target, self.root)
        create_link(self.link, self.target, self.root, True)
        self.assertEqual(other.read_text(), "other")
        self.assertEqual(self.link.resolve(), self.target)

    def test_self_target_and_directory_rejected(self):
        with self.assertRaises(ValueError):
            create_link(self.target, self.target, self.root, True)
        with self.assertRaises(ValueError):
            create_link(self.link, self.root, self.root, True)
        self.link.mkdir()
        with self.assertRaises(ValueError):
            create_link(self.link, self.target, self.root, True)
        self.assertEqual(self.target.read_text(), "canonical")

    def test_failure_preserves_existing_file(self):
        self.link.write_text("keep")
        with patch("symlink.os.symlink", side_effect=OSError("denied")):
            with self.assertRaises(OSError):
                create_link(self.link, self.target, self.root, True)
        self.assertEqual(self.link.read_text(), "keep")
        self.assertEqual(list(self.root.glob(".ai-ready-link-*")), [])

    def test_replace_failure_preserves_existing_file(self):
        probe = self.root / "probe"
        self.symlink(probe, self.target)
        probe.unlink()
        self.link.write_text("keep")
        with patch("symlink.os.replace", side_effect=OSError("denied")):
            with self.assertRaises(OSError):
                create_link(self.link, self.target, self.root, True)
        self.assertEqual(self.link.read_text(), "keep")

    def test_external_parent_existing_and_missing(self):
        external = self.base / "outside"
        external.mkdir()
        (external / "exists.md").write_text("outside")
        parent = self.root / "linked"
        self.symlink(parent, external, True)
        for name in ("exists.md", "missing.md"):
            path = parent / name
            self.assertTrue(classify(path, self.root).startswith("EXTERNAL_PATH"))
            with self.assertRaises(ValueError):
                create_link(path, self.target, self.root, True)
        self.assertEqual((external / "exists.md").read_text(), "outside")
        self.assertFalse((external / "missing.md").exists())

    def test_broken_and_cycle(self):
        self.symlink(self.link, self.root / "missing")
        self.assertTrue(classify(self.link, self.root).startswith("BROKEN_SYMLINK"))
        self.link.unlink()
        self.symlink(self.link, self.link)
        try:
            status = classify(self.link, self.root)
        except (OSError, RuntimeError):
            return
        self.assertTrue(status.startswith("BROKEN_SYMLINK"))

    def test_existing_regular_requires_force(self):
        self.link.write_text("keep")
        with self.assertRaises(FileExistsError):
            create_link(self.link, self.target, self.root)
        self.assertEqual(self.link.read_text(), "keep")

    def test_parent_traversal_not_lexically_normalized(self):
        with self.assertRaises(ValueError):
            classify(self.root / "alias" / ".." / "AGENTS.md", self.root)


if __name__ == "__main__":
    unittest.main()
