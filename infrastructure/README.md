# Infrastructure

Docker Compose setup for shared services used across all home-system modules.

## Services

| Service | Image | Port | Profile |
|---|---|---|---|
| Authentik (server) | `goauthentik/server:2025.2.4` | 9000 (HTTP), 9443 (HTTPS) | always |
| Authentik (worker) | `goauthentik/server:2025.2.4` | — | always |
| Authentik DB | `postgres:16-alpine` | internal | always |
| Redis | `redis:7-alpine` | internal | always |
| Diet Planner DB | `postgres:16-alpine` | 5432 | `diet-planner` |

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

| Variable | Description |
|---|---|
| `AUTHENTIK_SECRET_KEY` | Authentik master secret — generate with `openssl rand -base64 36` |
| `AUTHENTIK_DB_PASSWORD` | PostgreSQL password for Authentik |
| `AUTHENTIK_REDIS_PASSWORD` | Redis password |
| `DIETPLANNER_DB_PASSWORD` | PostgreSQL password for Diet Planner |

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

| Service | URL |
|---|---|
| Authentik admin | http://localhost:9000/if/admin/ |
| Authentik OIDC config | http://localhost:9000/application/o/home-system/.well-known/openid-configuration |
| Diet Planner DB | localhost:5432 |
