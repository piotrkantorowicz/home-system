# Household module — design & delivery plan

Status: **accepted — phase 1 and phase 2 delivered**
Owner: Piotr Kantorowicz
Last updated: 2026-09-26

---

## 1. Problem

The app is single-user today. Every DietPlanner aggregate is keyed by a raw
Authentik subject string (`string UserId`). There is no concept of a shared home,
no user registry (so no display names / avatars), and nothing can be shared
between people who live together.

The goal is to turn the app into a **home system**: a household groups the people
who live together, and shared resources (shopping list, meal plan, calendar,
later a budget) belong to the household rather than to one person. Body-centric
and private data (weight, water, diet goals, what you actually ate) stays
personal.

## 2. Scope decision — no IAM module

Authentik is the identity provider. It owns authentication, credentials, OIDC,
MFA and user lifecycle. We do **not** build an IAM module.

We do need a thin **Person registry** — a local mirror of the humans in the
system — to provide display names, avatars, a "who did this" anchor, and a home
for managed members (children with no login). That registry is small and lives
**inside the `Household` module**. `Person` and `Household` are one bounded
context: "who are the people in this home and how are they grouped".

If, later, multiple modules need person data independent of households and person
concerns grow, `Person` can be extracted into its own `Identity` module. Not now.

## 3. Core concepts

| Concept | Meaning |
|---|---|
| **Person** | A human. May be linked to an Authentik account (`AuthSubject`) or *managed* (no login — e.g. a child). Carries `DisplayName`, `Email`, `AvatarUrl`. |
| **Household** | The sharing boundary. Has a name and one or more members. |
| **HouseholdMember** | A `Person` in a `Household` with a `Role`. |
| **HouseholdInvitation** | A pending membership, addressed by email, by a known `Person`, or both. No email is ever sent — invitations are in-app only. Signing in never resolves it — the invitee must explicitly accept or decline (#251). |

### Roles

`Owner` · `Adult` · `Child` · `Guest` — enforced, not cosmetic.

| Capability | Owner | Adult | Child | Guest |
|---|---|---|---|---|
| Manage members / roles / rename / delete household | ✅ | — | — | — |
| Read shared resources (list, plan, calendar, budget) | ✅ | ✅ | ✅ | ✅ |
| Write shared resources | ✅ | ✅ | — | — |
| Write own personal resources | ✅ | ✅ | ✅ | — |
| Have personal resources managed *for* them by an adult | ✅ | ✅ | ✅ | — |

At least one `Owner` must exist in a household at all times.

## 4. Key decisions (locked)

1. **One household per person in v1.** Modelled as a `household_members` join
   table, but "a person has at most one active membership" is enforced as a
   *domain invariant*, not a database constraint — so multi-household can be
   unlocked later without a schema change. See §9 for when that day comes.
2. **Managed members ship in v1.** An owner/adult creates a `Person`
   (`IsManaged = true`, `AuthSubject = null`) and can log personal data for them.
3. **No email invitations.** Invitations are in-app only — the owner tells the
   invitee to sign in. This is the final design, not a v1 stop-gap: email
   invites (#231) and the Authentik user picker (#232) were dropped in phase 2. Signing in alone never joins them to the household:
   the invitee reviews the invitation on their own "Invitations for you" panel
   and explicitly accepts or declines it (#251) — a silent auto-add on login
   would grant household access (and visibility into shared resources) without
   consent.
4. **Module name is `Household`.** `Family` / "family member" survives only as a
   UI label where it reads warmer. Code, module, tables, contracts, events all
   use `Household`.
5. **Personal data keys on `PersonId`, not the Authentik `sub`.** This makes
   "managed member → real login account" a one-field pointer swap with zero
   history migration. The DietPlanner backfill maps `sub → PersonId` on personal
   tables.
6. **Persistence style 1 (DDD + EF Core).** Real invariants (last-owner rule,
   single active membership, invitation state machine, unique linkage), reads
   beyond key-by-userid, and it is foundational — retrofitting invariants later
   is the expensive path.

## 5. Backend shape (`src/Modules/Household`)

Style-1 DDD module, own PostgreSQL database, own EF migrations, own outbox.

```
Household.Domain/
  Aggregates/
    Household.cs                 # root; owns _members
    Person.cs                    # root
    HouseholdInvitation.cs       # root
  Entities/
    HouseholdMember.cs           # internal, child of Household
  ValueObjects/
    HouseholdId, PersonId, HouseholdMemberId, HouseholdInvitationId
    HouseholdRole (enum-like), InvitationStatus, PersonEmail
  Events/
    HouseholdCreatedDomainEvent
    MemberJoinedHouseholdDomainEvent
    MemberLeftHouseholdDomainEvent
    MemberRoleChangedDomainEvent
    PersonLinkedToAccountDomainEvent
  Abstractions/
    IHouseholdRepository, IPersonRepository, IHouseholdInvitationRepository
    IHouseholdUnitOfWork   # module-scoped — never the global IUnitOfWork

Household.Application/
  Commands/
    CreateHousehold, RenameHousehold, DeleteHousehold
    AddExistingPersonAsMember, CreateManagedMember
    InvitePersonByEmail, RevokeInvitation
    RemoveMember, ChangeMemberRole, LeaveHousehold
    ConvertManagedMemberToAccount           # creates pending link
  Queries/
    GetMyHousehold            -> MyHouseholdDto (household + my role + members)
    ListHouseholdMembers      -> MemberDto[]
    ListPendingInvitations    -> InvitationDto[]
    ListPickablePersons       -> PickablePersonDto[]   # for the "add member" picker
  EventHandlers/
    *DomainEventHandler -> map to integration events, publish via IIntegrationEventBus
  Identity/
    UpsertPersonOnLogin       # called by the host claims transformer / login hook

Household.Contracts/
  Interfaces/
    IHouseholdQueryService     # GetHouseholdContextForUser(authSubject) -> { HouseholdId, PersonId, Role, Members[] }
  Events/
    HouseholdCreatedIntegrationEvent
    MemberJoinedHouseholdIntegrationEvent
    MemberLeftHouseholdIntegrationEvent
    MemberRoleChangedIntegrationEvent

Household.Infrastructure/
  Persistence/ (HouseholdDbContext, configurations, repositories, migrations)
  Query/       (HouseholdQueryService : IHouseholdQueryService — AsNoTracking projections)
  DependencyInjection.cs  (AddCqrsHandlers + AddOutbox<HouseholdDbContext> + module-scoped IHouseholdUnitOfWork)

Household.Api/
  HouseholdEndpoints.cs
  PersonEndpoints.cs
  DependencyInjection.cs        # AddHouseholdModule() + MapHouseholdEndpoints()
```

> **Multi-module note.** `Household` is the second Style-1 (EF) module. The shared
> CQRS + messaging infra was adjusted for that in #234 — the host registers the
> dispatcher chain once via `AddCqrsDispatchers()`, modules only call
> `AddCqrsHandlers()`, and `Household` exposes `IHouseholdUnitOfWork` rather than
> rebinding the global `IUnitOfWork`.

### Invitation resolution — explicit consent (#251)

1. An owner issues an invitation, either:
   - `InvitePersonByEmail(householdId, email, role)` — addressed by email; if a
     `Person` with that email already exists, the invitation also carries their
     `PersonId` (a bonus for accept-by-id, not a fast path to membership); or
   - `AddExistingPersonAsMember(householdId, personId, role)` — addressed by a
     specific existing `Person` picked from `ListPickablePersons`. That person
     may have no email on file (a managed account, or an OIDC provider that
     omits the claim), so the invitation is created with `email: null` in that
     case — `TargetPersonId` alone still addresses it.
   - Either way this only ever creates `HouseholdInvitation { Pending, ExpiresAt = +30d }`
     — never immediate membership.
2. On every OIDC login the host calls `UpsertPersonOnLogin(claims)`, which
   upserts the `Person` by `AuthSubject` and links a matching managed person by
   email. It does **not** touch invitations — signing in is not consent.
3. The invitee sees their pending invitations (`GET /api/households/invitations/mine`,
   matched by `TargetPersonId` or `Email`) and explicitly calls
   `AcceptInvitationCommand` or `DeclineInvitationCommand`. Accepting adds the
   membership, marks the invitation `Accepted`, and raises
   `MemberJoinedHouseholdDomainEvent`; declining marks it `Declined` with no
   membership change.
4. The frontend shows a one-time "You've joined the _Kowalski_ household" toast
   right after a successful accept.

### Managed member → real account

`ConvertManagedMemberToAccount(personId, email)` sets a pending link on the
`Person`. On that person's first login, `UpsertPersonOnLogin` finds the managed
unlinked `Person` by email, calls `Person.LinkAuthSubject(sub)`, flips
`IsManaged = false`. All personal data — keyed by `PersonId` — is untouched.

### Host wiring

- `AddHouseholdModule(configuration)` in `HomeSystem.REST`.
- A `ClaimsTransformation` (or middleware) resolves the caller's household via
  `IHouseholdQueryService` and adds `household_id`, `person_id`, `household_role`
  claims. Endpoints read them the way they read `NameIdentifier` today.
- New Postgres database + docker-compose profile `household` (port 5434).

## 6. Shared vs personal resources

Rule of thumb: **shared if it is household logistics; personal if it is about one
body or one person's private money.**

**How sharing is scoped.** DietPlanner tables carry **no `household_id`
column.** Rows stay keyed by their owner (`PersonId`, or the creator's auth
subject for the library). On each request DietPlanner resolves the caller's
household through `IHouseholdQueryService` (`HouseholdRosterProvider`) and
widens the query to the members' ids. Membership changes therefore need no
data migration, and a failed lookup fails closed — only a confirmed "no
household" falls back to the caller alone.

| Resource | Scope | Notes |
|---|---|---|
| Shopping list | Household | members' entries; duplicate entries avoided by the live shared view |
| Meal plan / calendar | Household | the entry's `PersonId` is the assignee (no extra column); read any member's plan, plan per role rules below |
| Recipes, Products (library) | per-item `Visibility` | see *Library visibility* below |
| Budget / expenses (future) | Household | `PaidByPersonId` + `AddedByPersonId` distinct; per-account `Visibility` for joint vs personal envelopes |
| Weight entries | Personal | per body; adult may log for a managed member |
| Water intake | Personal | per body |
| Diet goals, profile (age / activity / TDEE) | Personal | per body |
| Meal actuals / consumption log | Personal | what *this* person actually ate |
| Notification preferences | Personal | already is |

### Meal plan rules (#229)

- Any member reads another member's plan and meal schedule
  (`?personId=`; outside the household → 403).
- Planning (create / edit / delete a meal): Owner / Adult for anyone in the
  household, Child for themselves, Guest for no one.
- Logging meal actuals (complete / override / reset / bulk-complete): the
  person themselves, or an Owner / Adult for a **managed** member.
- Updating the meal schedule (slots): same rule — the person themselves, or an
  Owner / Adult for a **managed** member. Reading it follows the first bullet.
- An entry outside the caller's household → 404; inside but not allowed → 403.

### Library visibility (#230)

Recipes and products carry `Visibility { Private, Household, Public }`,
default `Household`.

| | Read | Edit / delete |
|---|---|---|
| `Private` | creator | creator |
| `Household` | creator's household | creator, or an Owner / Adult of the creator's household |
| `Public` | everyone | creator, or an Owner / Adult of the creator's household |

A Guest never writes to the library. Only the creator changes an item's
visibility. Responses carry `canEdit` so the UI hides actions the caller can't
take.

### DietPlanner migration

- Personal tables: replace `user_id` with `person_id` (#222).
- Shared resources need no schema change — scoping is resolved per request (above).

> **Pre-release note (#221).** The app has no production users, so there is no
> historical data to back-fill. #221 ships nothing. A fresh account gets a `Person`
> on login (`SyncCurrentPerson`) but **not** a `Household` — the frontend onboarding
> (§7) catches `GET /api/households/me → 404` and drives the user through
> `POST /api/households` to name their home. `AddExistingPersonAsMember`,
> `ListPickablePersons` and invitation resolution all rely on people existing
> without a household until they act, so auto-provisioning one on login is
> deliberately avoided.

## 7. Frontend (`src/modules/household`)

Follows the module-registry pattern (`shared/lib/module-registry.ts`).

- **Pages**
  - `HouseholdPage` — member list with role, rename household, add member,
    pending invitations, remove member, leave household.
  - `AddMemberDialog` — tabbed: pick existing person · invite by email · create
    managed member. The first two tabs always create a pending invitation now
    (#251) — never immediate membership.
  - `MyInvitations` — shown on `HouseholdPage` when the caller has no household
    yet; lists invitations addressed to them with accept / decline actions. The
    "You've joined" toast fires from a successful accept, not globally.
- **Onboarding** — after person sync and invitation resolution, a person without
  a household must explicitly create one before entering feature pages. Prefill
  the editable name with “My home” / “Mój dom”; “Get started” / “Rozpocznij”
  creates the single-member household with the caller as owner. There is no skip.
  Adding other people is optional after creation. Keep the app shell visible
  during loading and errors, with retry; errors must not be treated as absence
  of a household. Existing members go directly to their requested page.
- **State**
  - `HouseholdProvider` mounted in the app shell; `useHousehold()` exported from
    the module `index.ts` returning `{ household, myRole, members, isLoading }`.
  - Other modules (DietPlanner) consume `useHousehold()` for member pickers,
    "assigned to" selectors and "shared with the household" badges.
- **Nav** — a `Household` item in the settings area of the sidebar.
- **API types** generated from the OpenAPI spec
  (`npm run generate:api:household` — new script).

## 8. Delivery plan

### Phase 1 — foundation (epic #213)

| Issue | |
|---|---|
| #214 | scaffold module (this PR) |
| #215 | Person registry + login upsert + claims transformer |
| #216 | Household + HouseholdMember aggregate + role rules |
| #217 | commands, queries, API endpoints |
| #218 | HouseholdInvitation + login-time resolution (no email) |
| #219 | Contracts — IHouseholdQueryService + integration events |
| #220 | convert managed member to real account |
| #221 | backfill one Person + Household per existing user |
| #222 | re-key DietPlanner personal data on PersonId |
| #223 | scope DietPlanner shopping list to household |
| #224 | UI — household module |
| #225 | UI — HouseholdProvider + useHousehold + accept-invite toast |
| #226 | integration tests |
| #227 | e2e — owner adds member, member sees shared shopping list |

Prerequisite #234 (shared infra: support multiple Style-1 EF modules) lands first.

### Phase 2 — shared planning (epic #228)

| Issue | |
|---|---|
| #229 | plan and read meals for household members |
| #419 | household calendar — person filter and assign picker |
| #230 | recipe and product visibility |
| #420 | visibility selector and badges in the library |
| #421 | align this design doc with phase 2 |

Dropped: email invitations (#231) and the Authentik Admin API user picker
(#232) — in-app invite + explicit accept (#251) is the final design.

### Phase 3 — budget module (epic #233)

- New `Budget` module, household-scoped, `BudgetAccount` envelopes with
  `Visibility`, `PaidByPersonId` vs `AddedByPersonId`, soft duplicate detection
  on create.
- Bank / CSV import as the real fix for double entry — deferred within the epic.

## 9. When multi-household becomes worth it

Revisit the "one household per person" decision when a real case appears:

- shared custody — a child's meals planned across two homes;
- co-parenting adults managing a child's resources across an ex's household;
- managing an aging parent's separate household (their shopping, their
  medication calendar) alongside your own;
- a shared flat list **and** a separate family-back-home household;
- a temporary second household for a trip, archived afterwards.

The schema already allows it (join table). The cost is entirely in request
scoping (an active-household switcher, `household_id` becomes a list, every
shared endpoint picks one) and UI — not in data model.

## 10. Open items

None. Resolved: claims travel via `HouseholdClaimsTransformation`; the
backfill was not needed (#221, pre-release).
