#!/usr/bin/env bash
# CLAUDE.md and AGENTS.md must describe the same repo. They differ only in tool-specific
# framing (imports vs. "read the file"), so compare the sections that must stay identical.
set -euo pipefail
cd "$(git rev-parse --show-toplevel)"

section() {  # section <file> <heading>  → body of that ## section, blank lines squeezed
  awk -v h="## $2" '
    $0 == h {on=1; next}
    on && /^## / {exit}
    on {print}
  ' "$1" | sed '/^\s*$/d'
}

fail=0
for sec in "Repository Structure" "Commands" "Non-negotiable Rules (Always Apply)"; do
  if ! diff -u <(section CLAUDE.md "$sec") <(section AGENTS.md "$sec") >/tmp/agent-sync.diff; then
    echo "::error::Section '## $sec' differs between CLAUDE.md and AGENTS.md"
    cat /tmp/agent-sync.diff
    fail=1
  fi
done

# Every rule doc must be referenced by both files.
for f in .claude/rules/*.md; do
  for doc in CLAUDE.md AGENTS.md; do
    grep -q "$f" "$doc" || { echo "::error::$doc does not reference $f"; fail=1; }
  done
done

# Every skill must be listed in both files.
for d in .agents/skills/*/; do
  name=$(basename "$d")
  grep -q "\`/$name\`" CLAUDE.md || { echo "::error::CLAUDE.md does not list skill /$name"; fail=1; }
  grep -q "\`\$$name\`" AGENTS.md || { echo "::error::AGENTS.md does not list skill \$$name"; fail=1; }
  [[ -L ".claude/skills/$name" ]] || { echo "::error::.claude/skills/$name is not a symlink to .agents/skills/$name"; fail=1; }
done

# Tripwire: a markdown table separator that lost its leading "|" is a botched edit.
for doc in CLAUDE.md AGENTS.md; do
  if grep -nE '^-{3}\|' "$doc"; then echo "::error::$doc has a dangling table fragment (see line above)"; fail=1; fi
done

(( fail == 0 )) && echo "CLAUDE.md ↔ AGENTS.md ↔ .agents/skills in sync"
exit $fail
