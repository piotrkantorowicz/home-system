---
name: github-code-scanning-alerts
description: Use when checking GitHub code scanning or CodeQL alerts.
category: github-workflow
---

# GitHub code scanning alerts

Try the API first. If the token lacks access, read the Security tab in the user's Chrome browser. Do not stop at the gh error.

1. `gh api repos/{owner}/{repo}/code-scanning/alerts --paginate`, keeping `state=="open"`. `{owner}` and `{repo}` are gh placeholders.
2. On 403 or "Resource not accessible": see the `github-cli-token-scopes` skill. Do not loop on `gh auth refresh`.
3. Browser fallback, using the Claude in Chrome tools:
   - Navigate a new tab to `https://github.com/{owner}/{repo}/security/code-scanning?query=is%3Aopen`. `get_page_text` fails on `chrome://newtab`, so navigate first.
   - `get_page_text` shows only page 1. Count across pages with `javascript_tool`: click Next, collect `#N` rows from `main` innerText, and group by rule plus severity.
   - If the page shows "Loading", wait and retry.
4. Close tabs you opened.
5. Report open and closed totals, then group by rule. Flag the few that are real (Dispose, using) versus style notes (Where, Select, ref counts, upcast noise). Offer fixes and wait for a go-ahead before branching or editing.
