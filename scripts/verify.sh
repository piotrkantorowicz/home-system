#!/usr/bin/env bash
# Path-aware verification runner. Single source of truth for "is this change green?"
# Used by: .husky/pre-commit (--staged), the `verify` skill (--branch), CI parity (--all).
#
#   scripts/verify.sh [--staged | --branch | --all] [--no-integration] [--no-build]
#
#   --staged          files in the git index (pre-commit)
#   --branch          files changed vs origin/main + working tree (default)
#   --all             ignore paths, run everything
#   --no-integration  skip *.IntegrationTests (no Docker / fast loop)
#   --no-build        skip `dotnet build`, assume it is fresh
#
# Exit 0 = everything selected passed. Prints a summary table at the end.
set -uo pipefail

# Resolve the repo root from this file, not from git — git hooks export a relative GIT_DIR
# that breaks `git rev-parse` after any `cd`.
ROOT=$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)
cd "$ROOT" || exit 1
unset GIT_DIR GIT_WORK_TREE GIT_INDEX_FILE 2>/dev/null || true

MODE=branch
RUN_INTEGRATION=1
RUN_BUILD=1
for arg in "$@"; do
  case "$arg" in
    --staged) MODE=staged ;;
    --branch) MODE=branch ;;
    --all) MODE=all ;;
    --no-integration) RUN_INTEGRATION=0 ;;
    --no-build) RUN_BUILD=0 ;;
    *) echo "unknown arg: $arg" >&2; exit 64 ;;
  esac
done

case "$MODE" in
  staged) FILES=$(git diff --cached --name-only --diff-filter=ACDMR) ;;
  branch)
    BASE=$(git merge-base HEAD origin/main 2>/dev/null || git merge-base HEAD main)
    FILES=$( { git diff --name-only "$BASE"...HEAD; git diff --name-only HEAD; git ls-files --others --exclude-standard; } | sort -u ) ;;
  all) FILES="" ;;
esac

matches() { [[ "$MODE" == all ]] || grep -Eq "$1" <<< "$FILES"; }

BACKEND_RE='^(src/(Apis|Modules|Shared)/|Directory\.(Build|Packages)\.props|HomeSystem\.slnx|\.editorconfig)'
FRONTEND_RE='^src/ui/'
E2E_RE='^e2e/'

NAMES=(); STATUSES=()
record() { NAMES+=("$1"); STATUSES+=("$2"); }
step() {                       # step <name> <command...>
  local name=$1; shift
  echo
  echo "▶ $name"
  if "$@"; then record "$name" "✅ pass"; else record "$name" "❌ FAIL"; FAILED=1; fi
}
FAILED=0

# WebApplicationFactory-based integration tests each open inotify watchers; a dev box with an
# IDE + Vite running hits the 128-instance user limit fast. Polling avoids it entirely.
export DOTNET_USE_POLLING_FILE_WATCHER=1

# ── Backend ──────────────────────────────────────────────────────────────────
if matches "$BACKEND_RE"; then
  (( RUN_BUILD )) && step "dotnet build" dotnet build HomeSystem.slnx --configuration Debug --verbosity minimal --nologo
  step "dotnet format" dotnet format HomeSystem.slnx --verify-no-changes --no-restore --verbosity minimal

  # Which test projects? Shared/ or props touched → all. Otherwise only the touched modules.
  if [[ "$MODE" == all ]] || grep -Eq '^(src/(Apis|Shared)/|Directory\.|HomeSystem\.slnx)' <<< "$FILES"; then
    TEST_PROJECTS=$(find src -name '*Tests.csproj' | sort)
  else
    MODULES=$(grep -Eo '^src/Modules/[^/]+' <<< "$FILES" | sort -u)
    TEST_PROJECTS=""
    for m in $MODULES; do
      TEST_PROJECTS+=$(find "$m" -name '*Tests.csproj' | sort)$'\n'
    done
  fi

  for proj in $TEST_PROJECTS; do
    [[ -z "$proj" ]] && continue
    if [[ "$proj" == *IntegrationTests* ]]; then
      if (( ! RUN_INTEGRATION )); then record "$(basename "$proj" .csproj)" "⏭ skipped (--no-integration)"; continue; fi
      if ! docker info >/dev/null 2>&1; then record "$(basename "$proj" .csproj)" "⏭ skipped (no Docker)"; continue; fi
    fi
    step "$(basename "$proj" .csproj)" dotnet test "$proj" --no-build --configuration Debug --logger "console;verbosity=minimal" --nologo
  done
fi

# ── Frontend ─────────────────────────────────────────────────────────────────
if matches "$FRONTEND_RE"; then
  pushd src/ui >/dev/null
  step "ui format:check" npm run -s format:check
  step "ui lint" npm run -s lint
  step "ui type-check" npm run -s type-check
  step "ui vitest" npx vitest run --reporter=dot
  step "ui build" npm run -s build
  popd >/dev/null
fi

# ── E2E (static only — the suite itself needs the full stack, see run-e2e skill) ──
if matches "$E2E_RE"; then
  pushd e2e >/dev/null
  step "e2e tsc" npx tsc --noEmit -p tsconfig.json
  popd >/dev/null
fi

# ── Summary ──────────────────────────────────────────────────────────────────
echo
echo "════════ verify ($MODE) ════════"
if (( ${#NAMES[@]} == 0 )); then
  echo "nothing to verify for the changed paths"
else
  for i in "${!NAMES[@]}"; do printf '%-40s %s\n' "${NAMES[$i]}" "${STATUSES[$i]}"; done
fi
echo "════════════════════════════════"
exit $FAILED
