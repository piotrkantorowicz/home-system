namespace DietPlanner.IntegrationTests.Storage;

using System.Net;
using DietPlanner.Application.Storage;
using DietPlanner.IntegrationTests.Infrastructure;

/// <summary>Proves the image-storage contract against isolated private S3-compatible storage.</summary>
/// <param name="fixture">The collection-owned private service.</param>
[Collection(ImageStorageCollectionDefinition.Name)]
public sealed class S3ImageObjectStorageTests(ImageStorageFixture fixture)
{
    /// <summary>Private objects round-trip and are removed by deletion.</summary>
    [Fact]
    public async Task PutReadDelete_WithPrivateObject_RoundTripsAndRemoves()
    {
        CancellationToken ct = TestContext.Current.CancellationToken;
        IImageObjectStorage storage = fixture.CreateStorage();
        string key = $"recipe-images/{Guid.CreateVersion7():N}/detail";
        byte[] expected = [1, 2, 3, 4];
        await using var input = new MemoryStream(expected);
        try
        {
            await storage.PutAsync(key, input, "image/jpeg", ct);
            input.CanRead.ShouldBeTrue();
            using (ImageObjectRead? read = await storage.OpenReadAsync(key, ct))
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
            await CleanupAsync(storage, key, ct);
        }
    }

    /// <summary>An absent key in an existing bucket is a normal missing result.</summary>
    [Fact]
    public async Task OpenRead_WhenObjectMissing_ReturnsNull()
    {
        using ImageObjectRead? read = await fixture.CreateStorage().OpenReadAsync(NewKey(), TestContext.Current.CancellationToken);

        read.ShouldBeNull();
    }

    /// <summary>Deleting an absent key repeatedly remains successful.</summary>
    [Fact]
    public async Task Delete_WhenObjectMissing_SucceedsTwice()
    {
        IImageObjectStorage storage = fixture.CreateStorage();
        string key = NewKey();
        CancellationToken ct = TestContext.Current.CancellationToken;

        await storage.DeleteAsync(key, ct);
        await storage.DeleteAsync(key, ct);
    }

    /// <summary>A nonexistent bucket is unavailable, not a missing object.</summary>
    [Fact]
    public async Task OpenRead_WhenBucketMissing_ReportsUnavailable()
    {
        IImageObjectStorage storage = fixture.CreateStorage($"missing-{Guid.CreateVersion7():N}");

        await ShouldReportUnavailableAsync(() => storage.OpenReadAsync(NewKey(), TestContext.Current.CancellationToken));
    }

    /// <summary>Unauthenticated requests cannot read a known existing private object.</summary>
    [Fact]
    public async Task OpenRead_WhenUnauthenticated_DeniesObjectAccess()
    {
        CancellationToken ct = TestContext.Current.CancellationToken;
        IImageObjectStorage storage = fixture.CreateStorage();
        string key = NewKey();
        byte[] expected = [1, 2, 3, 4];
        await using var input = new MemoryStream(expected);
        try
        {
            await storage.PutAsync(key, input, "image/jpeg", ct);
            (await ReadBytesAsync(storage, key, ct)).ShouldBe(expected);
            using var client = new HttpClient();
            using HttpResponseMessage response = await client.GetAsync(new Uri(fixture.Endpoint, $"/{fixture.Bucket}/{key}"), ct);

            response.StatusCode.ShouldBeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.Unauthorized);
        }
        finally
        {
            await CleanupAsync(storage, key, ct);
        }
    }

    /// <summary>Incorrect signing credentials cannot read an existing object.</summary>
    [Fact]
    public async Task OpenRead_WhenCredentialsInvalid_ReportsUnavailable()
    {
        CancellationToken ct = TestContext.Current.CancellationToken;
        IImageObjectStorage storage = fixture.CreateStorage();
        string key = NewKey();
        byte[] expected = [1, 2, 3, 4];
        await using var input = new MemoryStream(expected);
        try
        {
            await storage.PutAsync(key, input, "image/jpeg", ct);
            (await ReadBytesAsync(storage, key, ct)).ShouldBe(expected);
            IImageObjectStorage denied = fixture.CreateStorage(invalidCredentials: true);

            await ShouldReportUnavailableAsync(() => denied.OpenReadAsync(key, ct));
        }
        finally
        {
            await CleanupAsync(storage, key, ct);
        }
    }

    /// <summary>Incorrect signing credentials cannot create an object.</summary>
    [Fact]
    public async Task Put_WhenCredentialsInvalid_ReportsUnavailableAndDoesNotWrite()
    {
        CancellationToken ct = TestContext.Current.CancellationToken;
        IImageObjectStorage storage = fixture.CreateStorage();
        IImageObjectStorage denied = fixture.CreateStorage(invalidCredentials: true);
        string key = NewKey();
        await using var input = new MemoryStream([1, 2, 3, 4]);
        try
        {
            await ShouldReportUnavailableAsync(() => denied.PutAsync(key, input, "image/jpeg", ct));
            using ImageObjectRead? read = await storage.OpenReadAsync(key, ct);
            read.ShouldBeNull();
        }
        finally
        {
            await CleanupAsync(storage, key, ct);
        }
    }

    /// <summary>Incorrect signing credentials cannot remove an existing object.</summary>
    [Fact]
    public async Task Delete_WhenCredentialsInvalid_ReportsUnavailableAndPreservesObject()
    {
        CancellationToken ct = TestContext.Current.CancellationToken;
        IImageObjectStorage storage = fixture.CreateStorage();
        string key = NewKey();
        byte[] expected = [1, 2, 3, 4];
        await using var input = new MemoryStream(expected);
        try
        {
            await storage.PutAsync(key, input, "image/jpeg", ct);
            IImageObjectStorage denied = fixture.CreateStorage(invalidCredentials: true);

            await ShouldReportUnavailableAsync(() => denied.DeleteAsync(key, ct));
            (await ReadBytesAsync(storage, key, ct)).ShouldBe(expected);
        }
        finally
        {
            await CleanupAsync(storage, key, ct);
        }
    }

    /// <summary>Service outages report sanitized failures and reads recover after restart.</summary>
    [Fact]
    public async Task OpenRead_WhenStorageStopped_ReportsUnavailable()
    {
        CancellationToken ct = TestContext.Current.CancellationToken;
        IImageObjectStorage storage = fixture.CreateStorage();
        string key = NewKey();
        byte[] expected = [1, 2, 3, 4];
        await using var input = new MemoryStream(expected);
        try
        {
            await storage.PutAsync(key, input, "image/jpeg", ct);
            try
            {
                await fixture.StopAsync(ct);
                await ShouldReportUnavailableAsync(() => storage.OpenReadAsync(key, ct));
            }
            finally
            {
                await fixture.RestartAsync(ct);
                storage = fixture.CreateStorage();
            }
            (await ReadBytesAsync(storage, key, ct)).ShouldBe(expected);
        }
        finally
        {
            await CleanupAsync(storage, key, ct);
        }
    }

    /// <summary>A pre-canceled upload leaves no object behind.</summary>
    [Fact]
    public async Task Put_WhenTokenCanceled_DoesNotWriteObject()
    {
        CancellationToken ct = TestContext.Current.CancellationToken;
        IImageObjectStorage storage = fixture.CreateStorage();
        string key = NewKey();
        await using var input = new MemoryStream([1, 2, 3, 4]);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        try
        {
            OperationCanceledException failure = await Should.ThrowAsync<OperationCanceledException>(
                () => storage.PutAsync(key, input, "image/jpeg", cancellation.Token));
            failure.CancellationToken.ShouldBe(cancellation.Token);
            using ImageObjectRead? read = await storage.OpenReadAsync(key, ct);
            read.ShouldBeNull();
        }
        finally
        {
            await CleanupAsync(storage, key, ct);
        }
    }

    /// <summary>A pre-canceled read propagates cancellation.</summary>
    [Fact]
    public async Task OpenRead_WhenTokenCanceled_PropagatesCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        OperationCanceledException failure = await Should.ThrowAsync<OperationCanceledException>(
            () => fixture.CreateStorage().OpenReadAsync(NewKey(), cancellation.Token));

        failure.CancellationToken.ShouldBe(cancellation.Token);
    }

    /// <summary>A pre-canceled deletion keeps the original object readable.</summary>
    [Fact]
    public async Task Delete_WhenTokenCanceled_LeavesObjectReadable()
    {
        CancellationToken ct = TestContext.Current.CancellationToken;
        IImageObjectStorage storage = fixture.CreateStorage();
        string key = NewKey();
        byte[] expected = [1, 2, 3, 4];
        await using var input = new MemoryStream(expected);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        try
        {
            await storage.PutAsync(key, input, "image/jpeg", ct);
            OperationCanceledException failure = await Should.ThrowAsync<OperationCanceledException>(
                () => storage.DeleteAsync(key, cancellation.Token));
            failure.CancellationToken.ShouldBe(cancellation.Token);
            (await ReadBytesAsync(storage, key, ct)).ShouldBe(expected);
        }
        finally
        {
            await CleanupAsync(storage, key, ct);
        }
    }

    /// <summary>Disposing a read handle closes its response stream.</summary>
    [Fact]
    public async Task OpenRead_WhenDisposed_ReleasesStream()
    {
        CancellationToken ct = TestContext.Current.CancellationToken;
        IImageObjectStorage storage = fixture.CreateStorage();
        string key = NewKey();
        await using var input = new MemoryStream([1, 2, 3, 4]);
        try
        {
            await storage.PutAsync(key, input, "image/jpeg", ct);
            using ImageObjectRead? read = await storage.OpenReadAsync(key, ct);
            read.ShouldNotBeNull();
            read.Content.ReadByte().ShouldBe(1);

            read.Dispose();

            read.Content.CanRead.ShouldBeFalse();
        }
        finally
        {
            await CleanupAsync(storage, key, ct);
        }
    }

    /// <summary>The caller retains ownership of the uploaded stream.</summary>
    [Fact]
    public async Task Put_WhenCallerStreamUsed_LeavesStreamOpen()
    {
        CancellationToken ct = TestContext.Current.CancellationToken;
        IImageObjectStorage storage = fixture.CreateStorage();
        string key = NewKey();
        await using var input = new MemoryStream([1, 2, 3, 4]);
        try
        {
            await storage.PutAsync(key, input, "image/jpeg", ct);

            input.CanRead.ShouldBeTrue();
            input.Position = 0;
            input.ReadByte().ShouldBe(1);
        }
        finally
        {
            await CleanupAsync(storage, key, ct);
        }
    }

    /// <summary>Restarting the same service preserves the exact object bytes.</summary>
    [Fact]
    public async Task OpenRead_AfterRestart_ReturnsOriginalBytes()
    {
        CancellationToken ct = TestContext.Current.CancellationToken;
        IImageObjectStorage storage = fixture.CreateStorage();
        string key = NewKey();
        byte[] expected = [1, 2, 3, 4];
        await using var input = new MemoryStream(expected);
        try
        {
            await storage.PutAsync(key, input, "image/jpeg", ct);

            await fixture.RestartAsync(ct);
            storage = fixture.CreateStorage();

            (await ReadBytesAsync(storage, key, ct)).ShouldBe(expected);
        }
        finally
        {
            await CleanupAsync(storage, key, ct);
        }
    }

    private static string NewKey() => $"recipe-images/{Guid.CreateVersion7():N}/detail";

    private static async Task<byte[]> ReadBytesAsync(IImageObjectStorage storage, string key, CancellationToken ct)
    {
        using ImageObjectRead? read = await storage.OpenReadAsync(key, ct);
        read.ShouldNotBeNull();
        await using var output = new MemoryStream();
        await read.Content.CopyToAsync(output, ct);
        return output.ToArray();
    }

    private static async Task ShouldReportUnavailableAsync(Func<Task> operation)
    {
        ImageStorageUnavailableException failure = await Should.ThrowAsync<ImageStorageUnavailableException>(operation);
        failure.Message.ShouldBe("Recipe image storage is unavailable.");
        failure.InnerException.ShouldBeNull();
    }

    private static async Task CleanupAsync(IImageObjectStorage storage, string key, CancellationToken ct)
    {
        if (!ct.IsCancellationRequested)
        {
            await storage.DeleteAsync(key, ct);
        }
    }
}
