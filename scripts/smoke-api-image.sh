#!/usr/bin/env bash
# Smoke-test a built Home System API image: runs it non-root against a throwaway Postgres
# with every required setting passed as an environment variable, then checks readiness.
#   scripts/smoke-api-image.sh <image>
set -euo pipefail

image=${1:?usage: smoke-api-image.sh <image>}
run=smoke-$$
net=$run
trap 'docker rm -f "$run-api" "$run-db" >/dev/null 2>&1 || true; docker network rm "$net" >/dev/null 2>&1 || true' EXIT

docker network create "$net" >/dev/null
docker run -d --name "$run-db" --network "$net" -e POSTGRES_PASSWORD=smoke postgres:16-alpine >/dev/null
until docker exec "$run-db" pg_isready -h 127.0.0.1 -U postgres >/dev/null 2>&1; do sleep 1; done

env_args=(-e ASPNETCORE_ENVIRONMENT=Production
  -e Authentication__Authentik__Authority=https://authentik.invalid/application/o/home-system
  -e Authentication__Authentik__Audience=smoke)
for m in DietPlanner Notifications Household Budget Investments; do
  db=${m,,}
  docker exec "$run-db" createdb -U postgres "$db"
  env_args+=(-e "ConnectionStrings__$m=Host=$run-db;Database=$db;Username=postgres;Password=smoke")
done
docker run -d --name "$run-api" --network "$net" -p 127.0.0.1::8080 "${env_args[@]}" "$image" >/dev/null
port=$(docker port "$run-api" 8080/tcp | head -1 | sed 's/.*://')

fail() { echo "SMOKE FAIL: $*" >&2; docker logs "$run-api" 2>&1 | tail -40 >&2; exit 1; }

for _ in $(seq 60); do
  curl -fsS "http://127.0.0.1:$port/health" >/dev/null 2>&1 && break
  [[ $(docker inspect -f '{{.State.Running}}' "$run-api") == true ]] || fail "container exited"
  sleep 1
done
curl -fsS "http://127.0.0.1:$port/health" | grep -q '"healthy"' || fail "/health not healthy"

uid=$(docker exec "$run-api" id -u)
[[ $uid != 0 ]] || fail "runs as root"
[[ $(curl -s -o /dev/null -w '%{http_code}' "http://127.0.0.1:$port/openapi/v1.json") == 200 ]] || fail "OpenAPI doc not served"
[[ $(docker inspect -f '{{.State.Running}}' "$run-api") == true ]] || fail "container died after readiness"
echo "SMOKE OK ($image, uid $uid)"
