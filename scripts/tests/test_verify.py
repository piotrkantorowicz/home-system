"""Verify staged deletion routing using failing tool stubs, not a real stack."""

import os
from pathlib import Path
import shutil
import subprocess
import tempfile
import unittest


SCRIPT = Path(__file__).resolve().parents[1] / "verify.sh"


class VerifyTests(unittest.TestCase):
    def test_staged_deletions_run_checks_and_propagate_failure(self):
        for path, expected in (
            ("src/Modules/Demo/Demo.Domain/Required.cs", "dotnet build"),
            ("src/ui/src/required.ts", "ui build"),
            ("e2e/required.spec.ts", "e2e tsc"),
        ):
            with self.subTest(path=path), tempfile.TemporaryDirectory() as temp:
                root = Path(temp)
                def git(*args):
                    result = subprocess.run(["git", *args], cwd=root, capture_output=True, text=True)
                    self.assertEqual(0, result.returncode, result.stderr)
                git("init", "-b", "feat/1-example")
                (root / "scripts").mkdir()
                shutil.copy(SCRIPT, root / "scripts/verify.sh")
                target = root / path
                target.parent.mkdir(parents=True)
                target.write_text("required\n")
                git("add", ".")
                git("-c", "user.name=Test", "-c", "user.email=test@example.invalid",
                    "-c", "core.hooksPath=/dev/null", "-c", "commit.gpgsign=false",
                    "commit", "-m", "test: fixture")
                target.unlink()
                git("add", "-u")
                stubs = root / "stubs"
                stubs.mkdir()
                for name in ("dotnet", "npm", "npx"):
                    tool = stubs / name
                    tool.write_text("#!/bin/sh\nexit 1\n")
                    tool.chmod(0o755)
                result = subprocess.run(
                    ["bash", "scripts/verify.sh", "--staged"], cwd=root,
                    env={**os.environ, "PATH": f"{stubs}:{os.environ['PATH']}"},
                    capture_output=True, text=True,
                )
                self.assertEqual(1, result.returncode, result.stdout + result.stderr)
                self.assertIn(expected, result.stdout)
                self.assertNotIn("nothing to verify", result.stdout)


if __name__ == "__main__":
    unittest.main()
