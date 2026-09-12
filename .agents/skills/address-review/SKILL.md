---
name: address-review
description: "Work through the unresolved review threads on a pull request: fix what is right, push back with reasons on what is not, reply in every thread, resolve the ones you changed. Use after a bot or human review lands on the PR."
---

# address-review

```
/address-review <pr-number>
```

## Steps

1. **Be on the PR branch** with a clean tree (`gh pr checkout <n>` if needed).
2. **Load unresolved threads** (GraphQL — REST cannot see resolution state):
   ```bash
   gh api graphql -f query='
     query($owner:String!,$repo:String!,$pr:Int!){
       repository(owner:$owner,name:$repo){ pullRequest(number:$pr){
         reviewThreads(first:100){ nodes{
           id isResolved isOutdated path line
           comments(first:20){ nodes{ id databaseId author{login} body createdAt } } } } } } }' \
     -F owner=:owner -F repo=:repo -F pr=<n>
   ```
   Also read top-level review bodies: `gh pr view <n> --json reviews`.
3. **Triage each unresolved thread** into one of:
   - **Fix** — the comment is right. Change the code.
   - **Push back** — the comment is wrong or out of scope. Reply with the reason and, if
     out of scope, the follow-up issue number. Do not resolve; the reviewer does.
   - **Ask** — genuinely ambiguous. Reply with the question. Do not resolve.
4. **Fix commits.** One commit per logical fix, Conventional Commits, e.g.
   `fix(household): guard empty member list in shopping aggregation`. Never squash /
   force-push a reviewed branch — reviewers lose their anchors.
5. **Verify** (`/verify`) then `git push`.
6. **Reply in every thread** you touched, one or two lines: what changed + commit sha, or
   why not.
   ```bash
   gh api repos/{owner}/{repo}/pulls/<n>/comments/<databaseId>/replies -f body='…'
   ```
7. **Resolve only the threads you fixed:**
   ```bash
   gh api graphql -f query='mutation($id:ID!){ resolveReviewThread(input:{threadId:$id}){ thread{ isResolved } } }' -F id=<threadId>
   ```
8. **Summary comment** on the PR: fixed N / pushed back M / questions K, with the commit
   range. Then print the same to the user.

## Rules

- Address every unresolved thread — silence reads as ignored.
- A reviewer's suggestion that violates a rule doc is a push-back, cite the rule.
