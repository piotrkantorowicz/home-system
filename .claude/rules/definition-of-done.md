# Definition of Done

A change is done when **all** of the following hold. The `ship` skill refuses to open a PR
otherwise; the reviewer (agent or human) rejects a PR that skips one silently.

## Code

- [ ] Scope matches the issue. Nothing extra, nothing quietly dropped. Deferred parts are
      named in the PR under "Out of scope" with the follow-up issue number.
- [ ] Rule docs for the touched area were read and followed (see Quick Reference in
      `CLAUDE.md` / `AGENTS.md`). `review-arch` on the changed files reports no 🔴.
- [ ] No `TODO` / `FIXME` / commented-out code left behind. No `// eslint-disable` or
      `#pragma warning disable` without a `// REASON:` comment.
- [ ] Public API surface changed → OpenAPI regenerated (`npm run generate:api:<module>`)
      and the generated schema committed.
- [ ] Schema changed → migration committed (EF: `dotnet ef migrations add`; Dapper:
      numbered `.sql` under `Persistence/Migrations/`). Never edit a deployed migration.
- [ ] New user-facing text → both `en` and `pl` i18n keys.

## Tests

- [ ] Domain rule added or changed → unit test in `<Module>.UnitTests`.
- [ ] Endpoint / handler added or changed → integration test in `<Module>.IntegrationTests`.
- [ ] New UI flow → Playwright spec under `e2e/<module>/` plus a page object.
- [ ] `scripts/verify.sh --branch` is green. The PR body lists the commands run and the
      counts they produced — copied from the output, not typed from memory.

## Git

- [ ] Branch is `<type>/<issue>-<slug>`, based on current `origin/main`, rebased if stale.
- [ ] Every commit is Conventional Commits, one logical change each.
- [ ] PR title is a valid Conventional Commits subject (it becomes the squash commit).
- [ ] PR body follows `.github/pull_request_template.md` and ends with `Closes #<issue>`.
- [ ] No AI attribution anywhere (see Git Commit Policy in `CLAUDE.md`).

## What "verified" means

Only say a check passed if the command ran in this session and exited 0. If a check was
skipped (no Docker, no backend running), say so in the PR under Tests.
