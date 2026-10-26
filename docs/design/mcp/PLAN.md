# Home System MCP: monthly plan import, then API coverage

Status: approved; epic #605 and child issues #606–#616 created and linked.
Date: 2026-10-19.

MCP-1 (#606) outputs: [authentication decision record](AUTH.md) and
[API coverage inventory](API-INVENTORY.md).
These refine the existing backlog with an agent-independent server contract;
in-app assistant work is deferred. New contract/default decisions in those documents
await owner review and do not change the existing issues' approval status.

## Created GitHub issues

Epic: [#605](https://github.com/piotrkantorowicz/home-system/issues/605).

| Plan ID | Issue | Title |
|---|---|---|
| MCP-1 | [#606](https://github.com/piotrkantorowicz/home-system/issues/606) | docs(mcp): validate authentication and map API coverage |
| MCP-2 | [#607](https://github.com/piotrkantorowicz/home-system/issues/607) | feat(mcp): expose authenticated Diet Planner context over HTTP |
| MCP-3 | [#608](https://github.com/piotrkantorowicz/home-system/issues/608) | feat(mcp): expose visible product and recipe catalog tools |
| MCP-4 | [#609](https://github.com/piotrkantorowicz/home-system/issues/609) | feat(mcp): expose personal meal diary and nutrition summaries |
| MCP-5 | [#610](https://github.com/piotrkantorowicz/home-system/issues/610) | feat(mcp): create products with durable operation replay |
| MCP-6 | [#611](https://github.com/piotrkantorowicz/home-system/issues/611) | feat(mcp): create recipes from authorized product references |
| MCP-7 | [#612](https://github.com/piotrkantorowicz/home-system/issues/612) | feat(diet-planner): resolve monthly imports without silent skips |
| MCP-8 | [#613](https://github.com/piotrkantorowicz/home-system/issues/613) | feat(mcp): preview monthly plan imports before execution |
| MCP-9 | [#614](https://github.com/piotrkantorowicz/home-system/issues/614) | feat(mcp): execute monthly imports atomically with durable replay |
| MCP-10 | [#615](https://github.com/piotrkantorowicz/home-system/issues/615) | chore(mcp): configure deployment limits and operational controls |
| MCP-11 | [#616](https://github.com/piotrkantorowicz/home-system/issues/616) | test(mcp): verify conversational monthly import and document setup |

All 11 children are verified native sub-issues of #605. Dependencies in issue bodies
use GitHub issue references.


## 1. Product outcome

Discuss a monthly meal plan with an AI agent, refine it, preview its effect on Home
System, and import the agreed plan in one operation. Missing products and recipes
can be created in the same import. Standalone product and recipe creation is also
available through MCP for ongoing conversation and catalog maintenance.

Example journey:

1. Agent reads the user's meal slots, preferences available through existing APIs,
   existing products, recipes and scheduled meals for the target month.
2. User and agent discuss alternatives. The agent can read recipe details and
   create a reusable product or recipe when the user wants it saved independently.
3. Agent builds one structured payload containing products, recipes and dated meals.
   Definitions needed only by the plan can stay in that payload until import.
4. Preview resolves references and displays dates, planned meals, new/reused catalog
   items, occupied-slot conflicts, validation problems and nutrition completeness.
5. After the user agrees to that preview, the agent executes the exact plan.
6. App shows the entire month as planned meals. Lost responses and transport retries
   do not duplicate catalog items or meals. Nothing is marked eaten by import.

Long-term target: semantic MCP coverage of the application's user-facing API
operations, followed by explicitly scoped admin operations. This is a program of
epics, not a single PR or an unbounded first epic. Section 8 gives the expansion
backlog. Technical/test endpoints are classified rather than blindly exposed.

The assistant client remains external. No embedded chat UI, LLM provider or agent
scheduler is required for this integration.

## 2. Existing implementation and gaps

| Existing capability | Evidence and consequence |
|---|---|
| Batch import | `DietPlanner.Api/MealEndpoints.cs` exposes `POST /api/v1/meals/validate` and `POST /api/v1/meals/import`. Reuse and strengthen these application paths. |
| Combined payload | `ImportDtos.cs` has `Products`, `Recipes`, and `Schedule`; each scheduled date has meals referencing a recipe and meal type. This already models a month without one tool call per meal. |
| Product data | Import includes per-100g nutrition, units, density and grams per piece. Unknown nutrition must remain unknown, not invented by the agent or converted to zero. |
| Reference resolution | Ingredients refer to products by name; meals refer to recipes by name. Execution reuses matching catalog entries. Ambiguity and incompatible same-name content need explicit handling. |
| Existing preview | `ValidationResultDto` provides issues, `CanProceed`, and create/reuse counts. Extend rather than invent a disconnected validation model. |
| Partial execution risk | `ExecuteImportCommandHandler` skips invalid dates, unresolved products and unresolved recipes. The endpoint dispatches execution directly; it does not itself enforce the validation endpoint's outcome. A strict path is required for reliable monthly import. |
| Persistence | Execution calls one unit-of-work commit after building products, recipes, schedule and meals. Prove transactional behavior and add durable replay receipts within the same transaction. |
| Schedule changes | Execution can create a missing schedule and resolve unknown meal types with fallback behavior. Preview must reveal any schedule creation/fallback; strict import cannot hide these side effects. |
| Planned vs eaten | Meal completion is a separate command. Monthly import creates planned entries, never consumed entries. |
| Identity | Existing JWT and household/person claims pipeline must remain authoritative. A model-supplied person ID is not caller identity. |
| Runtime | `net10.0`, existing ASP.NET Core host and per-module persistence. Use the official C# MCP SDK in that host. |
| OAuth risk | Authentik is pinned to `2025.2.3`; the configured SPA client does not prove MCP discovery/registration compatibility. |

No MCP duplicate was found in GitHub searches for `MCP`, `"model context"` or
`"API parity"`. Import already shipped under issues #500 and #395; this work adds
agent access and strict/retry-safe semantics rather than rebuilding the import UI.
Recipe photo epic #563 remains independent.

## 3. Architecture and protocol decisions

```text
AI client -> HTTPS MCP endpoint
          -> validated token + existing household/person resolution
          -> per-operation authorization
          -> owning module's API tool adapter
          -> existing CQRS application commands/queries
          -> module database
```

- Prefer official `ModelContextProtocol.AspNetCore` with stateless HTTP. MCP-1 pins
  a released SDK and protocol version proven against the chosen client.
- Keep Diet Planner tool adapters in `DietPlanner.Mcp`; host only composes.
  Other modules own their adapters. No central tool assembly importing every
  module's application/domain internals, direct database tools or generic HTTP tool.
- Document MCP tool methods as transport endpoints under the existing rule
  “Dispatchers only in endpoints.” Dispatchers remain the application entry point;
  tools never inject individual handlers or copy business rules from them.
- Prefer explicit typed tools. OpenAPI may drive a coverage report, but publishing
  an operation requires intentional arguments, permissions, results and tests.
- Proposed grants: `mcp:diet:read`, `mcp:diet:write`, `mcp:diet:import`; execution
  requires write and import grants. Read-only tokens cannot perform writes, even
  through a direct tool call. Later modules get their own grants; admin is separate.
- Register tools explicitly and advertise read/destructive/replay behavior accurately.
  Annotations and a confirmation sentence never replace server authorization.
- Treat catalog names, recipe instructions and notes as data, not agent instructions.
- Preserve cancellation, bounded inputs, safe errors and request-scoped identity.
  Avoid dietary payloads, notes and tokens in logs.

### OAuth proof before implementation

Verify discovery metadata, registration method, exact callbacks, authorization code
with PKCE, issuer, audience/resource binding, grants and refresh against the actual
deployed versions. A distinct OAuth provider must still resolve the same human to
their existing person; do not assume subject claims are interchangeable.

Current Authentik DCR documentation starts at `2026.8.0` and describes authenticated
registration; neither this documentation nor an upgrade proves compatibility with
the chosen MCP client. Prefer pre-registration if supported. Record a bounded
prerequisite issue if upgrading or another authorization integration is required;
do not silently build an OAuth proxy or weaken audience checks.

The server is agent-independent. Use the official C# SDK client and an independently
implemented MCP client to validate the same tool contracts. MCP-1 records available
released client/SDK/protocol versions and their OAuth compatibility; this is test
coverage, not a choice of agent-specific server implementation. In-app assistant
work is deferred to a separate plan.

## 4. First epic and tool surface

Epic title: `feat(mcp): import monthly meal plans through an authenticated agent`.
Labels: `epic`, `backend`, `diet-planner`, `priority:medium`.

| Tool | Purpose | Delivery |
|---|---|---|
| `get_diet_context` | Caller readiness, meal slots, existing profile/goal data needed for planning; report missing setup without creating it. | MCP-2 |
| `search_products`, `get_product` | Find visible catalog items, units and nutrition without unnecessary owner data. | MCP-3 |
| `search_recipes`, `get_recipe` | Find reusable recipes and inspect ingredients and servings. | MCP-3 |
| `get_meal_entries`, `get_nutrition_summary` | Inspect occupied dates and existing nutrition semantics for an explicit date range. | MCP-4 |
| `create_product` | Save an explicit standalone product using existing validation and visibility rules. | MCP-5 |
| `create_recipe` | Save an explicit standalone recipe using authorized product references. | MCP-6 |
| `preview_plan_import` | Validate a combined product/recipe/schedule payload, resolve references and return an actionable preview. No catalog or diary writes. | MCP-8 |
| `execute_plan_import` | Commit the previewed monthly plan atomically with durable operation replay. | MCP-9 |

Eleven tools in the first epic. Single-meal planning/completion, edits and deletion
move into the API-coverage backlog; monthly import is the first complete workflow.

## 5. Monthly import contract

### Payload and reference semantics

- Reuse `ImportDto` as the compatibility baseline, including combined product and
  recipe definitions. Do not require hundreds of standalone creation calls first.
- At minimum support a complete 31-day month with six meals per day (186 entries)
  plus its catalog definitions in one execution. Proposed caps: 31 inclusive days,
  250 meals, 250 products and 250 recipes; set ingredient/byte limits from measured
  representative payloads within host limits and publish all enforced bounds.
- Require ISO calendar dates and explicit meal slots/types. Reject malformed dates,
  unresolved references, ambiguous names, unsupported units and invalid values with
  paths such as `schedule[4].meals[1].recipe`; never skip them silently.
- Produce a resolved application-level plan shared by preview and strict execution.
  Existing name references must either resolve uniquely or require disambiguation.
  If IDs/local references are needed, add a backward-compatible contract rather
  than replacing the SPA's import format.
- An existing same-name product/recipe with different relevant content produces
  an explicit conflict or reuse warning; never overwrite or blindly equate content.
  Imported catalog visibility follows existing defaults and is disclosed in preview.
- Missing meal schedule, unknown meal type and fallback-slot behavior must be
  explicit in preview. Default strict path asks for correction/setup rather than
  silently changing schedule configuration.
- Existing occupied slots default to a conflict. An explicit `append` choice may
  allow additional planned meals and must be included in preview and execution
  binding. Replacing a date range or deleting existing meals is a later issue.

### Preview and commit

Preview returns dates, daily meal summaries, new/reused products and recipes,
counts, reference resolutions, conflicts, warnings and nutrition completeness.
Calculate nutrition only from known application data; label incomplete totals.

Bind an expiring preview token to actor/person, normalized payload, selected
collision policy and resolved dependencies. Choose a signed token or stored preview
artifact during implementation; do not trust an unsigned client-supplied hash as
proof of a server preview. Revalidate permissions, references and conflicts at
execution. If the effective plan changed, require a new preview rather than commit
unreviewed changes. User confirmation happens in the client; the token binds the
reviewed data, not proof that a human clicked a button.

Execute returns a durable import operation ID, created/reused counts and enough
IDs/date information to verify the result in the app without a huge response.
Use the same operation key to recover a lost response; a separate status tool is
needed only if measured runtime requires asynchronous execution. Start with one
bounded synchronous transaction; do not add jobs/chunking without evidence.

### Transactions and retries

- Scope replay keys to actor, person and operation; bind canonical payload/policy.
- Same key and same request return the original result, including after restart.
  Same key with different content conflicts without mutation.
- Concurrent calls with the same key commit once using database-enforced uniqueness.
- Products, recipes, meal entries and successful operation receipt commit together.
  Injected failure mid-import leaves none of that import's new data committed.
- Preview is advisory until revalidated. Changed permissions/private references
  cannot be bypassed with an older preview token or replay request.
- Keep a documented replay retention policy. Pruning details must not silently
  turn a recognized old operation key into another month of meals. Distinct new
  keys remain distinct operations; key replay alone cannot catch every semantic
  duplicate, hence occupied-slot checking.
- Reuse narrowly scoped Diet Planner operation-receipt support for standalone
  product/recipe creation and batch execution; no generic shared-module framework.

Strict mode must be an application behavior used by the MCP transport, not a
second import engine in tool methods. Preserve existing REST/SPA compatibility;
make any changed legacy behavior explicit and regression-tested.

## 6. First epic: proposed child issues

Each local MCP ID becomes a GitHub issue after scope approval. Every issue is one
PR, has no new frontend UI scope unless stated, and runs `scripts/verify.sh --branch`
plus focused tests. Labels below use the existing repository vocabulary.

### MCP-1 — `docs(mcp): validate authentication and map API coverage`

**Context:** Resolve client compatibility and make the long-term API target finite.

**Scope:** Backend/infra proof of real-client OAuth, same-person identity, released
SDK/protocol choice and grants; document transport-endpoint rules. Inventory API
operations across all modules as planned MCP tools, transport-only or excluded with
reason. Include the existing import workflows and #500/#395 as prior work.

**Acceptance criteria:** Reproducible protected operation through the selected
client, or a specific prerequisite and recommendation. SPA/MCP person identity is
proven. Inventory records every discovered API operation and its target epic;
OpenAPI generation can assist the inventory but does not publish tools.

**Likely files:** This design directory, API/CQRS rule docs and local probe/config.
**Test plan:** Real-client discovery/login/refresh probe, safe claim inspection,
SPA login regression and operation-inventory reconciliation.
**Out of scope:** Automatic IdP upgrade, OAuth proxy and generated catch-all tools.
**Labels:** `documentation`, `backend`, `infra`, `shared`, `priority:medium`.
**Depends on:** None. **Size:** 0.5–1 day, excluding identified prerequisites.

### MCP-2 — `feat(mcp): expose authenticated Diet Planner context over HTTP`

**Context:** First useful protected connection for planning.

**Scope:** SDK/host wiring, explicit registration, metadata, verified client config,
read/write/import policies, feature switches and `get_diet_context`. Reuse existing
profile, goal and meal-schedule queries; no setup creation through a read tool.

**Acceptance criteria:** Correct caller context; two identities stay isolated;
missing setup is actionable; invalid/expired/wrong-audience tokens and missing
grants fail. Disabled endpoint is unavailable. Existing REST/SPA login still works.
Discovery metadata exceptions to endpoint authentication are explicit and bounded.

**Likely files:** Host/config, central packages, Authentik blueprint,
`DietPlanner.Mcp`, claims pipeline and integration-test fixtures.
**Test plan:** MCP SDK client through real ASP.NET auth/dispatchers/databases;
negative token/scope/context cases and real Authentik smoke.
**Out of scope:** Catalog writes and import execution.
**Labels:** `enhancement`, `backend`, `infra`, `diet-planner`, `priority:medium`.
**Depends on:** MCP-1 and any explicitly identified auth prerequisite.
**Size:** 1–2 days after compatibility proof.

### MCP-3 — `feat(mcp): expose visible product and recipe catalog tools`

**Context:** Reuse existing catalog while discussing a plan.

**Scope:** Product/recipe search and detail tools, explicit units/servings, typed
results, safe errors and bounded pages (proposed default 20, maximum 50).

**Acceptance criteria:** Visibility matches REST; guessed private IDs fail;
unknown nutrition stays null; pagination and missing-resource behavior are stable;
recipe details expose the information required for planning and ingredient reuse.

**Likely files:** `DietPlanner.Mcp` and module integration tests.
**Test plan:** Real database fixtures for public/own/shared/private items and two
identities; search/detail, bounds and empty-result cases through MCP.
**Out of scope:** Catalog mutation and photos.
**Labels:** `enhancement`, `backend`, `diet-planner`, `priority:medium`.
**Depends on:** MCP-2. **Size:** About 1 day.

### MCP-4 — `feat(mcp): expose personal meal diary and nutrition summaries`

**Context:** Inspect existing month before proposing more meals.

**Scope:** Caller-only diary/summary tools, explicit dates, planned/consumed status,
31-day inclusive bounds and documented existing nutrition calculation semantics.

**Acceptance criteria:** Full calendar month is queryable; invalid or longer range
fails; MCP values match REST for planned/completed fixtures; other users' diary
data remains inaccessible. No caller-identity or alternate-person argument.

**Likely files:** `DietPlanner.Mcp`, existing query DTO mappings and tests.
**Test plan:** REST/MCP behavioral comparison, fixed dates, empty month and identity
isolation through real dispatchers/PostgreSQL.
**Out of scope:** New nutrition formulas and household-member delegation.
**Labels:** `enhancement`, `backend`, `diet-planner`, `priority:medium`.
**Depends on:** MCP-2. **Size:** About 1 day.

### MCP-5 — `feat(mcp): create products with durable operation replay`

**Context:** Save missing catalog items during conversation without retry duplicates.

**Scope:** `create_product`, existing validation/default visibility and a narrow
Diet Planner operation-receipt persistence path reusable by the next write slices.
Receipt and product creation commit atomically; include migration and retention.

**Acceptance criteria:** Product fields/units match REST behavior; invented zero
nutrition is not substituted for unknown; unauthorized writes fail. Same-key replay
returns the original ID, concurrent replay creates once, changed payload conflicts,
and rollback commits neither product nor receipt. Return created ID and visibility.

**Likely files:** MCP tools, product application command, infrastructure receipts/
migration and module tests. Reuse business logic rather than copying handlers.
**Test plan:** PostgreSQL concurrency, fresh-host replay, rollback and write grants.
**Out of scope:** Update/delete and general cross-module replay framework.
**Labels:** `enhancement`, `backend`, `diet-planner`, `priority:medium`.
**Depends on:** MCP-3. **Size:** 1–2 days.

### MCP-6 — `feat(mcp): create recipes from authorized product references`

**Context:** Save reusable recipes developed in conversation.

**Scope:** `create_recipe`, existing recipe rules, explicit ingredient IDs/amounts/
units and serving semantics; reuse operation receipts from MCP-5.

**Acceptance criteria:** Saved recipe can be searched/read and used by import;
private/unknown product references and invalid quantities fail without partial
creation. Existing ownership/visibility rules hold; replay returns the same ID.

**Likely files:** MCP recipe tools, existing recipe commands and integration tests.
**Test plan:** Ingredient permissions, units/servings, invalid input, replay and
search-after-create through the actual transport/application pipeline.
**Out of scope:** Recipe editing/deletion and photo upload.
**Labels:** `enhancement`, `backend`, `diet-planner`, `priority:medium`.
**Depends on:** MCP-5. **Size:** About 1 day.

### MCP-7 — `feat(diet-planner): resolve monthly imports without silent skips`

**Context:** Existing execution can skip unresolved items; a whole-month workflow
needs a complete, authoritative resolved plan before writes begin.

**Scope:** Strict application-level validation/reference resolution shared by
preview and execution, bounded month payloads, occupied-slot policy and explicit
catalog/schedule reuse decisions. Preserve existing API contract compatibility.

**Acceptance criteria:** 31 days/186 meals plus catalog definitions resolve;
unresolved/ambiguous names, invalid dates/units and unauthorized references return
indexed errors. Incompatible same-name content and occupied slots are explicit.
No silent ingredient/meal skipping or hidden schedule creation in strict mode.
Legacy import behavior changes, if necessary, are explicit and regression-tested.

**Likely files:** `Commands/ValidateImport/`, `Commands/ExecuteImport/`, import DTOs,
application resolution types, repository query support and module tests.
**Test plan:** Real catalog fixtures, ambiguity, references to batch-created items,
custom slots, missing schedule, conflicts, 31-day limits and old import regressions.
**Out of scope:** MCP-specific token format, commit receipts, replace-range behavior.
**Labels:** `enhancement`, `backend`, `diet-planner`, `priority:medium`.
**Depends on:** MCP-1; implementation can precede transport after decisions settle.
**Size:** 1–2 days; split reference-contract changes if code inspection requires it.

### MCP-8 — `feat(mcp): preview monthly plan imports before execution`

**Context:** Let the user review the exact import after discussing it with the agent.

**Scope:** `preview_plan_import`, strict resolver integration, dates/day summaries,
counts, warnings/conflicts, nutrition completeness and actor-bound expiring preview
token. Reuse the combined import DTO instead of creating entities during preview.

**Acceptance criteria:** Preview describes all new/reused items and meals and
causes no catalog/diary mutations. Invalid plans have actionable paths and cannot
produce an executable preview. Token covers payload, target, policy and resolutions;
tampering/wrong user is rejected. Preview size stays bounded for a whole month.

**Likely files:** MCP import tools/DTOs, application preview binding and tests.
**Test plan:** Month-long fixture, error paths, no-write assertions, token tampering,
expiry and cross-user cases; changing payload invalidates its preview binding.
**Out of scope:** Execution, new import UI and storing chat conversations.
**Labels:** `enhancement`, `backend`, `diet-planner`, `priority:medium`.
**Depends on:** MCP-2, MCP-4 and MCP-7. **Size:** About 1 day.

### MCP-9 — `feat(mcp): execute monthly imports atomically with durable replay`

**Context:** Inject the agreed month with one call, not hundreds of uncertain writes.

**Scope:** `execute_plan_import`, preview revalidation, write/import grants, strict
execution and operation receipts from MCP-5 in one transaction. Return durable
operation ID, counts and bounded verification references.

**Acceptance criteria:** A 31-day/186-meal plan including new products and recipes
commits completely as planned entries. Changed references/permissions/conflicts
since preview require re-preview. Same-key retries after restart return the original
result; concurrent execution commits once. Different payload with same key fails.
Injected late failure rolls back products, recipes, meals and receipt together.
Existing meals are not overwritten; read-only users cannot import.

**Likely files:** MCP import tools, strict `ExecuteImport` application path,
infrastructure transaction/receipt support and integration tests.
**Test plan:** Real PostgreSQL transaction failure/concurrency tests, stale preview,
lost-response replay, missing grants, month completeness and SPA/API readback.
Measure realistic payload time/size against documented limits.
**Out of scope:** Background jobs unless measurements require re-planning, chunked
partial imports, implicit replacement or marking imported meals consumed.
**Labels:** `enhancement`, `backend`, `diet-planner`, `priority:medium`.
**Depends on:** MCP-5, MCP-6 and MCP-8. **Size:** 1–2 days.

### MCP-10 — `chore(mcp): configure deployment limits and operational controls`

**Context:** Operate monthly writes through the existing deployment.

**Scope:** Trusted public origin/proxy and HTTPS, appropriate Origin validation,
measured byte/item/runtime limits, per-user tool-call limits, read/write/import
switches, redacted diagnostics and rollback instructions.

**Acceptance criteria:** Public metadata URLs are correct and cannot be rewritten
by untrusted forwarded headers. Supported clients work; invalid Origins fail where
applicable. Switching off imports/writes prevents execution for existing clients.
Limits fail clearly without partial import. Logs contain correlation/operation ID,
outcome and duration without token, notes or full plan payload. REST remains usable.

**Likely files:** Host/MCP configuration, existing proxy/deployment files and runbook.
**Test plan:** Headers/Origin/switch/limit integration tests, representative proxy
smoke and log inspection with sensitive test data.
**Out of scope:** New gateway, monitoring platform or audit database.
**Labels:** `chore`, `backend`, `infra`, `diet-planner`, `priority:medium`.
**Depends on:** MCP-9. **Size:** About 1 day.

### MCP-11 — `test(mcp): verify conversational monthly import and document setup`

**Context:** Prove the user's monthly planning journey against a real client and app.

**Scope:** Deterministic SDK acceptance runner with real Authentik/databases;
supported-client walkthrough; verify month in SPA; client setup/troubleshooting guide.
Use isolated test identities and cleanup. Add focused Playwright coverage only
where existing import/calendar coverage cannot demonstrate persisted state.

**Acceptance criteria:** Catalog reuse and standalone creation work; then preview
and commit a 31-day plan containing missing products/recipes. Counts and dates match
preview and SPA readback; replay adds nothing. Cover independent second user,
read-only grant, stale preview, token refresh/re-login, conflicts and import-disable.
Record supported client/SDK/protocol/Authentik versions and measured limits.

**Likely files:** Integration/acceptance infrastructure, client docs and verification
or CI configuration; existing calendar/import E2E only if needed.
**Test plan:** Isolated full-month script, live client connection/tool walkthrough,
existing REST/SPA import regressions and final path-aware verification.
**Out of scope:** Automating unpredictable natural-language wording or a new chat UI.
**Labels:** `chore`, `backend`, `infra`, `diet-planner`, `priority:medium`; add `e2e`
only if the Playwright area changes.
**Depends on:** MCP-3 through MCP-10. **Size:** About 1 day.

## 7. First epic completion, dependencies and estimates

Done when a named client can read planning context, save products/recipes, preview
and atomically import a full month with new catalog definitions, recover a retry
without duplicates, and show the same plan in the SPA. Invalid plans must not
partially succeed; permissions must match the application.

```text
MCP-1 -> MCP-2 -> MCP-3 -> MCP-5 -> MCP-6 -----------+
             +-> MCP-4 ----------------+           |
MCP-1 -> MCP-7 ------------------------+-> MCP-8 -> MCP-9 -> MCP-10 -> MCP-11
```

Estimate: approximately 11–16 engineering days plus review/CI, assuming a viable
OAuth path. This replaces the earlier 7–10-day estimate for the smaller read/single
meal draft. An IdP upgrade or new authorization integration remains separately
estimated after MCP-1. Broad application API parity is not included in this range.

Independent child PRs target `main` with MCP disabled by default and execution
gated until complete. Earlier slices remain useful and deployable. No epic branch
is required if those conditions hold. Dependency independence does not authorize
parallel editing agents.

Delivery loop: start issue -> implement -> focused tests + verify -> ship -> review
in the other tool -> address review -> owner-approved merge. After issue-plan
approval, create/link children on GitHub and place approved work in `Todo`.

## 8. Application API coverage roadmap

User-facing operations remain in scope for the overall program. First epic is a
milestone, not a permanent exclusion list. MCP-1 creates the exact operation matrix
including method, route, ownership/role policy, proposed tool, destination issue,
status and deliberate transport-only exclusions. New API operations must be added
to that matrix so parity does not silently drift.

### Epic B — `feat(mcp): complete Diet Planner API workflows`

Proposed PR-sized children after the import epic:

| Local ID / proposed title | Scope / acceptance focus | Dependencies |
|---|---|---|
| DP-1 `feat(mcp): update and delete products with existing ownership rules` | Remaining product mutations, explicit destructive annotations, reference constraints and REST-equivalent permissions. | MCP-5 |
| DP-2 `feat(mcp): update and delete recipes with existing ownership rules` | Recipe mutations, servings/ingredient validation and referenced-recipe behavior. | MCP-6 |
| DP-3 `feat(mcp): create update and delete individual planned meals` | Existing meal CRUD with caller access checks and retry-safe creation. | MCP-4, MCP-9 |
| DP-4 `feat(mcp): complete reset and override meal consumption` | Complete/reset/actual-recipe/product overrides and bulk-complete with existing transition semantics. | DP-3 |
| DP-5 `feat(mcp): manage profile goals and weight prediction` | Profile/goal reads and writes plus existing prediction, using existing calculations. | MCP-2 |
| DP-6 `feat(mcp): log and manage weight entries` | Read/log/delete entries; explicit units, dates and repeat-safe append. | DP-5 |
| DP-7 `feat(mcp): manage hydration configuration and intake` | Existing config/intake operations and deletion, separate settings from logged events. | MCP-2 |
| DP-8 `feat(mcp): manage meal schedule and diet reminders` | Existing settings APIs and actual schedule-change constraints; no new scheduler. | MCP-2 |
| DP-9 `feat(mcp): expose shopping lists checks and weekly summaries` | Existing list/check/clear/summary operations and correct date/user scoping. | MCP-4 |

These are enhancement/backend/diet-planner/priority:medium candidates. Split any
slice whose real application behavior cannot fit one reviewable PR. Each requires
positive, validation and cross-user/role tests through MCP against real application
behavior. Existing photo work is linked if its API ships; file upload needs an
explicit transport design rather than an invented text-only equivalent.

An optional future issue can implement replacing an existing planned date range
after an explicit before/after preview, while preserving consumed entries. This
adds behavior beyond current append-style import and is not required for parity.

### Later module epics

| Proposed epic | Initial slice candidates from current endpoint groups |
|---|---|
| `feat(mcp): expose Household and person workflows` | Household context/membership reads; household/person mutations; membership/role actions. Break the 20 currently observed route declarations into authorization-coherent slices. |
| `feat(mcp): expose Budget workflows` | Budget/context reads; expense writes/corrections; settlement/repayment operations; envelope management. Inventory the 21 current route declarations before drafting exact child contracts. |
| `feat(mcp): expose Investments workflows` | Portfolio/wallet reads; wallet mutations; balance check-ins and supported history/correction operations. Inventory the 12 current route declarations and reuse their actual finance calculations. |
| `feat(mcp): expose notification preferences and inbox workflows` | Channel preferences; inbox reads and existing state mutations. Live SignalR transport is not automatically a tool. |
| `feat(mcp): expose privileged operational workflows` | Outbox administration and notification-delivery administration, with separate admin grants and existing admin authorization; classify destructive/replay actions individually. |

Counts are a source-code planning snapshot, not a validated OpenAPI parity report.
These later epics need module-specific rule review and detailed child issue bodies
before creation; do not claim their full scope or estimates are settled now.

Health probes, OpenAPI documents, test-support purge routes and protocol plumbing
are recorded in the inventory with their purpose; they are not business tools.
Admin tools require deliberate admin scope, not the everyday user's write grant.

## 9. Sources

Recheck version-sensitive decisions against the selected release in MCP-1.

- [Official C# SDK](https://github.com/modelcontextprotocol/csharp-sdk/blob/main/docs/concepts/getting-started.md)
- [C# SDK authorization filters](https://github.com/modelcontextprotocol/csharp-sdk/blob/main/docs/concepts/filters.md)
- [MCP authorization specification](https://github.com/modelcontextprotocol/modelcontextprotocol/blob/main/docs/specification/2026-07-28/basic/authorization/index.mdx)
- [Authentik OAuth2 provider](https://docs.goauthentik.io/add-secure-apps/providers/oauth2)
- [Authentik DCR version requirements and authorization](https://docs.goauthentik.io/add-secure-apps/providers/oauth2/dynamic-client-registration/)
- Existing import work: [#500](https://github.com/piotrkantorowicz/home-system/issues/500),
  [#395](https://github.com/piotrkantorowicz/home-system/issues/395).
