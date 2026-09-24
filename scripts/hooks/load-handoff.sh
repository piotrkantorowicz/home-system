#!/usr/bin/env bash
# SessionStart hook (matcher: clear). Loads a pending /handoff note into the new
# session's context, then consumes the flag so it only fires once.
set -uo pipefail

root=$(git rev-parse --show-toplevel 2>/dev/null) || root="${CLAUDE_PROJECT_DIR:-.}"
pending="$root/.claude/handoffs/.pending"

[[ -f "$pending" ]] || exit 0

filename=$(<"$pending")
handoff="$root/.claude/handoffs/$filename"

if [[ -z "$filename" || ! -f "$handoff" ]]; then
  rm -f "$pending"
  exit 0
fi

content=$(<"$handoff")
rm -f "$pending"

jq -n --arg content "$content" '{
  hookSpecificOutput: {
    hookEventName: "SessionStart",
    additionalContext: ("Handed off from the previous session:\n\n" + $content)
  }
}'
