---
name: review-arch
description: Review a file or directory for violations of this repo's backend (DDD/CQRS/EF) and frontend (React/TS/Tailwind) architecture rules. Use when asked to review architecture, check rule compliance, or audit a slice before a PR.
---

# review-arch

Review the specified file or directory for architecture rule violations.

## Invocation

```
$review-arch [file or directory path]
```

If no path is given, review the file the user currently has open (ask if unknown).

## Instructions

Read all of the following before reviewing:

**For backend (.cs) files:**
- `docs/rules/backend-module-structure.md`
- `docs/rules/backend-coding-standards.md`
- `docs/rules/backend-ddd-patterns.md`
- `docs/rules/backend-cqrs-patterns.md`
- `docs/rules/backend-integration-patterns.md`

**For frontend (.ts/.tsx) files:**
- `docs/rules/frontend-architecture.md`
- `docs/rules/frontend-react-typescript.md`
- `docs/rules/frontend-styling.md`

Then scan the target file(s) for **violations** in these categories. Report each finding with:
- **Severity**: 🔴 Critical | 🟡 Warning | 🔵 Suggestion
- **Location**: file name + line number
- **Issue**: what rule is violated
- **Fix**: the corrected code snippet

### Backend Check List

**Cross-module coupling** 🔴
- Importing another module's Domain or Infrastructure project
- Injecting another module's repository or DbContext
- Using another module's EF entity type directly

**DDD violations** 🔴
- Public setters on aggregate root or entity properties
- Missing private parameterless constructor (EF Core)
- Domain objects returned from Application layer (should be DTOs)
- Direct bus publishing instead of outbox
- Missing domain events on state mutations

**CQRS violations** 🔴
- `ICommandHandler<,>` or `IQueryHandler<,>` injected directly (should use dispatcher)
- Query handler loading full aggregates (should use `AsNoTracking()` + `Select()`)
- Multiple `CommitAsync()` calls in one handler
- AutoMapper usage (should map manually)

**Coding standards** 🟡
- Non-sealed classes that should be sealed
- Missing `CancellationToken` propagation
- `.Result` / `.Wait()` / `.GetAwaiter().GetResult()` on tasks
- Magic strings or numbers
- `var` used where type isn't obvious
- `any` without `// REASON:` comment (TypeScript)

**Naming violations** 🟡
- File name doesn't match type name
- Namespace doesn't match folder path
- Async method missing `Async` suffix
- CancellationToken not named `ct`

### Frontend Check List

**Architecture violations** 🔴
- Feature importing from another feature's internals (not via `index.ts`)
- Shared component importing from a feature
- `utils/` importing React

**React violations** 🔴
- Class components (should be function components)
- Default exports on non-page components
- Side effects in render body
- Business logic inside JSX
- `any` type without justification
- `eslint-disable` without `// REASON:`

**State management** 🟡
- Prop drilling beyond 2 levels
- Missing error/loading states in data queries
- useEffect with suppressed exhaustive-deps

**Styling** 🟡
- Inline `style={{}}` for Tailwind-expressible values
- String interpolation in class names
- Hardcoded color values (should use design tokens)

### Output Format

```
## Review: [file/directory path]

### 🔴 Critical

1. **[Location]** — [Issue]
   Fix: [corrected code]

### 🟡 Warning

1. **[Location]** — [Issue]
   Fix: [corrected code]

### 🔵 Suggestion

1. **[Location]** — [Issue]

### ✅ No issues found in: [list clean files]
```

If no violations are found, say so explicitly.
