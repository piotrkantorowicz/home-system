# MCP API coverage inventory

Status: snapshot of `main` at 2026-10-24 (`611ae18a`), produced for #606.
Scope: every HTTP operation the host maps, classified as a planned MCP tool,
transport-only plumbing, or excluded with a reason. This is the parity ledger for
epic #605 and its follow-up epics. **A new API operation is not done until it has a
row here.**

How it was produced: route declarations (`Map{Get,Post,Put,Delete,Patch}` inside each
module's `*.Api` project, including sub-groups passed in as a `RouteGroupBuilder`)
were extracted from source and reconciled against the
OpenAPI document groups. OpenAPI assists the inventory; it never publishes a tool.
Every tool is a hand-written, typed adapter (see `docs/rules/backend-api-patterns.md`
§ MCP tools).

## Totals

| Module | Operations | In epic #605 | Later epic | Transport-only / excluded |
|---|---:|---:|---:|---:|
| DietPlanner | 49 | 14 | 34 | 1 |
| Household | 20 | 0 | 19 | 1 |
| Budget | 23 | 0 | 23 | 0 |
| Investments | 12 | 0 | 12 | 0 |
| Notifications | 11 | 0 | 11 (5 admin) | 0 |
| Operations | 5 | 0 | 5 (admin) | 0 |
| Host | 6 | 0 | 0 | 6 |
| **Total** | **126** | **14** | **104** | **8** |

"In epic #605" counts routes whose behaviour a first-epic tool covers. Several of them
share one tool: `get_diet_context` reads profile, goals, meal schedule and reminder
settings. The import tools call the strict application commands from #612, not the
legacy REST routes. "Tbd" tool names are settled when that epic's child issues are
written.

**Kind**: `read`, `write` (creates or changes state), `destructive` (deletes, voids,
archives, leaves, or changes a role; needs the destructive annotation and its own
confirmation in the client), `admin-*` (needs the separate admin grant and the
existing admin policy, never the everyday write grant).

## Host and protocol plumbing

| Method | Route | Classification | Reason |
|---|---|---|---|
| GET | `/health` | excluded | Liveness probe, no user data |
| GET | `/openapi/{documentName}.json` | excluded | API description; feeds this inventory |
| GET | `/scalar/*` | excluded | Development-only API explorer |
| WS | `/hubs/notifications` | transport-only | SignalR push channel. Inbox state is covered by the Notifications REST reads |
| POST | `/mcp` | transport-only | The MCP endpoint itself (Streamable HTTP, stateless; only when `Mcp:Enabled`). Serves the epic #605 tools: `get_diet_context`, `search_products`, `get_product`, `search_recipes`, `get_recipe`, `get_meal_entries`, `get_nutrition_summary`, `create_product`, `create_recipe`, `preview_plan_import`, `execute_plan_import`; epic #639 adds `update_product`, `delete_product`, `update_recipe`, `delete_recipe`, `plan_meal`, `update_planned_meal`, `delete_planned_meal`, `complete_meal`, `override_meal`, `reset_meal`, `complete_day`, `set_profile`, `set_goals`, `get_weight_prediction`, `log_weight`, `get_weight_entries`, `delete_weight_entry`, `get_hydration`, `update_hydration_config`, `log_water_intake`, `delete_water_intake`, `update_meal_schedule`, `update_diet_reminders`, `get_shopping_list`, `check_shopping_item`, `clear_shopping_checks`, `get_weekly_summary` |
| GET | `/.well-known/oauth-protected-resource` | transport-only | RFC 9728 metadata for MCP clients; the only anonymous route MCP adds |

## Operations by module

### Budget

| Method | Route | Operation | Kind | Proposed tool | Target |
|---|---|---|---|---|---|
| GET | `/api/budget` | GetBudget | read | `get_budget_context` (`Budget`) | Budget epic (#660) |
| POST | `/api/budget` | InitializeBudget | write | tbd | Budget epic |
| GET | `/api/budget/accounts` | ListBudgetAccounts | read | `get_budget_context` (`Accounts`, active only) | Budget epic (#660) |
| POST | `/api/budget/accounts` | CreateBudgetAccount | write | `create_account` | Budget epic (#662) |
| GET | `/api/budget/accounts/{id:guid}` | GetBudgetAccount | read | `get_account` | Budget epic (#662) |
| PUT | `/api/budget/accounts/{id:guid}` | RenameBudgetAccount | write | `rename_account` | Budget epic (#662) |
| POST | `/api/budget/accounts/{id:guid}/archive` | ArchiveBudgetAccount | destructive | `archive_account` | Budget epic (#662) |
| POST | `/api/budget/accounts/{id:guid}/restore` | RestoreBudgetAccount | write | `restore_account` | Budget epic (#662) |
| GET | `/api/budget/expenses` | ListBudgetExpenses | read | `search_expenses` | Budget epic (#660) |
| GET | `/api/budget/expenses/{id:guid}` | GetBudgetExpense | read | `get_expense` | Budget epic (#660) |
| POST | `/api/budget/expenses` | CreateBudgetExpense | write | `create_expense` (`operationKey` = ClientRequestId) | Budget epic (#661) |
| PUT | `/api/budget/expenses/{id:guid}` | UpdateBudgetExpense | write | `update_expense` | Budget epic (#661) |
| POST | `/api/budget/expenses/{id:guid}/void` | VoidBudgetExpense | destructive | `void_expense` | Budget epic (#661) |
| POST | `/api/budget/imports/preview` | PreviewBudgetStatementImport | read | `preview_expense_import` (statement as text) | Budget epic (#676) |
| POST | `/api/budget/imports` | ImportBudgetExpenses | write | `execute_expense_import` (`operationKey` = ClientRequestId) | Budget epic (#676) |
| GET | `/api/budget/summary` | GetBudgetSummary | read | `get_budget_summary` | Budget epic (#660) |
| GET | `/api/budget/limits` | ListBudgetLimits | read | `list_limits` | Budget epic (#662) |
| PUT | `/api/budget/accounts/{id:guid}/limits/{month}` | SetBudgetLimit | write | `set_limit` | Budget epic (#662) |
| DELETE | `/api/budget/accounts/{id:guid}/limits/{month}` | ClearBudgetLimit | destructive | `clear_limit` | Budget epic (#662) |
| GET | `/api/budget/settlement` | GetBudgetSettlement | read | `get_settlement` | Budget epic (#663) |
| GET | `/api/budget/settlements` | ListBudgetRepayments | read | `list_repayments` | Budget epic (#663) |
| POST | `/api/budget/settlements` | RecordBudgetRepayment | write | `record_repayment` (`operationKey` = ClientRequestId) | Budget epic (#663) |
| POST | `/api/budget/settlements/{id:guid}/void` | VoidBudgetRepayment | destructive | `void_repayment` | Budget epic (#663) |

### DietPlanner

| Method | Route | Operation | Kind | Proposed tool | Target |
|---|---|---|---|---|---|
| GET | `/api/v1/diet-reminder-settings` | GetDietReminderSettings | read | `get_diet_context` (`Reminders`) | #658 |
| PUT | `/api/v1/diet-reminder-settings` | UpdateDietReminderSettings | write | `update_diet_reminders` | #658 |
| GET | `/api/v1/goals` | GetGoals | read | `get_diet_context` | #607 |
| POST | `/api/v1/goals` | CreateGoals | write | `set_goals` (upsert) | #655 |
| PUT | `/api/v1/goals` | UpdateGoals | write | `set_goals` (upsert) | #655 |
| GET | `/api/v1/hydration/config` | GetHydrationConfig | read | `get_hydration` | #657 |
| PUT | `/api/v1/hydration/config` | UpdateHydrationConfig | write | `update_hydration_config` | #657 |
| GET | `/api/v1/hydration/intake` | GetWaterIntake | read | `get_hydration` | #657 |
| POST | `/api/v1/hydration/intake` | LogWaterIntake | write | `log_water_intake` (operation key) | #657 |
| DELETE | `/api/v1/hydration/intake/{id:guid}` | DeleteWaterIntake | destructive | `delete_water_intake` | #657 |
| GET | `/api/v1/meals` | GetMeals | read | `get_meal_entries` | #609 |
| GET | `/api/v1/meals/nutrition-summary` | GetNutritionSummary | read | `get_nutrition_summary` | #609 |
| GET | `/api/v1/meals/shopping-list` | GetShoppingList | read | `get_shopping_list` | #659 |
| POST | `/api/v1/meals` | CreateMealEntry | write | `plan_meal` (operation key) | #653 |
| PUT | `/api/v1/meals/{id:guid}` | UpdateMealEntry | write | `update_planned_meal` | #653 |
| PUT | `/api/v1/meals/shopping-list/checks` | SetShoppingCheck | write | `check_shopping_item` | #659 |
| DELETE | `/api/v1/meals/shopping-list/checks` | ClearShoppingChecks | destructive | `clear_shopping_checks` | #659 |
| DELETE | `/api/v1/meals/{id:guid}` | DeleteMealEntry | destructive | `delete_planned_meal` | #653 |
| PATCH | `/api/v1/meals/{id:guid}/complete` | CompleteMealEntry | write | `complete_meal` | #654 |
| PATCH | `/api/v1/meals/{id:guid}/override` | OverrideMealEntry | write | `override_meal` | #654 |
| PATCH | `/api/v1/meals/{id:guid}/reset` | ResetMealEntry | write | `reset_meal` | #654 |
| POST | `/api/v1/meals/bulk-complete` | BulkCompleteMeals | write | `complete_day` | #654 |
| POST | `/api/v1/meals/validate` | ValidateImport | read | `preview_plan_import` (strict command, not this route) | #613 (resolver: #612) |
| POST | `/api/v1/meals/import` | ExecuteImport | write | `execute_plan_import` (strict command + receipt) | #614 |
| GET | `/api/v1/meal-schedule` | GetMealSchedule | read | `get_diet_context` | #607 |
| PUT | `/api/v1/meal-schedule` | UpdateMealSchedule | write | `update_meal_schedule` | #658 |
| GET | `/api/v1/products` | ListProducts | read | `search_products` | #608 |
| GET | `/api/v1/products/{id:guid}` | GetProduct | read | `get_product` | #608 |
| POST | `/api/v1/products` | CreateProduct | write | `create_product` | #610 |
| PUT | `/api/v1/products/{id:guid}` | UpdateProduct | write | `update_product` | #651 |
| DELETE | `/api/v1/products/{id:guid}` | DeleteProduct | destructive | `delete_product` | #651 |
| GET | `/api/v1/profile` | GetProfile | read | `get_diet_context` | #607 |
| GET | `/api/v1/profile/prediction` | GetWeightPrediction | read | `get_weight_prediction` | #655 |
| POST | `/api/v1/profile` | CreateProfile | write | `set_profile` (upsert) | #655 |
| PUT | `/api/v1/profile` | UpdateProfile | write | `set_profile` (upsert) | #655 |
| GET | `/api/v1/recipes` | ListRecipes | read | `search_recipes` | #608 |
| GET | `/api/v1/recipes/{id:guid}` | GetRecipe | read | `get_recipe` | #608 |
| POST | `/api/v1/recipes` | CreateRecipe | write | `create_recipe` | #611 |
| PUT | `/api/v1/recipes/{id:guid}` | UpdateRecipe | write | `update_recipe` | #652 |
| DELETE | `/api/v1/recipes/{id:guid}` | DeleteRecipe | destructive | `delete_recipe` | #652 |
| PUT | `/api/v1/recipes/{id:guid}/image` | UploadRecipeImage | write | tbd | Recipe photo follow-up (#563): needs an explicit file-transport design |
| GET | `/api/v1/recipes/{id:guid}/image` | GetRecipeImage | read | tbd | Recipe photo follow-up (#563): needs an explicit file-transport design |
| GET | `/api/v1/recipes/{id:guid}/image/thumbnail` | GetRecipeImageThumbnail | read | tbd | Recipe photo follow-up (#563): needs an explicit file-transport design |
| DELETE | `/api/v1/recipes/{id:guid}/image` | RemoveRecipeImage | destructive | tbd | Recipe photo follow-up (#563): needs an explicit file-transport design |
| DELETE | `/api/v1/test-support/purge-my-data` | PurgeMyData | excluded | — | Excluded: test-support only (E2E cleanup), not a business operation |
| GET | `/api/v1/weekly-summary` | GetWeeklySummary | read | `get_weekly_summary` | #659 |
| POST | `/api/v1/weight-entries` | LogWeightEntry | write | `log_weight` | #656 |
| GET | `/api/v1/weight-entries` | GetWeightEntries | read | `get_weight_entries` | #656 |
| DELETE | `/api/v1/weight-entries/{id:guid}` | DeleteWeightEntry | destructive | `delete_weight_entry` | #656 |

### Household

| Method | Route | Operation | Kind | Proposed tool | Target |
|---|---|---|---|---|---|
| POST | `/api/households` | CreateHousehold | write | tbd | Household epic |
| GET | `/api/households/me` | GetMyHousehold | read | tbd | Household epic |
| GET | `/api/households/pickable-persons` | ListPickablePersons | read | tbd | Household epic |
| PUT | `/api/households/{id:guid}` | RenameHousehold | write | tbd | Household epic |
| DELETE | `/api/households/{id:guid}` | DeleteHousehold | destructive | tbd | Household epic |
| POST | `/api/households/{id:guid}/leave` | LeaveHousehold | destructive | tbd | Household epic |
| GET | `/api/households/{id:guid}/members` | ListHouseholdMembers | read | tbd | Household epic |
| POST | `/api/households/{id:guid}/members` | AddHouseholdMember | write | tbd | Household epic |
| POST | `/api/households/{id:guid}/managed-members` | CreateManagedMember | write | tbd | Household epic |
| DELETE | `/api/households/{id:guid}/members/{personId:guid}` | RemoveHouseholdMember | destructive | tbd | Household epic |
| PUT | `/api/households/{id:guid}/members/{personId:guid}/role` | ChangeHouseholdMemberRole | destructive | tbd | Household epic |
| POST | `/api/households/{id:guid}/members/{personId:guid}/convert-to-account` | ConvertManagedMemberToAccount | write | tbd | Household epic |
| GET | `/api/households/{id:guid}/invitations` | ListPendingInvitations | read | tbd | Household epic |
| POST | `/api/households/{id:guid}/invitations` | InvitePersonByEmail | write | tbd | Household epic |
| DELETE | `/api/households/{id:guid}/invitations/{invitationId:guid}` | RevokeInvitation | destructive | tbd | Household epic |
| GET | `/api/households/invitations/mine` | ListMyInvitations | read | tbd | Household epic |
| POST | `/api/households/invitations/{invitationId:guid}/accept` | AcceptInvitation | write | tbd | Household epic |
| POST | `/api/households/invitations/{invitationId:guid}/decline` | DeclineInvitation | write | tbd | Household epic |
| POST | `/api/persons/me/sync` | SyncCurrentPerson | excluded | — | Transport-only: SPA login plumbing that links the auth subject to a person; MCP relies on the existing claims pipeline instead |
| GET | `/api/persons/me` | GetCurrentPerson | read | `get_me` | Household epic |

### Investments

| Method | Route | Operation | Kind | Proposed tool | Target |
|---|---|---|---|---|---|
| GET | `/api/investments/wallets` | ListInvestmentWallets | read | `list_wallets` | Investments epic (#665) |
| POST | `/api/investments/wallets` | CreateInvestmentWallet | write | `create_wallet` | Investments epic (#667) |
| GET | `/api/investments/wallets/{id:guid}` | GetInvestmentWallet | read | `get_wallet` | Investments epic (#665) |
| PUT | `/api/investments/wallets/{id:guid}` | UpdateInvestmentWallet | write | `update_wallet` | Investments epic (#667) |
| POST | `/api/investments/wallets/{id:guid}/archive` | ArchiveInvestmentWallet | destructive | `archive_wallet` | Investments epic (#667) |
| POST | `/api/investments/wallets/{id:guid}/restore` | RestoreInvestmentWallet | write | `restore_wallet` | Investments epic (#667) |
| GET | `/api/investments/wallets/{id:guid}/snapshots` | ListInvestmentSnapshots | read | `list_snapshots` | Investments epic (#665) |
| PUT | `/api/investments/wallets/{id:guid}/snapshots/{asOf}` | RecordInvestmentSnapshot | write | `record_snapshot` | Investments epic (#666) |
| DELETE | `/api/investments/wallets/{id:guid}/snapshots/{asOf}` | DeleteInvestmentSnapshot | destructive | `delete_snapshot` | Investments epic (#666) |
| PUT | `/api/investments/snapshots` | RecordInvestmentsCheckIn | write | `record_check_in` | Investments epic (#666) |
| GET | `/api/investments/net-worth` | GetInvestmentsNetWorth | read | `get_net_worth` | Investments epic (#665) |
| GET | `/api/investments/net-worth/history` | GetInvestmentsNetWorthHistory | read | `get_net_worth_history` | Investments epic (#665) |

### Notifications

| Method | Route | Operation | Kind | Proposed tool | Target |
|---|---|---|---|---|---|
| GET | `/api/notification-preferences` | GetNotificationChannelPreferences | read | tbd | Notifications epic |
| PUT | `/api/notification-preferences` | UpdateNotificationChannelPreferences | write | tbd | Notifications epic |
| GET | `/api/admin/notifications/deliveries/summary` | GetNotificationDeliveryBacklog | admin-read | tbd | Privileged epic (admin grant) |
| GET | `/api/admin/notifications/deliveries/dead-letters` | ListDeadLetterDeliveries | admin-read | tbd | Privileged epic (admin grant) |
| GET | `/api/admin/notifications/deliveries/{id:guid}/content` | GetNotificationDeliveryContent | admin-read | tbd | Privileged epic (admin grant) |
| POST | `/api/admin/notifications/deliveries/{id:guid}/retry` | RetryNotificationDelivery | admin-write | tbd | Privileged epic (admin grant) |
| POST | `/api/admin/notifications/deliveries/retry-all` | RetryAllNotificationDeliveries | admin-write | tbd | Privileged epic (admin grant) |
| GET | `/api/notifications` | ListNotifications | read | tbd | Notifications epic |
| POST | `/api/notifications/{id:guid}/read` | MarkNotificationRead | write | tbd | Notifications epic |
| POST | `/api/notifications/read` | BulkMarkNotificationsRead | write | tbd | Notifications epic |
| GET | `/api/notifications/unread-count` | GetNotificationsUnreadCount | read | tbd | Notifications epic |

### Operations

| Method | Route | Operation | Kind | Proposed tool | Target |
|---|---|---|---|---|---|
| GET | `/api/admin/outbox/summary` | GetOutboxBacklog | admin-read | tbd | Privileged epic (admin grant) |
| GET | `/api/admin/outbox/{module}/dead-letters` | ListOutboxDeadLetters | admin-read | tbd | Privileged epic (admin grant) |
| POST | `/api/admin/outbox/{module}/dead-letters/{id:guid}/retry` | RetryOutboxDeadLetter | admin-write | tbd | Privileged epic (admin grant) |
| GET | `/api/admin/outbox/{module}/dead-letters/{id:guid}/payload` | GetOutboxDeadLetterPayload | admin-read | tbd | Privileged epic (admin grant) |
| POST | `/api/admin/outbox/{module}/dead-letters/retry-all` | RetryAllOutboxDeadLetters | admin-write | tbd | Privileged epic (admin grant) |


## Prior work reused

- Bulk import: #395 (JSON import wizard) and #500 (review step). Their
  `POST /api/v1/meals/validate` and `/import` stay the SPA contract. #612 moves both onto
  a shared resolver with a strict mode the MCP tools use.
- Recipe photos (#563): upload, read, thumbnail and remove are mapped
  (`RecipeImageEndpoints`, a sub-group of `/api/v1/recipes`). They need an explicit
  file-transport design before any tool is added. A text-only stand-in is not acceptable.

## Keeping it current

`scripts/verify.sh` does not check this file. Reviewers check that a PR adding a
`Map*` call adds a row here, and an MCP tool PR updates the "Proposed tool" and
"Target" cells to the shipped name and PR.
