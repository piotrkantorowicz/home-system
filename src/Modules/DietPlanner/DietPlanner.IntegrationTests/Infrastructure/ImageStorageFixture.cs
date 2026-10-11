namespace DietPlanner.IntegrationTests.Infrastructure;

using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using DietPlanner.Application.Storage;
using DietPlanner.Infrastructure.Storage;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Volumes;
using Microsoft.Extensions.Options;

/// <summary>Owns an isolated private RustFS service, bucket, volume and SDK clients.</summary>
public sealed class ImageStorageFixture : IAsyncLifetime
{
    private readonly IVolume _volume = new VolumeBuilder().WithName($"image-storage-{Guid.CreateVersion7():N}").Build();
    private readonly IContainer _container;
    private readonly List<AmazonS3Client> _clients = [];
    private AmazonS3Client? _admin;
    private bool _bucketCreated;

    /// <summary>Builds a pinned service with generated test-only credentials and no developer resources.</summary>
    public ImageStorageFixture()
    {
        _container = new ContainerBuilder("rustfs/rustfs:1.0.1")
            .WithPortBinding(9000, true)
            .WithEnvironment("RUSTFS_ACCESS_KEY", AccessKey)
            .WithEnvironment("RUSTFS_SECRET_KEY", SecretKey)
            .WithEnvironment("RUSTFS_CONSOLE_ENABLE", "false")
            .WithVolumeMount(_volume, "/data")
            .WithCommand("/data")
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilHttpRequestIsSucceeded(request => request.ForPort(9000).ForPath("/health/ready"),
                    wait => wait.WithTimeout(TimeSpan.FromMinutes(1))))
            .Build();
    }

    /// <summary>The service's randomly mapped endpoint.</summary>
    public Uri Endpoint => new($"http://{_container.Hostname}:{_container.GetMappedPublicPort(9000)}");

    /// <summary>The fixture-owned private bucket.</summary>
    public string Bucket { get; } = $"image-storage-{Guid.CreateVersion7():N}";

    /// <summary>Generated test-only signing identity.</summary>
    public string AccessKey { get; } = $"test-{Guid.CreateVersion7():N}";

    /// <summary>Generated test-only signing secret.</summary>
    public string SecretKey { get; } = Guid.CreateVersion7().ToString("N");

    /// <summary>Starts the service and creates a bucket with no public policy or ACL.</summary>
    public async ValueTask InitializeAsync()
    {
        CancellationToken ct = TestContext.Current.CancellationToken;
        try
        {
            await _volume.CreateAsync(ct);
            await _container.StartAsync(ct);
            _admin = CreateClient(false);
            await _admin.PutBucketAsync(new PutBucketRequest { BucketName = Bucket }, ct);
            _bucketCreated = true;
        }
        catch
        {
            await DisposeAsync();
            throw;
        }
    }

    /// <summary>Creates the real adapter while retaining ownership of its SDK client.</summary>
    public IImageObjectStorage CreateStorage(string? bucket = null, bool invalidCredentials = false)
        => new S3ImageObjectStorage(CreateClient(invalidCredentials), Options.Create(new ImageStorageOptions
        {
            Enabled = true,
            Bucket = bucket ?? Bucket,
            Endpoint = Endpoint.AbsoluteUri,
            Region = "us-east-1",
            ForcePathStyle = true,
        }));

    /// <summary>Stops the service for an outage test.</summary>
    public Task StopAsync(CancellationToken ct) => _container.StopAsync(ct);

    /// <summary>Starts the same container and waits for readiness, preserving its object data.</summary>
    public async Task RestartAsync(CancellationToken ct)
    {
        await _container.StopAsync(ct);
        await _container.StartAsync(ct);
        // Docker may assign a new random host port when the same container restarts.
        _admin = CreateClient(false);
    }

    /// <summary>Deletes all test objects/bucket, then releases every owned resource even after a failure.</summary>
    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_bucketCreated && _admin is not null)
            {
                using var cleanup = new CancellationTokenSource(TimeSpan.FromMinutes(1));
                ListObjectsV2Response page;
                do
                {
                    page = await _admin.ListObjectsV2Async(new ListObjectsV2Request { BucketName = Bucket }, cleanup.Token);
                    foreach (S3Object item in page.S3Objects ?? [])
                    {
                        await _admin.DeleteObjectAsync(Bucket, item.Key, cleanup.Token);
                    }
                } while (page.IsTruncated == true);
                await _admin.DeleteBucketAsync(Bucket, cleanup.Token);
                _bucketCreated = false;
            }
        }
        finally
        {
            foreach (AmazonS3Client client in _clients)
            {
                client.Dispose();
            }
            try
            {
                await _container.DisposeAsync();
            }
            finally
            {
                await _volume.DisposeAsync();
            }
        }
    }

    private AmazonS3Client CreateClient(bool invalidCredentials)
    {
        var client = new AmazonS3Client(new BasicAWSCredentials(AccessKey,
            invalidCredentials ? Guid.CreateVersion7().ToString("N") : SecretKey), new AmazonS3Config
            {
                ServiceURL = Endpoint.AbsoluteUri,
                AuthenticationRegion = "us-east-1",
                ForcePathStyle = true,
                MaxErrorRetry = 0,
                Timeout = TimeSpan.FromSeconds(10),
            });
        _clients.Add(client);
        return client;
    }
}
