#!/usr/bin/env bash
# PreToolUse hook for Bash. Blocks git/gh commands that would bypass the delivery loop.
# Input: Claude Code hook JSON on stdin. Exit 2 = block (stderr goes back to the agent).
# Codex: register the same script in ~/.codex/hooks.json under PreToolUse.
set -uo pipefail
input=$(cat)
cmd=$(jq -r '.tool_input.command // empty' <<< "$input" 2>/dev/null)
[[ -z "$cmd" ]] && exit 0

block() { echo "🛑 guard-git: $1" >&2; exit 2; }

# Which checkout does the command act on? With worktrees the hook's own cwd can sit on a
# different branch than the one the command touches. Priority:
# `git -C <dir>` > leading `cd <dir>` (after `;`, `&&`, `|` too) > hook cwd.
target=$(grep -Eo -- 'git[[:space:]]+-C[[:space:]]+[^[:space:];&|]+' <<< "$cmd" | head -1 | awk '{print $3}')
[[ -z "$target" ]] && target=$(grep -Eo -- '(^|[;&|][[:space:]]*)cd[[:space:]]+[^[:space:];&|]+' <<< "$cmd" | head -1 | sed -E 's/.*cd[[:space:]]+//')
[[ -z "$target" ]] && target=$(jq -r '.cwd // empty' <<< "$input" 2>/dev/null)
# expand $VAR / ~ the way the shell would; fall back to "." on anything odd
target=$(eval "printf '%s' \"${target:-.}\"" 2>/dev/null || printf '.')
[[ -d "$target" ]] || target=.

# --show-current also works on an unborn branch (fresh clone / worktree, test repos).
branch=$(git -C "$target" branch --show-current 2>/dev/null || echo "")

# 1. Nothing is committed or pushed directly on main — always branch first.
if [[ "$branch" == "main" ]] && grep -Eq '(^|[;&|[:space:]])git[[:space:]]+(commit|push)([[:space:]]|$)' <<< "$cmd"; then
  block "on 'main'. Create a branch first: git checkout -b <type>/<issue>-<slug>"
fi

# 1b. An epic branch only receives commits through squash-merged child PRs. The one
#     local operation it allows is the rebase onto main before ship-epic, pushed with
#     --force-with-lease (rule 2).
if [[ "$branch" == epic/* ]] && grep -Eq '(^|[;&|[:space:]])git[[:space:]]+(commit|merge|cherry-pick|revert)([[:space:]]|$)' <<< "$cmd"; then
  block "on epic branch '$branch'. Commits land here only via child PRs — branch off it: git checkout -b <type>/<issue>-<slug>"
fi

# 2. No force push. --force-with-lease is tolerated on feature branches only.
# Scoped to the `git push ...` segment itself — not the full (possibly chained) command —
# so prose elsewhere in the command (e.g. a `gh pr create --title` containing the word
# "main") can't false-positive as a push destination.
push_segment=$(grep -Eo 'git[[:space:]]+push[^;&|]*' <<< "$cmd" | head -1)
if [[ -n "$push_segment" ]]; then
  if grep -Eq -- '(^|[[:space:]])(-f|--force)([[:space:]]|$)|[[:space:]]\+[[:alnum:]]' <<< "$push_segment"; then
    block "force push is not allowed. Use --force-with-lease on a feature branch if you must rewrite it."
  fi
  # A refspec's destination follows ':'; a bare ref is also its destination.
  # Include full refs, deletion refspecs and quoted arguments, but not main:feature.
  main_destination="(^|[[:space:]])[\"']?([^[:space:]:]*:)?(refs/heads/)?main[\"']?([[:space:];&|]|$)"
  if grep -Eq "$main_destination" <<< "$push_segment"; then
    block "never push directly to 'main'. Push a feature branch and open a PR."
  fi
  # An epic branch is pushed only by the rebase-onto-main sync (--force-with-lease).
  epic_destination="(^|[[:space:]])[\"']?([^[:space:]:]*:)?(refs/heads/)?epic/[^[:space:]\"']+[\"']?([[:space:];&|]|$)"
  if [[ "$branch" == epic/* ]] || grep -Eq "$epic_destination" <<< "$push_segment"; then
    grep -Eq -- '--force-with-lease' <<< "$push_segment" \
      || block "pushing to an epic branch is only allowed as the rebase sync: git push --force-with-lease origin epic/<n>-<slug>. Children land via PRs."
  fi
fi

# 3. No history / worktree destruction without the human.
if grep -Eq 'git[[:space:]]+(reset[[:space:]]+--hard|clean[[:space:]]+-[a-zA-Z]*f|checkout[[:space:]]+--[[:space:]]+\.|restore[[:space:]]+\.|branch[[:space:]]+-D[[:space:]]+main)' <<< "$cmd"; then
  block "destructive git command. Ask the user to run it themselves (prefix with '!' in the prompt)."
fi

# 4. Merge only after a human approval on the PR, and with the strategy the history
#    needs: epic → main is rebase-merged (child commits stay visible to semantic-release),
#    everything else is squash-merged (one Conventional Commit per PR).
if grep -Eq 'gh[[:space:]]+pr[[:space:]]+merge' <<< "$cmd"; then
  pr=$(grep -Eo 'merge[[:space:]]+[0-9]+' <<< "$cmd" | grep -Eo '[0-9]+' || true)
  read -r decision head < <(cd "$target" && gh pr view ${pr:-} --json reviewDecision,headRefName \
    -q '[.reviewDecision, .headRefName] | join(" ")' 2>/dev/null); decision=${decision:-NONE}; head=${head:-}
  [[ "$decision" == "APPROVED" ]] || block "PR ${pr:-for this branch} is not approved (reviewDecision=$decision). Merge happens after the owner's review."
  if [[ "$head" == epic/* ]]; then
    grep -Eq -- '--rebase' <<< "$cmd" || block "PR ${pr} is an epic branch — merge it with --rebase so the child commits stay on main."
  else
    grep -Eq -- '--(merge|rebase)' <<< "$cmd" && block "PR ${pr} is a feature branch — squash-merge it (--squash), one Conventional Commit per PR."
  fi
fi

exit 0
