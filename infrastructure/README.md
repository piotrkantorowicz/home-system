# Diet Planner Infrastructure Setup

This directory contains Docker Compose configuration for running Authentik (authentication) and PostgreSQL databases.

## Prerequisites

- Docker and Docker Compose installed
- OpenSSL (for generating secrets)

## Quick Start

### 1. Generate Environment File

```bash
# Copy the example file
cp .env.example .env

# Generate a secure Authentik secret key
echo "AUTHENTIK_SECRET_KEY=$(openssl rand -base64 36)" >> .env

# Edit .env and set secure database passwords
# Change AUTHENTIK_DB_PASSWORD and DIETPLANNER_DB_PASSWORD to strong passwords
```

### 2. Start Services

```bash
docker compose up -d
```

Wait ~30 seconds for all services to become healthy:

```bash
docker compose ps
```

### 3. Create Authentik Admin User

```bash
docker compose exec authentik-server ak create_admin_user
```

Follow the prompts to set up the admin user.

### 4. Access Authentik

Open your browser and navigate to:
- **Authentik Admin**: http://localhost:9000/if/admin/
- **Diet Planner DB**: localhost:5432 (accessible via psql or database tools)

Login with the admin credentials you just created.

## Configure Authentik for Diet Planner API

### Step 1: Create OAuth2/OIDC Provider

1. Go to **Applications → Providers → Create**
2. Select **OAuth2/OpenID Provider**
3. Configure:
   - **Name**: Diet Planner API
   - **Authorization flow**: default-provider-authorization-implicit-consent
   - **Client type**: Confidential
   - **Client ID**: `diet-planner-api`
   - **Client Secret**: Click "Generate" and **save this value** - you'll need it for the API configuration
   - **Redirect URIs**:
     ```
     http://localhost:5000/signin-oidc
     http://localhost:5000/swagger/oauth2-redirect.html
     ```
   - **Signing Key**: authentik Self-signed Certificate
   - **Advanced protocol settings**:
     - Subject mode: Based on the User's ID
     - Include claims in id_token: ✓ Checked
   - **Scopes**: Select `openid`, `email`, `profile`

4. Click **Create**

### Step 2: Create Application

1. Go to **Applications → Applications → Create**
2. Configure:
   - **Name**: Diet Planner
   - **Slug**: `diet-planner`
   - **Provider**: Diet Planner API (the provider you just created)

3. Click **Create**

### Step 3: Note Your Configuration

After setup, you'll need these values for the .NET API:

```
Authority:     http://localhost:9000/application/o/diet-planner/
Audience:      diet-planner-api
Client ID:     diet-planner-api
Client Secret: <the secret you generated in Step 1>
```

You can find the OpenID configuration at:
```
http://localhost:9000/application/o/diet-planner/.well-known/openid-configuration
```

## Testing Authentication

Test that you can get a token:

```bash
curl -X POST http://localhost:9000/application/o/token/ \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=password" \
  -d "client_id=diet-planner-api" \
  -d "client_secret=YOUR_CLIENT_SECRET" \
  -d "username=YOUR_ADMIN_USERNAME" \
  -d "password=YOUR_ADMIN_PASSWORD" \
  -d "scope=openid profile email"
```

You should receive a JSON response with an `access_token`.

## Useful Commands

```bash
# View logs
docker compose logs -f

# View specific service logs
docker compose logs -f authentik-server

# Stop all services
docker compose down

# Stop and remove volumes (CAUTION: deletes all data)
docker compose down -v

# Restart a service
docker compose restart authentik-server

# Access the Diet Planner database
docker compose exec dietplanner-db psql -U dietplanner -d dietplanner
```

## Troubleshooting

### Services won't start
```bash
# Check service status
docker compose ps

# Check logs for errors
docker compose logs
```

### Can't access Authentik UI
- Ensure services are healthy: `docker compose ps`
- Check if port 9000 is already in use: `lsof -i :9000`
- Try restarting: `docker compose restart authentik-server`

### Database connection issues
- Verify the database is healthy: `docker compose ps dietplanner-db`
- Test connection: `docker compose exec dietplanner-db psql -U dietplanner -d dietplanner -c "SELECT 1;"`

## Security Notes

- **Never commit the `.env` file** - it's in `.gitignore` for a reason
- Change all default passwords in production
- Use HTTPS in production (configure reverse proxy)
- Enable `RequireHttpsMetadata: true` in production API configuration
