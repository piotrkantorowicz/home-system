---
name: assistant-held-actions
description: Use when editing assistant write tools or the Confirm card.
category: assistant
---

# assistant-held-actions

Write tools never record on the first turn. The server holds the call as `pending_action`; the
panel shows a card; only `Confirm` records it. Keep three layers in step when you change one:

1. **Instructions (`AssistantAgent.cs`)** — tell the model every argument it must have before it
   proposes a write. A household envelope expense needs funding: who paid (one adult, for the
   participants) or household funds. If envelope, category, funding, amount or date is unclear,
   the model asks; it never guesses.
2. **Card (`AssistantPanel.tsx`)** — hide server-minted internal arguments such as `operationKey`
   (`.filter(([key]) => key !== 'operationKey')`). Show the rest. An argument that is a list of
   records (an import's rows) renders as a compact table via `RowsTable`; columns matching
   `/(Key|Id|Ids)$/` are hidden, since matching keys and person ids mean nothing to the user.
3. **Tests** — the held turn asserts `pending_action` and nothing persisted; Confirm then records.
   The UI test asserts the hidden key is absent (`not.toHaveTextContent`). The integration host
   must register the module the tool writes to (Budget) so the confirmed write is real.

Scripted-model tests: the arguments you script must include the funding field the instructions
now require, or the test documents a path the product no longer takes.

Attachments (bank statements, #731): the panel sends a file as `document: { name, text }` next to
`message`. The model sees only a note naming the file. The server fills the run-now tool's
`statement` argument (`preview_expense_import`) from the text in the same turn, so the file is
never stored and never copied by the model. A held write (`execute_expense_import`) is confirmed
like any other. Preview must happen in the turn the file was attached. Ceiling: the model writes
every row of a held import call, so a very long statement can hit `MaxOutputTokens`.
