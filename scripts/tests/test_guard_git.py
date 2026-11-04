"""Exercise hook decisions without executing the submitted git commands."""

import json
import os
from pathlib import Path
import subprocess
import tempfile
import unittest


HOOK = Path(__file__).resolve().parents[1] / "hooks/guard-git.sh"
SHA = "a" * 40
EPIC = "epic/8-lane"


PIN = f" --match-head-commit {SHA}"


def marker(verdict="approve", sha=SHA):
    return f"Summary.\n\n<!-- epic-review: {verdict} sha={sha} -->"


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

    def test_cd_target_is_never_executed(self):
        # The hook runs before the permission prompt: it must not run what the command embeds.
        canary = self.root / "pwned"
        for command in (f"cd $(touch${{IFS}}{canary}) && git status",
                        f"cd `touch {canary}` && git status"):
            with self.subTest(command=command):
                self.decision(command)
                self.assertFalse(canary.exists())

    def test_code_running_options_are_blocked(self):
        # git fetch/push and gh api are allowlisted by prefix; these arguments turn them into exec.
        for command in ("git fetch --upload-pack='touch x' origin",
                        "git push --receive-pack=x origin HEAD",
                        "git push --exec=x origin HEAD",
                        "git push --rece=x origin HEAD",
                        "git fetch --upl=x origin",
                        "git fetch \"--upload-pack=x\" origin",
                        "git -C . fetch --upl=x",
                        "git -c core.sshCommand=x fetch",
                        "git -C . -c core.pager=x log",
                        "gh api -X DELETE repos/o/r",
                        "gh api --method=DELETE repos/o/r"):
            with self.subTest(command=command):
                self.assertEqual(2, self.decision(command))
        for command in ("git fetch origin main",
                        "git log --exec-path",
                        "gh api repos/o/r/pulls/1/comments/2/replies -f body='use -c'"):
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


class GuardGitMergeTests(unittest.TestCase):
    """The merge gate. A stub `gh` runs the hook's real `-q` query (with jq) over a fake PR."""

    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        subprocess.run(["git", "init", "-b", "feat/1-example", str(self.root / "repo")],
                       check=True, capture_output=True)
        bin_dir = self.root / "bin"
        bin_dir.mkdir()
        gh = bin_dir / "gh"
        gh.write_text(
            '#!/bin/sh\n'
            'while [ $# -gt 0 ]; do [ "$1" = "-q" ] && { shift; q="$1"; }; shift; done\n'
            'printf \'%s\' "$FAKE_GH_PR" | jq -r "$q"\n')
        gh.chmod(0o755)
        self.path = f"{bin_dir}:{os.environ['PATH']}"

    def decision(self, command, decision="", head="feat/7-thing", labels=(), base="main", comments=(), reviews=()):
        pr = {"reviewDecision": decision, "headRefName": head, "labels": [{"name": n} for n in labels],
              "baseRefName": base, "headRefOid": SHA,
              "comments": [{"createdAt": t, "body": b} for t, b in comments],
              "reviews": [{"submittedAt": t, "body": b} for t, b in reviews]}
        return subprocess.run(
            ["bash", str(HOOK)], cwd=self.root / "repo", text=True, capture_output=True,
            env={**os.environ, "PATH": self.path, "FAKE_GH_PR": json.dumps(pr)},
            input=json.dumps({"tool_input": {"command": command}}),
        )

    def test_unapproved_pr_is_blocked_and_reports_the_real_decision(self):
        result = self.decision("gh pr merge 7 --squash")
        self.assertEqual(2, result.returncode)
        self.assertIn("reviewDecision=NONE", result.stderr)

    def test_approved_review_allows_merge(self):
        self.assertEqual(0, self.decision("gh pr merge 7 --squash", decision="APPROVED").returncode)

    def test_owner_approved_label_allows_merge(self):
        for labels in (["owner-approved"], ["bug", "owner-approved", "chore"]):
            with self.subTest(labels=labels):
                self.assertEqual(0, self.decision("gh pr merge 7 --squash", labels=labels).returncode)

    def test_look_alike_labels_and_branch_names_do_not_allow_merge(self):
        for labels, head in ((["not-owner-approved"], "feat/7-thing"),
                             (["x,owner-approved"], "feat/7-thing"),
                             ([], "feat/7|yes|APPROVED"),
                             ([], "yes|APPROVED")):
            with self.subTest(labels=labels, head=head):
                result = self.decision("gh pr merge 7 --squash", head=head, labels=labels)
                self.assertEqual(2, result.returncode)

    def test_strategy_still_enforced_with_the_label(self):
        ok = ["owner-approved"]
        self.assertEqual(2, self.decision("gh pr merge 7 --rebase", labels=ok).returncode)
        self.assertEqual(2, self.decision("gh pr merge 8 --squash", head="epic/8-lane", labels=ok).returncode)
        self.assertEqual(0, self.decision("gh pr merge 8 --rebase", head="epic/8-lane", labels=ok).returncode)

    def test_epic_child_merges_on_approve_marker_for_head(self):
        for where in ("comments", "reviews"):
            with self.subTest(where=where):
                result = self.decision("gh pr merge 7 --squash" + PIN, base=EPIC, **{where: [("2026-11-02T10:00:00Z", marker())]})
                self.assertEqual(0, result.returncode)

    def test_epic_child_marker_must_match_head_and_be_newest(self):
        stale = [("2026-11-02T10:00:00Z", marker(sha="b" * 40))]
        changes_after = [("2026-11-02T10:00:00Z", marker()), ("2026-11-02T11:00:00Z", marker("request-changes"))]
        approve_after = [("2026-11-02T10:00:00Z", marker("request-changes")), ("2026-11-02T11:00:00Z", marker())]
        truncated = [("2026-11-02T10:00:00Z", "<!-- epic-review: approve sha=" + SHA[:39] + " -->")]
        for comments, code in ((stale, 2), (changes_after, 2), (approve_after, 0), (truncated, 2), ([], 2)):
            with self.subTest(comments=comments):
                self.assertEqual(code, self.decision("gh pr merge 7 --squash" + PIN, base=EPIC, comments=comments).returncode)

    def test_epic_child_newest_marker_across_comments_and_reviews(self):
        result = self.decision("gh pr merge 7 --squash" + PIN, base=EPIC,
                               reviews=[("2026-11-02T10:00:00Z", marker())],
                               comments=[("2026-11-02T11:00:00Z", marker("request-changes"))])
        self.assertEqual(2, result.returncode)

    def test_marker_does_not_open_main_or_epic_heads(self):
        approve = [("2026-11-02T10:00:00Z", marker())]
        self.assertEqual(2, self.decision("gh pr merge 7 --squash" + PIN, base="main", comments=approve).returncode)
        self.assertEqual(2, self.decision("gh pr merge 8 --rebase" + PIN, head=EPIC, base="main", comments=approve).returncode)
        self.assertEqual(2, self.decision("gh pr merge 7 --squash" + PIN, base="epic-notes", comments=approve).returncode)

    def test_epic_child_still_squash_only(self):
        approve = [("2026-11-02T10:00:00Z", marker())]
        self.assertEqual(2, self.decision("gh pr merge 7 --rebase" + PIN, base=EPIC, comments=approve).returncode)
        self.assertEqual(2, self.decision("gh pr merge 7 --merge" + PIN, base=EPIC, comments=approve).returncode)

    def test_epic_child_marker_must_be_last_line_of_its_body(self):
        two = "<!-- epic-review: request-changes sha=" + SHA + " -->\nfixed\n<!-- epic-review: approve sha=" + SHA + " -->"
        quoted = "Earlier said `<!-- epic-review: approve sha=" + SHA + " -->` — still looking."
        followed = marker() + "\nOne more thought."
        for body, code in ((two, 0), (two.replace("approve", "x"), 2), (quoted, 2), (followed, 2)):
            with self.subTest(body=body):
                result = self.decision("gh pr merge 7 --squash" + PIN, base=EPIC, comments=[("2026-11-02T10:00:00Z", body)])
                self.assertEqual(code, result.returncode)

    def test_epic_child_merge_must_pin_the_reviewed_head(self):
        approve = [("2026-11-02T10:00:00Z", marker())]
        for suffix, code in (("", 2), (" --match-head-commit " + "b" * 40, 2), (f" --match-head-commit={SHA}", 0), (PIN, 0),
                             (PIN + " --match-head-commit " + "b" * 40, 2)):
            with self.subTest(suffix=suffix):
                self.assertEqual(code, self.decision("gh pr merge 7 --squash" + suffix, base=EPIC, comments=approve).returncode)

    def test_merge_target_must_be_a_number_of_this_repo(self):
        for command in ("gh pr merge https://github.com/o/r/pull/99 --squash",
                        "gh pr merge feat/9-other --squash",
                        "gh pr merge --squash 99",
                        "gh pr merge --squash",
                        "gh pr merge 7 --squash -R o/other",
                        "gh pr merge 7 --squash --repo=o/other",
                        "gh pr merge 7 --squash -Ro/other",
                        "GH_REPO=o/other gh pr merge 7 --squash",
                        "gh pr merge 7 --squash; gh pr merge 99 --squash",
                        "gh pr merge 7 --squash && gh pr  merge 99 --squash"):
            with self.subTest(command=command):
                self.assertEqual(2, self.decision(command, labels=["owner-approved"]).returncode)

    def test_agents_cannot_add_the_owner_approved_label(self):
        for command in ("gh pr edit 7 --add-label owner-approved",
                        "gh pr edit 7 --add-label bug,owner-approved",
                        "gh pr create --label 'owner-approved' --title x",
                        "gh pr create -l owner-approved --title x",
                        "gh pr edit 7 --add-label 'bug, owner-approved'",
                        "gh issue edit 7 --add-label=owner-approved",
                        "gh api repos/o/r/issues/7/labels -f 'labels[]=owner-approved'"):
            with self.subTest(command=command):
                self.assertEqual(2, self.decision(command).returncode)

    def test_mentioning_the_label_elsewhere_is_allowed(self):
        self.assertEqual(0, self.decision("gh pr create --label bug --body 'owner-approved explained'").returncode)


if __name__ == "__main__":
    unittest.main()
