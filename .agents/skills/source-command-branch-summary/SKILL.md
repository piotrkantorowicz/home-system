---
name: "source-command-branch-summary"
description: "Produce a commit-body-style summary of unmerged commits on the current branch"
---

# source-command-branch-summary

Use this skill when the user asks to run the migrated source command `branch-summary`.

## Command Template

Produce a concise commit-body-style summary of everything on the current branch that has not yet been merged into `main`.

## Instructions

1. Run the following commands to gather context:
   - `git log main..HEAD --oneline` — list all commits on this branch
   - `git diff main...HEAD --stat` — files changed with add/delete counts
   - `git diff main...HEAD` — full diff for detailed analysis

2. Analyse the diff thoroughly.

3. Output the summary in the exact format shown in the example below.

## Output format

One to three sentences describing what the branch does and why. No heading above it, just prose.

Changes:
- Add X (brief detail)
- Remove Y (reason)
- Fix Z (root cause → fix)
- Update A with B

## Format rules

- Start with a short prose paragraph (1–3 sentences). No `##` heading above it.
- Follow with a blank line, then `Changes:` (exact casing, colon).
- Each bullet starts with an imperative verb: Add, Remove, Fix, Update, Replace, Extract, etc.
- Keep each bullet to one line. Include a parenthetical for non-obvious detail.
- Group related bullets together — backend first, then frontend, then tests, then config.
- Aim for 8–15 bullets. Never list individual files; describe the logical change.
- Do **not** wrap the output in a code block.
- Output **only** the formatted text — no preamble, no commentary, nothing else.
