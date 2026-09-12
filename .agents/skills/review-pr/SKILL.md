---
name: review-pr
description: "Review a pull request for architecture-rule violations, correctness bugs, missing tests and security issues; post inline comments plus one summary comment on GitHub. Never approves. Use when asked to review a PR — ideally in the tool that did NOT write it."
---

# review-pr

```
/review-pr <pr-number>
```

Advisory bot review. It **comments**; approval is the owner's.

## Steps

1. **Fetch.**
   ```bash
   gh pr view <n> --json number,title,body,headRefName,baseRefName,files,author
   gh pr diff <n>
   ```
   Read the linked issue (`Closes #…` in the body) to know what was promised.
2. **Check the contract first.**
   - PR title is a Conventional Commits subject; body has What / Why / Tests / `Closes #`.
   - Tests section lists real commands + counts. If it says "all tests pass" with no
     numbers, that is a finding.
   - Scope vs issue: anything promised and missing, anything added and unasked.
3. **Architecture.** Run the `review-arch` checklist on every changed `.cs` / `.ts(x)` file
   (read the rule docs it names). 🔴 findings block; 🟡 are comments.
4. **Correctness.** For each changed handler / aggregate / component ask: what input breaks
   it? Off-by-one on dates, missing `ct`, unawaited task, N+1 in a query, nullable flow,
   missing `AsNoTracking()`, stale TanStack Query key, i18n key added in `en` only, race in
   `useEffect`. Report only what you can point to a line for.
5. **Tests.** Per `definition-of-done.md`: new domain rule without a unit test, new endpoint
   without an integration test, new UI flow without a Playwright spec. Check that tests
   assert behaviour, not implementation.
6. **Security.** Auth on every new endpoint group (`RequireAuthorization()`), user id from
   claims not from the request body, household/role checks on shared resources, no secrets
   in code, no raw SQL string concatenation.
7. **Post.** One review with inline comments (`event: COMMENT`) — each comment:
   *problem → why it matters → concrete fix*. One line per finding, no praise.
   ```bash
   gh api repos/{owner}/{repo}/pulls/<n>/reviews --input review.json
   ```
   `review.json`:
   ```json
   { "event": "COMMENT", "body": "<summary>", "comments": [ { "path": "...", "line": 42, "side": "RIGHT", "body": "..." } ] }
   ```
   Summary body = severity-ordered list (🔴 blocking / 🟡 should fix / 🔵 nit) + the
   contract check result + "Reviewed by <Claude Code | Codex> — advisory, not an approval."
8. Print the counts and the review URL.

## Rules

- Never `gh pr review --approve`. Never merge.
- Never comment on formatting that `dotnet format` / Prettier own.
- If the PR was written in this same tool, say so and suggest running the review in the
  other one — then review anyway if the user insists.
