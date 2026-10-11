# Private S3 Image Storage Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Deliver issue #564: a tested, optional private S3 storage adapter and persistent local development setup for later recipe photo endpoints.

**Architecture:** DietPlanner.Application owns a small storage interface and a disposable read handle; DietPlanner.Infrastructure owns AWS SDK configuration, failure translation, and implementation. RustFS runs only when explicitly enabled locally and in isolated Testcontainers fixtures. Image authorization, processing, database references, and lifecycle belong to #565.

**Tech Stack:** .NET 10 / C# 14, AWS SDK for .NET v4 (`AWSSDK.S3` 4.0.104.2), existing xUnit v3 / Shouldly, generic `Testcontainers` 4.15.0 matching the existing PostgreSQL package, Docker Compose, `rustfs/rustfs:1.0.1`.

**Spec:** `docs/superpowers/specs/2026-10-10-private-s3-image-storage.md`; authoritative issue https://github.com/piotrkantorowicz/home-system/issues/564 and parent #563.

## Execution notes — 2026-10-11

- Tasks 1–4 are implemented, committed, and independently reviewed with no remaining findings. Task 1's null-bucket validation finding was fixed before its final review.
- Task 1: `0192b87`, correction `1a44a30`; final configuration regression 32 passed, ownership test 1 passed.
- Task 2: `20f070f`; configuration/SDK boundary suite 62 passed, Infrastructure build/format clean, isolated default-credential-chain failure self-check passed. Bound options use mutable setters under repository conventions; the original init-only sketch below is not the implementation.
- Task 3: `fe01a37`; real private-storage suite 15 passed, combined Storage namespace 77 passed, isolated resources removed. Readiness uses `/health/ready`; restart clients are rebuilt for Docker's current random host port.
- Task 4: `e46b565`; isolated Compose smoke passed private access, content type, put/read/delete, restart and force-recreation digest checks, and targeted teardown. The ephemeral pinned AWS CLI image was a verification tool only because host AWS CLI was unavailable.
- Task 5 verification and delivery preparation: `scripts/verify.sh --branch` exited 0, full backend build had zero warnings/errors, format was clean, and all 908 tests passed with none failed or skipped. Existing recipe route tests use the real host without image-storage overrides. Durable acceptance evidence and all implementation rulings are in `../reports/2026-10-10-private-s3-image-storage.md`.
- Final whole-branch review, push, PR creation, and board transition are pending. Delivery must use the repository PR template, `Refs #563`, and final `Closes #564`; no merge or production provisioning is authorized by this plan.

## Global Constraints

- Work only in `.worktrees/564-private-s3-image-storage`, branch `feat/564-private-s3-image-storage`; eventual PR targets `main`.
- “Existing app startup remains usable without image-storage configuration.” Disabled is the default; no bucket probe or credential resolution during disabled startup.
- “Credentials never enter frontend bundles, committed config, request URLs, or logs.” Use AWS credential resolution from environment/profile/workload identity; no credential properties in application options.
- “No generic asset platform.” One DietPlanner-owned adapter; no media module, provider factory, upload endpoints, new worker, or schema migration.
- “Local data survives service restart; CI fixtures isolate and clean test objects.” Development uses a named volume; test containers and buckets are independent of development data.
- “Production setup documents least-privilege access and TLS.” HTTP is permitted only in Development/Testing; require HTTPS in all other environments.
- One type per file, namespace matches folders, classes sealed by default, documented public APIs, warnings treated as errors.
- Every new async I/O method takes `CancellationToken ct` last and forwards it. Tests use `TestContext.Current.CancellationToken`; xUnit lifetime overrides necessarily follow framework signatures.
- Reuse module registration and test conventions. No SDK types in Application/Domain, no `ConfigureAwait(false)` in module code, no skipped tests, no AI attribution in commits/PRs.
- Every recipe image revision gets new keys from the future lifecycle before uploads; storage never creates keys or returns permanent URLs.

---

## Repository findings and file map

Current main has no storage port, AWS SDK package, or S3 service. Infrastructure DI is `InfrastructureDependencyInjection.AddDietPlannerInfrastructure` in `DietPlanner.Infrastructure/DependencyInjection.cs`; it already uses bound options and `ValidateOnStart`. Existing integration fixtures live in `DietPlanner.IntegrationTests/Infrastructure`; `DatabaseFixture` starts PostgreSQL and `DietPlannerWebApplicationFactory` accepts configuration overrides. Keep S3 fixture independent so existing API tests do not start another container.

Central versions live in `Directory.Packages.props`. Unit tests currently reference Application, not Infrastructure: place adapter/configuration checks in the integration project, including tests that need no Docker. Backend CI already runs all test projects with Docker; no new CI job is needed. `scripts/verify.sh --branch` broadens to the full solution when central package versions change.

| File (under `src/Modules/DietPlanner/` unless absolute repository-relative path shown) | Responsibility                                                                                 |
| -------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------- |
| `DietPlanner.Application/Storage/IImageObjectStorage.cs`                               | SDK-free put, open-read, delete contract                                                       |
| `DietPlanner.Application/Storage/ImageObjectRead.cs`                                   | Read stream with deterministic ownership of underlying provider response                       |
| `DietPlanner.Application/Storage/ImageStorageUnavailableException.cs`                  | Sanitized application failure contract for #565                                                |
| `DietPlanner.Infrastructure/Storage/ImageStorageOptions.cs`                            | Enabled, endpoint, region, bucket, addressing settings                                         |
| `DietPlanner.Infrastructure/Storage/S3ImageObjectStorage.cs`                           | Single S3 implementation and precise failure translation                                       |
| `DietPlanner.Infrastructure/Storage/UnavailableImageObjectStorage.cs`                  | Disabled storage behavior, no network access                                                   |
| `DietPlanner.Infrastructure/DependencyInjection.cs`                                    | Options binding, validation, singleton client/storage registration                             |
| `DietPlanner.Infrastructure/DietPlanner.Infrastructure.csproj`                         | AWS SDK dependency                                                                             |
| `DietPlanner.UnitTests/Application/Storage/ImageObjectReadTests.cs`                    | Ownership/disposal check                                                                       |
| `DietPlanner.IntegrationTests/Storage/ImageStorageConfigurationTests.cs`               | DI, disabled configuration, invalid configuration, cancellation/error contracts                |
| `DietPlanner.IntegrationTests/Storage/S3ImageObjectStorageBoundaryTests.cs`            | Request mapping, precise failures, cancellation, validation and ownership at SDK boundary      |
| `DietPlanner.IntegrationTests/Storage/ImageStorageS3Proxy.cs`                          | Standard-library DispatchProxy for SDK boundary tests; inheritance required by generated proxy |
| `DietPlanner.IntegrationTests/Storage/S3ImageObjectStorageTests.cs`                    | Real private-store round trip and failure cases                                                |
| `DietPlanner.IntegrationTests/Infrastructure/ImageStorageFixture.cs`                   | Pinned RustFS container, private buckets, isolated keys, resource cleanup                      |
| `DietPlanner.IntegrationTests/Infrastructure/ImageStorageCollectionDefinition.cs`      | Serialize only tests sharing the S3 fixture                                                    |
| `DietPlanner.IntegrationTests/DietPlanner.IntegrationTests.csproj`                     | Explicit generic Testcontainers dependency                                                     |
| `Directory.Packages.props`                                                             | Pin SDK and generic Testcontainers versions                                                    |
| `infrastructure/docker-compose.image-storage.yml`                                      | Explicit opt-in RustFS override and persistent named volume                                    |
| `infrastructure/.env.example`                                                          | Empty credential variables and nonsecret defaults                                              |
| `infrastructure/README.md`                                                             | Setup, private bucket provisioning, restart check, production/recovery runbook                 |
| `src/Apis/HomeSystem.REST/appsettings.json`                                            | Disabled, credential-free configuration section                                                |

No changes to existing recipe callers, fixtures, API models, generated TypeScript, migrations, or exports. #565 becomes the first storage consumer. Do not move root checkout's untracked `docs/design/recipe-images/PLAN.md`; the spec above travels with this branch.

## Task 1: Define the storage boundary and optional configuration

**Files:** Create Application `Storage` files, Infrastructure `ImageStorageOptions.cs` and `UnavailableImageObjectStorage.cs`, both configuration/ownership test files. Modify Infrastructure DI and host `appsettings.json`.

**Interfaces:** Produces exactly these contracts; subsequent tasks use these names unchanged:

```csharp
namespace DietPlanner.Application.Storage;

public interface IImageObjectStorage
{
    Task PutAsync(string key, Stream content, string contentType, CancellationToken ct);
    Task<ImageObjectRead?> OpenReadAsync(string key, CancellationToken ct);
    Task DeleteAsync(string key, CancellationToken ct);
}
```

`PutAsync` leaves the caller's input stream open. Successful open-read transfers ownership to its result; missing object returns `null`. Delete is idempotent only for an absent object; other failures propagate as the storage failure contract. Caller-provided keys are internal opaque names, never filenames, identities, or URLs. R1 must create and persist each revision key before calling put; reusing a key is outside the contract.

- [x] **Step 1: Write the failing disabled-storage test.** Use the actual infrastructure registration with in-memory configuration; preserve the existing DI dependencies without starting hosted workers or contacting PostgreSQL.

```csharp
[Fact]
public async Task Put_WhenStorageDisabled_ReportsUnavailable()
{
    var configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DietPlanner"] = "Host=localhost;Database=unused",
            ["DietPlanner:ImageStorage:Enabled"] = "false",
        }).Build();
    var services = new ServiceCollection();
    services.AddLogging();
    services.AddSingleton<IConfiguration>(configuration);
    services.AddSingleton<IHostEnvironment>(new HostingEnvironment
    {
        EnvironmentName = Environments.Development,
    });
    services.AddDietPlannerInfrastructure(configuration);
    await using var provider = services.BuildServiceProvider();
    var storage = provider.GetRequiredService<IImageObjectStorage>();
    await using var content = new MemoryStream([1, 2, 3]);

    var failure = await Should.ThrowAsync<ImageStorageUnavailableException>(
        () => storage.PutAsync("recipe-images/disabled/detail", content,
            "image/jpeg", TestContext.Current.CancellationToken));
    failure.Message.ShouldBe("Recipe image storage is unavailable.");
}
```

Use `Microsoft.Extensions.Hosting.Internal.HostingEnvironment` for this test-only instance. Add separate open-read/delete disabled tests and pre-canceled-token tests; canceled calls must throw `OperationCanceledException`, not unavailable. Add a theory for missing bucket/region when enabled, malformed endpoint, endpoint userinfo/query/fragment, and non-HTTPS endpoint outside Development/Testing. Disabled incomplete options must validate successfully.

- [x] **Step 2: Run the focused test and confirm failure is caused by the absent contract.**

```bash
dotnet test --project src/Modules/DietPlanner/DietPlanner.IntegrationTests/DietPlanner.IntegrationTests.csproj --filter 'FullyQualifiedName~ImageStorageConfigurationTests'
```

- [x] **Step 3: Implement the boundary and disabled behavior.** Use this ownership shape; public members receive XML documentation in implementation:

```csharp
public sealed class ImageObjectRead(Stream content, IDisposable owner) : IDisposable
{
    public Stream Content { get; } = content;
    public void Dispose() => owner.Dispose();
}

public sealed class ImageStorageUnavailableException()
    : Exception("Recipe image storage is unavailable.");
```

Place each type in its own file. The owner must own and dispose Content; for S3 it is `GetObjectResponse`. A unit test constructs `ImageObjectRead` with a `MemoryStream` used as both content and owner, disposes the handle, and asserts the stream is closed. No general stream wrapper or buffered download is needed.

`UnavailableImageObjectStorage` checks `ct.ThrowIfCancellationRequested()` then throws the sanitized exception for all three methods. No AWS client is registered/resolved while disabled. Do not attach provider exceptions as `InnerException`: unhandled exception logging can otherwise expose request details or keys.

Bind these options under `DietPlanner:ImageStorage`:

```csharp
public sealed class ImageStorageOptions
{
    public const string SectionName = "DietPlanner:ImageStorage";
    public bool Enabled { get; init; }
    public string? Endpoint { get; init; }
    public string Region { get; init; } = "us-east-1";
    public string Bucket { get; init; } = "";
    public bool ForcePathStyle { get; init; }
}
```

Use existing `.BindConfiguration(...).Validate(...).ValidateOnStart()` pattern. All enabled-only checks short-circuit when disabled. Empty endpoint means normal AWS regional endpoint. A supplied endpoint must be an absolute HTTP(S) origin with no credentials, query, fragment, or non-root path. HTTPS is mandatory outside Development/Testing. Require a nonempty region and valid DNS-style bucket name when enabled. Validation messages name fields and constraints, never echo values.

Use existing `IHostEnvironment` from the host to validate transport; test configuration explicitly supplies it. Keep DI registration inside existing module registration, with disabled branch registered as singleton `IImageObjectStorage`. Task 2 replaces the enabled branch with real adapter/client registration.

Host configuration is credential-free and disabled:

```json
"DietPlanner": {
  "ImageStorage": {
    "Enabled": false,
    "Region": "us-east-1",
    "Bucket": "",
    "ForcePathStyle": false
  }
}
```

Merge this section with existing settings; do not replace unrelated values.

- [x] **Step 4: Run configuration and ownership tests.** Verify disabled registration has no SDK dependency or network probe. Exercise startup options validation by resolving `IStartupValidator` and calling `Validate()` in configuration tests.
- [x] **Step 5: Commit only this deliverable.** `feat(diet-planner): define optional image storage boundary`.

## Task 2: Implement one private S3 adapter with safe failure semantics

**Files:** Create `DietPlanner.Infrastructure/Storage/S3ImageObjectStorage.cs`; modify infrastructure DI, infrastructure project, central versions, and configuration tests.

**Interfaces:** Consumes `IImageObjectStorage`, `ImageObjectRead`, `ImageStorageUnavailableException`, `ImageStorageOptions` from Task 1. Produces `S3ImageObjectStorage(IAmazonS3 client, IOptions<ImageStorageOptions> options)` implementing the exact interface. Application has no Amazon namespace or dependency.

- [x] **Step 1: Pin dependencies and write failing adapter-contract checks.** Add central `AWSSDK.S3` version `4.0.104.2`, with a PackageReference only in Infrastructure. For pure SDK-boundary tests, use `DispatchProxy` from the standard library if necessary; prefer real fixture checks in Task 3 rather than adding a mocking dependency here. Configuration tests inspect a DI-resolved `IAmazonS3.Config` to prove endpoint, signing region, and addressing choices without sending requests.
- [x] **Step 2: Run focused configuration checks, observing the missing enabled client/adapter.** Same command as Task 1.
- [x] **Step 3: Register a DI-owned singleton AWS client and adapter only when enabled.**

```csharp
var clientConfig = new AmazonS3Config
{
    ForcePathStyle = options.ForcePathStyle,
    MaxErrorRetry = 2,
};
if (string.IsNullOrWhiteSpace(options.Endpoint))
    clientConfig.RegionEndpoint = RegionEndpoint.GetBySystemName(options.Region);
else
{
    clientConfig.ServiceURL = options.Endpoint;
    clientConfig.AuthenticationRegion = options.Region;
}
return new AmazonS3Client(clientConfig);
```

Use SDK's standard credentials chain; do not add options for secrets or construct production clients with local administrator credentials. Client construction occurs lazily on resolving enabled storage, not during disabled startup. SDK retries are bounded; do not add a second retry loop. Region must also be set for compatible endpoints, not inferred from URL.

Adapter operation bodies follow these shapes:

```csharp
await client.PutObjectAsync(new PutObjectRequest
{
    BucketName = bucket,
    Key = key,
    InputStream = content,
    ContentType = contentType,
    AutoCloseStream = false,
}, ct);

var response = await client.GetObjectAsync(new GetObjectRequest
{
    BucketName = bucket,
    Key = key,
}, ct);
return new ImageObjectRead(response.ResponseStream, response);

await client.DeleteObjectAsync(new DeleteObjectRequest
{
    BucketName = bucket,
    Key = key,
}, ct);
```

`bucket` is the bound options Bucket. Do not set a public ACL, retain uploads on disk, produce presigned URLs, list objects on request paths, or buffer downloads. Validate empty key, null/unreadable input stream, and empty content type before invoking SDK. Keys must be relative internal object names: reject absolute URLs, leading slash, and `.`/`..` segments; do not accept a browser-provided filename as a key. This validates the boundary without inventing an image-name parser or public key generator.

Do not use a `using` declaration for successful GetObjectResponse inside OpenReadAsync: that would close returned content. The result owns it until the caller finishes reading. On setup failure before ownership transfer, dispose the response.

Each method checks cancellation before I/O and forwards ct. Put does not close the input stream. Open-read returns null **only** for S3 `ErrorCode == "NoSuchKey"`; missing bucket, generic 404, invalid credentials, and permission denial must not masquerade as missing images. Delete treats `NoSuchKey` as success, but never `NoSuchBucket`.

Translate `AmazonS3Exception`, non-canceled `AmazonClientException`, and network `HttpRequestException` into `ImageStorageUnavailableException`. Preserve OperationCanceledException when ct is canceled. Do not swallow programmer exceptions. HTTP denial from storage represents server storage permissions/configuration failure, not application household authorization; do not turn it into a user-facing ForbiddenException. Do not log raw SDK exceptions; operational messages may report operation name and numeric provider status only, without credentials, endpoint URLs, keys, or raw provider response bodies.

- [x] **Step 4: Run focused configuration checks and build Infrastructure.**

```bash
dotnet build src/Modules/DietPlanner/DietPlanner.Infrastructure/DietPlanner.Infrastructure.csproj
dotnet test --project src/Modules/DietPlanner/DietPlanner.IntegrationTests/DietPlanner.IntegrationTests.csproj --filter 'FullyQualifiedName~ImageStorageConfigurationTests'
```

- [x] **Step 5: Commit.** `feat(diet-planner): add private S3 image storage adapter`.

## Task 3: Prove the adapter against isolated private storage

**Files:** Create S3 test class, fixture, and collection definition from file map. Add central `Testcontainers` version `4.15.0` and explicit reference in IntegrationTests. Do not change DatabaseFixture or start RustFS for existing recipe API tests.

**Interfaces:** Fixture exposes `Uri Endpoint`, `string Bucket`, `string AccessKey`, `string SecretKey`, and `IImageObjectStorage CreateStorage(string? bucket = null, bool invalidCredentials = false)`. It retains created `AmazonS3Client` instances and disposes them at teardown. It exposes `Task RestartAsync(CancellationToken ct)` for persistence proof. Each test chooses new keys with `Guid.CreateVersion7()` before writing.

- [x] **Step 1: Write the failing real-store round-trip test.**

```csharp
[Collection(ImageStorageCollectionDefinition.Name)]
public sealed class S3ImageObjectStorageTests(ImageStorageFixture fixture)
{
    [Fact]
    public async Task PutReadDelete_WithPrivateObject_RoundTripsAndRemoves()
    {
        var ct = TestContext.Current.CancellationToken;
        var storage = fixture.CreateStorage();
        var key = $"recipe-images/{Guid.CreateVersion7():N}/detail";
        byte[] expected = [1, 2, 3, 4];
        await using var input = new MemoryStream(expected);
        try
        {
            await storage.PutAsync(key, input, "image/jpeg", ct);
            input.CanRead.ShouldBeTrue();
            using (var read = await storage.OpenReadAsync(key, ct))
            {
                read.ShouldNotBeNull();
                await using var output = new MemoryStream();
                await read.Content.CopyToAsync(output, ct);
                output.ToArray().ShouldBe(expected);
            }
            await storage.DeleteAsync(key, ct);
            (await storage.OpenReadAsync(key, ct)).ShouldBeNull();
        }
        finally
        {
            await storage.DeleteAsync(key, ct);
        }
    }
}
```

- [x] **Step 2: Run the focused class and verify it fails because fixture is absent.**

```bash
dotnet test --project src/Modules/DietPlanner/DietPlanner.IntegrationTests/DietPlanner.IntegrationTests.csproj --filter 'FullyQualifiedName~S3ImageObjectStorageTests'
```

- [x] **Step 3: Implement fixture and collection.** Use generic Testcontainers with pinned image, generated test-only credentials, random host port mapped to container 9000, console disabled, and `/health/ready` HTTP readiness wait. `/health` reports liveness before the S3 engine is ready. Initial container shape:

```csharp
new ContainerBuilder("rustfs/rustfs:1.0.1")
    .WithPortBinding(9000, true)
    .WithEnvironment("RUSTFS_ACCESS_KEY", accessKey)
    .WithEnvironment("RUSTFS_SECRET_KEY", secretKey)
    .WithEnvironment("RUSTFS_CONSOLE_ENABLE", "false")
    .WithCommand("/data")
    .WithWaitStrategy(Wait.ForUnixContainer()
        .UntilHttpRequestIsSucceeded(request => request.ForPort(9000).ForPath("/health/ready")))
    .Build();
```

Use runtime-generated strings for `accessKey`/`secretKey`; never reuse developer credentials or include them in test failure messages. Compute Endpoint from container hostname/mapped port. Create a uniquely named bucket using a fixture-owned AWS client with path-style addressing; supply no public policy/ACL. Teardown deletes all test objects/bucket then disposes clients and container in finally, even when initialization fails. Testcontainers resource cleanup is the final safety net; no fixture accesses developer volumes or buckets.

Tests that stop/restart the shared container run in the same nonparallel collection. Use container StartAsync readiness or explicit SDK readiness retries bounded by cancellation, never fixed sleeps. Restart must preserve the same container and its `/data`; it must not silently create a fresh service.

- [x] **Step 4: Add and run each observable failure/ownership check.**

| Test                                                 | Setup and exact assertion                                                                                                  |
| ---------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------- |
| `OpenRead_WhenObjectMissing_ReturnsNull`             | Fresh valid bucket/key; result null                                                                                        |
| `Delete_WhenObjectMissing_SucceedsTwice`             | Delete same absent key twice; neither fails                                                                                |
| `OpenRead_WhenBucketMissing_ReportsUnavailable`      | Fresh nonexistent bucket; sanitized exception, not null                                                                    |
| `OpenRead_WhenUnauthenticated_DeniesObjectAccess`    | Upload valid object; bare HttpClient GET to path-style URL returns Forbidden/Unauthorized, never success; dispose response |
| `OpenRead_WhenCredentialsInvalid_ReportsUnavailable` | Existing object + wrong signing secret; sanitized exception; repeat put/delete denial as separate tests                    |
| `OpenRead_WhenStorageStopped_ReportsUnavailable`     | Stop fixture service; call storage; restart in finally; subsequent authenticated read succeeds                             |
| `Put_WhenTokenCanceled_DoesNotWriteObject`           | Pre-canceled token; OperationCanceledException; uncanceled read confirms null                                              |
| `OpenRead_WhenTokenCanceled_PropagatesCancellation`  | Pre-canceled token; OperationCanceledException                                                                             |
| `Delete_WhenTokenCanceled_LeavesObjectReadable`      | Upload, pre-cancel delete, then read with test token confirms original content                                             |
| `OpenRead_WhenDisposed_ReleasesStream`               | Read nonempty object, dispose handle; Content.CanRead false                                                                |
| `Put_WhenCallerStreamUsed_LeavesStreamOpen`          | Successful put leaves input CanRead true                                                                                   |
| `OpenRead_AfterRestart_ReturnsOriginalBytes`         | Upload, restart same service, compare original bytes, cleanup                                                              |

All keys unique; cleanup in finally uses live test cancellation token unless it has already been canceled, in which case fixture-wide teardown cleans remaining objects. Do not use anonymous success as a readiness check for an object. A denied read must hit an object known to exist.

- [x] **Step 5: Commit once focused checks pass.** `test(diet-planner): verify private S3 storage contract`.

## Task 4: Add opt-in persistent local storage and operational runbook

**Files:** Create `infrastructure/docker-compose.image-storage.yml`; modify `.env.example` and `README.md`. Reuse AWS CLI installed on the developer's machine for administrative setup; do not add another provisioner container, custom bootstrap app, or client SDK to frontend.

**Interfaces:** Produces local endpoint `http://127.0.0.1:9100`, bucket `home-system-recipe-images`, explicitly included development Compose override, named `recipe_image_data` volume. API credentials remain supplied separately through AWS environment variables/profile; provisioning credentials may create buckets but production application credentials may not.

- [x] **Step 1: Add explicit Compose override and credential-free examples.** File content:

```yaml
services:
  recipe-image-storage:
    image: rustfs/rustfs:1.0.1
    restart: unless-stopped
    ports:
      - "127.0.0.1:9100:9000"
    environment:
      RUSTFS_ACCESS_KEY: ${RECIPE_IMAGE_STORAGE_ACCESS_KEY:?Set local image-storage access key}
      RUSTFS_SECRET_KEY: ${RECIPE_IMAGE_STORAGE_SECRET_KEY:?Set local image-storage secret key}
      RUSTFS_CONSOLE_ENABLE: "false"
    command: ["/data"]
    volumes:
      - recipe_image_data:/data
    logging:
      driver: json-file
      options:
        max-size: "10m"
        max-file: "3"
    networks:
      - home-system-shared
volumes:
  recipe_image_data:
```

The existing base file provides `home-system-shared`. Use an explicitly included override because Compose interpolates required environment variables even for inactive profiles. Ordinary stack commands must never load image credentials or this override; enabling image storage with absent credentials must fail. YAML anchors cannot cross files, so keep the small logging block explicit and match the base file's actual rotation values when implementing.

Add only empty `RECIPE_IMAGE_STORAGE_ACCESS_KEY=` and `RECIPE_IMAGE_STORAGE_SECRET_KEY=` lines to `.env.example`. Explain that `.env` is gitignored and Docker interpolation does not configure API process credentials.

- [x] **Step 2: Document and run private bucket setup.** Supply credentials from shell environment/secure profile; do not put secrets in command arguments or print them. Commands operate only on this new development bucket:

```bash
docker compose -f docker-compose.yml -f docker-compose.image-storage.yml up -d recipe-image-storage
aws --endpoint-url http://127.0.0.1:9100 --region us-east-1 s3api create-bucket --bucket home-system-recipe-images
```

Treat an existing bucket as an inspection case, not a failure to ignore indiscriminately. Confirm it has no public policy/ACL and anonymous reads fail. API configuration uses `DietPlanner__ImageStorage__Enabled=true`, `Endpoint=http://127.0.0.1:9100`, `Region=us-east-1`, `Bucket=home-system-recipe-images`, `ForcePathStyle=true` under the same section; document full environment variable names. AWS_ACCESS_KEY_ID / AWS_SECRET_ACCESS_KEY are provided from secrets/profile, never appsettings. Do not modify shared machine-wide AWS configuration.

- [x] **Step 3: Verify opt-in isolation and restart persistence.** `docker compose config --quiet` must pass for ordinary stack without image configuration, using its existing infrastructure environment. Validate the merged image override with `docker compose -f docker-compose.yml -f docker-compose.image-storage.yml config --quiet`; use generated local credentials and avoid printing expanded config. With image credentials unset, the merged configuration must fail with the explicit missing-variable message. Upload a temporary test file under a unique smoke prefix, record SHA-256 locally, run `docker compose -f docker-compose.yml -f docker-compose.image-storage.yml restart recipe-image-storage`, wait for `/health/ready`, download and compare digest, then delete only that smoke object. Do not use `docker compose down -v`. Also recreate only this service with named volume preserved and repeat read proof. Record actual command results in PR.

- [x] **Step 4: Write concrete production and recovery sections.** Include:

1. Private bucket and workload-identity/secret setup; application policy allows only PutObject/GetObject/DeleteObject on `recipe-images/*`. Bucket creation, policy updates, backup, listing, and version administration belong to separate administrative credentials. If chosen provider requires ListBucket for precise NoSuchKey errors, allow only prefix-scoped listing after smoke-test proof; do not silently broaden permissions.
2. TLS verification enabled, endpoint/region/addressing examples, no public access/CORS/direct-browser URLs, no root/admin key in API, and numeric-status-only diagnostics. Store normal image ContentType but no source metadata or personal data in object keys.
3. Backup scope: database rows alone do not preserve photos. Coordinate a restore point by pausing image mutations/cleanup, snapshotting database and exporting objects with a manifest of referenced keys/versions and checksums, verifying both, then resuming. This slice has no image rows yet; document this as mandatory procedure for #565, not an executed full-stack recovery claim.
4. Retention tradeoff: versioning/retained delete markers increase cost but enable recovery; without versions, preserve full snapshot exports. Keep object recovery data at least as long as supported DB restore points plus cleanup grace. Never enable an age-based purge that can delete active image references.
5. Restore sequence: stop writes/cleanup, restore object bytes/required versions first, restore corresponding DB snapshot, audit every active reference against manifest, quarantine/clear missing references before enabling reads, then resume cleanup after checking pending leases. Do not purge objects referenced by another retained DB restore point. Account deletion remains #513.
6. Provider smoke matrix: exact region/signing/addressing, private anonymous denial, put/read/delete, content type, missing key vs missing bucket, restricted credentials, cancellation/network failure, and checksums/version backup restores. Run against a scratch bucket/prefix using chosen production provider before deployment; fixture success is insufficient.

- [x] **Step 5: Commit.** `feat(infra): add optional recipe image storage setup`.

## Task 5: Verify acceptance and prepare one reviewable PR

**Files:** Review only files listed above; include any changed generated/dependency artifacts only when justified by this slice. No recipe or product photo implementation.

- [x] **Step 1: Run focused configuration, ownership, and S3 tests after the complete change.** Retain actual test counts and failures. Check disabled host startup through the existing `DietPlannerWebApplicationFactory` with no storage settings; request an existing recipe route and verify unchanged behavior.
- [x] **Step 2: Inspect trust boundaries.** Confirm SDK references only Infrastructure/tests, secrets absent from committed configuration and plan implementation, input/read stream ownership correct, missing-bucket denial not swallowed, ct forwarded, and no public ACL or presigned URL calls. Confirm no bucket network call on default startup.
- [x] **Step 3: Run path-aware verification from this worktree.**

```bash
scripts/verify.sh --branch
```

Central package edits require full backend verification. This script does not validate Compose, bucket permissions, or persistence, so include Task 4 proofs separately. If Docker is unavailable, report integration tests unverified; do not claim the issue meets storage acceptance or ship it as complete.

- [ ] **Step 4: Review changed files against `docs/rules/definition-of-done.md` and relevant backend rules.** Follow repository `verify` and `ship` skills for delivery. Use Conventional Commits, one PR with `Closes #564`, parent `Refs #563`, actual check results, and explicit #565 follow-up for HTTP 503 mapping, normalization, concurrency, pending records, and durable cleanup. No merge or production provisioning occurs in this plan.

## Acceptance traceability

| Requirement                                                    | Evidence                                                                                |
| -------------------------------------------------------------- | --------------------------------------------------------------------------------------- |
| Private put/read/delete; unauthenticated denial                | Task 3 real-storage checks; Task 4 private provisioning                                 |
| Configurable endpoint/region/bucket/addressing; no SDK leakage | Tasks 1–2 contracts, config tests, reference inspection                                 |
| Secrets kept server-side; production TLS/least privilege       | Tasks 1–2 credential/config handling; Task 4 runbook                                    |
| Optional startup and unavailable operations                    | Task 1 disabled tests; Task 3 stopped-service checks; Task 5 existing-host regression   |
| Persistent local data; isolated clean CI fixtures              | Task 3 per-fixture service/bucket and teardown; Task 4 restart/recreate digest proof    |
| Coordinated backup/restore and retention                       | Task 4 concrete runbook; full metadata restore exercise explicitly #565                 |
| Production compatibility check                                 | Task 4 scratch-provider matrix; required before deployment, not local test substitution |

## Self-review and implementation risks

- This plan covers #564 only. No decoder dependency, user-facing endpoint, image worker, UI change, or cloud purchase.
- RustFS 1.0.1 is the selected local test service, not a production provider commitment. Verify image pull, readiness, SDK checksum behavior, private access, and named-volume permissions early; do not silently substitute an unpinned service.
- AWS SDK v4 and provider compatibility need real-service verification. Keep automatic checksum/signing defaults initially; change request settings only after a reproducible failing contract test and official provider evidence.
- Returning only a stream would hide response lifetime; the explicit read handle prevents early response disposal and avoids a general Stream subclass.
- Anonymous denial tests must operate on existing objects. Missing object errors depend on storage policy; distinct missing/denied behavior is part of deployment smoke tests.
- The R0 unavailable contract is not yet an HTTP response. R1 must add safe 503 mapping through the existing central exception pipeline before exposing image operations.
- Execution evidence is recorded above and in the durable report. Production-provider checks and full database/photo restore remain unexecuted deployment/#565 gates.

## Sources checked on 2026-10-10

- [AWS SDK v4 S3 configuration](https://docs.aws.amazon.com/sdkfornet/v4/apidocs/items/S3/TS3Config.html): endpoint and path-style configuration.
- [AWS SDK v4 retries/timeouts](https://docs.aws.amazon.com/sdk-for-net/v4/developer-guide/retries-timeouts.html): use SDK retry behavior with cancellation.
- [RustFS Docker setup](https://docs.rustfs.com/en/installation/container/docker): persistent volume, API port, credentials, health endpoint.
- [RustFS S3 protocol](https://docs.rustfs.com/en/administration/protocols/s3): S3 client integration.
- [RustFS 1.0.1 release](https://github.com/rustfs/rustfs/releases/tag/1.0.1): selected version.
- NuGet flat-container version indexes: `AWSSDK.S3` 4.0.104.2 exists; `Testcontainers` 4.15.0 matches current repository PostgreSQL package. Pin exact package versions rather than latest ranges.
