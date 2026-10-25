# Budget — bank statement import

Status: **approved 2026-10-25; bank profiles verified per fixture in #670**

Epic: [#668 — import expenses from bank statements](https://github.com/piotrkantorowicz/home-system/issues/668)

Prepared: 2026-10-25 (#669)

Parent design: [Budget design](README.md), deferred follow-up F1 (§3). Everything in that document still applies unless this one says otherwise: money rules (§4), access (§5), duplicate hints and request identity (§6), API conventions (§8).

## 1. Summary

The user uploads a statement file exported from their bank. The server parses it in memory and returns a preview. Each outgoing transaction becomes a candidate row, showing a suggested category and its duplicate status. The user reviews the rows and confirms the ones to keep. All confirmed rows then become ordinary expenses in a single transaction. Each imported expense keeps a hashed source key, so importing the same or an overlapping statement again adds nothing. The file itself is never stored.

Slices:

| Slice | Issue | Deliverable |
|---|---|---|
| BI1 | #669 | This document |
| BI2 | #670 | Parsers for the bank profiles in §2 |
| BI3 | #671 | `POST /api/budget/imports/preview` |
| BI4 | #672 | `POST /api/budget/imports` |
| BI5 | #673 | Category suggestions from earlier imports |
| BI6 | #674 | Import screen with row review |
| BI7 | #675 | E2E journey |
| BI8 | #676 | MCP tools and the in-app assistant |

## 2. Formats

The household uses **PKO BP, mBank, ING Bank Śląski and Revolut** (owner, 2026-10-25). None of these banks offers ISO 20022 CAMT.053 on personal accounts (PKO offers it only in iPKO biznes). All four export CSV. v1 therefore ships **CSV with one column profile per bank**.

- **CAMT.053 is deferred.** Add it as one more profile when a bank in use exports it. The parser contract in §2.2 already fits it.
- **MT940 is not supported.** Each bank above has a CSV alternative.

### 2.1 Bank profiles

The column names below come from each bank's current export documentation. The `mbank` row was checked against a real export from the owner on 2026-10-25; its encoding and delimiter are confirmed from the raw file in #670. The other rows are **unverified until the anonymised fixture for that bank lands in #670**. If a fixture disagrees with this table, the fixture wins and #670 updates the table.

| Profile | Encoding | Delimiter | Header detection | Date | Amount | Bank reference |
|---|---|---|---|---|---|---|
| `pko-bp` | Windows-1250 | `,` (quoted) | `"Data operacji","Data waluty","Typ transakcji","Kwota","Waluta"…` | `YYYY-MM-DD` | `-12.34`, sign carries direction | None → fingerprint |
| `mbank` | Windows-1250 | `;` | "Elektroniczne zestawienie operacji": ~37 preamble lines (`#Klient`, `#Za okres:`, `#Waluta`, `#Numer rachunku`, turnover summary, opening balance), then `#Data księgowania;#Data operacji;#Opis operacji;#Tytuł;#Nadawca/Odbiorca;#Numer konta;#Kwota;#Saldo po operacji;` | `YYYY-MM-DD`; period in preamble as `DD.MM.YYYY` | `-5 682,80`: comma decimal, space thousands separator, no currency (taken from `#Waluta`) | None → fingerprint |
| `ing` | Windows-1250 | `;` | Preamble lines, then `"Data transakcji";"Data księgowania";"Dane kontrahenta";"Tytuł";…;"Nr transakcji";"Kwota transakcji (waluta rachunku)";"Waluta"…` | `YYYY-MM-DD` | `-12,34`, comma decimal | `Nr transakcji` |
| `revolut` | UTF-8 | `,` | `Type,Product,Started Date,Completed Date,Description,Amount,Fee,Currency,State,Balance` | `YYYY-MM-DD HH:mm:ss` | `-12.34`; spent = `-(Amount) + Fee` | None → fingerprint |

Profile detection is automatic. The parser looks for the header row of each profile within the first 60 lines. The user never picks a bank. No match, or more than one match, returns 422 "Unrecognised statement format". The server never guesses.

Encoding: try UTF-8 strictly first, with a BOM allowed. On invalid UTF-8, fall back to the profile's declared encoding. Revolut is UTF-8 only.

### 2.2 Parser output (BI2)

Pure application code with no I/O beyond the input stream. For each data row it produces either a transaction or an issue:

- **Transaction:** profile, bank account identifier (from the statement header or the account column when present), booking date, signed amount in account currency, currency, counterparty, title/description, bank reference (nullable), bank-side type (e.g. `Typ transakcji`, Revolut `Type`), state, and the source line number.
- **Issue:** line number plus a reason code (`InvalidDate`, `InvalidAmount`, `MissingColumn`, `UnsupportedEncoding`…). A malformed row is reported, never skipped silently, and never stops the rest of the file from parsing.

Account identifier, currency and statement period come from the preamble when the profile has one (mBank `#Numer rachunku`, `#Waluta`, `#Za okres:`). Data rows end at the first blank or `#`-prefixed line; footer lines such as mBank's `#Saldo końcowe` and legal note are not rows and are not issues.

Amounts are parsed with `decimal` using the profile's decimal and thousands separators (space or no-break space). Excess precision is an issue on that row, never rounded away (parent §4).

## 3. Which rows become candidates

A row is a **candidate** when all of these hold:

1. The amount is negative, i.e. money leaving the account.
2. The row is booked: Revolut `State = COMPLETED`, and not an mBank or ING blocked-funds row (`blokada`).
3. Its currency is the Budget currency. A row in another currency is listed as skipped with `CurrencyMismatch`, which covers Revolut's multi-currency pockets. Card payments made abroad on a PLN account are booked in PLN and are candidates. Revolut's per-currency balance groups make this obvious.
4. It is not an own-account movement. This covers Revolut `Type` of `EXCHANGE` or `TOPUP` and pocket transfers, each Polish bank's own-transfer type (`Przelew własny`, `Przelew na rachunek własny`; #670 confirms the exact strings), and credit-card repayments (mBank `RĘCZNA SPŁATA KARTY KREDYT.`, plus the automatic variant). A card repayment moves money to the card, so treating it as spending would count the card purchases twice. Where a bank has no own-transfer marker, the row stays a candidate and the user deselects it in the review.

Everything else is **listed as skipped** with its reason, so the user can see nothing was dropped: incoming money, refunds, ATM cash withdrawals, other currencies, pending rows and own transfers. v1 does not let the user turn a skipped row into an expense. Income and cash belong to F2, and refunds to F3.

ATM withdrawals are skipped because cash leaving the account is not yet spending; that is what F2 cash accounts are for. Bank fees and card payments are candidates.

## 4. Duplicate rule

### 4.1 Source key

Each candidate gets a server-computed **source key**:

- When a bank reference exists: `SHA-256(profile | bank account id | bank reference)`.
- Otherwise, the fingerprint `SHA-256(profile | bank account id | booking date | amount | normalised description | n)`. Here `n` is the 1-based occurrence of that exact tuple within the file, so two identical coffees on the same day stay two rows. For Revolut, the booking date includes the `Started Date` time, which makes `n` almost always 1.

Normalised description means: trim, collapse whitespace, upper-case invariant, and strip card-masking noise such as `Nr karty ****1234`. #670 holds the exact rules plus unit tests.

Only the hash is stored. Bank account numbers and bank references never reach the database in clear text.

Accepted limitation: when a fingerprinted bank exports two files that split the same calendar day, `n` can differ between the files. Every bank in §2 exports whole days, so this does not happen in practice.

### 4.2 Uniqueness scope

The issue says "unique per budget account". This design narrows that to **unique per Budget and privacy scope**:

- A key imported into any household envelope is unique per Budget.
- A key imported into a personal envelope is unique per Budget and that envelope's owner.

Why: a single per-Budget key would let Bea's preview reveal that Alex already imported a row into his private envelope, which breaks parent §5 ("duplicate status must not escape"). The cost is that a joint-account row can deliberately be imported once as shared and once privately. That is the user's choice, as with manual entry.

Enforcement: a unique index on `(budget_id, scope, source_key)`, where `scope` is `household` or the owner `PersonId`.

### 4.3 What the preview shows per candidate

| Status | Meaning | Default selection |
|---|---|---|
| `New` | No match | Selected |
| `AlreadyImported` | The source key exists in the caller's visible scope for the chosen envelope | Not selectable |
| `PossibleDuplicate` | No source-key match, but the parent §6 rule matches a visible expense (exact amount, same category, within two days). This catches manual entries made before the import. Up to five matches are returned. | Not selected; the user can tick it ("Keep both") |

A duplicate check uses only the expenses the caller can see, as `GET /expenses` does.

## 5. Mapping to expenses

The user chooses **one target envelope per import**. Defaults are set once per import, and each row can override them:

| Expense field | Default | Per-row override |
|---|---|---|
| Envelope | Chosen for the import | No. Import again into another envelope instead. |
| `Amount` | `abs(amount)` (Revolut: `abs(Amount) + Fee`) | No. The amount is what the bank says. |
| `OccurredOn` | Purchase date: the `DATA TRANSAKCJI: YYYY-MM-DD` in the mBank card-payment title when present, else the operation date (`#Data operacji`, ING `Data transakcji`, PKO `Data operacji`); Revolut: date part of `Started Date` | No |
| `Category` | BI5 suggestion if one exists, else the import default, else `Other` | Yes |
| `Description` | `counterparty — title`, or just the title when the counterparty is empty (mBank card payments), with bank padding and the `DATA TRANSAKCJI: …` suffix removed; normalised and truncated to `Expense.MaxDescriptionLength` (80). Example: `ALDI SP. Z O.O. /GLIWICE` | Yes |
| Funding source | Shared envelope: chosen per import (`Individual` or `HouseholdFunds`). Personal envelope: always `Individual` with the owner as payer (parent §4). | Yes, shared envelopes only |
| Payer | The importing actor when `Individual` | Yes, any eligible current adult |
| Participants | All current Owner/Adult members, as in manual entry | Yes |

Every row is validated exactly like a manual expense: access, eligible payer and participants, money rules. One invalid row rejects the whole import with per-row errors, and nothing is written.

### 5.1 Category suggestions (BI5)

Store `(budget_id, scope, merchant_key) → category`, with the same scope as §4.2. That way a merchant learned from a private import is never suggested to another adult. `merchant_key` is the normalised counterparty or, when it is empty, the title up to the first `/` (mBank `KAUFLAND 6663      /GLIWICE` → `KAUFLAND 6663`). Store numbers in merchant names stay part of the key in v1. Each confirmed import upserts the rule for every imported row, so the user's latest choice wins. Suggestions are exact-match lookups only. Fuzzy and model-based matching belong to BI8.

### 5.2 Import provenance

An import writes one `statement_imports` row: import ID, `BudgetId`, `ActorPersonId`, `ClientRequestId`, envelope, the normalised request (stored the way expense revisions store theirs, parent §6) and a timestamp. Its unique `(BudgetId, ActorPersonId, ClientRequestId)` index is the replay key for the whole import. Each imported expense also gets one `imported_transactions` row (`budget_id`, `scope`, `source_key`, `import_id`, `expense_id`) carrying the §4.2 unique index. A replay returns the expense IDs found through `import_id`.

Each imported expense still writes its own revision 1. That revision cannot reuse the import's `ClientRequestId`: `ux_expense_revisions_request_identity` is unique per `(BudgetId, ActorPersonId, ClientRequestId)`, so a two-row import would collide. Revision 1 instead uses a per-row ID derived as UUIDv5(namespace = import `ClientRequestId`, name = `sourceKey`). It is deterministic on replay and unique within the import. Revision 1 records `Source = Import` and the import ID (BI4); a manual entry records `Source = Manual`. Later corrections and voids work as they do today. Voiding an imported expense keeps its source key, so re-importing the file does not bring the voided row back. To re-import a row, the user records it manually.

## 6. API shape

Both routes sit under `/api/budget` and follow parent §8 conventions.

| Route | Body | Result |
|---|---|---|
| `POST /imports/preview` | `multipart/form-data`: `file`, `budgetAccountId` | Profile, statement period, candidates (row key, date, amount, description, suggested category, status, matches), skipped rows with reasons, parse issues. No side effects. |
| `POST /imports` | JSON: `clientRequestId`, `budgetAccountId`, per-import defaults, selected rows (`sourceKey`, date, amount, description, category, funding, payer, participants) | `{ importId, expenseIds, skippedAlreadyImported }` |

Import rows are user-asserted facts, exactly like manual entries. The server does not re-check them against a file it no longer has. A forged `sourceKey` can only affect the caller's own duplicate protection.

Import replay follows parent §6, keyed on the `statement_imports` row (§5.2): the same request returns the original result, and a changed payload returns 409.

Race between preview and import: if a selected key was imported in the meantime, the whole import fails with a typed 409. The UI then offers a fresh preview (#672).

MCP and the assistant (BI8) call the same two handlers. The file content is passed as text in the tool input, so it is also bounded by the MCP `MaxRequestBytes` setting. BI8 defines the tool schema.

## 7. Privacy

- The file is parsed in memory from the request stream and discarded when the request ends. It is never written to disk, the database, object storage, the outbox or logs.
- The preview is not persisted. Each preview re-parses the uploaded file.
- Each imported expense stores only its expense fields (parent §8) plus the hashed source key, import ID and profile name. Counterparty account numbers, balances, card numbers and the bank's own categories are dropped.
- Logs and ProblemDetails may contain line numbers, profile names, counts and reason codes, but never row contents, amounts or account numbers.
- Import endpoints publish no integration events, consistent with parent §8.

## 8. Limits

| Limit | Value | Reason |
|---|---|---|
| File size | 2 MiB | Under the host's 5 MiB `MaxRequestBodySize`. A year of personal CSV is roughly 300 KiB. |
| Data rows per file | 1,000 | Covers several months of one account; a larger statement is split by date range |
| Selected rows per import | 1,000 | Matches the file limit; one Budget transaction |
| Header search | First 60 lines | The mBank preamble alone is ~37 lines |

Breaking a limit returns 400 with the limit named. Nothing is partially parsed or imported.

## 9. Access

Importing into an envelope requires the same permission as recording an expense in it (parent §5). Owner/Adult can import into shared envelopes. Anyone with write access to a personal envelope can import into it, which includes a Child for their own envelope. A Guest has no access. An envelope the caller cannot see returns 404.

## 10. Decisions

| # | Decision | Status |
|---|---|---|
| D1 | Banks: PKO BP, mBank, ING Bank Śląski, Revolut | Owner, 2026-10-25 |
| D2 | CSV profiles only in v1; CAMT.053 deferred; no MT940 | Owner, 2026-10-25 |
| D3 | Duplicate uniqueness per Budget and privacy scope (§4.2), not per envelope | Owner, 2026-10-25 |
| D4 | ATM withdrawals, incoming money, refunds and own transfers are skipped in v1 | Owner, 2026-10-25 |
| D5 | A voided imported expense is not re-imported | Owner, 2026-10-25 |
| D6 | Limits from §8 | Owner, 2026-10-25 |
