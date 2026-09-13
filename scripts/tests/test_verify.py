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

    def test_module_only_change_builds_and_formats_only_that_module(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            def git(*args):
                result = subprocess.run(["git", *args], cwd=root, capture_output=True, text=True)
                self.assertEqual(0, result.returncode, result.stderr)
            git("init", "-b", "feat/1-example")
            (root / "scripts").mkdir()
            shutil.copy(SCRIPT, root / "scripts/verify.sh")
            changed = root / "src/Modules/Demo/Demo.Domain/Required.cs"
            changed.parent.mkdir(parents=True)
            changed.write_text("before\n")
            csproj = root / "src/Modules/Demo/Demo.UnitTests/Demo.UnitTests.csproj"
            csproj.parent.mkdir(parents=True)
            csproj.write_text("<Project />\n")
            (root / "src/Modules/Other/Other.UnitTests").mkdir(parents=True)
            (root / "src/Modules/Other/Other.UnitTests/Other.UnitTests.csproj").write_text("<Project />\n")
            git("add", ".")
            git("-c", "user.name=Test", "-c", "user.email=test@example.invalid",
                "-c", "core.hooksPath=/dev/null", "-c", "commit.gpgsign=false",
                "commit", "-m", "test: fixture")
            changed.write_text("after\n")
            git("add", "-u")
            stubs = root / "stubs"
            stubs.mkdir()
            log = root / "dotnet.log"
            tool = stubs / "dotnet"
            # Copy any solution filter next to the log — verify.sh deletes it after the build.
            tool.write_text(
                f'#!/bin/sh\necho "$@" >> "{log}"\n'
                f'case "$2" in *.slnf) cp "$2" "{root}/built.slnf" ;; esac\nexit 0\n')
            tool.chmod(0o755)
            result = subprocess.run(
                ["bash", "scripts/verify.sh", "--staged"], cwd=root,
                env={**os.environ, "PATH": f"{stubs}:{os.environ['PATH']}"},
                capture_output=True, text=True,
            )
            self.assertEqual(0, result.returncode, result.stdout + result.stderr)
            calls = log.read_text().splitlines()
            self.assertRegex(" ".join(calls), r"build \S+\.slnf")
            self.assertNotIn("build HomeSystem.slnx", " ".join(calls))
            self.assertNotIn("Other.UnitTests", " ".join(calls))
            slnf = (root / "built.slnf").read_text()
            self.assertIn("src/Modules/Demo/Demo.UnitTests/Demo.UnitTests.csproj", slnf)
            self.assertNotIn("Other", slnf)
            self.assertIn("--include src/Modules/Demo/Demo.Domain/Required.cs", " ".join(calls))


if __name__ == "__main__":
    unittest.main()
