# Git Workflow

## Branching Strategy

This project uses a **simplified trunk-based workflow** built around a single long-lived branch: `main`.

```
main  ──────────────────────────────────────────────────► (production-ready at all times)
         ↑            ↑              ↑
   feat/add-login  fix/api-timeout  feat/export-csv
```

### Why not GitFlow?

A shared `develop` branch becomes a merge bottleneck. It leads to long-lived branches that drift, complex cherry-picks, and hard-to-resolve merge conflicts. Instead, **every feature branch is short-lived and merges directly into `main`**. Releases are controlled via tags, not branch topology.

### Long-lived branches

| Branch | Purpose |
|---|---|
| `main` | Always reflects production-ready state. Protected. Deployments and releases are tagged here. |

All other branches are **short-lived** and deleted after merge.

---

## Branch Naming

Every branch must correspond to a **GitHub Issue** (ticket). Branch names follow this pattern:

```
<type>/<issue-number>-short-description-in-kebab-case
```

### Types

| Type | When to use |
|---|---|
| `feat` | New feature or enhancement |
| `fix` | Bug fix |
| `hotfix` | Urgent production fix (see hotfix workflow) |
| `chore` | Maintenance, dependency updates, config changes |
| `refactor` | Code restructuring without behavior change |
| `docs` | Documentation only |
| `test` | Adding or updating tests |
| `epic` | Integration branch for a multi-PR epic — created by `start-epic`, never committed to directly (see `agent-workflow.md` § Epic lane) |

### Examples

```
feat/142-user-authentication
fix/87-null-reference-on-payment
hotfix/201-token-expiry-crash
chore/115-update-nuget-packages
refactor/99-extract-email-service
```

### Rules

- Use **kebab-case** only — no underscores, no uppercase
- Keep descriptions **concise but meaningful**
- Always include the **issue number** — links the branch to the ticket automatically
- No branches without a corresponding GitHub Issue — open the ticket first

---

## Commit Conventions

This project follows the [Conventional Commits](https://www.conventionalcommits.org/) specification.

### Format

```
<type>(<scope>): <short description>

[optional body]

[optional footer(s)]
```

### Types

| Type | Description |
|---|---|
| `feat` | A new feature |
| `fix` | A bug fix |
| `docs` | Documentation changes only |
| `style` | Formatting, missing semicolons, etc. (no logic change) |
| `refactor` | Code change that neither fixes a bug nor adds a feature |
| `perf` | Performance improvement |
| `test` | Adding or correcting tests |
| `chore` | Build process, tooling, dependencies |
| `ci` | CI/CD configuration changes |
| `revert` | Reverts a previous commit |

### Rules

- Use **imperative mood**: *"add login"* not *"added login"* or *"adds login"*
- Keep the first line **under 72 characters**
- Do **not** end the description with a period
- Reference the issue in the footer: `Closes #142` or `Refs #87`
- Breaking changes must include `BREAKING CHANGE:` in the footer
- Each commit = **one logical change**. If you're writing "and", split it.

### Good examples

```
feat(auth): add OAuth2 login with GitHub provider

Implements OAuth2 authorization code flow using IdentityServer.
Adds callback endpoint and session persistence.

Closes #142
```

```
fix(api): return 404 instead of 500 for missing resource

Previously, accessing a non-existent resource ID caused an unhandled
NullReferenceException. Guard clause added in the service layer.

Refs #87
```

```
feat(exports)!: change CSV export format to include headers

BREAKING CHANGE: CSV files now include a header row by default.
Consumers relying on positional parsing must update their parsers.

Closes #201
```

---

## Pull Requests

### Every branch merges via PR — no direct pushes to `main`

- Open a PR as soon as the branch is ready for review (or earlier as Draft)
- Link the PR to its GitHub Issue using `Closes #<issue-number>`
- A PR = **complete, independently deployable unit of work**

### If the ticket is large

Split into multiple independent PRs:

1. Break work into vertical slices (data layer → service → API → UI)
2. Each slice gets its own branch and PR
3. PRs can be merged independently
4. Use feature flags to hide incomplete features in production

### PR checklist

- [ ] Branch name follows naming convention
- [ ] All commits follow Conventional Commits format
- [ ] PR description explains *what* and *why*, not just *what*
- [ ] Linked to a GitHub Issue
- [ ] Tests added or updated
- [ ] No unresolved TODO comments left behind
- [ ] CI passes

### Merge strategy

- **Feature branches → `main` or → `epic/*`:** **Squash and Merge.** One Conventional
  Commit per PR; the PR title is the subject.
- **`epic/*` → `main`:** **Rebase and Merge.** The epic branch already holds one squashed
  commit per child PR; rebasing replays them onto `main` unchanged, so history stays linear
  and the release tooling sees every child. Never squash an epic PR.
- Merge commits are disabled in the repository settings. Rebase and merge is enabled
  only for the epic case — `guard-git.sh` refuses it for any other head.

> **Squash + commit body:** When a branch is squashed, GitHub builds the squashed commit
> message from the PR title (subject) and PR description (body). Per-commit conventional
> messages on the branch get folded into a list, so detailed multi-paragraph commit bodies
> like the examples above are most useful for **PR descriptions**, not for individual
> commits. Keep individual commits focused; put the rationale in the PR.

---

## Release Workflow

Releases are **tags on `main`**, created automatically by `.github/workflows/release.yml`
(semantic-release, config in `release.config.js`) after every push to `main`.

```
main  ──●──────●──────●──────●──────►
        │      │      │      │
       v1.0   v1.1   v1.2   v2.0
```

The bump is computed from the Conventional Commits since the previous tag:

| Commit type | Version bump |
|---|---|
| `feat` | Minor (`v1.3.0`) |
| `fix`, `perf`, `refactor`, `hotfix`, `revert` | Patch (`v1.2.1`) |
| `type!` or `BREAKING CHANGE:` footer | Major (`v2.0.0`) |
| `docs`, `style`, `test`, `chore`, `ci`, `build` | none — no release |

What a release produces: the tag `vX.Y.Z`, a GitHub Release with generated notes grouped
by type, and a "released in vX.Y.Z" comment on each closed issue / merged PR. Nothing is
committed back to the repository — no `CHANGELOG.md`, no version bump in `package.json`
or `Directory.Build.props`. The tag is the version; a deploy pipeline reads it.

Because the squash commit's subject is the PR title, **the PR title decides the bump**.
`feat(x): …` on a PR that only refactors ships a minor release; label the work honestly.

Preview the next version locally:

```bash
GITHUB_TOKEN=$(gh auth token) npm run release:preview
```

## Hotfix Workflow

A hotfix is a **temporary mitigation** for an urgent production issue. It ships fast, but it is **not a substitute for a proper fix**. Every hotfix must be followed up with a real solution in the normal development cycle.

```
main ──●──────────────────●──────────────────●──────►
      v1.2              v1.2.1              v1.3.0
        \                ↑                    ↑
         hotfix/201-fix  /         feat/210-proper-fix
          ──────────────              (next release)
```

### Steps

1. **Open two GitHub Issues** before touching any code:
   - One for the hotfix itself (label: `hotfix`) — e.g. *#201 Token expiry crash in production*
   - One for the proper follow-up fix (label: `tech-debt`) — e.g. *#210 Redesign token refresh flow*
   - Link them: mention `Follow-up: #210` in issue #201

2. Branch from `main`:

```bash
git checkout main
git pull
git checkout -b hotfix/201-token-expiry-crash
```

3. Implement the **minimal fix**. The commit message must reference both issues:

```
fix(auth): guard against expired token during refresh

Temporary mitigation — catches the null ref and returns 401.
This addresses the symptom only; proper fix tracked in #210.

Closes #201
```

4. Open a PR targeting `main`. The PR description must include:

```markdown
## What
Short description of the production issue and what this patch does.

## ⚠️ Temporary fix
This is a mitigation only. It does not address the root cause.
Follow-up: #210
```

5. After merge, `release.yml` tags the patch release (`hotfix` → patch) — no manual tag.

6. Deploy from the tag.

7. **Immediately schedule the follow-up issue** (#210) into the next sprint or milestone. Do not leave it in the backlog without a milestone — unscheduled tech-debt tickets disappear.

### Hotfix Rules

- A hotfix branch **must never contain refactoring** — change only what stops the bleeding
- If the proper fix is straightforward and low-risk, skip the hotfix and do a fast-tracked `fix/` branch
- The follow-up `feat/` or `refactor/` branch must reference the hotfix PR for traceability
- Follow-up issue must have a **milestone** before the hotfix PR is merged

---

## Enforcing Conventions — Husky + commitlint

### Initial Setup

```bash
npm install --save-dev husky @commitlint/cli @commitlint/config-conventional lint-staged
npx husky init
```

### commitlint config

Create `commitlint.config.js` in the project root:

```js
export default {
  extends: ['@commitlint/config-conventional'],
  rules: {
    'type-enum': [
      2,
      'always',
      ['feat', 'fix', 'docs', 'style', 'refactor', 'perf', 'test', 'chore', 'ci', 'revert', 'hotfix']
    ],
    'subject-case': [2, 'always', 'lower-case'],
    'header-max-length': [2, 'always', 72],
  },
};
```

### Husky Hooks

**Commit message validation** (`.husky/commit-msg`):

```bash
npx --no -- commitlint --edit $1
```

**Pre-commit lint** (`.husky/pre-commit`):

```bash
npx lint-staged
```

**Branch name enforcement** (`.husky/pre-push`):

```bash
branch=$(git rev-parse --abbrev-ref HEAD)
pattern="^(feat|fix|hotfix|chore|refactor|docs|test)\/[0-9]+-[a-z0-9-]+$"

if [[ "$branch" == "main" ]]; then
  exit 0
fi

if ! [[ "$branch" =~ $pattern ]]; then
  echo "❌ Branch name '$branch' does not follow naming convention."
  echo "   Expected: <type>/<issue-number>-short-description"
  echo "   Example:  feat/142-user-authentication"
  exit 1
fi
```

### lint-staged config

In `package.json`:

```json
{
  "lint-staged": {
    "*.{ts,tsx}": ["eslint --fix", "prettier --write"],
    "*.{cs}": ["dotnet format --include"],
    "*.{json,css,md}": ["prettier --write"]
  }
}
```

### CI enforcement

Add to GitHub Actions so hooks cannot be bypassed:

```yaml
# .github/workflows/lint-commits.yml
name: Lint Commits

on:
  pull_request:
    branches: [main]

jobs:
  commitlint:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
        with:
          fetch-depth: 0
      - uses: actions/setup-node@v4
        with:
          node-version: '22'
      - run: npm ci
      - run: npx commitlint --from ${{ github.event.pull_request.base.sha }} --to ${{ github.event.pull_request.head.sha }} --verbose
```

### GitHub Branch Protection (Settings → Branches → main)

- ✅ Require pull request before merging
- ✅ Require status checks to pass (CI, commitlint)
- ✅ Require branches to be up to date before merging
- ✅ Do not allow bypassing the above settings

---

## Daily Workflow — Quick Reference

```bash
# 1. Open a GitHub Issue first
# 2. Create branch from main
git checkout main && git pull
git checkout -b feat/142-user-authentication

# 3. Work in small commits
git add -p   # stage selectively
git commit -m "feat(auth): add login endpoint"
git commit -m "feat(auth): add token refresh logic"

# 4. Push and open PR → main
git push -u origin feat/142-user-authentication
# Open PR on GitHub, link: "Closes #142"

# 5. After merge, clean up
git checkout main && git pull
git branch -d feat/142-user-authentication
```
