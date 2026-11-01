---
name: budget-statement-import
description: Use when touching Budget bank statement import or dedupe.
category: budget
---

# budget-statement-import

Parse first, dedupe by key, then write rows through `ImportExpenseRow`.

- **Encoding:** `StatementParser` tries strict UTF-8, and on invalid bytes falls back to
  Windows-1250 (Polish banks export in it). Do not add a third decoder. The browser-side
  `readDocument.ts` (assistant attachments, #731) mirrors this order (strict UTF-8, then
  Windows-1250); when either side changes, change the other and its test.
- **Dedupe:** `StatementSourceKey` hashes the row material with SHA-256 (hex lower). The per-row
  request id is SHA-1 of the import request id plus the source key (`StatementRowRequestId`).
  A re-import of the same file must produce the same keys.
- **Row shape:** `ImportExpenseRow` carries `SourceKey`, `Amount`, `OccurredOn`, `Category`,
  optional `FundingSource`, `PaidByPersonId`, `ParticipantIds`, `Description`, and optional
  `MerchantKey`. Build merchant keys with `StatementText.MerchantKey` (counterparty, else the
  title's first `/` segment), which truncates to `CategoryRule.MaxMerchantKeyLength`.
- **Search:** when grepping this module, exclude `bin`/`obj` (see the global
  `dotnet-source-search` skill) or results repeat from build copies.
