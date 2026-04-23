#!/usr/bin/env bash
# Wait for the HomeSystem backend to be healthy.
#
# Usage:
#   wait-for-backend.sh            # defaults — 120s timeout, localhost:5000
#   wait-for-backend.sh 180

set -euo pipefail

TIMEOUT="${1:-120}"
BACKEND_URL="${BACKEND_URL:-http://localhost:5000}"
START="$(date +%s)"

log() { printf '[%s] %s\n' "$(date +%H:%M:%S)" "$*"; }

log "Waiting for backend /health (<${TIMEOUT}s)..."
until curl -fsS -o /dev/null "${BACKEND_URL}/health"; do
  elapsed=$(( $(date +%s) - START ))
  if [[ $elapsed -ge $TIMEOUT ]]; then
    log "timed out after ${elapsed}s"
    exit 1
  fi
  sleep 1
done
log "Backend up after $(( $(date +%s) - START ))s"
