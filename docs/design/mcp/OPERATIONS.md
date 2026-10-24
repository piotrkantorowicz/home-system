# MCP operations runbook

Covers the controls added by #615. Design and auth: `PLAN.md`, `AUTH.md` (both in this directory).

## Configuration (`Mcp` section, `Authentication:Mcp`)

| Key | Default | Meaning |
|---|---|---|
| `Mcp:Enabled` | `false` | Maps `/mcp` and `/.well-known/oauth-protected-resource`. Off → both return 404 and REST is untouched. |
| `Mcp:PublicBaseUrl` | — (required when enabled) | Public HTTPS origin clients use, e.g. `https://home.example.com`. The resource metadata (`resource`) and the `WWW-Authenticate` `resource_metadata` URL are built from it. Request `Host` and `X-Forwarded-*` headers cannot rewrite them; metadata requests for any other host get 404. |
| `Mcp:AllowedOrigins` | `[]` | Browser origins allowed on `/mcp`. Requests without `Origin` (native clients) pass; any other origin gets 403 (DNS-rebinding protection). |
| `Mcp:WritesEnabled` | `true` | Grant switch for `mcp:diet:write`. Off → `create_product`, `create_recipe` and `execute_plan_import` disappear from `tools/list` and are refused, whatever the token grants. |
| `Mcp:ImportsEnabled` | `false` | Grant switch for `mcp:diet:import` (also needs writes). Off → `execute_plan_import` is hidden and refused; `preview_plan_import` keeps working. |
| `Mcp:RequestsPerMinute` | `120` | Fixed window per caller (`sub`). Over the limit → 429. Must be ≥ 1, or startup fails. |
| `Mcp:MaxRequestBytes` | `1048576` | Largest `/mcp` body. Declared larger bodies → 413 before auth-dependent work; the server caps chunked bodies at the same size. Must be ≥ 1, or startup fails. |
| `Mcp:DataProtectionKeysPath` | unset | Directory for the key ring that encrypts preview tokens. Unset → in memory: tokens die on restart (users re-preview) and do not work across instances. Set it to a persistent volume for multi-instance or restart-tolerant previews. The keys are stored unencrypted, and anyone who can read them can mint preview tokens (still bound to subject and plan, and re-checked at execution): mount a private volume readable only by the app user, and add `ProtectKeysWithCertificate` once a deployment target exists. |
| `Authentication:Mcp:Authority` | — (required when enabled) | Issuer of the MCP OAuth provider (Authentik `home-system-mcp`). |
| `Authentication:Mcp:Audience` | — (required when enabled) | Client id / audience of MCP access tokens. |

Switch changes take effect on restart.

## Measured limits

| Item | Value | Where enforced |
|---|---|---|
| Days per import | 31 inclusive | strict resolver |
| Meals / products / recipes per import | 250 / 250 / 250 | strict resolver (rejected before catalog queries) |
| Ingredients per recipe | 100 | strict resolver |
| Whole-month plan (31 × 6 meals, 2 products, 6 recipes) | ≈ 16 KB JSON | well under `MaxRequestBytes` |
| Full-month preview + execute (test host) | ≈ 2.6 s incl. host start; preview ≈ 0.9 s | one synchronous transaction |

## Logs

Every tool call logs one line from category `HomeSystem.Mcp`:

```
MCP tool {Tool} finished with {Outcome} in {ElapsedMs} ms (trace {TraceId})
```

`Outcome` is `ok`, `error` (an actionable tool error), or the exception type. Tool arguments,
results, notes, plans and tokens are never logged. Correlate with the HTTP trace id. The
execute result's `operationId` is returned to the client and stored in `operation_receipts`.

## Proxy and TLS

Pending a deployment target. Requirements whatever the proxy is:

- Terminate TLS in front of the host; `PublicBaseUrl` is the HTTPS origin clients see.
- Forward `/mcp` and `/.well-known/oauth-protected-resource` unchanged; do not buffer SSE
  responses (Streamable HTTP may stream `text/event-stream`).
- Forwarded headers are not trusted for MCP URLs; no `UseForwardedHeaders` change is needed.

## Rollback

| Situation | Action |
|---|---|
| Stop all agent access | `Mcp:Enabled=false`, restart. REST/SPA unaffected. |
| Stop writes but keep reads | `Mcp:WritesEnabled=false`, restart. |
| Stop imports only | `Mcp:ImportsEnabled=false`, restart. |
| Revert code | Migrations `AddOperationReceipts` and `AddOperationReceiptResult` only add a table/column; older code ignores them. Keep the table: dropping receipts would let old operation keys write again. |
