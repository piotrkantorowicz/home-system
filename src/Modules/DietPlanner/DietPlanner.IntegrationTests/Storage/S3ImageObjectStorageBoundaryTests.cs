namespace DietPlanner.IntegrationTests.Storage;

using System.Net;
using System.Net.Sockets;
using System.Reflection;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using DietPlanner.Application.Storage;
using DietPlanner.Infrastructure.Storage;
using Microsoft.Extensions.Options;

/// <summary>Checks validation, ownership and failure translation at the external SDK boundary.</summary>
public sealed class S3ImageObjectStorageBoundaryTests
{
    private const string Key = "recipe-images/opaque-revision/detail";

    /// <summary>Uploads forward the configured bucket, key, media type, original stream and cancellation.</summary>
    [Fact]
    public async Task Put_WithValidContent_ForwardsRequestAndPreservesStream()
    {
        await using var content = new MemoryStream([1, 2, 3]);
        content.Position = content.Length;
        CancellationToken ct = TestContext.Current.CancellationToken;
        S3ImageObjectStorage storage = CreateStorage((request, token) =>
        {
            var put = request.ShouldBeOfType<PutObjectRequest>();
            put.BucketName.ShouldBe("recipe-images");
            put.Key.ShouldBe(Key);
            put.ContentType.ShouldBe("image/jpeg");
            put.InputStream.ShouldBeSameAs(content);
            put.InputStream.Position.ShouldBe(0);
            put.AutoCloseStream.ShouldBeFalse();
            put.CannedACL.ShouldBeNull();
            token.ShouldBe(ct);
            return Task.FromResult(new PutObjectResponse());
        });

        await storage.PutAsync(Key, content, "image/jpeg", ct);

        content.CanRead.ShouldBeTrue();
    }

    /// <summary>A successful read streams the original content until its owning handle is disposed.</summary>
    [Fact]
    public async Task OpenRead_WithObject_TransfersResponseOwnership()
    {
        using var content = new MemoryStream([1, 2, 3]);
        var response = new GetObjectResponse { ResponseStream = content };
        CancellationToken ct = TestContext.Current.CancellationToken;
        S3ImageObjectStorage storage = CreateStorage((request, token) =>
        {
            var get = request.ShouldBeOfType<GetObjectRequest>();
            get.BucketName.ShouldBe("recipe-images");
            get.Key.ShouldBe(Key);
            token.ShouldBe(ct);
            return Task.FromResult(response);
        });

        ImageObjectRead? read = await storage.OpenReadAsync(Key, ct);

        read.ShouldNotBeNull();
        read.Content.ShouldBeSameAs(content);
        content.CanRead.ShouldBeTrue();
        read.Dispose();
        content.CanRead.ShouldBeFalse();
    }

    /// <summary>Cancellation before read ownership transfers disposes the SDK response.</summary>
    [Fact]
    public async Task OpenRead_WhenCanceledAfterResponse_DisposesResponse()
    {
        using var content = new MemoryStream([1, 2, 3]);
        using var cancellation = new CancellationTokenSource();
        S3ImageObjectStorage storage = CreateStorage((_, _) =>
        {
            cancellation.Cancel();
            return Task.FromResult(new GetObjectResponse { ResponseStream = content });
        });

        await Should.ThrowAsync<OperationCanceledException>(() => storage.OpenReadAsync(Key, cancellation.Token));

        content.CanRead.ShouldBeFalse();
    }

    /// <summary>Deletes forward the configured bucket, key and cancellation token.</summary>
    [Fact]
    public async Task Delete_WithValidKey_ForwardsRequest()
    {
        CancellationToken ct = TestContext.Current.CancellationToken;
        S3ImageObjectStorage storage = CreateStorage((request, token) =>
        {
            var delete = request.ShouldBeOfType<DeleteObjectRequest>();
            delete.BucketName.ShouldBe("recipe-images");
            delete.Key.ShouldBe(Key);
            token.ShouldBe(ct);
            return Task.FromResult(new DeleteObjectResponse());
        });

        await storage.DeleteAsync(Key, ct);
    }

    /// <summary>Only an explicit missing-key provider error means the object is absent.</summary>
    [Fact]
    public async Task OpenRead_WhenNoSuchKey_ReturnsNull()
    {
        S3ImageObjectStorage storage = CreateStorage((_, _) => throw S3Failure("NoSuchKey"));

        ImageObjectRead? read = await storage.OpenReadAsync(Key, TestContext.Current.CancellationToken);

        read.ShouldBeNull();
    }

    /// <summary>Deletion tolerates an explicit missing key.</summary>
    [Fact]
    public async Task Delete_WhenNoSuchKey_Succeeds()
    {
        S3ImageObjectStorage storage = CreateStorage((_, _) => throw S3Failure("NoSuchKey"));

        await storage.DeleteAsync(Key, TestContext.Current.CancellationToken);
    }

    /// <summary>Provider failures never leak credentials, endpoints, keys or provider details.</summary>
    [Theory]
    [InlineData("NoSuchBucket")]
    [InlineData("")]
    [InlineData("AccessDenied")]
    [InlineData("InvalidAccessKeyId")]
    [InlineData("client")]
    [InlineData("network")]
    [InlineData("timeout")]
    [InlineData("io")]
    [InlineData("socket")]
    public async Task Operation_WhenProviderFails_ReportsSanitizedUnavailable(string error)
    {
        S3ImageObjectStorage storage = CreateStorage((_, _) => throw error switch
        {
            "client" => new AmazonClientException("secret https://provider.example/key"),
            "network" => new HttpRequestException("secret https://provider.example/key"),
            "timeout" => new TaskCanceledException("secret https://provider.example/key"),
            "io" => new IOException("secret https://provider.example/key"),
            "socket" => new SocketException((int)SocketError.ConnectionReset),
            _ => S3Failure(error),
        });
        await using var content = new MemoryStream([1]);
        CancellationToken ct = TestContext.Current.CancellationToken;
        Func<Task>[] operations =
        [
            () => storage.PutAsync(Key, content, "image/jpeg", ct),
            () => storage.OpenReadAsync(Key, ct),
            () => storage.DeleteAsync(Key, ct),
        ];

        foreach (Func<Task> operation in operations)
        {
            ImageStorageUnavailableException failure = await Should.ThrowAsync<ImageStorageUnavailableException>(operation);
            failure.Message.ShouldBe("Recipe image storage is unavailable.");
            failure.InnerException.ShouldBeNull();
        }

        content.CanRead.ShouldBeTrue();
    }

    /// <summary>Invalid internal names fail at the boundary before any SDK operation.</summary>
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("/absolute")]
    [InlineData("https://provider.example/key")]
    [InlineData("recipe-images/./detail")]
    [InlineData("recipe-images/../detail")]
    [InlineData("..")]
    public async Task Operation_WhenKeyInvalid_RejectsBeforeSdk(string key)
    {
        S3ImageObjectStorage storage = CreateStorage((_, _) => throw new InvalidOperationException("SDK must not be invoked."));
        await using var content = new MemoryStream([1]);
        CancellationToken ct = TestContext.Current.CancellationToken;

        await Should.ThrowAsync<ArgumentException>(() => storage.PutAsync(key, content, "image/jpeg", ct));
        await Should.ThrowAsync<ArgumentException>(() => storage.OpenReadAsync(key, ct));
        await Should.ThrowAsync<ArgumentException>(() => storage.DeleteAsync(key, ct));
    }

    /// <summary>Uploads validate readable streams and media types before invoking the SDK.</summary>
    [Fact]
    public async Task Put_WhenContentInvalid_RejectsBeforeSdk()
    {
        S3ImageObjectStorage storage = CreateStorage((_, _) => throw new InvalidOperationException("SDK must not be invoked."));
        using var content = new MemoryStream([1]);
        CancellationToken ct = TestContext.Current.CancellationToken;
        await Should.ThrowAsync<ArgumentException>(() => storage.PutAsync(Key, content, " ", ct));
        await Should.ThrowAsync<ArgumentNullException>(() => storage.PutAsync(Key, null!, "image/jpeg", ct)); // Tests the runtime null boundary.
        await content.DisposeAsync();
        await Should.ThrowAsync<ArgumentException>(() => storage.PutAsync(Key, content, "image/jpeg", ct));
    }

    /// <summary>Empty and non-seekable uploads fail before the SDK can store unusable content.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Put_WhenEmptyOrNonSeekable_RejectsBeforeSdk(bool seekable)
    {
        S3ImageObjectStorage storage = CreateStorage((_, _) => throw new InvalidOperationException("SDK must not be invoked."));
        using var buffer = new MemoryStream();
        using Stream content = seekable
            ? buffer
            : new System.IO.Compression.GZipStream(buffer, System.IO.Compression.CompressionMode.Decompress);

        await Should.ThrowAsync<ArgumentException>(
            () => storage.PutAsync(Key, content, "image/jpeg", TestContext.Current.CancellationToken));
    }

    /// <summary>Cancellation takes precedence and SDK cancellation failures preserve cancellation.</summary>
    [Theory]
    [InlineData("put", false)]
    [InlineData("read", false)]
    [InlineData("delete", false)]
    [InlineData("put", true)]
    [InlineData("read", true)]
    [InlineData("delete", true)]
    public async Task Operation_WhenCanceled_PreservesCancellation(string operation, bool duringSdk)
    {
        using var cancellation = new CancellationTokenSource();
        S3ImageObjectStorage storage = CreateStorage((_, _) =>
        {
            if (!duringSdk)
            {
                throw new InvalidOperationException("SDK must not be invoked.");
            }

            cancellation.Cancel();
            throw new AmazonClientException("secret provider cancellation details");
        });
        await using var content = new MemoryStream([1]);
        if (!duringSdk)
        {
            await cancellation.CancelAsync();
        }

        OperationCanceledException failure = await Should.ThrowAsync<OperationCanceledException>(() => operation switch
        {
            "put" => storage.PutAsync(Key, content, "image/jpeg", cancellation.Token),
            "read" => storage.OpenReadAsync(Key, cancellation.Token),
            _ => storage.DeleteAsync(Key, cancellation.Token),
        });

        failure.CancellationToken.ShouldBe(cancellation.Token);
    }

    /// <summary>Programmer errors remain visible instead of being recast as outages.</summary>
    [Fact]
    public async Task Delete_WhenProgrammerError_DoesNotTranslate()
    {
        var failure = new InvalidOperationException("programmer error");
        S3ImageObjectStorage storage = CreateStorage((_, _) => throw failure);

        InvalidOperationException actual = await Should.ThrowAsync<InvalidOperationException>(
            () => storage.DeleteAsync(Key, TestContext.Current.CancellationToken));

        actual.ShouldBeSameAs(failure);
    }

    private static S3ImageObjectStorage CreateStorage(Func<AmazonWebServiceRequest, CancellationToken, object> operation)
    {
        IAmazonS3 client = DispatchProxy.Create<IAmazonS3, ImageStorageS3Proxy>();
        ((ImageStorageS3Proxy)client).Operation = operation;
        return new S3ImageObjectStorage(client, Options.Create(new ImageStorageOptions { Bucket = "recipe-images" }));
    }

    private static AmazonS3Exception S3Failure(string error)
        => new("secret https://provider.example/key")
        {
            ErrorCode = error,
            StatusCode = error == "AccessDenied" ? HttpStatusCode.Forbidden : HttpStatusCode.NotFound,
        };
}
