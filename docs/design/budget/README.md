# Budget module — design and delivery plan

Status: **v1 approved; review amendments accepted 2026-10-02; implementation waits for documentation issue #450 to merge**

Epic: [#233 — household-scoped expenses](https://github.com/piotrkantorowicz/home-system/issues/233)

Prepared: 2026-09-26; decisions updated: 2026-10-02

Related design: [Household §8, phase 3](../household/README.md#phase-3--budget-module-epic-233)

## 1. Recommendation

Build a household expense tracker with spending envelopes, optional monthly limits, and a reliable settle-up view. Start with manual entry. Give every amount a clear owner, audience, funding source, and effect on settlement.

The first useful experience is: open Budget, add groceries paid by one adult, see the household's spending, and see exactly why another adult owes money. A private purchase must never appear in another adult's totals or duplicate warnings.

Use the existing DDD + EF module pattern, a dedicated PostgreSQL database, Household Contracts, existing UI primitives, and existing test infrastructure. No banking SDK, new state library, generic accounting engine, or cross-module permission framework is needed.

The owner approved the v1 scope and publication of B1–B11 on 2026-09-29, and the review response amendments on 2026-10-02. This file is the authoritative specification; the [review](../../superpowers/specs/2026-10-02-budget-design-review.md) is historical evidence and the [implementation plan](../../superpowers/plans/2026-10-02-budget-implementation-plan.md) links back here. #450 now publishes the documentation prerequisite; its former duplicate-hint work moves into #448 and #452. The product is an expense log with limits and categorization. Decisions are recorded in §13. The refund design in §7 is a proposed deferred follow-up, explained in response to the owner’s question; it is not added to v1.

## 2. What the repository already provides

Reviewed local checkout `31a3c3c` on `fix/428-epic-228-e2e-failures`. GitHub issue status checked on 2026-09-26. This is a design against that snapshot; implementation must start from fresh `origin/main`.

| Evidence | Implication |
|---|---|
| [#213](https://github.com/piotrkantorowicz/home-system/issues/213) and [#234](https://github.com/piotrkantorowicz/home-system/issues/234) are closed | Household foundation and multiple EF module support are available. The epic's prerequisite is satisfied. |
| The initial review found no Budget child issues | Approved B1–B11 were created as #446–#456 on 2026-09-29; see §10 for links. |
| [`IHouseholdQueryService`](../../../src/Modules/Household/Household.Contracts/Interfaces/IHouseholdQueryService.cs) returns [`HouseholdContext`](../../../src/Modules/Household/Household.Contracts/Interfaces/HouseholdContext.cs) and member IDs, names, roles, and `IsManaged` | Budget can resolve identity and current membership without new Household APIs. |
| [`HouseholdQueryService`](../../../src/Modules/Household/Household.Infrastructure/Query/HouseholdQueryService.cs) suppresses the caller's ambient transaction | Preserve that boundary; do not enlist two module databases in one transaction. |
| [`HouseholdClaimsTransformation`](../../../src/Modules/Household/Household.Api/Identity/HouseholdClaimsTransformation.cs) catches lookup failures and may leave household claims absent | Budget must independently resolve current access through Contracts and fail closed. Missing claims are not permission to fall back to personal mode. |
| [`HouseholdRosterProvider`](../../../src/Modules/DietPlanner/DietPlanner.Application/Households/HouseholdRosterProvider.cs) and [`LibraryAccess`](../../../src/Modules/DietPlanner/DietPlanner.Application/Households/LibraryAccess.cs) demonstrate access patterns | Follow the approach locally in Budget; importing DietPlanner internals would violate module boundaries. |
| [`DeleteHouseholdCommandHandler`](../../../src/Modules/Household/Household.Application/Commands/DeleteHousehold/DeleteHouseholdCommandHandler.cs) deletes the household; current Contracts expose no household-deleted event | Do not assume cleanup notifications exist. Define retention and access after deletion explicitly. |
| [`main.tsx`](../../../src/ui/src/app/main.tsx) registers UI modules | Register Budget there. The frontend rule doc's router-registration example differs from current code. |
| [`verify.sh`](../../../scripts/verify.sh) discovers touched module test projects | Add Budget projects to the solution; avoid unnecessary verification-script changes. |
| [Label catalog](../../../.github/labels.json) has no `budget` label | Drafts use existing area labels. Add a module label only when publishing the plan, if desired. |

These observations describe the original review snapshot. The v1 design below was accepted on 2026-09-29; deferred follow-ups remain proposals.

## 3. Scope and vocabulary

The epic currently combines accounts, envelopes, allowances, expense categories, and debt. Give each a single meaning:

| Concept | v1 meaning |
|---|---|
| `Budget` | One household's spending workspace, with one currency. |
| `BudgetAccount`, UI “Envelope” | A named spending bucket with `Household` or `Personal` visibility. Examples: Everyday, Holidays, Alex's spending, Sam's allowance. |
| Category | A stable reporting code attached to each expense: groceries, housing, utilities, transport, health, leisure, education, other. Localized labels; no category administration in v1. |
| Funding source | `Individual` or `HouseholdFunds`, chosen on each shared expense. An envelope is not a bank account. |
| Expense | A positive amount, purchase date, category, envelope, payer where applicable, recorder, and stored shares. |
| Monthly limit | Optional spending target for one envelope and calendar month. Exceeding it warns; recording real spending still succeeds. |
| Settlement | A record that one person already paid another to reduce outstanding shared debt. It does not send money. |

An envelope groups purpose or ownership; category describes a purchase. “Everyday / groceries” and “Holiday / groceries” remain useful distinct combinations. Selecting a category does not create another envelope or double-count spending.

**Included:** envelope setup and archive, shared and personal expenses, exact equal splits among selected adults, corrections and voiding, duplicate hints, retry safety, monthly spending limits, outstanding balances, recorded repayments, English/Polish UI, access tests.

**Deferred follow-ups, outside the v1 completion gate:**

| Draft follow-up | Scope and trigger |
|---|---|
| F1 — bank/CSV import | Dedicated later epic: preview, source mapping, stable imported IDs, overlap reconciliation with manual entries, and import audit. Begin when manual entry becomes the main friction. Import alone does not prevent duplicates. |
| F2 — income and cash accounts | Bank balances, opening balances, deposits, transfers, actual allowance payments, and joint-pot contribution accounting. Needed when users ask “how much cash is available?” |
| F3 — refunds | Linked partial/full refunds, original-share reversal, cumulative caps, and atomic protection against over-refunding. Needed before claiming net spending after merchant returns. v1 accepts expenses only; corrections must not be presented as refunds. |
| F4 — recurring expenses and richer planning | Scheduled entries, recurring limit templates, rollover, annual targets, receipt attachments, custom categories, unequal/custom splits, notifications. Add only for observed use. |
| F5 — portability and retention | Private-history access after leaving, user export, household deletion/purge workflow. Resolve before broader deployment beyond the current home-app scope. |

F1–F5 are proposal identifiers, not existing GitHub issue numbers. No fake references or implied delivery commitments.

## 4. Money, dates, and expense rules

### Currency and precision

Default currency: PLN. Allow PLN, EUR, or USD at initialization; each uses two decimal places. A Budget's currency is immutable once created. Each envelope and transaction inherits it; reject mismatched input. No conversion or mixed-currency settlement.

Represent money with a validated Budget-local value object backed by C# `decimal`; store `numeric(18,2)`. Request/response money fields are explicit decimal strings such as `"123.45"`, documented as strings in OpenAPI. Do not depend on an assumed host-wide decimal converter. Require a positive expense/settlement amount, at most two fractional digits, no exponent notation, and a maximum of `9999999999.99` per entry. Reject excess precision instead of silently rounding. Limits may be zero. Aggregated totals may exceed one entry's limit.

The browser keeps input as text, accepts the active locale's decimal separator, and normalizes to the API format. Server computes authoritative shares, totals, and balances. Use exact minor-unit arithmetic for split calculations; no binary floating-point money math. Format output with existing locale facilities without converting amounts through an imprecise arithmetic path.

### Dates

`OccurredOn` is a `DateOnly` supplied as `YYYY-MM-DD`. Creation, correction, and void timestamps are UTC instants from `TimeProvider`. UI defaults the purchase date to the user's local today; a timezone conversion must not move the purchase to another day. v1 permits any valid purchase date, including future dates, but flags future dates in the form. A monthly report uses `[first day, first day of next month)` on `OccurredOn`.

### Ownership and attribution

Resolve `HouseholdId`, actor `PersonId`, and role from authenticated subject through Household Contracts. Never trust these actor fields from a request body. The selected envelope, payer, and participants must be valid for the resolved household and access policy.

`AddedByPersonId` records the human who entered the row and is immutable. `PaidByPersonId` records whose personal money funded it; it is independent of the recorder. For example, Alex records a purchase paid by Bea: Alex is recorder, Bea is payer.

For `HouseholdFunds`, `PaidByPersonId` is null: nobody receives personal credit. The UI labels this “Paid from household funds.” It counts toward spending and envelope limits, with no settlement shares. Funding the joint pot is outside v1.

A personal expense uses `Individual` funding, its envelope owner as payer, and no shared split. An adult recording for a managed child remains the recorder while the child remains the spending owner. Adults recording their own payment for a child's benefit should use a shared envelope instead.

### Shared splits

For an individual-funded shared expense, default to equal shares among current Owner/Adult members. Display the proposed participants and amounts before saving. Allow an explicit non-empty subset; the server computes equal shares. Custom per-person amounts are deferred. The request sends explicit participant IDs, never a dynamic “all current adults” token. The payer must be an eligible current adult but need not owe a share. At least one share is positive; very small expenses may produce zero-cent shares. Reject duplicate participants, unknown IDs, and client-supplied share amounts.

Children and guests are excluded from debt participation in v1. A parent can pay for a child's needs and split that cost among adults.

Persist the final participant IDs and exact amounts. Never recalculate historical shares from today's roster, roles, settings, or names. Store `AddedByDisplayName` and nullable `PaidByDisplayName` on each expense, `PersonDisplayName` on each share, and `FromDisplayName`, `ToDisplayName`, and `AddedByDisplayName` on each settlement. Expense revision snapshots preserve these names with the financial fields. For current members, display the current roster name; otherwise display the stored snapshot. For an unchanged former participant or a new repayment involving them, reuse their latest available snapshot from authorized history in this Budget, ordered by recorded timestamp and ID; settlement resolves former names from shared-ledger history only. Names never grant access. No participant registry or write-on-read refresh is needed.

For equal splits, convert to minor units, divide by participant count, and allocate remaining cents by canonical `PersonId` order. A `100.00` expense among three sorted participants yields `33.34`, `33.33`, `33.33`. Server and tests use the same documented ordering; the UI consumes the result. This deterministically assigns remainder cents to the same ordered participants; the cumulative bias is accepted for v1. Fair rotation is deferred, not silently introduced through an unspecified runtime hash.

### Corrections and concurrency

Correct an expense in place, keeping its ID and original `AddedByPersonId`. In the same Budget transaction, append an immutable `ExpenseRevision` containing the full resulting financial state, all exact shares and name snapshots, operation kind, actor, reason, timestamp, normalized request, request ID, and applied revision number. Creation writes revision 1. Every later revision retains its predecessor, so all previous values and shares remain available. This is audit history, not event sourcing: current reads use the live expense and its share rows.

Require a short reason and expected revision for update/void. Use an EF concurrency token following Household's existing PostgreSQL `xmin` mapping; keep an explicit increasing revision number for durable history/API results. Every share-only edit also updates the parent expense so the concurrency check applies. History append, parent update, and share replacement commit or roll back together. A fresh stale request returns typed 409 with a reload action; a recognized replay returns its original operation result before checking its now-stale expected revision (§6).

Keep household, envelope, and original recorder immutable. Category, amount, purchase date, funding source, payer, and selected participants may change subject to their invariants. Unchanged historical payer/participant IDs may survive departure or role demotion, but cannot be newly introduced. If amount and participant set are unchanged, retain exact stored shares; if either changes, recompute the equal split for the supplied set, permitting retained historical IDs only. To move an expense between envelopes, void it and deliberately create a new one; no privacy change is hidden in an edit.

Voiding marks the row inactive and appends a revision. Repeating a successful void does not add another revision. Edits of voided expenses are rejected. Authorized history includes full before/after values via adjacent revisions, never another person's private data. No replacement expense or replacement-link graph is created.

Historical edits/voids recalculate spending and outstanding balances, retaining actual repayments. A resulting reverse credit is shown honestly. There is no permanently closed month in v1.

## 5. Access and membership lifecycle

### Accepted Budget-specific permissions

Approved on 2026-09-29: a Child sees only their own data; Guest has no Budget access. Shared financial data is restricted to Owner/Adult. This Budget-specific exception is also recorded in the Household design; application enforcement will ship in the implementation slices.

| Resource/action | Owner / Adult | Child with login | Guest |
|---|---|---|---|
| Shared envelopes, expenses, totals, settlement | Read/write | No access | No access |
| Own personal envelope and expenses | Read/write | Read/write | No access |
| Another independent person's personal data | No access | No access | No access |
| Personal envelope for a current managed member | Read/write | No access | No access |
| Initialize Budget | Allowed | Not allowed | Not allowed |

Owner status does not unlock another adult's private money. A Child may create an own personal envelope after Budget initialization. An adult may create a personal envelope for themselves or a managed member only. Shared-envelope creation and management require Owner/Adult. A Guest has no Budget access, including to an envelope they owned before a role change.

`Household` envelopes have no personal owner; `Personal` envelopes require one. Visibility and personal ownership are immutable. Archive hides an envelope from new-entry choices, retains authorized history, and can be undone by an authorized manager. Historical corrections remain possible while archived; new expenses and new limits do not.

Apply identical visibility rules before list filtering, pagination counts, detail lookup, monthly aggregation, duplicate matching, and history projection. A hidden account's name, existence, amount, count, or duplicate status must not escape through another response. Personal spending never feeds household totals or settlement. Label totals explicitly as “Shared spending” or “My spending.”

Outside-household or invisible resource IDs return 404. A visible resource with a disallowed action returns 403. No household produces a setup-required state, not an empty personal budget. A failed Household lookup returns the existing error path and no Budget data; the UI offers retry. Do not swallow lookup errors into empty or permissive results.

### Membership changes

Each command/query resolves current membership. Do not authorize from a cached roster or solely from augmented JWT claims. A member removed before the next request loses access. Because Household and Budget use separate databases, removal racing with an already-authorized in-flight transaction can allow that transaction to finish; v1 makes no claim of distributed atomic revocation.

Stored shared expenses and shares remain attached to their original `HouseholdId`. Remaining adults can see historical former-member labels and balances. New expenses cannot select departed members. Recorded repayments may reference a former adult already present in that household's shared ledger. Never carry their debt into a new household. The same rule applies to an adult demoted to Child/Guest: retain their historical debt/credit in adult settlement views and mark them as a former adult; exclude them from new expense participants. They lose shared-ledger access under their new role, but remaining adults may record actual repayments on their behalf. Unpaid balances remain outstanding. Never fabricate a repayment to clear debt; explicit forgiveness/write-off is deferred.

Personal envelopes of departed members become inaccessible through v1's household-scoped API. Remaining adults cannot inherit them. Rejoining the same household restores access under current role/ownership rules. Linking a managed person to a real account preserves `PersonId`; adult delegated access ends on the next request when `IsManaged` becomes false.

If the household is deleted, all Budget rows are inaccessible through normal APIs and retained in its database. No silent cascading deletion, no automatic transfer to a new home, and no invented deletion event. This retention limitation is a product decision in §13, with F5 covering a complete lifecycle later.

## 6. Duplicate warnings and retry safety

These solve different problems and need separate acceptance tests.

**Possible duplicate:** another non-void expense in the same household, same currency, exact amount, same category, and `OccurredOn` within two calendar days inclusive. Payer and envelope do not narrow the match, preserving the epic's rule. Visibility always narrows it. Exclude the expense being corrected and voided originals.

The form calls the existing authorized `GET /expenses` with exact `amount`, `category`, inclusive `from`/`to` dates, optional `excludeId` for edits, and `pageSize=5`. Budget currency comes from the current Budget, not a caller-selected cross-currency scope. The list applies visibility and active-state predicates before every filter, count, and page. Reuse its projection and query path; do not add a duplicate-check endpoint or a create-time re-check.

Show up to five possible matches, with “Keep both” and “Cancel.” Changing amount/category/date reruns the hint before confirmation; checking failure must not be displayed as “no matches.” The user can retry or explicitly save without a hint. A deliberate second expense always remains possible. Two concurrent submissions can miss each other; this accepted best-effort race does not justify a “saved, possible duplicate” response state. The create API returns only the operation result.

### Durable request identity without a receipt table

Keep server-generated `Guid.CreateVersion7()` resource IDs. Clients generate a `ClientRequestId` per logical create/edit/void submission and preserve it on retry. There is no generic `operation_receipts` table or fingerprint hash.

For expenses, store request identity and normalized request fields in each immutable `expense_revisions` row, including creation revision 1. Unique key: `(BudgetId, ActorPersonId, ClientRequestId)`. The stored request identifies action, target expense where applicable, expected revision for mutations, and all normalized user inputs. Normalize money strings and participant ordering; do not include server-calculated names, timestamps, or a newly resolved roster in request equality. Retain the server-calculated shares in the revision snapshot separately.

For repayment creation, use a unique `(BudgetId, AddedByPersonId, ClientRequestId)` on the settlement row and compare its immutable creation fields. Financial fields stay immutable when a repayment is voided. Expense and repayment request-ID namespaces are separate. Repayment void is an idempotent transition with expected revision and reason; retrying a completed void is a no-op.

Authorize the current caller and resource before any replay result. Then look up the request identity before stale-revision or current-participant validation: a request already committed does not execute again. Same key/action/target/normalized payload returns the original `{ resourceId, appliedRevision }`; changed action/target/payload returns 409. A key from another actor does not expose or reuse their data. The UI fetches current state separately, so replaying an older operation never reinstalls an outdated version into its cache.

For a new expense request, validate current membership and all domain rules, then commit row/shares/revision atomically. On a uniqueness race, roll back and reread the winner in a fresh transaction/context. If an edit loses a concurrency race, first check whether its own request already succeeded; return the prior result for that replay, otherwise 409. Never query an aborted PostgreSQL transaction.

Required sequence: create → edit → retry original create; the retry matches revision 1, not today's live row. Likewise edit → another edit → retry first edit returns its original result without reverting either change. Actor scoping, current visibility, changed-payload, concurrent retry, and retry-after-void tests remain mandatory.

## 7. Monthly spending and settlement

### Monthly envelopes

Store one optional limit per `(BudgetAccountId, MonthStart)`. There is no rollover, auto-copy, income allocation, or account balance. Missing limit means “No limit,” not zero. A `0.00` limit means any spending exceeds the target.

`spent = sum(active expenses in the selected envelope/month)` and `remaining = limit - spent`. Settlements never count as spending. Corrections affect the purchase month, not the correction timestamp's month. Future-dated expenses count in their specified month. Overspending is visible text as well as color. Private-envelope limits have the same privacy as their expenses.

### Who owes whom

Compute from all active individual-funded shared expenses and all active repayments, regardless of their entered dates. There is no `asOf` parameter or server-default “today” cutoff. Future-dated records count immediately; the entry form explains that these are recorded facts, not scheduled purchases/payments. Label the page “Outstanding balance — all recorded entries.” Month filters on spending never restrict debt. Monthly spending still follows each explicit `OccurredOn` month.

For person `p`, in exact minor units:

`net[p] = personal payments[p] - assigned shares[p] + repayments sent[p] - repayments received[p]`

Positive means the person should receive money; negative means they owe money. Across every participant, the sum must be zero. Personal expenses, household-funded expenses, limits, and voided rows contribute nothing.

| Entry | Alex net | Bea net | Spending |
|---|---:|---:|---:|
| Alex pays 120.00; split 60.00 each | +60.00 | -60.00 | 120.00 |
| Bea pays 40.00; split 20.00 each | +40.00 | -40.00 | 160.00 |
| Bea repays Alex 25.00 | +15.00 | -15.00 | 160.00 |
| Household funds pay 80.00 | +15.00 | -15.00 | 240.00 |
| Alex adds private 30.00 | +15.00 | -15.00 | Shared remains 240.00 |

Each row shows cumulative balances. A membership change does not change these numbers.

Generate deterministic suggestions by matching debtors and creditors, sorted by remaining amount and then canonical person ID, transferring the smaller outstanding magnitude each step. Suggestions conserve every cent and end at zero; they need not minimize the number of transfers globally. Explain any simplification of pairwise debts in the UI: suggested recipients may differ from the original payer when three or more people share costs.

Repayments record an amount actually paid, date, sender, recipient, recorder, and optional note. Sender and recipient must differ and must be current eligible adults or historical shared-ledger participants. Owner/Adult may record on behalf of either party. Confirmation states who paid whom. This is a household record, with no bank transfer or recipient-approval workflow.

Do not reject an actual payment merely because it exceeds a current suggestion; it may create a reverse credit. Show a warning, then record the stated fact. Require positive amounts, idempotency, and explicit void reasons. Correct a repayment by voiding and re-entering. Persisted payments survive new expenses and expense corrections; suggestions are always derived.

### Deferred refund design — F3

Refunds remain outside v1. The following is the proposed follow-up behavior requested during decision review; no refund implementation issue is included in B1–B11.

**User flow:** open an expense → “Record refund” → enter the amount actually received, receipt date, and optional note → review the effect on spending and shared debt → save. Offer “Full remaining amount” as a shortcut. An expected return is not recorded until the merchant has returned the money. A refund is a separate linked record; the original purchase remains visible.

**Attribution and scope:** inherit the original expense’s currency, envelope, category, and visibility. Record the current actor separately. For an individual-funded expense, v1 of F3 assumes the refund goes back to its original payer; for household funds it returns to household funds. Money returned to someone else or converted to store credit needs a later extension, not a guessed recipient. Private refunds remain private, and household-funded refunds never create personal debt.

**Full and partial refunds:** amounts are positive and cannot exceed the original amount less active refunds. Multiple refunds are allowed. For shared individual-funded expenses, reverse cost shares using the stored participants, never the current roster. For a partial refund, allocate exact minor units in proportion to each participant’s remaining unrefunded share: floor each proportional allocation, then distribute leftover cents by descending fractional remainder, breaking ties by canonical person ID. Store that allocation on the refund. Each allocation is capped by the remaining share; a full remaining refund reverses every remaining cent exactly. A person who has left the household keeps their historical share.

**Settlement effect:** subtract the refund from the original payer’s personal-payment credit and add each refunded share back to its participant’s net balance. Existing repayments stay recorded. An expense of 120.00 paid by Alex and split equally starts with Bea owing Alex 60.00. A 40.00 refund to Alex reduces the net expense to 80.00, so Bea now owes 40.00. If Bea already repaid the original 60.00, Alex instead owes Bea 20.00. A full refund after that original repayment leaves Alex owing Bea 60.00.

**Monthly limits:** show gross expenses, refunds received, and net spending separately. Attribute the refund to its receipt month and the original category/envelope. A September purchase refunded in October reduces October net spending and increases October remaining limit; it does not rewrite September’s purchase-date report. Net monthly spending can be negative. All-time expense detail also shows the purchase’s remaining net cost. Label this clearly; no automatic limit rollover is introduced.

**Integrity:** use durable request IDs like expenses. Refund creation and voiding serialize through the original expense’s revision/lock inside the Budget database transaction, so two concurrent refunds cannot exceed its remaining amount. Reject refunds on voided expenses. Permit refunds to archived envelopes because they adjust history. Reject correction/void of a purchase while it has active refunds; the user must first void those refund records, with reasons, then explicitly re-record the correct facts. Voiding a refund reverses its stored allocation, without recalculating later refund allocations. Refund history follows the same access and retention rules as its purchase.

**Required follow-up checks:** partial/full/repeated refunds; concurrent cap enforcement; exact uneven-cent allocation; refund after settlement; refund after member departure; different purchase/refund months; private and household-funded refunds; retry/void history. No negative expenses or silent purchase edits stand in for merchant refunds in v1.

## 8. Architecture and API shape

### Aggregate boundaries and storage

| Root/table | Responsibility |
|---|---|
| `Budget` / `budgets` | Household identity and immutable currency. Unique household constraint. No transaction collection. |
| `BudgetAccount` / `budget_accounts` | Envelope name, visibility, owner, archive state, revision. |
| `Expense` / `expenses` | Current money, category, date, funding source, payer/recorder with name snapshots, void metadata and revision; owns a small share collection. |
| `MonthlyLimit` / `monthly_limits` | Envelope/month target and revision; unique envelope/month constraint. |
| `Settlement` / `settlements` | Recorded repayment, attribution, date, void metadata, revision. |
| `ExpenseRevision` / `expense_revisions` | Immutable full expense/share snapshots from creation onward, actor/reason/time, normalized request and request ID; local audit record owned by the expense, not a separate aggregate. |

Keep expense history out of the Budget aggregate so one purchase never loads years of rows. Use typed aggregate IDs, `Guid.CreateVersion7()`, private setters, named domain methods, `TimeProvider`, and `ct` propagation. Local database constraints enforce positive amounts, owner/visibility shape, unique keys, and local references. Cross-module person IDs have no cross-database foreign key; validate through Contracts.

Every row is reachable through a Budget household boundary. Where household/budget IDs are repeated for query efficiency, enforce consistency with local composite foreign keys rather than allowing mismatched account/expense ownership. Server equal-split calculation and domain validation enforce share totals; expense/share inserts commit atomically.

Queries use `IBudgetReadDbContext`, `AsNoTracking()` and projections following Household's existing read-context pattern. Commands use narrow repositories and `IBudgetUnitOfWork`. Register handlers per module; retain the single host dispatcher chain. Use one scoped concrete Budget access helper rather than a new generic authorization abstraction. Unknown role strings deny access. Do not add cross-module caching to optimize the separate claims-transformation lookup without measured need.

Add Budget's own migrations and database, solution projects, host registration, connection-string configuration, Compose profile/volume, and local-stack/E2E setup instructions. Allocate an unused database port after checking the current compose file. The real-stack E2E workflow must start/stop the new profile and pass its connection string. Configure module outbox infrastructure according to repository conventions, but publish no financial integration events without a consumer. Contracts must never expose private transaction data speculatively.

Indexes: unique household Budget; envelope lookup by budget/visibility/owner; expense list by household/date/ID; duplicate lookup by household/category/amount/date with void filtering; shares by person/expense; settlement household/date; unique envelope/month; unique expense revision number per expense and request identity per Budget/actor; unique settlement creation request identity per Budget/actor. Verify query behavior on PostgreSQL before adding further indexes.

### Proposed endpoint inventory

All routes require authentication; handlers enforce access. Every endpoint uses typed results, an operation name, existing ProblemDetails conventions, and generated OpenAPI types.

| Route under `/api/budget` | Purpose |
|---|---|
| `GET /`, `POST /` | Read or initialize the current household's Budget; concurrent initialization returns the existing same-currency Budget, incompatible currency returns 409. No side effects on GET. |
| `GET /accounts`, `POST /accounts` | Page visible envelopes; create one. |
| `PUT /accounts/{id}`, `POST /accounts/{id}/archive`, `POST /accounts/{id}/restore` | Rename/archive/restore with expected revision; visibility/ownership cannot be edited. |
| `GET /expenses`, `GET /expenses/{id}` | Authorized filtered list/detail including permitted actions; detail exposes authorized immutable revision history. |
| `POST /expenses`, `PUT /expenses/{id}`, `POST /expenses/{id}/void` | Record, edit with history, or void expenses; mutation responses identify resource and applied revision. |
| `GET /summary?month=YYYY-MM&scope=shared\|personal` | Server totals by envelope/category; personal scope means the caller, with an explicitly authorized owner filter for managed members. |
| `GET /limits?month=YYYY-MM`, `PUT /accounts/{id}/limits/{month}`, `DELETE /accounts/{id}/limits/{month}` | Read, set, or clear monthly limits; expected revision on existing rows. |
| `GET /settlement` | All-recorded balances, deterministic suggestions, and currency; no date cutoff. |
| `GET /settlements`, `POST /settlements`, `POST /settlements/{id}/void` | Page repayment history; record or void a payment. |

List endpoints use `PagedList<T>` with page-size cap 100 and stable ordering. Summary and settlement are bounded aggregate DTOs, not raw transaction dumps. Invalid filters/amounts use 400, domain violations 422, invisible IDs 404, denied actions 403, and conflicts typed 409. Add focused conflict responses without globally remapping unrelated exceptions.

## 9. UI and interaction

Register `budgetModule` in the existing app registry with `/budget`, `/budget/expenses`, `/budget/envelopes`, and `/budget/settlement`. Settlement/shared navigation is absent for children. Use `useHousehold()` only through Household's public module export.

| Screen | Main behavior |
|---|---|
| First use | Adult chooses currency and initializes Budget with one shared “Everyday” envelope. Concurrent initialization creates no duplicates. Child sees “Ask an adult to set up Budget”; Guest gets an unavailable state. |
| Overview | Month selector, clearly separate Shared/My spending, envelope spent/limit/remaining, recent visible expenses, Add expense. No “bank balance” label. |
| Add expense | Amount, date, category, envelope; shared entries add funding source, payer, and participant/share preview. Advanced details hold note and selected adult participants; shares are equal and computed by the server. Recorder is automatic. |
| Expense history/detail | Date/category/envelope/payer filters, recorded-by attribution, correction history, permitted correction/void actions. Preserve form contents on failure. |
| Envelopes | Name, audience/owner, optional monthly limit, archive/restore. Show private-access explanation during creation. |
| Settle up | Outstanding balances, suggested transfers, “Record payment,” dated payment history. Explain that recording a payment does not move money. |

Use existing Field/Dialog/Sheet/Button components, react-hook-form + Zod, native date input where suitable, named exports, generated API types, and `queryOptions()`. Keep money as decimal text. Use server-computed capabilities in DTOs (`canEdit`, `canVoid`, etc.); UI hiding does not replace server checks. Add English and Polish strings together.

Query keys include authenticated subject, household ID, and every filter. Invalidate expense lists, summary, and settlement after relevant writes. Clear Budget cache on logout/household change/access denial; do not show cached financial rows after a permission failure. Avoid persistent browser storage of financial responses. Refetch on focus; live push and offline entry are deferred.

Mobile forms must fit 390px, have labeled controls and visible keyboard focus, announce duplicate warnings, and explain overspending without color alone. Loading, genuine empty data, setup-required, forbidden, and load failure are distinct states.

## 10. Approved child issues — one PR each

B1–B11 are stable design identifiers; published GitHub issue numbers are recorded alongside the delivery table. Feature slices use exactly `enhancement` plus listed area labels and `priority:low`. Keep the epic's current priority unless the owner changes it. Documentation prerequisite B5 uses `documentation`, `backend`, and `priority:low`. The final test slice uses `chore` because no `test` type label exists, plus `e2e` and `priority:low`.

Every draft context is: **Part of #233. Design: `docs/design/budget/README.md`, sections identified below.** Common requirements apply in each slice: privacy before projection, typed endpoints, generated schemas when the contract changes, appropriate tests in that PR, and no new cross-module domain dependencies.

| Slice | Title | Depends on | Deliverable |
|---|---|---|---|
| B5 / [#450](https://github.com/piotrkantorowicz/home-system/issues/450) | `docs(budget): publish accepted design and delivery plan` | None | Docs-only prerequisite PR; authoritative spec available on main. |
| B1 / [#446](https://github.com/piotrkantorowicz/home-system/issues/446) | `feat(budget): initialize a household budget` | B5; existing #213, #234 | Runnable Budget API and database, safe initialization. |
| B2 / [#447](https://github.com/piotrkantorowicz/home-system/issues/447) | `feat(budget): manage shared and personal envelopes` | B1 | Authorized envelope lifecycle. |
| B3 / [#448](https://github.com/piotrkantorowicz/home-system/issues/448) | `feat(budget): record expenses with exact stored shares` | B2 | Expense create/list/detail, money rules and retry safety. |
| B4 / [#449](https://github.com/piotrkantorowicz/home-system/issues/449) | `feat(budget): correct and void expenses with history` | B3 | Atomic correction, conflict handling, audit trail. |
| B6 / [#451](https://github.com/piotrkantorowicz/home-system/issues/451) | `feat(budget): add budget setup and envelope screens` | B2 | Budget navigation, setup, envelope UI. |
| B7 / [#452](https://github.com/piotrkantorowicz/home-system/issues/452) | `feat(budget): add expense entry and history screens` | B4, B6 | Complete daily expense journey. |
| B8 / [#453](https://github.com/piotrkantorowicz/home-system/issues/453) | `feat(budget): track monthly envelope limits and spending` | B3, B6 | Monthly totals, targets, overspend feedback. |
| B9 / [#454](https://github.com/piotrkantorowicz/home-system/issues/454) | `feat(budget): show outstanding household settlements` | B3, B6 | Exact balances, suggestions, explanation UI. |
| B10 / [#455](https://github.com/piotrkantorowicz/home-system/issues/455) | `feat(budget): record and void household repayments` | B9 | Persistent payments and updated suggestions. |
| B11 / [#456](https://github.com/piotrkantorowicz/home-system/issues/456) | `test(e2e): cover budget privacy and settlement journeys` | B7, B8, B10 | Final real-stack journeys and release evidence. |

### B1 — initialize a household budget

**Context:** §2, §4 currency, §8 architecture; starts only after B5 documentation merges. **Labels:** `enhancement`, `backend`, `infra`, `priority:low`.

**Scope:** Backend — module projects, dedicated DB/migration, scoped UoW/read context, current-household access helper, GET/POST initialization, shared default envelope in the same transaction. Frontend — none. Infrastructure — host, Compose, real-stack CI and run/stop documentation.

**Out of scope:** envelope administration B2; expense model B3; UI B6.

**Acceptance:** only Owner/Adult can initialize; one Budget and one default envelope survive simultaneous requests; a different requested currency conflicts; unknown/unavailable Household never yields Budget data; existing modules still start and dispatch correctly.

**Likely files:** `src/Modules/Budget/Budget.{Domain,Application,Infrastructure,Api,Contracts,UnitTests,IntegrationTests}/`, `HomeSystem.slnx`, `src/Apis/HomeSystem.REST/`, `infrastructure/`, `.github/workflows/e2e-nightly.yml`, `.agents/skills/{run-project,stop-project}/`, `docs/e2e/README.md`.

**Test plan:** HTTP-to-PostgreSQL initialization/isolation/role tests, concurrent unique-key test, multi-module startup and scoped UoW check. Contracts stays minimal until another module needs a public surface.

### B2 — manage shared and personal envelopes

**Context:** §3, §5. **Labels:** `enhancement`, `backend`, `priority:low`.

**Scope:** Backend — paged visible envelopes, create, rename, archive/restore, immutable owner/visibility, revision conflicts, managed-member access. Frontend — none.

**Out of scope:** expense records B3; limits B8; visibility conversion and account balances F2.

**Acceptance:** private adult envelopes are absent from other adults' results and counts; Child can manage own personal envelope; Guest sees no data; adults manage only currently managed dependents; linking/role changes affect the next request; archived envelopes retain history and reject new entries once B3 exists.

**Likely files:** Budget domain/accounts, Application access and account handlers, Api endpoints, EF configuration/migration.

**Test plan:** domain invariant tests; integration permission matrix across two households; archived/revision conflict tests. Test both collection and direct-ID access.

### B3 — record expenses with exact stored shares

**Context:** §4, §5, §6 retry safety, §8. **Labels:** `enhancement`, `backend`, `priority:low`.

**Scope:** Backend — decimal-string money, category and amount/date/excludeId filters on the existing visible expense list, funding source, payer/recorder separation, stored equal shares for selected adults, create/list/detail, creation revision snapshot and durable request identity. Frontend — none; B7 consumes list filters for duplicate hints.

**Out of scope:** edits/void B4; duplicate-hint UI B7; settlement B9; refunds F3; custom split amounts F4.

**Acceptance:** every expense has a supported category and category filtering returns only matching visible expenses; 100.00 split three ways is exactly conserved; invalid precision, participant sets, and custom share inputs are rejected; household funds give no personal credit; actor cannot be spoofed; archived/inaccessible envelopes cannot receive entries; retries create one expense; mismatched replay payload returns 409; later membership changes do not rewrite shares; ±2-day exact-amount/category filters return only visible active entries and exclude the supplied expense ID; hidden entries never affect counts.

**Likely files:** Budget expense/share value objects and root, commands/queries, persistence mappings, creation revision storage, API request/DTO records.

**Test plan:** table-driven money/split tests and real PostgreSQL concurrent retry tests; API spoofing, privacy, pagination, and date-boundary tests. This is the largest backend slice; keep category management and unrelated abstractions out.

### B4 — correct and void expenses with history

**Context:** §4 corrections, §5 history, §6 replay rules. **Labels:** `enhancement`, `backend`, `priority:low`.

**Scope:** Backend — edit in place, immutable full revision snapshots including shares/names, actor/reason/time, concurrency tokens and durable revision numbers, update/void request identity, archived-envelope history operations. Frontend — none.

**Out of scope:** replacement expense graphs; repayment corrections B10; merchant refunds F3; owner/visibility changes.

**Acceptance:** edit and history commit or roll back together; two fresh edits with one expected revision yield one success and one 409; share-only edits enforce parent concurrency; original recorder remains immutable; authorized history contains previous values and shares; create → edit → retry create returns the creation result without duplication; edit → edit → retry first edit does not revert data; void retry adds no revision; retained historical participants survive departure/demotion without allowing newly introduced ineligible participants.

**Likely files:** Expense domain methods, immutable ExpenseRevision records, update/void commands, revision queries/DTOs, EF concurrency configuration.

**Test plan:** real PostgreSQL rollback, concurrent edits and same-operation races, replay after later edits/voids, actor isolation, exact historical share recovery, and private history access.

### B5 — publish accepted design and delivery plan

**Context:** §1, §10, §13 review amendments; implementation prerequisite. **Labels:** `documentation`, `backend`, `priority:low`.

**Scope:** Documentation — publish the authoritative Budget spec, accepted review disposition, implementation plan, and verified Household README corrections in one docs-only PR. Update epic #233 and child issue bodies to agree with the spec. Backend/Frontend — no implementation. This replaces the original dedicated duplicate-check ticket: its filters move into B3 and its UI into B7.

**Out of scope:** Budget implementation; a second authoritative spec; speculative Household changes; merging without owner review.

**Acceptance:** fresh main checkout contains the approved Budget spec after this PR merges; D1–D11 dispositions are recorded; no competing spec is introduced; all eleven child issues link the current design and correct dependencies; B1 explicitly waits for this documentation PR; stale Household command/contract/provider descriptions match inspected code.

**Likely files:** `docs/design/budget/README.md`, `docs/design/household/README.md`, `docs/superpowers/specs/2026-10-02-budget-design-review.md`, `docs/superpowers/plans/2026-10-02-budget-implementation-plan.md`.

**Test plan:** resolve local Markdown file links, check issue dependency graph and superseded terms, run `git diff --check` and docs-only `scripts/verify.sh --branch`; application tests are not applicable.

### B6 — add budget setup and envelope screens

**Context:** §5, §9. **Labels:** `enhancement`, `frontend`, `priority:low`.

**Scope:** Frontend — module registry, generated API/client, setup, envelope list/create/rename/archive/restore, localized access states and cache boundaries. Backend — only DTO capability gaps discovered while consuming B1/B2.

**Out of scope:** expense forms B7; monthly overview B8; settlement B9.

**Acceptance:** adult can initialize and manage envelopes; Child sees own envelope or adult-setup state; Guest has no Budget entry; privacy description matches server behavior; a failed lookup offers retry; logout or household change clears Budget cache.

**Likely files:** `src/ui/src/modules/budget/`, `src/ui/src/app/main.tsx`, `src/ui/package.json`.

**Test plan:** Vitest/MSW setup/role/error/cache tests; a focused real-stack setup/envelope journey; keyboard and narrow-viewport checks.

### B7 — add expense entry and history screens

**Context:** §4, §6, §9. **Labels:** `enhancement`, `frontend`, `priority:low`.

**Scope:** Frontend — add/detail/history/filter/correct/void flows, exact text input, payer/split preview, duplicate warning and keep-both, attribution. Backend — no new product feature.

**Out of scope:** dashboards B8; settlement screens B9/B10; receipt uploads F4.

**Acceptance:** duplicate hint uses only the filtered expense list and supports explicit save without a hint on lookup failure; no create-time duplicate response state exists; actor and payer are visibly distinct; private purchases stay private; warning can cancel before creation or deliberately keep both; a network retry keeps its request ID; failed submission retains input; correction conflict offers reload; all operations work in English/Polish and on mobile.

**Likely files:** Budget expense pages/forms, queries/mutations/query keys, locales, generated schema.

**Test plan:** form/duplicate/retry/conflict component tests and an add → correct → void browser journey with real API. No frontend reimplementation of settlement math.

### B8 — track monthly envelope limits and spending

**Context:** §7 monthly envelopes, §9 overview. **Labels:** `enhancement`, `backend`, `frontend`, `priority:low`.

**Scope:** Backend — month summary, exact aggregates, monthly-limit CRUD/revision constraints. Frontend — month/scope selector and envelope spent/remaining display.

**Out of scope:** rollover, recurring targets, cash balances and income F2/F4; refunds F3.

**Acceptance:** missing and zero limits differ; overspending never blocks an expense; settlement rows never enter spending; correction affects purchase month; private totals are excluded from Shared; monthly filters handle year and leap-month boundaries.

**Likely files:** Budget MonthlyLimit root/configuration/migration, summary queries/endpoints; Budget overview/envelope UI.

**Test plan:** PostgreSQL aggregate/visibility/concurrent-limit tests; UI overspend/no-limit states; one real monthly-summary journey.

### B9 — show outstanding household settlements

**Context:** §7 settlement. **Labels:** `enhancement`, `backend`, `frontend`, `priority:low`.

**Scope:** Backend — all-recorded balances without date cutoff, pure deterministic suggestion function, per-row historical participant labels, explicit former-adult handling after role demotion. Frontend — shared adult-only settle-up page and explanation.

**Out of scope:** recording payments B10; optimal minimum-transfer solver; monthly debt resets.

**Acceptance:** example in §7 before repayments is reproduced; net sums to zero; applying suggestions brings every balance to zero; private/shared-fund expenses have no effect; future-dated entries count immediately; local-midnight entries cannot disappear behind a UTC cutoff; departed/demoted participants retain balances; children/guests cannot query settlement.

**Likely files:** Budget settlement query/DTO, pure calculation function, settlement page/locales.

**Test plan:** deterministic cases for zero/one/multiple debtors, uneven cents and tie ordering; DB projection and role tests; UI explanation/empty-state tests.

### B10 — record and void household repayments

**Context:** §6 retries, §7 settlement. **Labels:** `enhancement`, `backend`, `frontend`, `priority:low`.

**Scope:** Backend — Settlement root, idempotent record/list/void, attribution and revision. Frontend — record-payment confirmation, history, void and balance refresh.

**Out of scope:** money movement, payment-provider integration, recipient approval, bank reconciliation F1.

**Acceptance:** §7's 25.00 payment leaves 15.00 owed; void restores 40.00; no spending total changes; overpayment creates visible reverse credit; sender/recipient cannot match; unrelated outsiders are rejected; actual payments involving departed/demoted ledger participants can be recorded; unpaid debt stays outstanding with no fake-payment shortcut; creation retries after void still return the original result; retries do not double-record.

**Likely files:** Budget settlement domain/commands/configuration/API; payment dialog/history/queries.

**Test plan:** unit conservation/overpayment checks, integration replay-after-void/actor authorization/void races, real repayment involving a departed or demoted participant, UI confirmation and one partial-payment browser journey.

### B11 — cover budget privacy and settlement journeys

**Context:** §11 completion gate. **Labels:** `chore`, `e2e`, `priority:low`.

**Scope:** Tests — real-stack cross-user journeys, lifecycle checks, shared fixtures/cleanup using existing worker auth. Documentation — record supported scope and deferred limitations. Backend/frontend fixes belong here only if found by these checks and remain small.

**Out of scope:** replacing unit/integration tests required in earlier slices; performance benchmark framework; new product scope.

**Acceptance:** two adults see shared entries; neither sees the other's private entries or hints; correct/void updates spending; partial repayment survives reload; removing/changing a member revokes next-request access; managed-to-linked privacy changes; Budget DB is included in CI start/stop. Other module tests stay green.

**Likely files:** `e2e/budget/`, existing auth/seed helpers as needed, `docs/e2e/README.md`, Budget integration tests for lifecycle assertions better expressed through HTTP.

**Test plan:** focused Playwright journeys against actual API/PostgreSQL/Authentik, independent identities, unique data, explicit cleanup; `scripts/verify.sh --branch` and relevant existing regression suites.

### Delivery lane and sequencing

B5 (#450) lands first as a docs-only PR. Every implementation slice depends transitively on it. Retain eleven independently reviewable issues rather than inventing an eight-slice count containing multi-PR slices. Children target `main`. Each backend slice exposes a complete tested API capability; B6 introduces only completed UI capabilities, and B7 adds expense entry once correction and duplicates work. No navigation links to unfinished screens. This permits independent merges without a long-lived epic branch or speculative feature-flag framework.

Dependencies allow B6 after B2 while B3–B4 are developed, and B8/B9 after their prerequisites. This is a dependency map, not a request to run parallel agents. B1 and B3 carry the greatest uncertainty; reassess their size after detailed implementation planning. Each child remains one PR; split a draft further if it cannot stay reviewable.

## 11. Completion and validation

Epic closes only when all agreed v1 slices are delivered and these checks pass:

- A fresh household can initialize one Budget and record shared or personal expenses.
- Payer and recorder remain distinct through corrections and history.
- Exact shares and all settlement balances conserve money; partial/overpayments and voids behave as specified.
- Private data is absent from unauthorized details, lists, counts, summaries, duplicate responses, and cached UI states.
- Membership/role changes affect the next request; former participants remain in historical calculations.
- Duplicate warnings are optional to heed; retrying one operation creates one financial record.
- Monthly spending/limits agree with active expense rows and exclude repayments.
- Typed OpenAPI contracts generate usable TypeScript money strings; no hand-edited schema.
- Dedicated Budget persistence, local stack, verification, and real-stack E2E paths are wired.
- English/Polish mobile and keyboard flows work; loading, absence, error, and forbidden states are distinct.

Tests belong to the slice that introduces behavior. Prioritize cross-household ID attacks, personal-data inference, concurrent retries/corrections, database rollback, exact rounding, month boundaries, and membership changes over implementation-shaped mock assertions. Use repository-standard xUnit/Shouldly/Testcontainers and Vitest/MSW/Playwright; no new test framework.

For this planning change, review Markdown links and consistency only. No application test pass is claimed: no implementation changed.

## 12. Approved epic body

The following body includes amendments approved on 2026-10-02. Keep child tracking in GitHub sub-issues; do not maintain a second numbered checklist of children in the body.

> **Goal**
>
> Household members can record shared and private expenses, understand monthly envelope spending, and see or record who owes whom, with exact money handling and enforced privacy.
>
> **Design**
>
> `docs/design/budget/README.md` — accepted v1 scope, permissions, monetary rules, and delivery slices. Extends Household design §8 phase 3.
>
> **Scope**
>
> Dedicated Budget module (DDD + EF Core, own PostgreSQL database); one currency per household; household/personal envelopes; payer distinct from recorder; stored equal adult shares among selected participants; individual versus household-fund payments; soft duplicate hints; idempotent writes; auditable correction/void; monthly limits; all-history settlement and recorded repayments; English/Polish UI.
>
> **Done when**
>
> - [ ] Household setup, shared/private entry, correction, void, and envelope management work end to end.
> - [ ] Private data stays private in queries, counts, summaries, duplicate hints, and UI cache.
> - [ ] Spending and settlement conserve exact amounts and survive membership changes.
> - [ ] Duplicate warnings never block legitimate entries; retries never double-record one operation.
> - [ ] Partial repayments update debt without changing spending totals.
> - [ ] Monthly limits warn on overspending without blocking expense entry.
> - [ ] Local infrastructure, generated API types, and unit/integration/E2E checks include Budget.
>
> **Dependencies**
>
> Household #213 and shared EF infrastructure #234 are complete. Children depend on each other as listed in the design.
>
> **Deferred**
>
> Bank/CSV import, cash balances/income/transfers, merchant refunds, recurring entries/rollover, attachments, multi-currency conversion, and private-history portability. Import becomes a separate follow-up epic; it is not required to close this manual-entry release.
>
> **Epic branch**
>
> No — tested child capabilities merge directly to main; UI exposes only delivered flows.

Publication metadata: use exactly one type label, `epic`, removing the extra `enhancement`. Retain existing area/priority labels. Child issues use the existing label catalog; no new `budget` label is required.

## 13. Owner decision log — 2026-09-29

The owner approved the numbered decisions from the original proposal. “Yes” is recorded against that row’s recommended default; the refund question requests an explanation, not an expansion of v1.

| # | Owner response | Recorded decision |
|---|---|---|
| 1 | “an expenses log with limits and ability to categorize” | Expense tracking with categories and optional monthly envelope limits. Envelopes group spending; real cash/bank balances remain outside scope. |
| 2 | “yes, child should see only its own data” | Budget-specific access accepted: children see only their own personal data, shared financial data is adult-only, guests have no access. |
| 3 | “agree” | PLN default; PLN/EUR/USD supported; one immutable currency per Budget. |
| 4 | “yes” | Retain private history after leaving/deletion, but deny access through the household-scoped API; no automatic purge or transfer. F5 remains deferred. |
| 5 | “yes, but how the refunds will work?” | Bank/CSV import and refunds remain outside the first release. Proposed full/partial refund behavior is documented in §7, including shared-debt and month-limit effects. |
| 6 | “yes” | Adults may record repayments for others, with explicit confirmation and immutable recorder attribution. |
| 7 | “ok” | B1–B11 approved for publication as sub-issues, epic body replacement, and Todo scheduling. Children target main. |

No v1 decision remains pending. The 2026-10-02 amendments below supersede conflicting details of the initial plan. F3’s detailed refund behavior is a follow-up proposal to review when refunds are scheduled; it does not block the approved expense-log release. This approval covers planning/publication, not implementation or merging PRs.

### Review amendments accepted — 2026-10-02

The owner accepted the review response with “ok so update tickets and doc.” These decisions supersede the Stage 1 review’s original recommendations where they differ.

| Review ID | Accepted disposition |
|---|---|
| D1 | Remove the separate receipt table. Keep server IDs and actor-scoped request IDs; immutable creation/edit snapshots provide retry identity independent of current mutable state. Retain privacy and concurrent replay tests. |
| D2 | Use filters on the existing visible expense list. Remove preview endpoint, create re-check, and saved-duplicate state. |
| D3 | Settlement includes all recorded active entries, including future-dated entries. Remove `asOf` and implicit today. Monthly reports still use an explicit month. |
| D4 | Keep no parental access to a child-with-login’s private envelope. Managed-member access stays. |
| D5 | Equal splits among a selected adult subset only; custom amounts deferred. |
| D6 | Keep `Individual` / `HouseholdFunds` enum and its payer invariants. |
| D7 | Edit expenses in place with immutable complete revisions, shares included, committed atomically. |
| D8 | Keep former/departed/demoted participants in the ledger; record actual repayments only. Forgiveness remains deferred. |
| D9 | Keep per-row name snapshots; no participant table or write-on-read refresh. |
| D10 | Keep one issue per PR. Reuse #450 for the documentation prerequisite; move duplicate filters/UI to #448/#452. Eleven issues remain. |
| D11 | Publish this spec, Household corrections, review disposition, and linked implementation plan in docs-only PR before Budget implementation. |

Known v1 choices remain explicit: deterministic remainder allocation can accumulate bias; independently linked children's private spending stays private; shared-fund spending is supported; refunds, forgiveness, import, and custom split amounts are deferred. No feature work starts before #450 merges.
