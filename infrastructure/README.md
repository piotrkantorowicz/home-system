# Infrastructure

Docker Compose setup for shared services used across all home-system modules.

## Services

| Service | Image | Port | Profile |
|---|---|---|---|
| Authentik (server) | `goauthentik/server:2024.12.3` | 9000 (HTTP), 9443 (HTTPS) | always |
| Authentik (worker) | `goauthentik/server:2024.12.3` | — | always |
| Authentik DB | `postgres:16-alpine` | internal | always |
| Redis | `redis:7-alpine` | internal | always |
| Diet Planner DB | `postgres:16-alpine` | 5432 | `diet-planner` |

All services share the `home-system-shared` bridge network.

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

After Authentik is running you need to create an application and OAuth2 provider in the Authentik admin UI:

- **Provider type**: OAuth2/OIDC
- **Client type**: Confidential
- **Redirect URIs**: `http://localhost:5173/callback`, `http://localhost:5173/silent-renew`
- **Scopes**: `openid`, `profile`, `email`

Copy the generated client ID and secret into `appsettings.Development.json` and `src/ui/.env`.

## Ports

| Service | URL |
|---|---|
| Authentik admin | http://localhost:9000/if/admin/ |
| Authentik OIDC config | http://localhost:9000/application/o/diet-planner-ui/.well-known/openid-configuration |
| Diet Planner DB | localhost:5432 |
