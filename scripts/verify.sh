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
# Backend scope: Shared/, Apis/, props, slnx or .editorconfig touched → whole solution.
# Otherwise only the touched modules — their test projects are built (deps come along) and
# `dotnet format` sees only the changed .cs files.
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
  # Shared/, Apis/, props, slnx or .editorconfig touched → whole solution. Otherwise only the
  # touched modules: build their test projects (deps build transitively) and format only the
  # changed .cs files.
  if [[ "$MODE" == all ]] || grep -Eq '^(src/(Apis|Shared)/|Directory\.|HomeSystem\.slnx|\.editorconfig)' <<< "$FILES"; then
    FULL_BACKEND=1
    TEST_PROJECTS=$(find src -name '*Tests.csproj' | sort)
  else
    FULL_BACKEND=0
    MODULES=$(grep -Eo '^src/Modules/[^/]+' <<< "$FILES" | sort -u)
    TEST_PROJECTS=""
    for m in $MODULES; do
      TEST_PROJECTS+=$(find "$m" -name '*Tests.csproj' | sort)$'\n'
    done
    # A module without test projects has nothing to narrow to — compile the lot instead.
    if [[ -z "${TEST_PROJECTS//[[:space:]]/}" ]]; then FULL_BACKEND=1; fi
  fi

  # Integration tests need Docker; decide once so the build step can skip them too.
  DOCKER_OK=0; docker info >/dev/null 2>&1 && DOCKER_OK=1
  skip_integration() {           # skip_integration <project> → prints reason or nothing
    [[ "$1" == *IntegrationTests* ]] || return 0
    if (( ! RUN_INTEGRATION )); then echo "--no-integration"; return 0; fi
    if (( ! DOCKER_OK )); then echo "no Docker"; fi
  }

  if (( RUN_BUILD )); then
    if (( FULL_BACKEND )); then
      step "dotnet build" dotnet build HomeSystem.slnx --configuration Debug --verbosity minimal --nologo
    else
      # One build over a solution filter: every project of the touched modules (so the Api /
      # Infrastructure projects compile even when the unit tests don't reference them), minus
      # integration-test projects that won't run anyway. Referenced projects build transitively.
      SLNF=$(mktemp --suffix=.slnf)
      {
        printf '{ "solution": { "path": "%s/HomeSystem.slnx", "projects": [' "$ROOT"
        sep=""
        for m in $MODULES; do
          for proj in $(find "$m" -name '*.csproj' | sort); do
            [[ -n "$(skip_integration "$proj")" ]] && continue
            printf '%s"%s"' "$sep" "$proj"; sep=", "
          done
        done
        printf '] } }\n'
      } > "$SLNF"
      step "dotnet build ($(tr '\n' ' ' <<< "$MODULES" | sed 's#src/Modules/##g; s/ $//'))" \
        dotnet build "$SLNF" --configuration Debug --verbosity minimal --nologo
      rm -f "$SLNF"
    fi
  fi

  if (( FULL_BACKEND )); then
    step "dotnet format" dotnet format HomeSystem.slnx --verify-no-changes --no-restore --verbosity minimal
  else
    # Only files that still exist — deleted ones make `--include` fail.
    CS_FILES=$(grep -E '\.cs$' <<< "$FILES" | while read -r f; do [[ -f "$f" ]] && echo "$f"; done)
    if [[ -n "$CS_FILES" ]]; then
      # shellcheck disable=SC2086  # REASON: word-splitting the file list is the point
      step "dotnet format" dotnet format HomeSystem.slnx --verify-no-changes --no-restore --verbosity minimal --include $CS_FILES
    else
      record "dotnet format" "⏭ skipped (no .cs changes)"
    fi
  fi

  for proj in $TEST_PROJECTS; do
    [[ -z "$proj" ]] && continue
    reason=$(skip_integration "$proj")
    if [[ -n "$reason" ]]; then record "$(basename "$proj" .csproj)" "⏭ skipped ($reason)"; continue; fi
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
