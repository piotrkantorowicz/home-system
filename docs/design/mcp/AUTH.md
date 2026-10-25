# MCP authentication: decision record

Status: **decided, 2026-10-24 (#606, #607).** The first client is **Claude Code**, and the
real-client probe passed against local Authentik (see "Client decision and probe result").

## Where we are

| Fact | Evidence |
|---|---|
| IdP is Authentik `2025.2.3` | `infrastructure/docker-compose.yml` (`ghcr.io/goauthentik/server:2025.2.3`) |
| One OAuth2 provider, public client, used by the SPA | `infrastructure/authentik/blueprints/home-system.yaml` (`client_type: public`, redirect `http://localhost:5173/callback`) |
| `sub_mode: hashed_user_id`, `issuer_mode: per_provider` | same blueprint |
| API accepts exactly one issuer and one audience | `src/Apis/HomeSystem.REST/Program.cs`: `AddJwtBearer` with `Authority` and `Audience` from `Authentication:Authentik`, `ValidateIssuer`/`ValidateAudience` on |
| Caller identity = `sub` → person via Household | `Household.Api/Identity/HouseholdClaimsTransformation.cs` adds `person_id`, `household_id` and `household_role` from `GetHouseholdContextForUserAsync(sub)`. Modules read `person_id` through `PersonalDataClaims` |
| No dynamic client registration in this Authentik version | Authentik documents DCR from `2026.8.0` |

Consequence: **an MCP token resolves to the same person as the SPA token only if it
carries the same `sub`.** Nothing else links the two. The person pipeline needs no
changes as long as that holds.

## Recommendation

1. **Separate Authentik provider for MCP** (`home-system-mcp`): a public client with
   PKCE (S256) and pre-registered redirect URIs taken from the chosen client. It uses
   `sub_mode: hashed_user_id`, like the SPA provider. In Authentik, `hashed_user_id`
   is derived from the user, not the provider, so both providers should issue the
   same `sub`. **This is the claim the probe must prove** (step 4).
2. **Second JwtBearer scheme in the host** (`Mcp`) with its own `Authority` (MCP
   provider issuer) and `Audience` (MCP client id). The `/mcp` endpoint's
   authorization policy accepts only that scheme. REST keeps the current scheme.
   Result: SPA tokens cannot call `/mcp` and MCP tokens cannot call REST. No
   audience check is relaxed, and no shared multi-audience list is needed.
3. **Grants as scopes on the MCP provider only:** `mcp:diet:read`, `mcp:diet:write`,
   `mcp:diet:import`, plus `read` / `write` per further module, each added
   with its first tool (`mcp:investments:read`). Read tools need `read`. Write tools need `write`. Execute
   import needs `write` **and** `import`. Tool annotations describe behaviour, but
   only the server-side policy is authoritative.
4. **Protected Resource Metadata** (`/.well-known/oauth-protected-resource`,
   RFC 9728) served by the SDK's MCP authentication handler. It points at the MCP
   provider's issuer, and 401 responses carry `WWW-Authenticate` with
   `resource_metadata`. This metadata document is the **only** anonymous endpoint
   added.
5. **No DCR, no OAuth proxy.** Pre-registration is enough for a first release with
   one named client. A client that *requires* DCR blocks on an Authentik upgrade to
   `>= 2026.8.0`. That upgrade becomes its own prerequisite issue, estimated
   separately, and is not folded into #607.

## SDK and protocol

- Package: `ModelContextProtocol.AspNetCore` **2.2.0** (released 2026-08-13), pinned in
  `Directory.Packages.props` by #607. 2.3.0 (2026-10-20) is too new to adopt blind;
  #607 may move to it after reading its changelog.
- Transport: Streamable HTTP, **stateless** (no server session affinity), mapped at
  `/mcp` inside `HomeSystem.REST`. A feature switch keeps it disabled by default.
- Protocol revision: whatever the SDK and the chosen client negotiate. #607 records
  the negotiated `protocolVersion` from a real handshake in this file, and that
  version is the supported baseline.

## Probe procedure (runs once the client is chosen)

1. Apply the `home-system-mcp` provider through the blueprint with the client's exact
   callback URLs and the three scope mappings.
2. Discovery: `GET /.well-known/oauth-protected-resource` from the stub endpoint →
   `authorization_servers` → Authentik's
   `/.well-known/openid-configuration` for that provider. Confirm
   `code_challenge_methods_supported` contains `S256`.
3. Connect the client, complete authorization code + PKCE, and call one protected
   no-op tool. Record the access token's `iss`, `aud`, `sub`, `scope`, `exp` (claim
   names and shapes only, never the token).
4. **Same-person check:** decode an SPA access token for the same user and compare
   `sub`. Then call `GET /api/persons/me` with the SPA token and the stub tool with
   the MCP token. Both must report the same `personId`.
5. Negative checks: SPA token → `/mcp` returns 401. MCP token → `/api/v1/products`
   returns 401. A token missing `mcp:diet:write` cannot call a write tool (403).
6. Refresh: let the access token expire (or shorten its validity), then confirm the
   client refreshes with `offline_access` without asking the user to log in again.
7. SPA regression: log in and out of the SPA with the new provider present.

If step 4 fails (different `sub`), stop. Do not map subjects in code. Raise a
prerequisite issue to make the providers agree, for example by changing the sub
mode on both, which needs a data migration of stored auth subjects.

## Client decision and probe result

**Owner decision (2026-10-24): the first release targets Claude Code.** It runs on the
owner's machine, so the API needs no public exposure. A later in-app assistant (#634)
reuses the tools in-process instead of calling `/mcp`.

Claude Code takes a pre-registered public client, so no DCR and no Authentik upgrade
are needed:

```
claude mcp add --transport http --client-id home-system-mcp --callback-port 33418 \
  home-system http://localhost:5050/mcp
```

The blueprint (`infrastructure/authentik/blueprints/home-system.yaml`) registers provider
`home-system-mcp`: public client, PKCE, explicit consent, redirect URIs
`http://localhost:33418/callback` and `http://127.0.0.1:33418/callback`, `hashed_user_id`,
and the three grant scopes plus `openid` and `offline_access`.

**Authentik keeps granted scopes off the JWT.** The token records them, but the access
token had no `scope` claim, so every tool was hidden. Each grant mapping therefore returns
`{"scope": " ".join(token.scope)}`, which puts the space-separated `scope` claim the API
checks into the token.

Probe on 2026-10-24 (Authentik 2025.2.3, Claude Code 2.1.304, API from the epic branch):

| Step | Result |
|---|---|
| Discovery | Protected resource metadata names the `home-system-mcp` issuer and the three scopes; Authentik advertises `S256` |
| Anonymous `POST /mcp` | 401 with `WWW-Authenticate: Bearer resource_metadata=…` |
| Login | Authorization code + PKCE through the browser with consent; callback on port 33418 accepted |
| Token claims | `iss`, `aud` = `home-system-mcp`, `sub`, `scope`, `exp`, `azp`, `sid`, `uid`, `acr`, `amr`, `auth_time`, `iat` |
| Same person | MCP and SPA tokens carry the same `sub`; `get_diet_context` returns the owner's profile, goal and slots |
| MCP token → `GET /api/v1/products` | 401 |
| SPA token → `POST /mcp` | 401 |
| Missing grant → tool call | Covered by `McpEndpointTests` and the per-tool grant tests (test authentication handler) |
| Refresh with `offline_access` | Claude Code got a new access token about 14 minutes after the last browser authorization, with no new login |
| Real import | `preview_plan_import` then `execute_plan_import` of a 30-day plan from the conversation: 120 meals, 32 products and 18 recipes reused, no conflicts |
| Retry with the same operation key | Replayed the stored result (`replayed: true`) with a stale preview token; still 120 meals |
