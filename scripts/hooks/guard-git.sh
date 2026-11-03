#!/usr/bin/env bash
# PreToolUse hook for Bash. Blocks git/gh commands that would bypass the delivery loop.
# Input: Claude Code hook JSON on stdin. Exit 2 = block (stderr goes back to the agent).
# Codex: register the same script in ~/.codex/hooks.json under PreToolUse.
set -uo pipefail
input=$(cat)
cmd=$(jq -r '.tool_input.command // empty' <<< "$input" 2>/dev/null)
[[ -z "$cmd" ]] && exit 0

block() { echo "🛑 guard-git: $1" >&2; exit 2; }

# 0. Options that make an auto-approved git / gh command run arbitrary code or delete things.
#    The allowlist approves `git fetch:*`, `git push:*`, `gh api:*` by prefix, so the arguments
#    must be vetted here.
if grep -Eq -- '(^|[[:space:]])--(upload-pack|receive-pack|exec)([=[:space:]]|$)' <<< "$cmd" \
   || grep -Eq -- '(^|[;&|[:space:]])git([[:space:]]+-C[[:space:]]+[^[:space:]]+)?[[:space:]]+(-c|--config-env)([=[:space:]]|$)' <<< "$cmd"; then
  block "git --upload-pack/--receive-pack/--exec and 'git -c' overrides can run arbitrary commands. Ask the user to run it themselves."
fi
if grep -Eq -- 'gh[[:space:]]+api([[:space:]].*)?[[:space:]](-X|--method)[=[:space:]]*["'"'"']?DELETE' <<< "$cmd"; then
  block "gh api DELETE is not allowed. Ask the user to run it themselves."
fi

# Which checkout does the command act on? With worktrees the hook's own cwd can sit on a
# different branch than the one the command touches. Priority:
# `git -C <dir>` > leading `cd <dir>` (after `;`, `&&`, `|` too) > hook cwd.
target=$(grep -Eo -- 'git[[:space:]]+-C[[:space:]]+[^[:space:];&|]+' <<< "$cmd" | head -1 | awk '{print $3}')
[[ -z "$target" ]] && target=$(grep -Eo -- '(^|[;&|][[:space:]]*)cd[[:space:]]+[^[:space:];&|]+' <<< "$cmd" | head -1 | sed -E 's/.*cd[[:space:]]+//')
[[ -z "$target" ]] && target=$(jq -r '.cwd // empty' <<< "$input" 2>/dev/null)
# Expand a leading ~ and one plain $VAR / ${VAR} prefix; never eval command text (the hook
# runs before the permission check, so eval would execute whatever the command embeds).
target=${target:-.}
target=${target#[\"\']}; target=${target%[\"\']}
case "$target" in '~'|'~/'*) target="$HOME${target#\~}" ;; esac
if [[ "$target" =~ ^\$\{?([A-Za-z_][A-Za-z0-9_]*)\}?(/.*)?$ ]]; then
  v=${BASH_REMATCH[1]}; target="${!v:-}${BASH_REMATCH[2]}"
fi
[[ "$target" == *[\$\`\"\\]* ]] && target=.
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

# 4. Merge only after the owner's OK on the PR, and with the strategy the history
#    needs: epic → main is rebase-merged (child commits stay visible to semantic-release),
#    everything else is squash-merged (one Conventional Commit per PR).
#    The OK is an approving review or, for PRs the owner can't approve (agents push as the
#    owner, so that's all of them), the `owner-approved` label — added by the owner, never an agent.
#    A child PR into an epic branch needs no owner OK: the newest reviewer marker
#    `<!-- epic-review: approve sha=<head sha> -->` (comment or review body) must match the
#    current head, so any push after the review needs a new one, and must be the body's last line
#    (a quoted marker doesn't count). The merge must pin that head with --match-head-commit so a push
#    between this check and the merge fails it. The owner reviews the epic → main PR instead
#    (docs/design/epic-autopilot/README.md §5). Required checks are the epic ruleset's job.
#    The PR is named by number right after `merge`, never a URL / branch / --repo: the check below
#    looks up that number, so any other target could make it vet one PR and merge another.
if grep -Eq 'gh[[:space:]]+pr[[:space:]]+merge' <<< "$cmd"; then
  [[ $(grep -Eo 'pr[[:space:]]+merge' <<< "$cmd" | wc -l) -eq 1 ]] || block "one PR merge per command — the check below vets a single PR."
  pr=$(grep -Eo 'pr[[:space:]]+merge([[:space:]]+[^[:space:];&|]+)?' <<< "$cmd" | head -1 | awk '{print $3}')
  [[ "$pr" =~ ^[0-9]+$ ]] || block "name the PR by number right after merge (gh pr merge <n> ...), not a URL, branch or flag."
  grep -Eq -- '(^|[[:space:]])(-R|--repo)|GH_REPO' <<< "$cmd" && block "merge PRs of this repo only — drop -R/--repo/GH_REPO."
  # jq decides (exact label match, no string joining to fool); '|' separator because whitespace
  # would collapse an empty reviewDecision; the branch name goes last so a '|' in it can't shift fields.
  IFS='|' read -r ok decision approved head < <(cd "$target" && gh pr view "$pr" \
    --json reviewDecision,headRefName,labels,baseRefName,headRefOid,comments,reviews \
    -q '([((.comments // [])[] | {t: .createdAt, b: .body}), ((.reviews // [])[] | {t: .submittedAt, b: .body})]
          | sort_by(.t) | map([.b // "" | capture("<!-- epic-review: (?<v>approve|request-changes) sha=(?<s>[0-9a-f]{40}) -->\\s*$")])
          | add // [] | last) as $marker
        | [(if .reviewDecision == "APPROVED" or any(.labels[]; .name == "owner-approved") then "yes"
            elif ((.baseRefName // "") | startswith("epic/")) and (.headRefName | startswith("epic/") | not)
                 and $marker.v == "approve" and $marker.s == .headRefOid then "marker"
            else "no" end),
           .reviewDecision, ($marker.s // ""), .headRefName] | join("|")' 2>/dev/null)
  decision=${decision:-NONE}; head=${head:-}
  if [[ "${ok:-no}" == "marker" ]]; then
    [[ $(grep -Eo -- '--match-head-commit' <<< "$cmd" | wc -l) -eq 1 ]] \
      && grep -Eq -- "--match-head-commit[=[:space:]]+[\"']?${approved}([\"'[:space:]]|$)" <<< "$cmd" \
      || block "PR ${pr} merges on the reviewer marker — pin the reviewed head: --match-head-commit ${approved}"
    ok=yes
  fi
  [[ "${ok:-no}" == "yes" ]] \
    || block "PR ${pr:-for this branch} is not approved (reviewDecision=$decision, no owner-approved label, no reviewer approve marker on the current head for an epic child). Merge happens after the owner's OK."
  if [[ "$head" == epic/* ]]; then
    grep -Eq -- '--rebase' <<< "$cmd" || block "PR ${pr} is an epic branch — merge it with --rebase so the child commits stay on main."
  else
    grep -Eq -- '--(merge|rebase)' <<< "$cmd" && block "PR ${pr} is a feature branch — squash-merge it (--squash), one Conventional Commit per PR."
  fi
fi

# 5. `owner-approved` is the owner's merge OK: agents never put it on a PR or issue.
#    Best effort — it catches the gh CLI routes, not every possible HTTP client.
#    The actor can't be checked either: agents act as the owner's account.
if grep -Eq -- "(-l|--label|--add-label)[=[:space:]]+[\"']?([^\"']*,[[:space:]]*)?owner-approved" <<< "$cmd" \
   || grep -Eq "gh[[:space:]]+api[^|;&]*labels[^|;&]*owner-approved" <<< "$cmd"; then
  block "only the owner adds the owner-approved label. Ask them to add it in GitHub."
fi

exit 0
