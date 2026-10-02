# Budget + Household design — stage 1 review

Status: **review concluded — response amendments accepted 2026-10-02**.
Original findings and recommendations below are retained as review history; they
are not all accepted. The accepted disposition is in §8 and the authoritative
[Budget decision log](../../design/budget/README.md#review-amendments-accepted--2026-10-02).
Date: 2026-10-02.
Reviewed: `docs/design/household/README.md` (incl. uncommitted edit), `docs/design/budget/README.md`
(untracked), issues #233 and #446–#456, and the code they reference
(`Household.Contracts`, `HouseholdClaimsTransformation`, `HouseholdRosterProvider`,
`infrastructure/docker-compose.yml`).

Scope note: the Household design is delivered (epics #213, #228 closed). The only open work
is phase 3, Budget (#233). So this review targets Budget and the Household seams it relies on.

---

## 1. Verdict

The Budget design is thorough and mostly sound: money rules, privacy-before-projection,
exact-share conservation, and the fail-closed Household lookup are right. The problem is
**size, not correctness**. It specifies a small accounting product (stored shares,
funding sources, void-and-replace history, as-of settlement, receipts table, preview
endpoint, 11 PRs) for a home with roughly two adults. Several pieces can be dropped or
replaced by something that already exists, with no loss of the stated goal.

There are also **two real defects** (§2.1, §2.2), **one process blocker** (§2.3), and a
handful of **unspecified edge cases** (§3).

---

## 2. Defects and blockers

### 2.1 "Today" is undefined on the server → settlement can silently drop an expense (bug-in-spec)

§7: settlement "default is all entries through today… Future-dated entries are excluded
until that date." The server has only `TimeProvider` (UTC). In Poland (UTC+1/+2), between
00:00 and 01:00/02:00 local time, an expense the user dated *local today* is *tomorrow in
UTC* → "future" → excluded from balances and from the default summary. The user adds a
purchase and does not see it in "who owes whom". #414 (per-user time zone, open) shows the
repo already knows about this class of problem.

Fix options: (a) drop `asOf` and the future-date exclusion entirely (recommended — see
§4.3); (b) make `asOf` a required client-supplied parameter, never defaulted by the server.

### 2.2 Duplicate-check preview is a second query surface that must replicate every visibility rule

§6 adds `POST /expenses/duplicate-check` **and** a re-check inside create **and** a
"Saved; possible duplicate found" state. Each is another place where the §5 rule ("hidden
account's existence, amount, count, or duplicate status must not escape") can regress,
and B5 is a whole PR for it. The match key (household + currency + exact amount +
category + ±2 days) is expressible as filters on the `GET /expenses` list that B3 already
has to build with correct visibility. See §4.2.

(Rated "defect" because it multiplies the privacy test surface, not because the logic is wrong.)

### 2.3 Issues point at a document that is not on `main`

`docs/design/budget/` is **untracked**, and the Household README edit is uncommitted. All
11 child issues cite `docs/design/budget/README.md §n`; the issue bodies themselves say
"the full design is mirrored in epic #233 while the repository document awaits commit".
Any agent following `/start-issue` from a fresh `origin/main` will not find the design.
**Commit (docs-only PR) before any implementation starts.** Whatever stage 2 changes
should land in the same PR so issues and doc agree.

---

## 3. Gaps (behaviour the design does not define)

| # | Gap | Why it matters | Suggested resolution |
|---|---|---|---|
| G1 | **Role demotion with open balance.** An Adult with an outstanding balance is changed to Child/Guest. §7 settlement is Owner/Adult-only, so that person's debt/credit becomes invisible to them and unsettleable by them. §5 only covers *removal*. | Silent orphan balance. | Define: demoted person stays in the ledger; adults can still record repayments on their behalf; balance shown to adults as "former adult". Or block demotion with non-zero balance (needs a Contracts call — too heavy). Recommend the former. |
| G2 | **Departed member with non-zero balance.** Remaining adults can only record a "repayment on behalf of" — a fake payment — to clear it. No write-off. | Ledger can never reach zero; suggestions keep proposing a transfer to someone gone. | Either accept the "record on behalf" workaround and say so, or add a single `Forgive` settlement kind (same table, same void). Recommend documenting the workaround for v1. |
| G3 | **Former-member display names.** `HouseholdContext` returns *current* members only; there is no Contracts call to resolve a removed person's name. Design says "store display-name snapshots" but not where. | Without it, history shows GUIDs. | Per-row snapshot on share/settlement rows (as designed, denormalised) **or** one `budget_participants(person_id, display_name, last_seen)` table refreshed on each request. The second is smaller and avoids stale-name drift. |
| G4 | **Parent visibility of a Child-with-login's money.** Table §5: Owner/Adult have *no* access to a Child's personal envelope unless the child is managed. Household README says a Child can have personal resources "managed *for* them by an adult". | A parent cannot see a teenager's allowance spending, which is the usual reason to give a child a personal envelope. May be intended, but it is not stated as a decision. | Decide explicitly (see §5, D4). |
| G5 | **Household deletion with open debts.** Owner can delete the household at any time; Budget rows are retained but unreachable. Nothing warns about outstanding settlement. | Data-loss-looking behaviour; no purge path (F5 deferred). | Accept for pre-release; add a line to the Household delete confirmation copy only if cheap. Otherwise leave to F5. |
| G6 | **Equal-split remainder is biased.** Leftover cents go in canonical `PersonId` order; IDs are `Guid.CreateVersion7()` (time-ordered), so the longest-standing member always gets the extra cent. | Cosmetic (±1 cent) but systematic. | Order by `hash(expenseId ‖ personId)` or rotate by expense id. Keep deterministic for tests. |
| G7 | **Per-request double lookup.** `HouseholdClaimsTransformation` already resolves the household on every request; Budget's helper resolves it again (correctly — claims can be absent). Two cross-DB queries per call. | Fine at this scale; only a problem if the helper is not request-scoped. | Keep helper scoped and memoised per request (the design says "one scoped helper" — add "memoised"). |
| G8 | **B6 exposes Budget navigation before expenses exist.** "UI exposes only delivered flows", but after B6 a user sees a Budget item whose only screen is envelopes. | Dead-end screen on `main`. | Acceptable pre-release; otherwise hide the nav item until B7 via a one-line module flag. |
| G9 | **Idempotency has no precedent in the repo.** No existing `ClientRequestId`/receipt pattern (verified by grep; only generated schema files mention the word). | B3 invents infra: receipts table, payload fingerprint, actor scoping, fresh-context replay on unique violation. | See §4.1. |
| G10 | **Roster cache of `Role` strings.** Contracts return roles as strings (`"Owner"`, `"Adult"`). Budget access helper will switch on strings. | Typo-class bugs. | Parse once into a Budget-local enum in the helper; unknown role ⇒ deny. |

---

## 4. Better solutions (each is a proposal to accept or reject)

### 4.1 Client-generated resource ID instead of an `operation_receipts` table
Let the client generate the expense/settlement/replacement `Guid` and send it
(`PUT /expenses/{id}` or `POST` with `id`). The primary key *is* the idempotency key:

- replay with same id + same payload → return stored row (compare stored fields; no fingerprint column);
- same id, different payload → 409;
- unique violation race → catch, re-read in a fresh scope (same as designed).

Removes: `operation_receipts` table, actor/operation scoping, fingerprint hashing, and a
whole class of "key belongs to another actor" tests. Leaves one real question: cross-household
id collision. Use composite key `(budget_id, id)` and treat a collision as 409 with no data.
Void is naturally idempotent. **Saves roughly a third of B3's risk.**

### 4.2 Duplicate hint = a filtered list query
`GET /expenses?amount=&category=&from=&to=&excludeId=` (needed anyway for history
filters) already enforces visibility. The form calls it with ±2 days and shows up to five
rows. Drop: the preview endpoint, the create-time re-check, and the "saved; possible
duplicate" state. B5 shrinks from a backend PR to an `amount` filter plus UI in B7. The
race ("two people add the same purchase simultaneously") is already documented as
acceptable.

### 4.3 Drop `asOf` and the future-date exclusion
Settlement = all active shared expenses and repayments, period. A future-dated purchase
counts now. Removes §2.1, the "Outstanding through [date]" label, the as-of parameter,
and a test dimension. If a planned purchase should not count yet, the user does not
enter it yet.

### 4.4 Simplify funding/split options for v1
Keep the exact stored-share rule (it is the part that makes settlement trustworthy).
Candidates to cut or defer, ordered by saving:

1. **Custom per-person amounts** — keep *equal among a chosen subset*; add custom amounts
   when someone asks. Removes the "sums exactly" validation matrix and the custom-split UI.
2. **`HouseholdFunds` funding source** — equals "shared expense with no settlement
   effect". Could be a boolean `Settle: true/false`. Same behaviour, no new concept.
3. **Personal envelopes for managed members** — adult-on-behalf access adds a
   `IsManaged` re-check path with its own lifecycle test (linking). Keep only if
   children's allowance is a real near-term use.
4. **Archive/restore** for envelopes — keep archive; `restore` is rarely needed.

### 4.5 Correction without a replacement graph
"Atomic void + linked replacement + reason + revision" is the heaviest domain piece.
Cheaper equivalent: **edit in place with an append-only `expense_revisions` row** (old
values + actor + reason + timestamp), `xmin` concurrency (already used in Household).
Shares stay stored on the live row. Gives the same audit trail and the same 409; drops
"two simultaneous corrections produce one successor" and replacement-link queries.
Trade-off: loses the "original row remains a first-class expense" property. State which
you value.

### 4.6 Re-slice the PRs
After 4.1–4.3 the backend is thinner. Proposed slices (11 → 8):

| New | Replaces | Content |
|---|---|---|
| S1 | B1 | module, DB, Compose profile, init |
| S2 | B2 | envelopes |
| S3 | B3 + B5 | expenses, shares, list filters (incl. amount/date for duplicate hint) |
| S4 | B4 | correction / void |
| S5 | B8 + B9 backend | summary, limits, settlement read |
| S6 | B6 + B7 + UI of S5 | one UI epic split by screen, not by backend feature |
| S7 | B10 | repayments |
| S8 | B11 | e2e |

Rationale: B6/B7 are blocked behind backend slices and B9 mixes backend + frontend in one
PR, which contradicts "one PR each ≈ reviewable". Treat S6 as 2–3 PRs, not one.

### 4.7 Where the design is right and should be kept
- Money as `numeric(18,2)`, decimal *strings* on the wire, reject excess precision.
- Visibility applied before filter/count/aggregate/duplicate (now just filter/count/aggregate).
- Never trust request body for actor/household; always resolve via Contracts; fail closed
  (matches `HouseholdRosterProvider`; correctly avoids the claims transformation, which
  swallows lookup errors by design).
- Settlement derived, repayments persisted; month filter never restricts debt.
- No cross-database transaction; accept the documented revocation race.
- Own database/port: compose uses 5432/5433/5434, so Budget takes **5435**; add to the
  compose `profiles`, `docs/e2e/README.md`, `.github/workflows/e2e-nightly.yml`, and the
  run/stop skills (B1 already lists these).

---

## 5. Decisions needed before stage 2

| ID | Decision | Options | Recommendation |
|---|---|---|---|
| D1 | Idempotency mechanism | (a) receipts table as designed (b) client-generated resource id | **(b)** |
| D2 | Duplicate hint | (a) preview endpoint + create re-check (b) filtered list query | **(b)** |
| D3 | Settlement date handling | (a) `asOf` as designed (b) none; all active entries | **(b)** |
| D4 | Parent visibility of a Child-with-login's personal envelope | (a) none (as designed) (b) Owner/Adult read-only | owner's call; default **(a)** |
| D5 | Split options in v1 | (a) equal + subset + custom (b) equal + subset | **(b)** |
| D6 | Funding source | (a) `Individual`/`HouseholdFunds` enum (b) `Settle` boolean | **(b)** |
| D7 | Correction model | (a) void + linked replacement (b) in-place + revision log | **(b)** unless "original stays an expense" matters |
| D8 | Orphan balances (G1, G2) | document workaround vs add `Forgive` kind | document workaround |
| D9 | Former-member names (G3) | per-row snapshot vs `budget_participants` | `budget_participants` |
| D10 | PR slicing | keep B1–B11 vs the 8-slice proposal in §4.6 | 8 slices (needs issue edits) |
| D11 | Before implementation | commit both design docs (§2.3) in a docs-only PR | yes |

Historical proposal only: defaults in this table were superseded by the accepted response in §8.

---

## 6. Household README — stale items (fix with the D11 docs PR)

1. Header: `Last updated: 2026-09-26`, but contains 2026-09-29 content.
2. §5 command list omits `AcceptInvitation`, `DeclineInvitation`, `SyncCurrentPerson`
   (all exist under `Household.Application/Commands/`) while §5 step 3 describes them.
3. §5 `IHouseholdQueryService` shows one method; code also has
   `GetAuthSubjectForPersonAsync(personId)`.
4. §8 phase 1: `#214 … (this PR)` is stale; `#221` is listed as "backfill one Person +
   Household" but §6 says #221 shipped nothing.
5. §6 table row *Budget / expenses (future)* says only "Household"; the Budget design
   narrows it: Owner/Adult for shared, personal envelopes per owner, none for Guest. Say so.
6. §4 decision 1 / §9 (multi-household): add one line that Budget keys rows by
   `HouseholdId`, so it stays compatible, but its access helper depends on the
   single-context `GetHouseholdContextForUserAsync` and would need to change with it.
7. §7 `useHousehold()` return shape should be checked against the module export before
   Budget UI relies on it (not verified in this review).

---

## 7. Next step

Answer D1–D11 (a short reply like "all recommended, except D4=b" is enough). Stage 2 then
produces, under `docs/superpowers/`:

- `specs/2026-10-0x-budget-spec.md` — the revised, authoritative spec (replaces or
  amends `docs/design/budget/README.md`), and
- `plans/2026-10-0x-budget-plan.md` — per-slice implementation plan (files, tests,
  order, verification commands), plus the exact edits needed on #233 and #446–#456.

## 8. Accepted response and implementation handoff — 2026-10-02

The owner accepted the review response, not “all recommended.” The authoritative spec remains `docs/design/budget/README.md`; no second Budget spec is introduced. Implementation details live in [the linked plan](../plans/2026-10-02-budget-implementation-plan.md).

| Decision | Accepted result |
|---|---|
| D1 | Separate receipt table removed; server IDs retained. Expense revision snapshots and immutable settlement creation data carry actor-scoped operation identities. Replay is evaluated against the original request, not an edited live row. |
| D2 | Existing visible expense-list filters provide the only duplicate hint. No preview endpoint/create re-check. |
| D3 | All active recorded entries count, including future dates; no as-of filter or server today. |
| D4 | Keep (a), existing private child-with-login policy; managed-member support retained. |
| D5 | (b), equal among chosen adults only. |
| D6 | (a), meaningful funding-source enum retained. |
| D7 | (b), atomic in-place edits plus complete immutable revision history, including shares. |
| D8 | Historical participants remain visible to adults after departure/demotion. Actual repayments only; no fake-payment workaround. Forgiveness deferred. |
| D9 | Per-row name snapshots retained and their fields specified. No participant table. |
| D10 | One issue per PR; eleven issues retained. #450 becomes docs prerequisite; duplicate backend/UI work moves into #448/#452. |
| D11 | Docs-only PR first; implementation begins after #450 merges. |

Corrections to review claims: §2.1 identifies a missing date policy, not an implemented bug; explicit-month summaries were already defined. §2.2 identifies extra surface area, not evidence of a leak. Historical shares/participants and name snapshots already existed in the spec; the amendment makes demotion and snapshot storage explicit. The proposed “eight slices” included 2–3 PRs in S6 and was therefore not eight PRs. Memoizing only Budget’s own helper would not remove the separate claims-transformation lookup; no cross-module cache is added for that speculative optimization. Stable UUID ordering has cumulative remainder bias; it remains a documented v1 trade-off.

The original sections above must not be used to override the accepted decision log or issue bodies.
