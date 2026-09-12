#!/usr/bin/env bash
# PostToolUse hook for Edit/Write. Formats the touched frontend file so lint-staged and
# CI never fail on formatting. C# is left to `dotnet format` in verify.sh (too slow per-edit).
set -uo pipefail
file=$(jq -r '.tool_input.file_path // empty' 2>/dev/null)
[[ -z "$file" || ! -f "$file" ]] && exit 0
root=$(git rev-parse --show-toplevel 2>/dev/null) || exit 0
rel=${file#"$root"/}
case "$rel" in
  src/ui/*.ts|src/ui/*.tsx|src/ui/*.css|src/ui/*.json)
    (cd "$root/src/ui" && npx --no -- prettier --log-level warn --write "$file") ;;
  e2e/*.ts)
    (cd "$root/src/ui" && npx --no -- prettier --log-level warn --write "$file") ;;
esac
exit 0
