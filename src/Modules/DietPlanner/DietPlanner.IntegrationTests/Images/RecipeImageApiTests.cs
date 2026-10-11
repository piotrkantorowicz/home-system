namespace DietPlanner.IntegrationTests.Images;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using DietPlanner.Api;
using DietPlanner.Application.Images;
using DietPlanner.Application.Queries.SearchRecipes;
using DietPlanner.Application.Storage;
using DietPlanner.Domain.Ledgers;
using DietPlanner.Domain.ValueObjects;
using DietPlanner.Infrastructure.Images;
using DietPlanner.Infrastructure.Persistence;
using DietPlanner.IntegrationTests.Infrastructure;
using Household.Contracts.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

/// <summary>Real PostgreSQL/S3 API lifecycle, permissions, failures and cleanup races.</summary>
[Collection(DatabaseCollectionDefinition.Name)]
public sealed class RecipeImageApiTests(DatabaseFixture database, ImageStorageFixture objects)
    : IClassFixture<ImageStorageFixture>, IDisposable
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private readonly List<WebApplicationFactory<Program>> _hosts = [];
    private readonly PhotoTestHouseholds _households = new();

    /// <summary>Releases all per-test API hosts.</summary>
    public void Dispose()
    {
        foreach (WebApplicationFactory<Program> host in _hosts) host.Dispose();
    }

    private WebApplicationFactory<Program> Host(string user, IImageObjectStorage? storage = null, bool failCommit = false, bool anonymous = false)
    {
        var root = new DietPlannerWebApplicationFactory(database.ConnectionString, user);
        _hosts.Add(root);
        WebApplicationFactory<Program> host = root.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<IImageObjectStorage>();
            services.AddSingleton(storage ?? objects.CreateStorage());
            services.RemoveAll<IHouseholdQueryService>();
            services.AddSingleton<IHouseholdQueryService>(_households);
            if (anonymous)
                services.AddAuthentication("NoPhotoUser").AddScheme<AuthenticationSchemeOptions, PhotoAnonymousAuthHandler>("NoPhotoUser", _ => { });
            if (failCommit)
            {
                services.RemoveAll<DbContextOptions<DietPlannerDbContext>>();
                services.AddDbContext<DietPlannerDbContext>(options => options.UseNpgsql(database.ConnectionString)
                    .AddInterceptors(new PhotoCommitFailureInterceptor()));
            }
        }));
        _hosts.Add(host);
        return host;
    }

    private static async Task<Guid> CreateAsync(HttpClient client, string visibility = "Private")
    {
        HttpResponseMessage product = await client.PostAsJsonAsync("/api/v1/products",
            new CreateProductRequest($"ingredient-{Guid.CreateVersion7():N}", 100, 10, 10, 1, 1, "g", null, null), Ct);
        product.EnsureSuccessStatusCode();
        Guid productId = await product.Content.ReadFromJsonAsync<Guid>(Ct);
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/recipes",
            new CreateRecipeRequest($"photo-{Guid.CreateVersion7():N}", null, null, 1, null,
                [new RecipeIngredientRequest(productId, 100, "g")], visibility), Ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Guid>(Ct);
    }

    private static async Task<HttpResponseMessage> UploadAsync(HttpClient client, Guid id, byte[]? bytes = null)
    {
        using var form = new MultipartFormDataContent();
        using var file = new ByteArrayContent(bytes ?? RecipeImageProcessorTests.Photo());
        file.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        form.Add(file, "file", "photo.jpg");
        return await client.PutAsync($"/api/v1/recipes/{id}/image", form, Ct);
    }

    private static async Task<RecipeImageDto> UploadOkAsync(HttpClient client, Guid id)
    {
        HttpResponseMessage response = await UploadAsync(client, id);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<RecipeImageDto>(Ct))!;
    }

    private static async Task<RecipeDto> DetailAsync(HttpClient client, Guid id)
        => (await client.GetFromJsonAsync<RecipeDto>($"/api/v1/recipes/{id}", Ct))!;

    private static async Task CleanupAsync(WebApplicationFactory<Program> host)
    {
        await using AsyncServiceScope scope = host.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<RecipeImageCleanup>().RunOnceAsync(Ct);
    }

    /// <summary>Create remains compatible; replace/remove preserve recipe content and retire bytes.</summary>
    [Fact]
    public async Task Lifecycle_ValidUploadReplaceRemove_ReturnsPrivateNormalizedBytes()
    {
        string user = $"photo-{Guid.CreateVersion7():N}";
        WebApplicationFactory<Program> host = Host(user);
        using HttpClient client = host.CreateClient();
        Guid id = await CreateAsync(client);
        (await DetailAsync(client, id)).Image.ShouldBeNull();
        RecipeImageDto first = await UploadOkAsync(client, id);
        (await DetailAsync(client, id)).Image.ShouldBe(first);
        foreach (string suffix in new[] { "", "/thumbnail" })
        {
            using HttpResponseMessage image = await client.GetAsync($"/api/v1/recipes/{id}/image{suffix}", Ct);
            image.StatusCode.ShouldBe(HttpStatusCode.OK);
            image.Headers.CacheControl!.NoStore.ShouldBeTrue();
            image.Headers.CacheControl.Private.ShouldBeTrue();
            image.Content.Headers.ContentType!.MediaType.ShouldBe("image/jpeg");
            (await image.Content.ReadAsByteArrayAsync(Ct)).Length.ShouldBe(suffix.Length == 0 ? first.Size : first.ThumbnailSize);
        }
        string json = await client.GetStringAsync($"/api/v1/recipes?onlyMine=true", Ct);
        json.ShouldContain(first.Version.ToString());
        json.ShouldNotContain("recipe-images/");
        RecipeImageDto second = await UploadOkAsync(client, id);
        second.Version.ShouldNotBe(first.Version);
        await CleanupAsync(host);
        using ImageObjectRead? old = await objects.CreateStorage().OpenReadAsync(RecipeImageKeys.Detail(first.Version), Ct);
        old.ShouldBeNull();
        (await client.DeleteAsync($"/api/v1/recipes/{id}/image", Ct)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await client.GetAsync($"/api/v1/recipes/{id}/image", Ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await DetailAsync(client, id)).Image.ShouldBeNull();
        await CleanupAsync(host);
        using ImageObjectRead? removed = await objects.CreateStorage().OpenReadAsync(RecipeImageKeys.Detail(second.Version), Ct);
        removed.ShouldBeNull();
    }

    /// <summary>Invalid bytes and a partial S3 write leave the old active photo intact.</summary>
    [Fact]
    public async Task Upload_ValidationOrPartialStorageFailure_PreservesOldImageAndCleansIntent()
    {
        string user = $"photo-{Guid.CreateVersion7():N}";
        var storage = new PhotoTestStorage(objects.CreateStorage());
        WebApplicationFactory<Program> host = Host(user, storage);
        using HttpClient client = host.CreateClient();
        Guid id = await CreateAsync(client);
        RecipeImageDto first = await UploadOkAsync(client, id);
        (await UploadAsync(client, id, [1, 2, 3])).StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        storage.BeforePut = (key, _) => key.EndsWith("thumbnail.jpg", StringComparison.Ordinal)
            ? throw new ImageStorageUnavailableException() : Task.CompletedTask;
        (await UploadAsync(client, id)).StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        (await DetailAsync(client, id)).Image.ShouldBe(first);
        Guid pending;
        await using (AsyncServiceScope scope = host.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DietPlannerDbContext>();
            pending = await db.RecipeImageRevisions.Where(r => r.RecipeId == RecipeId.From(id) && !r.Published).Select(r => r.Id).SingleAsync(Ct);
        }
        host.Services.GetRequiredService<TimeProvider>().ShouldBeOfType<Microsoft.Extensions.Time.Testing.FakeTimeProvider>().Advance(TimeSpan.FromMinutes(16));
        await CleanupAsync(host);
        using ImageObjectRead? orphan = await objects.CreateStorage().OpenReadAsync(RecipeImageKeys.Detail(pending), Ct);
        orphan.ShouldBeNull();
        (await client.GetAsync($"/api/v1/recipes/{id}/image", Ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    /// <summary>Database failure after both S3 writes leaves durable intent and keeps the prior photo.</summary>
    [Fact]
    public async Task Upload_DatabaseCommitFailure_PreservesOldImage()
    {
        string user = $"photo-{Guid.CreateVersion7():N}";
        WebApplicationFactory<Program> host = Host(user);
        using HttpClient client = host.CreateClient();
        Guid id = await CreateAsync(client);
        RecipeImageDto first = await UploadOkAsync(client, id);
        using HttpClient broken = Host(user, failCommit: true).CreateClient();
        (await UploadAsync(broken, id)).StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        (await DetailAsync(client, id)).Image.ShouldBe(first);
        await using AsyncServiceScope scope = host.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DietPlannerDbContext>();
        (await db.RecipeImageRevisions.CountAsync(r => r.RecipeId == RecipeId.From(id) && !r.Published, Ct)).ShouldBe(1);
    }

    /// <summary>Overlapping uploads cannot silently overwrite one another; cleanup skips live uploads.</summary>
    [Fact]
    public async Task Upload_ConcurrentReplacement_OneWinsAndCleanupSkipsLockedIntents()
    {
        var storage = new PhotoTestStorage(objects.CreateStorage());
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        int arrivals = 0;
        storage.BeforePut = async (key, ct) =>
        {
            if (!key.EndsWith("thumbnail.jpg", StringComparison.Ordinal)) return;
            if (Interlocked.Increment(ref arrivals) == 2) entered.SetResult();
            await release.Task.WaitAsync(ct);
        };
        WebApplicationFactory<Program> host = Host($"photo-{Guid.CreateVersion7():N}", storage);
        using HttpClient client = host.CreateClient();
        Guid id = await CreateAsync(client);
        Task<HttpResponseMessage> first = UploadAsync(client, id);
        Task<HttpResponseMessage> second = UploadAsync(client, id);
        await entered.Task.WaitAsync(Ct);
        host.Services.GetRequiredService<TimeProvider>().ShouldBeOfType<Microsoft.Extensions.Time.Testing.FakeTimeProvider>().Advance(TimeSpan.FromMinutes(16));
        await CleanupAsync(host);
        release.SetResult();
        HttpResponseMessage[] results = await Task.WhenAll(first, second);
        results.Count(r => r.StatusCode == HttpStatusCode.OK).ShouldBe(1);
        results.Count(r => r.StatusCode == HttpStatusCode.Conflict).ShouldBe(1);
        await CleanupAsync(host);
        (await client.GetAsync($"/api/v1/recipes/{id}/image", Ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    /// <summary>Current visibility is checked on every read; soft deletion revokes reads and cleanup retries.</summary>
    [Fact]
    public async Task Read_AccessChangedOrRecipeDeleted_RevokesBytesAndCleanupRetries()
    {
        string user = $"photo-{Guid.CreateVersion7():N}";
        var storage = new PhotoTestStorage(objects.CreateStorage());
        WebApplicationFactory<Program> host = Host(user, storage);
        using HttpClient client = host.CreateClient();
        Guid id = await CreateAsync(client, "Public");
        RecipeImageDto image = await UploadOkAsync(client, id);
        using HttpClient stranger = Host($"stranger-{Guid.CreateVersion7():N}").CreateClient();
        (await stranger.GetAsync($"/api/v1/recipes/{id}/image", Ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
        RecipeDto detail = await DetailAsync(client, id);
        (await client.PutAsJsonAsync($"/api/v1/recipes/{id}", new UpdateRecipeRequest(detail.Name, null, null, 1, null,
            [.. detail.Ingredients.Select(i => new RecipeIngredientRequest(i.ProductId, i.Amount, i.Unit))], "Private"), Ct)).EnsureSuccessStatusCode();
        (await stranger.GetAsync($"/api/v1/recipes/{id}/image", Ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await client.DeleteAsync($"/api/v1/recipes/{id}", Ct)).EnsureSuccessStatusCode();
        (await client.GetAsync($"/api/v1/recipes/{id}/image", Ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        storage.FailDelete = true;
        await Should.ThrowAsync<ImageStorageUnavailableException>(() => CleanupAsync(host));
        storage.FailDelete = false;
        await CleanupAsync(host);
        using ImageObjectRead? removed = await objects.CreateStorage().OpenReadAsync(RecipeImageKeys.Detail(image.Version), Ct);
        removed.ShouldBeNull();
    }

    /// <summary>Recipe visibility and household roles match the existing library permission matrix.</summary>
    [Theory]
    [InlineData("Owner", "Household", 200, 200)]
    [InlineData("Adult", "Household", 200, 200)]
    [InlineData("Child", "Household", 200, 403)]
    [InlineData("Guest", "Household", 200, 403)]
    [InlineData("Adult", "Private", 404, 404)]
    [InlineData("Guest", "Public", 200, 403)]
    public async Task Access_RoleAndVisibility_MatchLibrary(string role, string visibility, int readStatus, int writeStatus)
    {
        string creator = $"creator-{Guid.CreateVersion7():N}";
        string reader = $"reader-{Guid.CreateVersion7():N}";
        var householdId = Guid.CreateVersion7();
        HouseholdContextMember[] members = [new(Guid.CreateVersion7(), "Creator", "Owner", false, creator),
            new(Guid.CreateVersion7(), "Reader", role, false, reader)];
        _households.Contexts[creator] = new HouseholdContext(householdId, members[0].PersonId, "Owner", members);
        _households.Contexts[reader] = new HouseholdContext(householdId, members[1].PersonId, role, members);
        using HttpClient owner = Host(creator).CreateClient();
        Guid id = await CreateAsync(owner, visibility);
        await UploadOkAsync(owner, id);
        using HttpClient other = Host(reader).CreateClient();
        ((int)(await other.GetAsync($"/api/v1/recipes/{id}/image", Ct)).StatusCode).ShouldBe(readStatus);
        ((int)(await UploadAsync(other, id)).StatusCode).ShouldBe(writeStatus);
        _households.Contexts.Remove(reader);
        if (visibility == "Household")
            (await other.GetAsync($"/api/v1/recipes/{id}/image", Ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    /// <summary>Even public photos require authentication for reads and mutations.</summary>
    [Fact]
    public async Task Access_Anonymous_DeniesAllPhotoRoutes()
    {
        using HttpClient client = Host("", anonymous: true).CreateClient();
        var id = Guid.CreateVersion7();
        (await client.GetAsync($"/api/v1/recipes/{id}/image", Ct)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await client.GetAsync($"/api/v1/recipes/{id}/image/thumbnail", Ct)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await UploadAsync(client, id)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await client.DeleteAsync($"/api/v1/recipes/{id}/image", Ct)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    /// <summary>Crash after intent/detail write is reclaimed; a delayed object write is swept again.</summary>
    [Fact]
    public async Task Cleanup_InterruptedAndLateUpload_ReconcilesWithoutReusingRevision()
    {
        WebApplicationFactory<Program> host = Host($"photo-{Guid.CreateVersion7():N}");
        using HttpClient client = host.CreateClient();
        Guid id = await CreateAsync(client);
        var version = Guid.CreateVersion7();
        TimeProvider clock = host.Services.GetRequiredService<TimeProvider>();
        await using (AsyncServiceScope scope = host.Services.CreateAsyncScope())
        {
            var persistence = scope.ServiceProvider.GetRequiredService<IRecipeImagePersistence>();
            await persistence.CreatePendingAsync(RecipeImageRevision.Create(version, RecipeId.From(id), clock.GetUtcNow()), Ct);
        }
        IImageObjectStorage storage = objects.CreateStorage();
        using var bytes = new MemoryStream(RecipeImageProcessorTests.Photo());
        await storage.PutAsync(RecipeImageKeys.Detail(version), bytes, "image/jpeg", Ct);
        clock.ShouldBeOfType<Microsoft.Extensions.Time.Testing.FakeTimeProvider>().Advance(TimeSpan.FromMinutes(16));
        await CleanupAsync(host);
        using ImageObjectRead? removed = await storage.OpenReadAsync(RecipeImageKeys.Detail(version), Ct);
        removed.ShouldBeNull();
        // Simulate S3 completing an in-flight write after the abandoned process lost its row lock.
        await storage.PutAsync(RecipeImageKeys.Detail(version), bytes, "image/jpeg", Ct);
        clock.ShouldBeOfType<Microsoft.Extensions.Time.Testing.FakeTimeProvider>().Advance(TimeSpan.FromDays(1));
        await CleanupAsync(host);
        using ImageObjectRead? late = await storage.OpenReadAsync(RecipeImageKeys.Detail(version), Ct);
        late.ShouldBeNull();
        await using AsyncServiceScope finalScope = host.Services.CreateAsyncScope();
        var db = finalScope.ServiceProvider.GetRequiredService<DietPlannerDbContext>();
        (await db.RecipeImageRevisions.SingleAsync(r => r.Id == version, Ct)).Retired.ShouldBeTrue();
    }

    /// <summary>Fresh API/DB/object-service instances retain photos; coordinated metadata/object recovery survives cleanup.</summary>
    [Fact]
    public async Task Persistence_RestartAndCoordinatedRestore_PreservesReferencedObjects()
    {
        string user = $"photo-{Guid.CreateVersion7():N}";
        WebApplicationFactory<Program> host = Host(user);
        using HttpClient client = host.CreateClient();
        Guid id = await CreateAsync(client);
        RecipeImageDto backup = await UploadOkAsync(client, id);
        byte[] detail = await client.GetByteArrayAsync($"/api/v1/recipes/{id}/image", Ct);
        byte[] thumbnail = await client.GetByteArrayAsync($"/api/v1/recipes/{id}/image/thumbnail", Ct);
        await host.DisposeAsync();
        await database.RestartAsync(Ct);
        await objects.RestartAsync(Ct);
        WebApplicationFactory<Program> restored = Host(user);
        using HttpClient fresh = restored.CreateClient();
        (await fresh.GetByteArrayAsync($"/api/v1/recipes/{id}/image", Ct)).ShouldBe(detail);
        (await fresh.DeleteAsync($"/api/v1/recipes/{id}/image", Ct)).EnsureSuccessStatusCode();
        await CleanupAsync(restored);
        // Recovery runs with automatic cleanup paused (Testing), restores bytes before references.
        IImageObjectStorage storage = objects.CreateStorage();
        using var detailStream = new MemoryStream(detail);
        using var thumbnailStream = new MemoryStream(thumbnail);
        await storage.PutAsync(RecipeImageKeys.Detail(backup.Version), detailStream, "image/jpeg", Ct);
        await storage.PutAsync(RecipeImageKeys.Thumbnail(backup.Version), thumbnailStream, "image/jpeg", Ct);
        await using (AsyncServiceScope scope = restored.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DietPlannerDbContext>();
            await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE recipes SET image_version = {backup.Version}, image_width = {backup.Width}, image_height = {backup.Height}, image_size = {backup.Size}, image_thumbnail_width = {backup.ThumbnailWidth}, image_thumbnail_height = {backup.ThumbnailHeight}, image_thumbnail_size = {backup.ThumbnailSize} WHERE id = {id}", Ct);
            await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE recipe_image_revisions SET retired = false, published = true WHERE id = {backup.Version}", Ct);
        }
        await CleanupAsync(restored);
        (await fresh.GetByteArrayAsync($"/api/v1/recipes/{id}/image", Ct)).ShouldBe(detail);
        (await DetailAsync(fresh, id)).Image.ShouldBe(backup);
    }

    /// <summary>OpenAPI includes binary JPEG responses and the multipart upload field.</summary>
    [Fact]
    public async Task OpenApi_PhotoEndpoints_DescribesBinaryReadsAndMultipartUpload()
    {
        using HttpClient client = Host($"photo-{Guid.CreateVersion7():N}").CreateClient();
        using System.Text.Json.JsonDocument document = System.Text.Json.JsonDocument.Parse(await client.GetStringAsync("/openapi/v1.json", Ct));
        System.Text.Json.JsonElement paths = document.RootElement.GetProperty("paths");
        paths.GetProperty("/api/v1/recipes/{id}/image").GetProperty("get").GetProperty("responses")
            .GetProperty("200").GetProperty("content").TryGetProperty("image/jpeg", out _).ShouldBeTrue();
        paths.GetProperty("/api/v1/recipes/{id}/image/thumbnail").GetProperty("get").GetProperty("responses")
            .GetProperty("200").GetProperty("content").TryGetProperty("image/jpeg", out _).ShouldBeTrue();
        paths.GetProperty("/api/v1/recipes/{id}/image").GetProperty("put").GetProperty("requestBody")
            .GetProperty("content").TryGetProperty("multipart/form-data", out _).ShouldBeTrue();
    }

    /// <summary>Permission revocation or recipe deletion during upload prevents publication.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Upload_AccessOrRecipeRevokedBeforePublish_DoesNotResurrectPhoto(bool deleteRecipe)
    {
        string creator = $"creator-{Guid.CreateVersion7():N}";
        string adult = $"adult-{Guid.CreateVersion7():N}";
        HouseholdContextMember[] members = [new(Guid.CreateVersion7(), "Creator", "Owner", false, creator),
            new(Guid.CreateVersion7(), "Adult", "Adult", false, adult)];
        var householdId = Guid.CreateVersion7();
        _households.Contexts[adult] = new HouseholdContext(householdId, members[1].PersonId, "Adult", members);
        using HttpClient owner = Host(creator).CreateClient();
        Guid id = await CreateAsync(owner, "Public");
        RecipeImageDto original = await UploadOkAsync(owner, id);
        var storage = new PhotoTestStorage(objects.CreateStorage());
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        storage.BeforePut = async (key, ct) =>
        {
            if (!key.EndsWith("thumbnail.jpg", StringComparison.Ordinal)) return;
            entered.SetResult();
            await release.Task.WaitAsync(ct);
        };
        using HttpClient uploader = Host(adult, storage).CreateClient();
        Task<HttpResponseMessage> upload = UploadAsync(uploader, id);
        await entered.Task.WaitAsync(Ct);
        if (deleteRecipe)
            (await owner.DeleteAsync($"/api/v1/recipes/{id}", Ct)).EnsureSuccessStatusCode();
        else
            _households.Contexts.Remove(adult);
        release.SetResult();
        (await upload).StatusCode.ShouldBe(deleteRecipe ? HttpStatusCode.NotFound : HttpStatusCode.Forbidden);
        if (deleteRecipe)
            (await owner.GetAsync($"/api/v1/recipes/{id}/image", Ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        else
            (await DetailAsync(owner, id)).Image.ShouldBe(original);
    }
}
