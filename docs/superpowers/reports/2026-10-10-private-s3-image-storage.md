# Private S3 image storage — acceptance and delivery preparation

Issue: [#564](https://github.com/piotrkantorowicz/home-system/issues/564).
Parent: [#563](https://github.com/piotrkantorowicz/home-system/issues/563).
Branch: `feat/564-private-s3-image-storage`. Evidence recorded 2026-10-11.

## Scope and status

Tasks 1–4 are committed and independently reviewed with no remaining findings:
`0192b87` / null-bucket correction `1a44a30`, `20f070f`, `fe01a37`, and
`e46b565`. This slice adds one DietPlanner-owned SDK-free storage port, streaming
read ownership, disabled behavior, the private S3 adapter, isolated real-service
tests, an explicit persistent local Compose override, and the operational runbook.
The [spec](../specs/2026-10-10-private-s3-image-storage.md) is unchanged;
the [plan](../plans/2026-10-10-private-s3-image-storage.md) records execution.

Task 5 verification, delivery preparation, and final whole-branch architecture/
quality review are complete. Final review of `4f84795` found no Critical, Important,
or Minor issues and no architecture violations. Push, PR creation, and board
transition are the remaining delivery steps at the time this report is committed.
The PR must follow the repository template, reference `Refs #563`, and end with
`Closes #564`. No merge, production provisioning, or cloud purchase occurred.

## Acceptance evidence

| Requirement                                                     | Actual evidence                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                |
| --------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Private put/read/delete and denied access                       | Task 3 real RustFS suite: 15 passed; known existing objects deny anonymous and invalid-credential reads; invalid put/delete cannot mutate objects. Missing key, idempotent delete, missing bucket, outage recovery, cancellation, ownership, and same-service restart are covered.                                                                                                                                                                                                                                                                                                                             |
| Configurable endpoint/region/bucket/addressing and SDK boundary | Task 2 configuration/SDK boundary suite: 62 passed; Task 3 combined Storage namespace: 77 passed. Exact request/config mapping and safe validation are asserted. AWSSDK.S3 4.0.104.2 is referenced only by Infrastructure; SDK types occur only there and in tests, never Domain/Application contracts. Testcontainers 4.15.0 is centrally pinned and referenced by IntegrationTests.                                                                                                                                                                                                                          |
| Optional startup and unchanged existing routes                  | `Validate_WhenConfigurationMissing_UsesDisabledStorage` validates startup without the storage section and asserts disabled storage plus no `IAmazonS3`. Existing `RecipeListFilterTests` boot `DietPlannerWebApplicationFactory` with no storage overrides, create recipes, and request `/api/v1/recipes`; these passed in the final real-host suite. DI inspection confirms disabled registration constructs no client or bucket probe.                                                                                                                                                                       |
| Secrets, TLS, ownership, cancellation, and precise failures     | Credential-free options/appsettings and empty Compose credential examples; standard SDK credential chain, disabled SDK logging, fixed-message exceptions without inner exceptions. Isolated missing-credentials self-check passed for put/read/delete. Production HTTP is rejected. Upload streams remain open; successful reads own the SDK response; canceled ownership transfer disposes it. All operations check and forward `ct`; only `NoSuchKey` is absence, while missing bucket/denial/outage remain unavailable. No public ACL, presigned URL, buffered download, or request-path listing was added. |
| Persistent local data and isolated clean fixtures               | Task 4 isolated Compose smoke exited 0: ordinary config passes without image credentials; merged config rejects each absent credential; only loopback S3 is exposed, console disabled, private bucket has no public policy/grants. Anonymous existing-object GET returned 403; signed bytes and `ContentType=image/png` matched. Restart retained container ID; force-recreation changed ID and retained the named volume. SHA-256 matched both reads. Targeted deletion returned subsequent 404; no scratch container/volume/network remained. Task 3 independently removed its generated containers/volumes. |
| Backup, retention, and provider readiness                       | [Infrastructure runbook](../../../infrastructure/README.md) documents TLS, restricted application credentials, separate administration, provider smoke matrix, coordinated DB/object manifests/checksums, object-first restore, active-reference audits, pending leases, and retention across supported restore points. These are procedures, not executed production/full-restore claims.                                                                                                                                                                                                                     |

Task 4 digest before/after restart/recreation:
`606e69b03f22142d3c189f252b53fe251cf4cd85101c888ee455d3dba5e83cd8`.
Its smoke command was `bash .superpowers/sdd/2026-10-10-private-s3-image-storage/task-4-smoke.sh`.
Host AWS CLI was unavailable; the already-pulled pinned
`public.ecr.aws/aws-cli/aws-cli:2.31.0` image was an ephemeral verification tool,
with generated credentials inherited by environment names and no secret output.

## Full backend verification

`scripts/verify.sh --branch` ran in this execution from the worktree after Task 3,
concurrently with Task 4's infrastructure/docs-only work, and exited 0.
Central package changes selected the full backend. Build had zero warnings/errors;
format was clean. The exact table from `final-verification.md` is:

| Check                             | Result | Tests passed |
| --------------------------------- | ------ | -----------: |
| dotnet build                      | Pass   |            — |
| dotnet format                     | Pass   |            — |
| Budget.IntegrationTests           | Pass   |          114 |
| Budget.UnitTests                  | Pass   |          109 |
| DietPlanner.IntegrationTests      | Pass   |          193 |
| DietPlanner.UnitTests             | Pass   |          256 |
| Household.IntegrationTests        | Pass   |           46 |
| Household.UnitTests               | Pass   |           73 |
| Notifications.IntegrationTests    | Pass   |           20 |
| Notifications.UnitTests           | Pass   |           44 |
| Shared.Messaging.IntegrationTests | Pass   |           20 |
| Shared.Messaging.Tests            | Pass   |           19 |
| Shared.Web.IntegrationTests       | Pass   |           14 |
| Total                             | Pass   |          908 |

No failed or skipped tests. Storage counts above are subsets/prior focused runs,
not additions to 908. Task 1's ownership test passed and remains in the final unit
suite. Passing suites were not repeated during documentation preparation.
Frontend/Playwright were not selected because no UI flow or HTTP route changed;
Compose/privacy/persistence were proved separately.

## Recorded rulings and costs if wrong

The execution ledger's complete ruling lines are retained verbatim, including
their stated rework costs:

- Ruling: Use mutable bound options properties as repository rules require, rather than plan's init-only example — binding and repository conventions take precedence — incorrect choice would require option/test adjustment.
- Ruling: Preserve startup when disabled; enabled registration comes in Task 2 and complete enabled validation can be deferred with it — no temporary fake enabled storage — incorrect staging would require one commit adjustment.
- Ruling: Superpowers helper scripts lack execute bits; use equivalent task extraction if necessary rather than modifying installed plugin permissions — same task requirements preserved — incorrect extraction would require refreshing brief.
- Task 1: Ruling: Commit-msg binary missing only in isolated worktree; after green unchanged staged precommit tree, run existing root commitlint directly then retry commit without rerunning completed hooks — preserves all hook checks while avoiding duplicate 862-test suite — wrong parity would require commit validation/rework.
- Task 3: Ruling: Replace plan's /health wait with /health/ready; observed /health responds before S3 engine is ready and official RustFS status docs distinguish liveness/readiness — use actual readiness rather than fixed sleeps or unnecessary retry layer — wrong readiness would require fixture adjustment; real operation checks remain decisive.
- Task 3: Ruling: Rebuild fixture SDK clients after Docker restart because dynamically published host port changed while container/data stayed unchanged — fixture endpoint must reflect actual port; production/local Compose uses fixed endpoint — wrong assumption would require fixture corrections, not adapter changes.
- Ruling: Intermediate implementation commits use focused tests, scoped format, diff checks, and explicit commitlint; final scripts/verify.sh --branch runs complete required backend gates before delivery — avoids repeated full-suite hooks for each staged task — wrong coverage would require rerunning affected suites before shipping.

## Definition of Done walkthrough

| Checklist item                  | Assessment                                                                                                                                                                                                                                                                                   |
| ------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Issue scope and named deferrals | #564 storage/setup scope met. #565 owns HTTP 503 mapping before exposure, authorization, decoding/normalization, immutable revision/pending records, concurrency, and durable cleanup. UI #566, thumbnails #567, lifecycle/access E2E #568, account deletion #513 remain outside this slice. |
| Area rules and architecture     | Root AGENTS and backend coding/module/DDD/CQRS/integration/testing rules read; source inspection found no cross-module or SDK boundary violation. Task reviews 1–4 and final whole-branch architecture/quality review are clean.                                                             |
| No unfinished code/suppression  | Changed implementation files contain no TODO/FIXME, commented-out code, or unexplained warning suppression. The test proxy is intentionally inheritable for DispatchProxy; xUnit lifetime signatures are framework-defined.                                                                  |
| OpenAPI, migrations, i18n       | Not applicable: no HTTP API models/routes, database schema, or user-facing UI text changed.                                                                                                                                                                                                  |
| Domain/handler/UI coverage      | No domain rule, endpoint, handler, or UI flow changed. Storage logic has configuration/boundary/ownership tests and real S3 tests; existing recipe routes passed through the real host.                                                                                                      |
| Required verification           | Full path-aware backend gate passed 908 tests; focused Compose smoke, formatting, whitespace, and commitlint passed in the task execution.                                                                                                                                                   |
| Branch/base and logical commits | Correct issue branch; fresh origin/main check found behind 0/ahead 5 before this documentation commit, so no rebase was needed. Existing commits are focused Conventional Commits. Final shipping must confirm clean committed state.                                                        |
| PR title/body and attribution   | PR title/body/template and final commitlint are delivery checks; PR opening remains pending. Required issue lines are stated above. No assistant attribution is present in implementation commits or these documents.                                                                        |

## Remaining limits

Production provider selection/provisioning and its actual SDK/signing/checksum/
restricted-permission smoke matrix remain unexecuted and required before deployment.
Local RustFS success does not establish compatibility with a future provider.
Full database/photo restore cannot be exercised until #565 supplies image references
and lifecycle; the coordinated runbook is the #564 deliverable. This application
exception is not an HTTP 503 mapping yet. These limits are explicit follow-ups,
not substituted test claims.
