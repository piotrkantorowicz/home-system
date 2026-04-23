#!/usr/bin/env bash
# Wait for Authentik to be fully ready: server healthy AND the blueprint users
# provisioned. The /-/health/ready/ endpoint returns 200 before blueprints are
# applied, so we also verify the E2eWorker0 user exists.
#
# Usage:
#   wait-for-authentik.sh               # defaults — 180s timeout, localhost:9000
#   wait-for-authentik.sh 300           # custom timeout
#
# Required env:
#   AUTHENTIK_BOOTSTRAP_TOKEN  — used to call the Authentik API

set -euo pipefail

TIMEOUT="${1:-180}"
AUTHENTIK_URL="${AUTHENTIK_URL:-http://localhost:9000}"
START="$(date +%s)"

if [[ -z "${AUTHENTIK_BOOTSTRAP_TOKEN:-}" ]]; then
  echo "wait-for-authentik: AUTHENTIK_BOOTSTRAP_TOKEN is required" >&2
  exit 2
fi

log() { printf '[%s] %s\n' "$(date +%H:%M:%S)" "$*"; }

# Phase 1 — server accepts HTTP connections.
log "Waiting for Authentik HTTP (<${TIMEOUT}s)..."
until curl -fsS -o /dev/null "${AUTHENTIK_URL}/-/health/ready/"; do
  elapsed=$(( $(date +%s) - START ))
  if [[ $elapsed -ge $TIMEOUT ]]; then
    log "timed out after ${elapsed}s waiting for /-/health/ready/"
    exit 1
  fi
  sleep 2
done
log "Authentik HTTP up after $(( $(date +%s) - START ))s"

# Phase 2 — blueprint applied (worker 0 exists).
log "Waiting for blueprint users..."
until
  resp=$(curl -fsS \
    -H "Authorization: Bearer ${AUTHENTIK_BOOTSTRAP_TOKEN}" \
    "${AUTHENTIK_URL}/api/v3/core/users/?username=E2eWorker0" 2>/dev/null) \
  && [[ "$(echo "$resp" | jq -r '.pagination.count // 0')" -ge 1 ]]
do
  elapsed=$(( $(date +%s) - START ))
  if [[ $elapsed -ge $TIMEOUT ]]; then
    log "timed out after ${elapsed}s waiting for E2eWorker0 user"
    exit 1
  fi
  sleep 2
done
log "Blueprint applied after $(( $(date +%s) - START ))s total"
