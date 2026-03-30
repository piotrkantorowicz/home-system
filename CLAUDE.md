# CLAUDE.md

> **Entry point for Claude Code.** This file is the root rules document.
> All referenced files below are authoritative — read the relevant one before generating or editing code.

---

## Git Commit Policy

Do **not** add `Co-Authored-By: Claude` trailers to commits. Claude's contributions are tracked via the GitHub issue (piotrkantorowicz/home-system#24) and are not attributed in commit metadata.

---

## Quick Reference

| I'm working on… | Read this |
|---|---|
| C# naming, nullability, style | `.claude/rules/backend-coding-standards.md` |
| Module / folder structure | `.claude/rules/backend-module-structure.md` |
| Aggregates, Entities, Value Objects | `.claude/rules/backend-ddd-patterns.md` |
| Commands, Queries, Mapping | `.claude/rules/backend-cqrs-patterns.md` |
| EF Core, DbContext, Migrations | `.claude/rules/backend-ef-core-patterns.md` |
| Cross-module integration, RabbitMQ | `.claude/rules/backend-integration-patterns.md` |
| API endpoints, request/response | `.claude/rules/backend-api-patterns.md` |
| Backend unit & integration tests | `.claude/rules/backend-testing-standards.md` |
| React + TypeScript coding standards | `.claude/rules/frontend-react-typescript.md` |
| Frontend architecture & file structure | `.claude/rules/frontend-architecture.md` |
| Tailwind CSS v4 styling | `.claude/rules/frontend-styling.md` |
| Vitest + Testing Library | `.claude/rules/frontend-testing.md` |
| Playwright E2E testing | `.claude/rules/frontend-playwright.md` |
| Frontend performance | `.claude/rules/frontend-performance.md` |
| ESLint + Prettier + Husky | `.claude/rules/frontend-tooling.md` |
| Git workflow, branching, commits | `.claude/rules/git-workflow.md` |
| CQRS dispatcher full source | `.claude/skills/backend-cqrs.md` |
| RabbitMQ messaging full source | `.claude/skills/backend-messaging.md` |

---

## Slash Commands

| Command | What it does |
|---|---|
| `/scaffold-module` | Scaffold a new backend module (DDD or CRUD) |
| `/scaffold-aggregate` | Add a new aggregate root to an existing DDD module |
| `/scaffold-endpoint` | Add a new API endpoint with command or query |
| `/scaffold-feature` | Scaffold a new frontend feature module |
| `/scaffold-component` | Create a new shared UI component with tests |
| `/review-arch` | Review file(s) for architecture rule violations |

---

## Non-negotiable Rules (Always Apply)

1. **No MediatR. No AutoMapper. No MassTransit.** Use the custom dispatcher stack and raw RabbitMQ with explicit mapping.
2. **No cross-module domain imports.** Modules communicate through Contracts and integration events only.
3. **No public setters on aggregates or entities.** All mutations go through named methods.
4. **No returning domain objects from Application layer.** Always map to DTOs.
5. **Every async method propagates CancellationToken.** Parameter name: `ct`.
6. **One type per file. File name = type name. Namespace = folder path.**
7. **Queries bypass the repository.** Use `DbContext` with `AsNoTracking()` + `Select()`.
8. **All new classes are `sealed` by default** unless inheritance is explicitly needed.
9. **Dispatchers only in endpoints.** Never inject `ICommandHandler<,>` or `IQueryHandler<,>` directly.
10. **TypeScript `strict: true` always.** No `any` without a `// REASON:` comment.
11. **Named exports only** in frontend (exception: lazy-loaded page components).
12. **Conventional Commits** for all commit messages. Enforced by Husky + commitlint.
13. **Trunk-based workflow.** All branches merge to `main` via PR. No `develop` branch.

---

## Imported Rules

@.claude/rules/backend-coding-standards.md
@.claude/rules/backend-module-structure.md
@.claude/rules/backend-ddd-patterns.md
@.claude/rules/backend-cqrs-patterns.md
@.claude/rules/backend-ef-core-patterns.md
@.claude/rules/backend-integration-patterns.md
@.claude/rules/backend-api-patterns.md
@.claude/rules/backend-testing-standards.md
@.claude/rules/frontend-react-typescript.md
@.claude/rules/frontend-architecture.md
@.claude/rules/frontend-styling.md
@.claude/rules/frontend-testing.md
@.claude/rules/frontend-playwright.md
@.claude/rules/frontend-performance.md
@.claude/rules/frontend-tooling.md
@.claude/rules/git-workflow.md
@.claude/skills/backend-cqrs.md
@.claude/skills/backend-messaging.md
