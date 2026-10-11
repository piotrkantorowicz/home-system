# Infrastructure

Docker Compose setup for shared services used across all home-system modules.

## Services

| Service              | Image                         | Port                      | Profile                         |
| -------------------- | ----------------------------- | ------------------------- | ------------------------------- |
| Authentik (server)   | `goauthentik/server:2025.2.3` | 9000 (HTTP), 9443 (HTTPS) | always                          |
| Authentik (worker)   | `goauthentik/server:2025.2.3` | —                         | always                          |
| Authentik DB         | `postgres:16-alpine`          | internal                  | always                          |
| Redis                | `redis:7-alpine`              | internal                  | always                          |
| Diet Planner DB      | `postgres:16-alpine`          | 5432                      | `diet-planner`                  |
| Recipe image storage | `rustfs/rustfs:1.0.1`         | 127.0.0.1:9100 (S3)       | explicit image-storage override |

All services share the `home-system-shared` bridge network.

Authentik 2025.2 added public-client token revocation, required by the SPA's
logout flow. Server and worker must run the same version. Before upgrading an
existing installation, back up the Authentik database with `pg_dump -Fc` and
retain the previous Compose configuration. A rollback requires restoring that
database backup together with the previous image version; do not run an older
image against a migrated database. See the [upgrade guide](https://docs.goauthentik.io/install-config/upgrade).

## Setup

```bash
cp .env.example .env
# Fill in the required secrets in .env
```

Required `.env` variables:

| Variable                   | Description                                                       |
| -------------------------- | ----------------------------------------------------------------- |
| `AUTHENTIK_SECRET_KEY`     | Authentik master secret — generate with `openssl rand -base64 36` |
| `AUTHENTIK_DB_PASSWORD`    | PostgreSQL password for Authentik                                 |
| `AUTHENTIK_REDIS_PASSWORD` | Redis password                                                    |
| `DIETPLANNER_DB_PASSWORD`  | PostgreSQL password for Diet Planner                              |

## Usage

```bash
# Start core services (Authentik + Redis)
docker compose up -d

# Start with Diet Planner database
docker compose --profile diet-planner up -d

# View logs
docker compose logs -f

# Stop
docker compose down

# Stop and remove volumes (deletes all data)
docker compose down -v
```

## Optional private recipe image storage

Image storage is disabled in the API by default. Ordinary stack commands above
do not include `docker-compose.image-storage.yml` and need no image credentials.
An explicit override avoids Compose interpolating required secrets in an inactive
profile. Never set this override as a default `COMPOSE_FILE`.

Run the following from `infrastructure/`. Set `RECIPE_IMAGE_STORAGE_ACCESS_KEY`
and `RECIPE_IMAGE_STORAGE_SECRET_KEY` through a secret manager, shell environment,
or the gitignored `.env`; the example deliberately leaves both empty. Missing
values cause the merged configuration to fail. Keep the same local credentials
when restarting or recreating the service.

```bash
# Ordinary stack remains valid without image variables.
docker compose config --quiet

# Validate and start only the optional storage service, waiting for readiness.
docker compose -f docker-compose.yml -f docker-compose.image-storage.yml config --quiet
docker compose -f docker-compose.yml -f docker-compose.image-storage.yml \
  up -d --wait --wait-timeout 180 recipe-image-storage
```

The S3 port binds only to loopback, the console is disabled, and object data lives
in the project-scoped `recipe_image_data` named volume. Preserve this volume;
`down -v` deletes photos as well as databases. The healthcheck uses
[`/health/ready`](https://docs.rustfs.com/en/operations/status-check), which waits
for storage and IAM readiness. `/health` reports process liveness and can succeed
before S3 operations work.

### Create and inspect the development bucket

Use the AWS CLI already installed on your machine with local administrative
credentials supplied through environment variables or a dedicated secure profile.
For shell-provided local credentials, assign `AWS_ACCESS_KEY_ID` from
`RECIPE_IMAGE_STORAGE_ACCESS_KEY` and `AWS_SECRET_ACCESS_KEY` from
`RECIPE_IMAGE_STORAGE_SECRET_KEY` in that shell. Compose's `.env` interpolation
does **not** configure AWS CLI or API credentials. Do not change shared AWS
profiles, put credentials in command arguments, or enable shell tracing/CLI debug
logs. These local development keys administer RustFS; never use root/admin keys
for the production API.

```bash
aws --endpoint-url http://127.0.0.1:9100 --region us-east-1 \
  s3api create-bucket --bucket home-system-recipe-images

# Inspect privacy using administrative credentials.
aws --endpoint-url http://127.0.0.1:9100 --region us-east-1 \
  s3api get-bucket-policy --bucket home-system-recipe-images
aws --endpoint-url http://127.0.0.1:9100 --region us-east-1 \
  s3api get-bucket-acl --bucket home-system-recipe-images
```

An already-existing bucket requires ownership and privacy inspection before use;
do not ignore all creation errors. `NoSuchBucketPolicy` is the expected policy
inspection result for a fresh private bucket. Reject a public policy or grants to
`AllUsers`/`AuthenticatedUsers`; an inspection failure is not proof of privacy.
Confirm anonymous denial on an **existing** smoke object as below.

### Configure the API process

Provide these variables to the backend process, separately from Docker Compose:

```text
DietPlanner__ImageStorage__Enabled=true
DietPlanner__ImageStorage__Endpoint=http://127.0.0.1:9100
DietPlanner__ImageStorage__Region=us-east-1
DietPlanner__ImageStorage__Bucket=home-system-recipe-images
DietPlanner__ImageStorage__ForcePathStyle=true
```

The SDK credential chain uses `AWS_ACCESS_KEY_ID`/`AWS_SECRET_ACCESS_KEY` (and
`AWS_SESSION_TOKEN` for temporary credentials), a secure profile, or workload
identity. Keep credentials out of appsettings, source control, frontend variables,
request URLs, and logs. This slice provides the storage adapter; image rows,
endpoints, and lifecycle/cleanup operations follow in #565.

### Prove private access and persistence

Use only a fresh opaque smoke key in this development bucket. The following
uploads a small object, checks anonymous access, and compares SHA-256 after both
restart and recreation. AWS credentials must already be available to the CLI.
Stop on any failure; retain the key for targeted cleanup if interrupted.

```bash
set -e
smoke_dir=$(mktemp -d)
smoke_key="recipe-images/smoke-$(openssl rand -hex 16)"
printf 'recipe image storage smoke\n' > "$smoke_dir/source"
smoke_sha=$(sha256sum "$smoke_dir/source" | cut -d ' ' -f 1)

aws --endpoint-url http://127.0.0.1:9100 --region us-east-1 s3api put-object \
  --bucket home-system-recipe-images --key "$smoke_key" \
  --body "$smoke_dir/source" --content-type image/png >/dev/null
anonymous_status=$(curl -sS -o /dev/null -w '%{http_code}' \
  "http://127.0.0.1:9100/home-system-recipe-images/$smoke_key")
test "$anonymous_status" = 403

docker compose -f docker-compose.yml -f docker-compose.image-storage.yml \
  restart recipe-image-storage
docker compose -f docker-compose.yml -f docker-compose.image-storage.yml \
  up -d --wait --wait-timeout 180 recipe-image-storage
aws --endpoint-url http://127.0.0.1:9100 --region us-east-1 s3api get-object \
  --bucket home-system-recipe-images --key "$smoke_key" "$smoke_dir/restarted" >/dev/null
test "$smoke_sha" = "$(sha256sum "$smoke_dir/restarted" | cut -d ' ' -f 1)"

docker compose -f docker-compose.yml -f docker-compose.image-storage.yml \
  up -d --force-recreate --wait --wait-timeout 180 recipe-image-storage
aws --endpoint-url http://127.0.0.1:9100 --region us-east-1 s3api get-object \
  --bucket home-system-recipe-images --key "$smoke_key" "$smoke_dir/recreated" >/dev/null
test "$smoke_sha" = "$(sha256sum "$smoke_dir/recreated" | cut -d ' ' -f 1)"

aws --endpoint-url http://127.0.0.1:9100 --region us-east-1 s3api delete-object \
  --bucket home-system-recipe-images --key "$smoke_key" >/dev/null
rm -r -- "$smoke_dir"
```

## Production image storage

The production provider is undecided. Provision a private bucket using separate
administrative credentials; disable public access and keep policy/ACL private.
Do not configure public URLs, bucket CORS, or direct browser access. Image bytes
will be served through authenticated API operations. The local RustFS setup is a
development service, not a production deployment recipe.

Give the API workload identity or a rotated secret restricted to
`s3:PutObject`, `s3:GetObject`, and `s3:DeleteObject` on
`arn:aws:s3:::<bucket>/recipe-images/*` (or the provider's equivalent resource).
For AWS S3, also grant `s3:ListBucket` on `arn:aws:s3:::<bucket>` so a missing key
returns 404 rather than 403. Use a dedicated image bucket: this permission lets
the workload list its keys, while object access remains limited to `recipe-images/*`.
Do not add an `s3:prefix` condition to this grant: GetObject does not supply the
listing prefix parameter. See the [AWS GetObject permission contract](https://docs.aws.amazon.com/AmazonS3/latest/API/API_GetObject.html).
Other providers must prove equivalent missing-key behavior with restricted credentials.
Bucket creation, policy updates, backups, and version administration use separate
administrative credentials. Access denial remains a storage failure, never a missing image.

For AWS S3, set `Region` to the bucket's exact region, omit `Endpoint` to use the
SDK regional endpoint, and set `ForcePathStyle=false`. For compatible providers,
set `Endpoint=https://<provider-s3-host>`, the provider's signing region, and its
required `ForcePathStyle`; keep all settings under `DietPlanner__ImageStorage__`.
Always verify TLS certificates; never disable verification to fix configuration.
Store the normal image `ContentType`, but no source metadata or personal data in
keys. Keys must be opaque and immutable per image revision. Diagnostics should
report numeric status codes only, without SDK exception bodies, credentials,
signed URLs, or object contents.

### Required production-provider smoke matrix

Before deployment, run these checks against a scratch bucket/prefix with the
chosen provider and the actual adapter/SDK. Record provider, SDK version, region,
addressing style, results, and cleanup. Local fixture success is insufficient.

| Check                              | Required proof                                                                                                                                                                          |
| ---------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Region, signing, addressing        | Exact deployment region and endpoint accept signed operations with the selected path/virtual-host style and SDK checksum defaults.                                                      |
| Privacy                            | Anonymous reads of an existing object fail; policy/ACL grant no public access.                                                                                                          |
| Object operations                  | Put/read/delete succeed, bytes/checksums match, and `ContentType` survives reads.                                                                                                       |
| Missing resources                  | Missing key is distinguished from missing bucket, access denial, and service outage; repeated deletion is safe.                                                                         |
| Restricted application credentials | Object access works only under `recipe-images/*`; bucket creation, policy changes, and access to other buckets are denied. AWS credentials may list keys in the dedicated image bucket. |
| Cancellation and network failure   | Cancellation propagates; failed/unavailable operations return safe storage failures without leaking provider details.                                                                   |
| Backup/restore                     | Export and restore checksums and required versions/delete markers; validate the selected provider's versioning and retention behavior.                                                  |

### Coordinated backup and recovery

Database rows alone do not preserve photos. This slice has no image rows yet;
the following is the mandatory procedure for #565, not a claim of an executed
full-stack restore test. Use separate backup/admin credentials and protect both
backup stores and manifests from public access.

To capture a consistent restore point:

1. Pause image mutations and cleanup, and settle or record pending operations.
2. Snapshot the database and export the corresponding object bytes/versions. Save
   a manifest of referenced keys, required version IDs, and SHA-256 checksums
   alongside the database snapshot and object export.
3. Verify the database backup is restorable and every referenced object/version
   is present with the recorded checksum, then resume mutations and cleanup.

Versioning and retained delete markers increase storage cost but permit recovery
from overwrite/deletion. Without versioning, retain complete object snapshot
exports. Preserve recovery data at least as long as every supported database
restore point plus cleanup grace. Never enable an age-based purge that can delete
active references, or purge objects needed by another retained database snapshot.

To restore:

1. Stop writes and cleanup. Restore object bytes and required versions first.
2. Restore the corresponding database snapshot.
3. Audit every active reference against the manifest and object checksums.
   Quarantine or clear missing references before enabling reads.
4. Check pending upload/cleanup leases against restored state before resuming
   writes and cleanup; keep objects required by other retained restore points.

Account deletion and its retention obligations remain in #513.

## Authentik Initial Setup

After first start, create the admin user:

```bash
docker compose exec authentik-server ak create_admin_user
```

Then open http://localhost:9000/if/admin/ to complete configuration.

### OIDC Setup for Diet Planner

The blueprint in `authentik/blueprints/home-system.yaml` configures the application
and OAuth2 provider for the SPA:

- **Provider type**: OAuth2/OIDC
- **Client type**: Public (PKCE)
- **Redirect URIs**: `http://localhost:5173/callback`, `http://localhost:5173/silent-renew`, `http://localhost:5173`
- **Scopes**: `openid`, `profile`, `email`, `offline_access`

The SPA client ID must match the blueprint. Do not put a client secret in frontend
configuration. Logout revokes tokens before redirecting to Authentik's end-session
endpoint.

In local development, revocation uses Vite's `/authentik` proxy because Authentik's
revocation endpoint does not return CORS headers. Other deployments need a
same-origin revocation proxy or appropriate CORS headers at their reverse proxy.

## Ports

| Service               | URL                                                                              |
| --------------------- | -------------------------------------------------------------------------------- |
| Authentik admin       | http://localhost:9000/if/admin/                                                  |
| Authentik OIDC config | http://localhost:9000/application/o/home-system/.well-known/openid-configuration |
| Diet Planner DB       | localhost:5432                                                                   |
