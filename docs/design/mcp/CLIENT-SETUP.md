# MCP client setup: Claude Code

How to connect Claude Code to Home System and import a monthly meal plan through it.
Auth design: [AUTH.md](AUTH.md). Configuration, limits, logs and rollback:
[OPERATIONS.md](OPERATIONS.md).

Supported and verified (2026-10-24): **Claude Code 2.1.304**, `ModelContextProtocol.AspNetCore`
2.2.0 (Streamable HTTP, stateless), **Authentik 2025.2.3**.

## 1. Authentik

The blueprint `infrastructure/authentik/blueprints/home-system.yaml` registers the
`home-system-mcp` provider and application: a public client with PKCE, explicit consent,
callbacks `http://localhost:33418/callback` and `http://127.0.0.1:33418/callback`, and
the grants `mcp:diet:read`, `mcp:diet:write`, `mcp:diet:import`,
`mcp:investments:read` and `mcp:investments:write`.

A fresh stack applies it on startup. On a running stack the worker re-applies changed
blueprints within minutes; to apply at once:

```bash
docker exec authentik-worker ak apply_blueprint /blueprints/custom/home-system.yaml
```

Check: `http://localhost:9000/application/o/home-system-mcp/.well-known/openid-configuration`
lists the three grants and `S256`.

## 2. API

MCP is off by default. Turn it on with configuration (environment variables shown;
`/run-project` passes the same ones):

```bash
Mcp__Enabled=true
Mcp__PublicBaseUrl=http://localhost:5050
Mcp__ImportsEnabled=true
Authentication__Mcp__Authority=http://localhost:9000/application/o/home-system-mcp/
Authentication__Mcp__Audience=home-system-mcp
```

Check: `GET http://localhost:5050/.well-known/oauth-protected-resource` names the
`home-system-mcp` issuer, and an anonymous `POST /mcp` returns 401 with
`WWW-Authenticate: Bearer resource_metadata=…`.

## 3. Claude Code

```bash
claude mcp add --transport http --client-id home-system-mcp --callback-port 33418 \
  home-system http://localhost:5050/mcp
```

Start a new `claude` session (tools load at session start), run `/mcp`, pick
**home-system** → **Authenticate**. Authentik opens in the browser: log in as yourself
and approve the grants. There is no client secret; PKCE and the fixed localhost
callback protect the exchange. Claude Code refreshes the 5-minute access token by
itself for 30 days.

## 4. Importing a month

1. Ask Claude Code to preview the plan, for example *"Preview importing
   `docs/diet-plans/plan-2026-11-19.json` with home-system and wait for my confirmation."*
   `preview_plan_import` writes nothing. It returns counts (new and reused products and
   recipes, meals), the date range, conflicts, and a preview token valid for 30 minutes.
2. Read the summary. Reused products and recipes keep their catalog values; the file's
   nutrition for them is ignored.
3. Confirm. `execute_plan_import` commits everything in one transaction with the preview
   token and an operation key. If the data or the reused catalog items changed since the
   preview, it refuses and asks for a new preview.
4. Retrying with the same operation key replays the stored result and imports nothing.

The plan format is the one the web app's import accepts (`products`, `recipes`,
`schedule`); meal `type` values must match your meal slot names.

## Troubleshooting

| Symptom | Cause | Fix |
|---|---|---|
| Authenticated, but no tools in the session | Tools load at session start; or the token has no `scope` claim | Restart `claude`. If still empty, check the grant mappings in the blueprint emit `scope` (AUTH.md) |
| Authentik: redirect URI error | Callback port differs from the registered one | Use `--callback-port 33418`, or add the URI to the blueprint |
| 401 on every call | `Authentication:Mcp` issuer or audience differs from the token | Authority must be the provider issuer with its trailing `/`; audience `home-system-mcp` |
| Tools fail with an error about joining or creating a household | The account has no person yet (never signed in to the web app) | Sign in to the SPA once and finish the household setup |
| `execute_plan_import` missing | Imports are off (`Mcp:ImportsEnabled=false`) or the import grant was not approved | Turn imports on, then re-authenticate to approve the grant |
| "changed since the preview" | Diary or reused catalog items changed after preview | Preview again and confirm the new summary |
| Conflict on occupied slots | The month already has meals in those slots | Pick another range, or ask for appending to occupied slots |
| 429 | More than `Mcp:RequestsPerMinute` calls per minute | Wait a minute |

## Acceptance coverage (#616)

Integration tests run on real PostgreSQL with locally signed tokens (`McpTestHost`).
The live run used the real client against real Authentik.

| Criterion | Where proven |
|---|---|
| Catalog reuse | `CreateRecipe_IsReusedByImport`; live: 32 products and 18 recipes reused |
| Standalone creation | `CreateProduct_*`, `CreateRecipe_FromOwnAndSharedProducts_IsReadableAndSearchable` |
| 31-day plan with missing products and recipes, counts and dates match the REST readback the SPA uses | `PreviewPlanImport_FullMonth_DescribesEverythingAndWritesNothing`, `ExecutePlanImport_FullMonth_CommitsEverythingAsPlannedMeals` |
| Replay adds nothing | `ExecutePlanImport_RetriedWithSameKey_ReplaysAcrossRestart`, `…_ConcurrentWithSameKey_ImportsOnce`; live replay with a stale token |
| Independent second user | `GetDietContext_ForSecondIdentity_DoesNotSeeFirstIdentitysSetup`, `ExecutePlanImport_WithAnotherUsersToken_Fails`, `ExecutePlanImport_NamesOwnedByAnotherUser_CreatesOwnCopies` |
| Read-only grant | `ExecutePlanImport_WithoutWriteAndImportGrants_IsForbidden`, `GetDietContext_WithoutReadScope_IsHiddenAndRejected` |
| Stale preview | `ExecutePlanImport_AfterDataChangedSincePreview_RequiresNewPreview`, `…_AfterCatalogChangedSincePreview_RequiresNewPreview` |
| Token refresh / re-login | Live: new access token without a browser login; re-authentication after the blueprint change |
| Conflicts | `ImportPlanResolverTests` occupied-slot and in-file slot cases; `ExecutePlanImport_SameKeyDifferentPlan_Conflicts` |
| Import disabled | `ImportsDisabled_HidesAndRefusesExecution`, `WritesDisabled_RefusesWritesButServesReads` |
| Versions and measured limits | This file and OPERATIONS.md § Measured limits |

Live run, 2026-10-24: a 30-day plan (120 meals, 32 products, 18 recipes, 23 KB) was
previewed and executed from a Claude Code conversation; the diary held exactly 120 meals
in 120 distinct slots afterwards, and the same operation key replayed without writing.
