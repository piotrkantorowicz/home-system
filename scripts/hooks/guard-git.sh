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

branch=$(git -C "$target" rev-parse --abbrev-ref HEAD 2>/dev/null || echo "")

# 1. Nothing is committed or pushed directly on main — always branch first.
if [[ "$branch" == "main" ]] && grep -Eq '(^|[;&|[:space:]])git[[:space:]]+(commit|push)([[:space:]]|$)' <<< "$cmd"; then
  block "on 'main'. Create a branch first: git checkout -b <type>/<issue>-<slug>"
fi

# 2. No force push. --force-with-lease is tolerated on feature branches only.
if grep -Eq 'git[[:space:]]+push' <<< "$cmd"; then
  if grep -Eq -- '(^|[[:space:]])(-f|--force)([[:space:]]|$)|[[:space:]]\+[[:alnum:]]' <<< "$cmd"; then
    block "force push is not allowed. Use --force-with-lease on a feature branch if you must rewrite it."
  fi
  # A refspec's destination follows ':'; a bare ref is also its destination.
  # Include full refs, deletion refspecs and quoted arguments, but not main:feature.
  main_destination="(^|[[:space:]])[\"']?([^[:space:]:]*:)?(refs/heads/)?main[\"']?([[:space:];&|]|$)"
  if grep -Eq "$main_destination" <<< "$cmd"; then
    block "never push directly to 'main'. Push a feature branch and open a PR."
  fi
fi

# 3. No history / worktree destruction without the human.
if grep -Eq 'git[[:space:]]+(reset[[:space:]]+--hard|clean[[:space:]]+-[a-zA-Z]*f|checkout[[:space:]]+--[[:space:]]+\.|restore[[:space:]]+\.|branch[[:space:]]+-D[[:space:]]+main)' <<< "$cmd"; then
  block "destructive git command. Ask the user to run it themselves (prefix with '!' in the prompt)."
fi

# 4. Merge only after a human approval on the PR.
if grep -Eq 'gh[[:space:]]+pr[[:space:]]+merge' <<< "$cmd"; then
  pr=$(grep -Eo 'merge[[:space:]]+[0-9]+' <<< "$cmd" | grep -Eo '[0-9]+' || true)
  decision=$(cd "$target" && gh pr view ${pr:-} --json reviewDecision -q .reviewDecision 2>/dev/null); decision=${decision:-NONE}
  [[ "$decision" == "APPROVED" ]] || block "PR ${pr:-for this branch} is not approved (reviewDecision=$decision). Merge happens after the owner's review."
fi

exit 0
