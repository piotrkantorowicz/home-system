#!/usr/bin/env bash
# Smoke-test a built Home System UI image: runs it like the cluster does (non-root, read-only root
# filesystem, writable /tmp, config.js mounted over the bundled one) and checks what nginx serves.
#   scripts/smoke-ui-image.sh <image>
set -euo pipefail

image=${1:?usage: smoke-ui-image.sh <image>}
run=smoke-ui-$$
config=$(mktemp)
trap 'docker rm -f "$run" >/dev/null 2>&1 || true; rm -f "$config"' EXIT

echo 'window.__CONFIG__ = {"authentikDomain":"https://auth.smoke"};' > "$config"
chmod 644 "$config"
docker run -d --name "$run" --read-only --tmpfs /tmp -p 127.0.0.1::8080 \
  -v "$config:/usr/share/nginx/html/config.js:ro" "$image" >/dev/null
port=$(docker port "$run" 8080/tcp | head -1 | sed 's/.*://')

fail() { echo "SMOKE FAIL: $*" >&2; docker logs "$run" 2>&1 | tail -40 >&2; exit 1; }
status() { curl -s -o /dev/null -w '%{http_code}' "http://127.0.0.1:$port$1"; }

for _ in $(seq 30); do
  curl -fsS "http://127.0.0.1:$port/" >/dev/null 2>&1 && break
  [[ $(docker inspect -f '{{.State.Running}}' "$run") == true ]] || fail "container exited"
  sleep 1
done

curl -fsS "http://127.0.0.1:$port/" | grep -q '<div id="root">' || fail "/ is not the app shell"
curl -fsS "http://127.0.0.1:$port/diet-planner/products" | grep -q '<div id="root">' || fail "client route does not fall back to the app shell"
curl -fsS "http://127.0.0.1:$port/config.js" | grep -q 'auth.smoke' || fail "mounted config.js not served"
[[ $(status /assets/missing.js) == 404 ]] || fail "missing asset is not a 404"
curl -fsSI "http://127.0.0.1:$port/" | grep -qi '^cache-control: no-cache' || fail "app shell is cacheable"

uid=$(docker exec "$run" id -u)
[[ $uid != 0 ]] || fail "runs as root"
echo "SMOKE OK ($image, uid $uid)"
