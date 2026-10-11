# Investments module — design and delivery plan

Status: **phase 1 approved 2026-10-11; phases 2 and 3 are outlines that need their own specs**

Epic: [#571 — Investments phase 1, net worth](https://github.com/piotrkantorowicz/home-system/issues/571)

Prepared: 2026-10-10

Related designs: [Household](../household/README.md), [Budget](../budget/README.md) (access model and money conventions reused here)

UI mockups (phase 1): [`phase-1-mockups.html`](phase-1-mockups.html) — open in a browser; example data. Same page published as a private artifact: <https://claude.ai/artifact/RU2c865DHigRitKHyPWWQQ>. Approved screens: Dashboard A, Wallet detail, Monthly check-in. Dashboard B is kept as the phase-2 allocation reference (§10).

## 1. Recommendation

Build a household-scoped Investments module that tracks every wallet the household holds and, in three phases, grows from a net-worth tracker into a portfolio and tax helper.

The first useful experience is: open Investments, record this month's balance for each wallet on one screen, and see total household net worth in PLN and how it changed since 1 January. Another adult's private wallet never appears in your totals, lists, or history.

Use the existing DDD + EF module pattern (Style 1), a dedicated PostgreSQL database, Household Contracts for identity and membership, existing UI primitives, and existing test infrastructure. Market data (NBP FX in phase 1, Stooq quotes in phase 2) lives inside the module. No broker SDK, API sync, new state library, or generic accounting engine.

## 2. Scope by phase

| Phase | Outcome | Detail |
|---|---|---|
| **1 — Net worth** | “How much do we have, where, and is it growing?” | Wallets, manual balance snapshots, NBP FX, net-worth dashboard and history, monthly check-in. Fully specified here (§3–§9). |
| **2 — Portfolio** | “What do I hold, what did it cost, how is it doing?” | Instruments, transactions, holdings, FIFO cost basis, unrealized P/L, Stooq prices, treasury-bond valuation, CSV import per institution. Outline in §10; own spec before implementation. |
| **3 — Tax** | “What do I declare for year X?” | Realized gains, dividends and withholding, PIT-38 / PIT-ZG summary, IKE/IKZE/PPK exclusion, IKZE limit tracker. Outline in §11; own spec before implementation. |

**Asset classes in scope:** stocks/ETFs, Polish treasury bonds, retirement wrappers (IKE, IKZE, PPK), savings/deposits/cash.

**Institutions in use:** XTB, Saxo, Bossa, mBank (eMakler and savings), PKO BP (treasury bonds), Allianz (PPK).

**Out of scope for all phases until requested:** crypto, real estate, broker/bank API sync, notifications, integration events, debt/liabilities. API sync is expected later; phase 2 leaves an `IImportSource` seam for it (§10), and nothing else is built for it now.

## 3. Phase 1 vocabulary

| Concept | Meaning |
|---|---|
| `Wallet` | One account at one institution. Name, institution, kind, currency, owner person, visibility, archived flag. |
| Institution | `XTB`, `Saxo`, `Bossa`, `mBank`, `PKO BP`, `Allianz`, `Other`. Display/grouping only; drives importer selection in phase 2. |
| Kind | `Brokerage`, `IKE`, `IKZE`, `PPK`, `TreasuryBonds`, `Savings`, `Cash`. Drives grouping now and tax treatment in phase 3. |
| `BalanceSnapshot` | A wallet's total value on one date, in the wallet's currency, with recorder and optional note. |
| Net worth | Sum over visible, non-archived wallets of each wallet's latest snapshot on or before the reference date, converted to PLN (§5). |
| Check-in | One screen that records a new balance for any subset of wallets on one date, in one request. |

## 4. Access, ownership, and membership

Resolve `HouseholdId`, actor `PersonId`, and role from the authenticated subject through `IHouseholdQueryService` on every request. Never trust actor fields from a request body. No household → setup-required state. Failed Household lookup → existing error path, no Investments data.

### Ownership and visibility

Every wallet has a **legal owner** (`OwnerPersonId`, required): accounts are always in one person's name, and IKE/IKZE/PPK are individual by law. A joint account picks one owner and is marked `Shared`.

Visibility is separate from ownership:

- `Shared` — visible to every current Owner/Adult of the household.
- `Private` — visible only to the owner, and to Owner/Adult members when the owner is a current managed member.

Owner, kind, and currency are immutable after creation. The owner (or an adult managing a managed owner) may change visibility; privacy is the owner's decision. Changing visibility does not rewrite history; it changes who can read it from the next request.

### Permission matrix

Follows the Budget-specific exception recorded in the Household design: shared financial data is Owner/Adult only.

| Resource/action | Owner / Adult | Child with login | Guest |
|---|---|---|---|
| Shared wallets, their snapshots, household net worth | Read/write | No access | No access |
| Own private wallets | Read/write | Read/write | No access |
| Wallets owned by a current managed member | Read/write | No access | No access |
| Another independent person's private wallet | No access | No access | No access |
| Create wallet | For self or a managed member | For self only | Not allowed |

Owner role does not unlock another adult's private wallet. Unknown role strings deny access.

Apply identical visibility rules before list filtering, counts, detail lookup, net-worth aggregation, history aggregation, and check-in validation. A hidden wallet's name, existence, value, or count must not leak through any response, including the check-in batch result. Invisible or outside-household IDs → 404. Visible resource with a disallowed action → 403.

### Scopes

- **Household** — all shared wallets plus the caller's own private wallets (and managed members' wallets). Labelled “Household net worth”.
- **Mine** — wallets the caller owns (plus managed members' wallets when explicitly filtered). Labelled “My net worth”.

Two adults therefore see different “Household” totals when either holds private wallets. That is intended; the UI never claims the household total is identical for everyone.

### Membership changes

Authorize from current membership on every request; no cached roster. A departed member's wallets drop out of household views and remain in the database, inaccessible through the household-scoped API. Remaining adults do not inherit them. Rejoining restores access under current rules. A member demoted to Child keeps their own wallets and loses shared ones; demoted to Guest loses all access. Linking a managed person to a real account preserves `PersonId`; delegated adult access ends when `IsManaged` becomes false. Household deletion: rows retained, inaccessible, no cascade — same as Budget §5.

## 5. Money, dates, and FX

### Money

Represent amounts with a module-local value object backed by `decimal`. Store balances as `numeric(18,2)`, FX rates as `numeric(18,6)`. API money fields are decimal strings (`"12450.10"`), documented as strings in OpenAPI. At most two fractional digits, no exponent, maximum `9999999999.99`. Reject excess precision; never round silently. Balances may be zero, never negative (no liabilities in scope).

Wallet currencies: `PLN`, `EUR`, `USD`. Reporting currency: PLN, fixed in phase 1 (configurable later: [#588](https://github.com/piotrkantorowicz/home-system/issues/588)). Adding a currency is a code change to the allowed list plus the FX fetch list.

The browser keeps input as text, accepts the active locale's decimal separator, and normalizes to the API format. The server computes all totals and conversions.

### Dates

`AsOf` is a `DateOnly` (`YYYY-MM-DD`). Future dates are rejected. Timestamps (`RecordedAt`, `UpdatedAt`) come from `TimeProvider` as UTC instants. The UI defaults the date to the user's local today.

### FX conversion

- Source: NBP table A mid rates (`https://api.nbp.pl/api/exchangerates/rates/a/{code}/{from}/{to}/`).
- Rate for a date D: the most recent published rate with `EffectiveDate <= D`. Weekends and holidays fall back to the prior business day.
- **Which D:** current figures use today's rate, i.e. the newest stored rate. That covers the wallet list, wallet detail, every row of the snapshot history, and `GET /net-worth` even with a past `asOf`. Only the month-end history chart values each point at that month end's rate, so it shows FX moves over time.
- PLN wallets need no rate.
- Responses that convert foreign amounts include the rate, its effective date, and the NBP table number used.
- If no applicable rate exists for a needed currency, that wallet is returned with `rateMissing: true`, contributes nothing to the PLN total, and the response sets a top-level `incomplete: true`. Never guess or reuse a later rate.

Phase 3 needs D-1 rates for tax; storing every published rate makes that a query, not new infrastructure.

## 6. Architecture

### Module layout

Standard Style-1 module: `Investments.Domain`, `.Application`, `.Contracts`, `.Infrastructure`, `.Api`, `.UnitTests`, `.IntegrationTests` under `src/Modules/Investments/`. Own database `investments`, Compose profile `investments`, host port `5436`. Own migrations, connection string, host registration, and E2E stack wiring, mirroring Budget §8.

`Investments.Contracts` stays empty in phase 1. No integration events are published (no consumer). Contracts must never expose private wallet data speculatively.

### Aggregates and tables

| Root/table | Responsibility |
|---|---|
| `Wallet` / `wallets` | Household, owner, owner name snapshot, name, institution, kind, currency, visibility, archived flag, `xmin` concurrency token. |
| `BalanceSnapshot` / `balance_snapshots` | Wallet, household (denormalized, composite FK), `AsOf`, amount, note, recorder + name snapshot, recorded/updated timestamps. Unique `(wallet_id, as_of)`. |
| `FxRate` / `fx_rates` | Currency, effective date, mid rate, NBP table number. Unique `(currency, effective_date)`. Reference data, not household-scoped. |

Snapshots are a separate aggregate so a wallet never loads its history. Typed IDs, `Guid.CreateVersion7()`, private setters, named methods (`Rename`, `ChangeVisibility`, `Archive`, `Restore`, `BalanceSnapshot.Correct`), `TimeProvider`, `ct` everywhere.

Database constraints: non-negative amounts, unique keys above, enum check constraints, composite FK `(wallet_id, household_id)` so a snapshot can never point at another household's wallet.

Indexes: wallets by `(household_id, owner_person_id)`; snapshots by `(wallet_id, as_of desc)`; FX by `(currency, effective_date desc)`.

### Writes

- Snapshot write is an **upsert keyed by `(wallet, AsOf)`**: same date replaces amount and note, updates `UpdatedAt`. This makes retries idempotent without a request-ID table.
- Snapshots on archived wallets: corrections and deletes allowed, new dates rejected (matches Budget's archive semantics).
- Check-in batch: one transaction; every entry validated (visibility, archive state, precision, date) before any write. Any invalid entry → 422 listing the failing entries by the wallet IDs the caller sent; nothing is saved.
- Wallet edits use the `xmin` token; stale edit → typed 409.

### Reads

Read DbContext with `AsNoTracking()` + projections. Net worth and month-end history are computed on read: for each visible wallet, latest snapshot `<= D` joined to the applicable rate. History returns one point per month end in `[from, to]`, plus `asOf` for the current month. Expected size (≈10 wallets × ≈120 months) makes this trivial; materialize only on measured need.

### NBP FX worker

- `NbpFxWorker : BackgroundService`, following `RetryDeliveryWorker` / `DietReminderTickService`. Runs daily after 12:30 Europe/Warsaw (NBP publishes table A ~11:45–12:15). Fetches the gap from the last stored date to today for each non-PLN currency, upserts.
- On-demand backfill: when a snapshot or check-in introduces a foreign-currency date earlier than the oldest stored rate, the command fetches the missing range (≤367 days per NBP request; loop if longer). Failure does not fail the write; the read side shows `rateMissing` until the worker or a retry fills it.
- `INbpClient`: typed `HttpClient`, one implementation. Exists so integration tests can fake NBP. 10-second timeout, no retry policy: the next worker run is the retry.

## 7. API (`/api/investments`)

All routes require authentication; handlers enforce access. Typed results, operation names, ProblemDetails, generated OpenAPI types (`npm run generate:api:investments`).

| Route | Purpose |
|---|---|
| `GET /wallets?includeArchived=` | Visible wallets with latest snapshot, its PLN value, staleness, permitted actions. |
| `POST /wallets` | Create. Body: name, institution, kind, currency, ownerPersonId (self or managed member), visibility. |
| `PUT /wallets/{id}` | Rename and/or change visibility; expected concurrency token. |
| `POST /wallets/{id}/archive`, `POST /wallets/{id}/restore` | Archive/restore. |
| `GET /wallets/{id}` | Detail incl. latest balance and conversion. |
| `GET /wallets/{id}/snapshots` | Paged history, newest first, each with PLN value and rate used. |
| `PUT /wallets/{id}/snapshots/{asOf}` | Upsert one snapshot. |
| `DELETE /wallets/{id}/snapshots/{asOf}` | Remove one snapshot. |
| `PUT /snapshots` | Check-in batch: `{ asOf, entries: [{ walletId, amount, note? }] }`. All or nothing. |
| `GET /net-worth?asOf=&scope=household\|mine` | Total PLN, breakdown by kind, by owner, by wallet; `incomplete` flag; rates used. |
| `GET /net-worth/history?from=&to=&scope=` | Month-end series in PLN; each point carries `incomplete`. Range capped at 10 years. |

Status codes: 400 invalid input, 403 disallowed action on a visible resource, 404 invisible/unknown, 409 stale concurrency token, 422 domain violation (future date, archived wallet, precision). List endpoints use `PagedList<T>`, page size cap 100, stable ordering.

## 8. UI

Module `src/ui/src/modules/investments/`, registered in the module registry with routes `/investments`, `/investments/wallets/:id`, `/investments/check-in`. English and Polish strings; a single active language per the redesign rule. Built on existing redesign tokens and primitives. Reference: [`phase-1-mockups.html`](phase-1-mockups.html). The mockup shows layout and behaviour; production code uses existing UI primitives and tokens rather than copying its CSS.

- **Dashboard** (mockup “Dashboard A”, approved). Household/Mine scope toggle; net worth total; change since 1 January (amount and percent); 12-month line chart; breakdown by kind (stacked bar + legend); wallets table grouped by kind, showing owner, private chip, native and PLN values, last balance date. Primary action “Update balances” → check-in.
- **Stale flag:** wallet whose latest snapshot is older than 45 days shows “Updated N days ago” in warning style. Constant in code; not configurable.
- **Incomplete flag:** when `incomplete` is true, the total shows a warning naming the wallets without a rate.
- **Wallet detail:** native-currency balance first, PLN second with the NBP rate and table number; inline “Record balance” form (date, amount, note; same date replaces); history table with change vs previous and PLN at today's rate; Edit / Archive.
- **Monthly check-in** (approved for phase 1): one date for the batch; one row per active visible wallet with last balance and date; empty input means “skip”; live change per row; Save button labelled with the count; single batch request.
- **Add/edit wallet:** form with name, institution, kind, currency, owner (self or managed members), visibility. Owner, kind, and currency read-only after creation.
- Empty state: no wallets → explains what a wallet is and offers “Add wallet”.

## 9. Testing

Per `docs/rules/backend-testing-standards.md` and `docs/rules/frontend-testing.md`.

- **Unit (domain/application):** wallet creation rules (owner allowed for actor, kind/currency immutability); visibility change permissions; snapshot validation (precision, non-negative, no future date, archived wallet); net-worth calculation (latest snapshot ≤ D, rate fallback to prior business day, missing rate → incomplete, archived wallets excluded); month-end series boundaries.
- **Integration (Testcontainers):** full permission matrix per role; private wallet absent from list, detail (404), net worth, history, and check-in; snapshot upsert idempotency; check-in atomicity (one invalid entry → nothing saved); composite FK rejects cross-household snapshot; concurrency 409; FX backfill and missing-rate behaviour with a fake `INbpClient`.
- **Vitest:** PL/EN decimal input normalization; stale chip threshold; check-in row change computation and Save button count; incomplete warning rendering.
- **E2E (Playwright):** create wallet → record balance → check-in two wallets → dashboard total and history reflect them. Second user does not see the first user's private wallet.

## 10. Phase 2 outline — Portfolio (needs own spec)

- `Instrument` reference data: ticker, ISIN, exchange, currency, Stooq symbol, manual-price flag.
- `Transaction` per wallet: `Buy`, `Sell`, `Dividend`, `Interest`, `Fee`, `Tax`, `Deposit`, `Withdrawal`, `FxConversion`. Corrections append revisions (Budget §4 pattern).
- Wallet `ValuationMode`: `Snapshot` (phase 1 behaviour) or `Transactions`. Switching is one-way, from an opening position, and keeps phase-1 history valid. Net worth uses derived value for transaction-mode wallets.
- Holdings, FIFO lots, cost basis, unrealized P/L computed from transactions; never stored as source of truth.
- `StooqQuoteWorker` daily; manual price override per instrument when Stooq lacks the symbol.
- Treasury-bond valuation engine (EDO, COI, ROR and other series): per-series terms (first-period rate, margin over CPI or reference rate, capitalisation, early-redemption fee); CPI entered manually or fetched from GUS. Pure function, exhaustive unit tests.
- CSV import: upload → preview → map → confirm; stable source-row identity makes re-import idempotent; one PR per institution, ordered XTB, mBank, Bossa, Saxo, PKO BP bonds. Allianz PPK stays snapshot-only unless a usable export exists. Parsers implement `IImportSource` so a future API sync produces the same source rows.
- Dashboard B (allocation first) from the mockup becomes useful here as an allocation tab.

## 11. Phase 3 outline — Tax (needs own spec)

- Realized gains per tax year: FIFO matching, each leg in PLN at the NBP rate from the business day before the transaction, fees as costs.
- Dividends and interest: gross, foreign withholding, Polish top-up to 19%.
- PIT-38 and PIT-ZG (per country) summaries. IKE, IKZE, and PPK wallets excluded automatically by kind.
- IKZE contribution tracker against the yearly limit; the limit is entered per year, not hard-coded.
- Read-only report; no e-filing.

## 12. Phase 1 delivery — one PR each

Delivered through the epic lane `epic/571-investments-phase-1-net-worth`. This document and the mockup ship first in #572.

| # | Slice | Acceptance |
|---|---|---|
| I1 ([#573](https://github.com/piotrkantorowicz/home-system/issues/573)) | Module skeleton | Projects, solution entries, DB + Compose profile `investments` on port 5436, initial migration, host registration, auth-protected empty route group, E2E stack starts the profile, verify script covers the module, `generate:api:investments` npm script added. |
| I2 ([#574](https://github.com/piotrkantorowicz/home-system/issues/574)) | Wallets | Create/list/detail/rename/visibility/archive/restore; permission matrix enforced and integration-tested; UI wallet list, add/edit form, empty state. |
| I3 ([#575](https://github.com/piotrkantorowicz/home-system/issues/575)) | Balance snapshots | Upsert/delete by date, paged history; wallet detail page with record form and history (PLN column may show “—” until I4). |
| I4 ([#576](https://github.com/piotrkantorowicz/home-system/issues/576)) | NBP FX | `fx_rates`, `INbpClient`, `NbpFxWorker`, on-demand backfill, rate selection rule, `rateMissing`; PLN values appear in detail and list. |
| I5 ([#577](https://github.com/piotrkantorowicz/home-system/issues/577)) | Net worth dashboard | `GET /net-worth` and `/history`; dashboard with scope toggle, total, change since 1 Jan, chart, kind breakdown, wallets table, stale and incomplete flags. |
| I6 ([#578](https://github.com/piotrkantorowicz/home-system/issues/578)) | Monthly check-in | `PUT /snapshots` all-or-nothing; check-in screen; E2E happy path. |

## 13. Decisions

| Date | Decision |
|---|---|
| 2026-10-10 | Scope: net worth → portfolio → tax, phased; household-scoped like Budget. |
| 2026-10-10 | Data entry: manual + CSV import; API sync deferred, seam only. |
| 2026-10-10 | Assets: stocks/ETFs, treasury bonds, IKE/IKZE/PPK, savings/cash. Crypto and real estate out. |
| 2026-10-10 | Prices: Stooq daily quotes + NBP FX, manual override per instrument. |
| 2026-10-10 | Architecture: new `Investments` module, DDD + EF, market data inside the module. |
| 2026-10-10 | Every wallet has a legal owner; visibility Shared/Private is separate and owner-changeable. No workspace/initialize step; reporting currency PLN fixed. |
| 2026-10-10 | UI: Dashboard A (timeline first); monthly check-in included in phase 1. |
| 2026-10-11 | PLN values use today's NBP rate everywhere except the month-end history chart (month-end rates). Configurable reporting currency deferred to #588. |
