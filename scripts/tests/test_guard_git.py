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

    def test_pushes_to_epic_destination_need_force_with_lease(self):
        for destination in ("epic/269-modernise", "HEAD:epic/269-modernise",
                            "HEAD:refs/heads/epic/269-modernise", "'HEAD:epic/269-modernise'"):
            with self.subTest(destination=destination):
                self.assertEqual(2, self.decision(f"git push origin {destination}"))
                self.assertEqual(0, self.decision(f"git push --force-with-lease origin {destination}"))

    def test_epic_prefixed_feature_branch_is_not_an_epic(self):
        self.assertEqual(0, self.decision("git push origin HEAD:feat/3-epic/thing"))
        self.assertEqual(0, self.decision("git push origin HEAD:epic-notes"))


class GuardGitOnEpicBranchTests(GuardGitTests):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        subprocess.run(["git", "init", "-b", "epic/269-modernise", str(self.root)],
                       check=True, capture_output=True)

    def test_feature_pushes_remain_allowed(self):
        # Overrides the feature-branch case: a bare push from an epic checkout is the
        # direct-commit path and is blocked unless it is the rebase sync.
        self.assertEqual(2, self.decision("git push -u origin HEAD"))
        self.assertEqual(2, self.decision("git push origin HEAD:feat/1-example"))
        self.assertEqual(0, self.decision("git push --force-with-lease origin HEAD"))

    def test_epic_prefixed_feature_branch_is_not_an_epic(self):
        # From an epic checkout every non-lease push is the direct-commit path.
        self.assertEqual(2, self.decision("git push origin HEAD:feat/3-epic/thing"))

    def test_local_history_changes_are_blocked(self):
        for command in ("git commit -m 'x'", "git merge feat/1-example",
                        "git cherry-pick abc123", "git revert HEAD"):
            with self.subTest(command=command):
                self.assertEqual(2, self.decision(command))

    def test_read_and_branch_commands_remain_allowed(self):
        for command in ("git status", "git log --oneline", "git fetch origin main",
                        "git rebase origin/main", "git checkout -b feat/270-time-provider"):
            with self.subTest(command=command):
                self.assertEqual(0, self.decision(command))


class GuardGitOnMainTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        subprocess.run(["git", "init", "-b", "main", str(self.root)],
                       check=True, capture_output=True)

    def decision(self, command):
        return subprocess.run(
            ["bash", str(HOOK)], cwd=self.root, text=True, capture_output=True,
            input=json.dumps({"tool_input": {"command": command}}),
        ).returncode

    def test_commit_and_push_are_blocked(self):
        self.assertEqual(2, self.decision("git commit -m 'x'"))
        self.assertEqual(2, self.decision("git push origin HEAD"))

    def test_merge_is_not_blocked_by_the_epic_rule(self):
        self.assertEqual(0, self.decision("git merge --ff-only origin/main"))


if __name__ == "__main__":
    unittest.main()
