"""Exercise hook decisions without executing the submitted git commands."""

import json
from pathlib import Path
import subprocess
import tempfile
import unittest


HOOK = Path(__file__).resolve().parents[1] / "hooks/guard-git.sh"


class GuardGitTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        subprocess.run(["git", "init", "-b", "feat/1-example", str(self.root)],
                       check=True, capture_output=True)

    def decision(self, command):
        return subprocess.run(
            ["bash", str(HOOK)], cwd=self.root, text=True, capture_output=True,
            input=json.dumps({"tool_input": {"command": command}}),
        ).returncode

    def test_pushes_to_main_are_blocked_from_feature_branch(self):
        for destination in ("main", "HEAD:main", "HEAD:refs/heads/main",
                            "refs/heads/main", ":main", "'HEAD:main'", '"HEAD:main"'):
            for flag in ("", "--force-with-lease "):
                command = f"git push {flag}origin {destination}"
                with self.subTest(command=command):
                    self.assertEqual(2, self.decision(command))

    def test_feature_pushes_remain_allowed(self):
        for command in (
            "git push -u origin HEAD",
            "git push origin HEAD:feat/1-example",
            "git push --force-with-lease origin HEAD:refs/heads/feat/1-example",
            "git push origin main:feat/1-example",
            "git push origin HEAD:main-backup",
        ):
            with self.subTest(command=command):
                self.assertEqual(0, self.decision(command))

    def test_unconditional_force_remains_blocked(self):
        for flag in ("--force", "-f"):
            with self.subTest(flag=flag):
                self.assertEqual(2, self.decision(f"git push {flag} origin HEAD"))


if __name__ == "__main__":
    unittest.main()
