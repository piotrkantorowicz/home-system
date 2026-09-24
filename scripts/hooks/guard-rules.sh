#!/usr/bin/env bash
# PreToolUse hook for Edit/Write/MultiEdit. Blocks the first edit in a rule-governed
# area until the matching docs/rules/*.md doc has been Read this session. Enforces
# "read the relevant rule doc before editing" (CLAUDE.md) now that rule docs are no
# longer preloaded via @imports — see CLAUDE.md § entry-point paragraph.
# Input: Claude Code hook JSON on stdin. Exit 2 = block (stderr goes back to the agent).
set -uo pipefail
input=$(cat)
file=$(jq -r '.tool_input.file_path // empty' <<< "$input" 2>/dev/null)
transcript=$(jq -r '.transcript_path // empty' <<< "$input" 2>/dev/null)
[[ -z "$file" ]] && exit 0
root=$(git rev-parse --show-toplevel 2>/dev/null) || exit 0
rel=${file#"$root"/}

# Never gate edits to the rules themselves, migrations, or generated/vendored files.
case "$rel" in
  docs/rules/*|*/Migrations/*|*/generated/*|*/node_modules/*) exit 0 ;;
esac

rule=""
case "$rel" in
  */*.Domain/*.cs)                              rule=backend-ddd-patterns.md ;;
  */*.Application/*.cs)                         rule=backend-cqrs-patterns.md ;;
  */*.Contracts/*.cs)                           rule=backend-integration-patterns.md ;;
  */*.Api/*.cs)                                 rule=backend-api-patterns.md ;;
  */*.UnitTests/*.cs|*/*.IntegrationTests/*.cs) rule=backend-testing-standards.md ;;
  */*.Infrastructure/*.cs)                      rule=backend-persistence-styles.md ;;
  *.cs)                                         rule=backend-coding-standards.md ;;
  src/ui/*.test.ts|src/ui/*.test.tsx)           rule=frontend-testing.md ;;
  e2e/*.ts)                                     rule=frontend-playwright.md ;;
  src/ui/src/modules/*|src/ui/src/shared/*)     rule=frontend-react-typescript.md ;;
esac

[[ -z "$rule" ]] && exit 0

if [[ -n "$transcript" && -f "$transcript" ]]; then
  rule_path="$root/docs/rules/$rule"
  was_read=$(jq -s --arg f "$rule_path" '
    ([.[] | select(.type=="user") | .message.content[]? | select(.type=="tool_result" and (.is_error!=true)) | .tool_use_id]) as $ok
    | ([.[] | select(.type=="assistant") | .message.content[]? | select(.type=="tool_use" and .name=="Read" and .input.file_path==$f) | .id]) as $reads
    | any($reads[]; . as $id | $ok | index($id))
  ' "$transcript" 2>/dev/null)
  [[ "$was_read" == "true" ]] && exit 0
fi

echo "🛑 guard-rules: read docs/rules/$rule before editing $rel (see CLAUDE.md Quick Reference)." >&2
exit 2
