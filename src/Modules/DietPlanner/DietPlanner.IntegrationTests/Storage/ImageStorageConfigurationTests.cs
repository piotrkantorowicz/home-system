namespace DietPlanner.IntegrationTests.Storage;

using Amazon.S3;
using DietPlanner.Application.Storage;
using DietPlanner.Infrastructure;
using DietPlanner.Infrastructure.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Options;

/// <summary>Verifies optional storage registration and startup validation without network access.</summary>
public sealed class ImageStorageConfigurationTests
{
    /// <summary>Disabled writes report a sanitized failure and preserve the caller's stream.</summary>
    [Fact]
    public async Task Put_WhenStorageDisabled_ReportsUnavailable()
    {
        await using ServiceProvider provider = BuildProvider();
        IImageObjectStorage storage = provider.GetRequiredService<IImageObjectStorage>();
        await using var content = new MemoryStream([1, 2, 3]);

        ImageStorageUnavailableException failure = await Should.ThrowAsync<ImageStorageUnavailableException>(
            () => storage.PutAsync("recipe-images/disabled/detail", content,
                "image/jpeg", TestContext.Current.CancellationToken));

        failure.Message.ShouldBe("Recipe image storage is unavailable.");
        failure.InnerException.ShouldBeNull();
        content.CanRead.ShouldBeTrue();
    }

    /// <summary>Disabled reads report storage unavailable rather than a missing object.</summary>
    [Fact]
    public async Task OpenRead_WhenStorageDisabled_ReportsUnavailable()
    {
        await using ServiceProvider provider = BuildProvider();
        IImageObjectStorage storage = provider.GetRequiredService<IImageObjectStorage>();

        ImageStorageUnavailableException failure = await Should.ThrowAsync<ImageStorageUnavailableException>(
            () => storage.OpenReadAsync("recipe-images/disabled/detail", TestContext.Current.CancellationToken));

        failure.Message.ShouldBe("Recipe image storage is unavailable.");
        failure.InnerException.ShouldBeNull();
    }

    /// <summary>Disabled deletes report storage unavailable rather than successful deletion.</summary>
    [Fact]
    public async Task Delete_WhenStorageDisabled_ReportsUnavailable()
    {
        await using ServiceProvider provider = BuildProvider();
        IImageObjectStorage storage = provider.GetRequiredService<IImageObjectStorage>();

        ImageStorageUnavailableException failure = await Should.ThrowAsync<ImageStorageUnavailableException>(
            () => storage.DeleteAsync("recipe-images/disabled/detail", TestContext.Current.CancellationToken));

        failure.Message.ShouldBe("Recipe image storage is unavailable.");
        failure.InnerException.ShouldBeNull();
    }

    /// <summary>Cancellation takes precedence over disabled storage for every operation.</summary>
    [Theory]
    [InlineData("put")]
    [InlineData("read")]
    [InlineData("delete")]
    public async Task Operation_WhenAlreadyCanceled_ReportsCancellation(string operation)
    {
        await using ServiceProvider provider = BuildProvider();
        IImageObjectStorage storage = provider.GetRequiredService<IImageObjectStorage>();
        await using var content = new MemoryStream([1, 2, 3]);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() => operation switch
        {
            "put" => storage.PutAsync("recipe-images/disabled/detail", content, "image/jpeg", cancellation.Token),
            "read" => storage.OpenReadAsync("recipe-images/disabled/detail", cancellation.Token),
            _ => storage.DeleteAsync("recipe-images/disabled/detail", cancellation.Token),
        });
    }

    /// <summary>Existing hosts can validate startup with no image storage configuration.</summary>
    [Fact]
    public void Validate_WhenConfigurationMissing_UsesDisabledStorage()
    {
        using ServiceProvider provider = BuildProvider();

        provider.GetRequiredService<IStartupValidator>().Validate();

        provider.GetRequiredService<IOptions<ImageStorageOptions>>().Value.Enabled.ShouldBeFalse();
        provider.GetRequiredService<IImageObjectStorage>().ShouldBeOfType<UnavailableImageObjectStorage>();
        provider.GetService<IAmazonS3>().ShouldBeNull();
    }

    /// <summary>Late options overrides select the same implementation as the validated configuration.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Resolve_WhenEnabledOverridden_UsesValidatedOptions(bool enabled)
    {
        using ServiceProvider provider = BuildProvider(new Dictionary<string, string?>
        {
            ["Enabled"] = (!enabled).ToString(),
            ["Bucket"] = "recipe-images",
        }, configure: services => services.Configure<ImageStorageOptions>(options => options.Enabled = enabled));

        provider.GetRequiredService<IStartupValidator>().Validate();
        IImageObjectStorage storage = provider.GetRequiredService<IImageObjectStorage>();
        if (enabled)
        {
            storage.ShouldBeOfType<S3ImageObjectStorage>();
        }
        else
        {
            storage.ShouldBeOfType<UnavailableImageObjectStorage>();
        }
    }

    /// <summary>Another module's S3 registration cannot replace the DietPlanner client.</summary>
    [Fact]
    public void Resolve_WhenAnotherModuleRegistersS3_KeepsModuleClient()
    {
        using var otherClient = new AmazonS3Client(new Amazon.Runtime.BasicAWSCredentials("test", "test"),
            new AmazonS3Config { ServiceURL = "https://other-module.example.com" });
        using ServiceProvider provider = BuildProvider(new Dictionary<string, string?>
        {
            ["Enabled"] = "true",
            ["Bucket"] = "recipe-images",
            ["Endpoint"] = "https://diet-planner.example.com",
        }, configure: services => services.AddSingleton<IAmazonS3>(otherClient));

        provider.GetRequiredService<IImageObjectStorage>().ShouldBeOfType<S3ImageObjectStorage>();
        provider.GetRequiredService<IAmazonS3>().ShouldBeSameAs(otherClient);
        IAmazonS3 client = provider.GetRequiredKeyedService<IAmazonS3>(typeof(S3ImageObjectStorage));
        client.ShouldNotBeSameAs(otherClient);
        client.Config.ServiceURL.TrimEnd('/').ShouldBe("https://diet-planner.example.com");
    }

    /// <summary>Disabled storage exposes invalid keys through faulted tasks, like enabled storage.</summary>
    [Theory]
    [InlineData("../x")]
    [InlineData("/absolute")]
    [InlineData("https://provider.example/key")]
    public async Task Operation_WhenDisabledWithInvalidKey_ReturnsFaultedTask(string key)
    {
        using ServiceProvider provider = BuildProvider();
        IImageObjectStorage storage = provider.GetRequiredService<IImageObjectStorage>();
        using var content = new MemoryStream([1]);
        CancellationToken ct = TestContext.Current.CancellationToken;
        Task[] operations =
        [
            storage.PutAsync(key, content, "image/jpeg", ct),
            storage.OpenReadAsync(key, ct),
            storage.DeleteAsync(key, ct),
        ];

        foreach (Task operation in operations)
        {
            operation.IsFaulted.ShouldBeTrue();
            await Should.ThrowAsync<ArgumentException>(() => operation);
        }
    }

    /// <summary>Unused disabled storage settings do not prevent startup.</summary>
    [Fact]
    public void Validate_WhenDisabledWithInvalidOptions_Succeeds()
    {
        using ServiceProvider provider = BuildProvider(new Dictionary<string, string?>
        {
            ["Enabled"] = "false",
            ["Region"] = "",
            ["Bucket"] = "INVALID BUCKET",
            ["Endpoint"] = "not an endpoint",
        }, Environments.Production);

        provider.GetRequiredService<IStartupValidator>().Validate();
        provider.GetRequiredService<IImageObjectStorage>().ShouldBeOfType<UnavailableImageObjectStorage>();
    }

    /// <summary>Enabled invalid settings fail without disclosing supplied values.</summary>
    [Theory]
    [InlineData("Bucket", null)]
    [InlineData("Bucket", "")]
    [InlineData("Bucket", "ab")]
    [InlineData("Bucket", "UPPERCASE")]
    [InlineData("Bucket", "images..private")]
    [InlineData("Bucket", "images.-private")]
    [InlineData("Bucket", "images-.private")]
    [InlineData("Bucket", "127.0.0.1")]
    [InlineData("Region", "")]
    [InlineData("Region", " ")]
    [InlineData("Endpoint", "not an endpoint")]
    [InlineData("Endpoint", "ftp://localhost")]
    [InlineData("Endpoint", "https://user:secret@localhost")]
    [InlineData("Endpoint", "https://localhost?secret=value")]
    [InlineData("Endpoint", "https://localhost#secret")]
    [InlineData("Endpoint", "https://localhost/bucket")]
    public void Validate_WhenEnabledWithInvalidField_ReportsSafeFailure(string field, string? value)
    {
        using ServiceProvider provider = BuildProvider(new Dictionary<string, string?>
        {
            ["Enabled"] = "true",
            ["Region"] = "us-east-1",
            ["Bucket"] = "recipe-images",
            [field] = value,
        });

        OptionsValidationException failure = Should.Throw<OptionsValidationException>(
            () => provider.GetRequiredService<IStartupValidator>().Validate());

        failure.Message.ShouldContain(field);
        failure.Message.ShouldNotContain("secret");
        failure.Message.ShouldNotContain("localhost");
    }

    /// <summary>Nonlocal environments require HTTPS for storage transport.</summary>
    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    public void Validate_WhenHttpOutsideLocalEnvironment_RejectsEndpoint(string environment)
    {
        using ServiceProvider provider = BuildProvider(new Dictionary<string, string?>
        {
            ["Enabled"] = "true",
            ["Region"] = "us-east-1",
            ["Bucket"] = "recipe-images",
            ["Endpoint"] = "http://localhost:9000",
        }, environment);

        OptionsValidationException failure = Should.Throw<OptionsValidationException>(
            () => provider.GetRequiredService<IStartupValidator>().Validate());

        failure.Message.ShouldContain("HTTPS");
        failure.Message.ShouldNotContain("localhost");
    }

    /// <summary>Regional endpoints and permitted explicit origins pass startup validation.</summary>
    [Theory]
    [InlineData(null, "Production")]
    [InlineData("", "Production")]
    [InlineData(" ", "Production")]
    [InlineData("https://storage.example.com", "Production")]
    [InlineData("https://storage.example.com/", "Production")]
    [InlineData("http://localhost:9000", "Development")]
    [InlineData("http://localhost:9000", "Testing")]
    public void Validate_WhenEnabledWithValidOptions_Succeeds(string? endpoint, string environment)
    {
        using ServiceProvider provider = BuildProvider(new Dictionary<string, string?>
        {
            ["Enabled"] = "true",
            ["Region"] = "us-east-1",
            ["Bucket"] = "recipe-images",
            ["Endpoint"] = endpoint,
        }, environment);

        provider.GetRequiredService<IStartupValidator>().Validate();
    }

    /// <summary>Enabled clients use the configured origin, signing region and addressing style without requests.</summary>
    [Theory]
    [InlineData("http://localhost:9000", true)]
    [InlineData("https://storage.example.com", false)]
    [InlineData(null, false)]
    public void Resolve_WhenEnabled_UsesConfiguredS3Client(string? endpoint, bool forcePathStyle)
    {
        using ServiceProvider provider = BuildProvider(new Dictionary<string, string?>
        {
            ["Enabled"] = "true",
            ["Region"] = "eu-central-1",
            ["Bucket"] = "recipe-images",
            ["Endpoint"] = endpoint,
            ["ForcePathStyle"] = forcePathStyle.ToString(),
        });

        IAmazonS3 client = provider.GetRequiredKeyedService<IAmazonS3>(typeof(S3ImageObjectStorage));
        var config = client.Config.ShouldBeOfType<AmazonS3Config>();

        config.ForcePathStyle.ShouldBe(forcePathStyle);
        config.MaxErrorRetry.ShouldBe(2);
        config.DisableLogging.ShouldBeTrue();
        if (endpoint is null)
        {
            config.RegionEndpoint.SystemName.ShouldBe("eu-central-1");
        }
        else
        {
            config.ServiceURL.TrimEnd('/').ShouldBe(endpoint);
            config.AuthenticationRegion.ShouldBe("eu-central-1");
        }

        provider.GetRequiredService<IImageObjectStorage>().ShouldBeOfType<S3ImageObjectStorage>();
        provider.GetRequiredKeyedService<IAmazonS3>(typeof(S3ImageObjectStorage)).ShouldBeSameAs(client);
        provider.GetService<IAmazonS3>().ShouldBeNull();
    }

    private static ServiceProvider BuildProvider(
        Dictionary<string, string?>? options = null,
        string environment = "Development",
        Action<IServiceCollection>? configure = null)
    {
        var values = new Dictionary<string, string?>
        {
            ["ConnectionStrings:DietPlanner"] = "Host=localhost;Database=unused",
        };
        foreach (KeyValuePair<string, string?> option in options ?? [])
        {
            values[$"DietPlanner:ImageStorage:{option.Key}"] = option.Value;
        }

        IConfigurationRoot configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton<IHostEnvironment>(new HostingEnvironment { EnvironmentName = environment });
        services.AddDietPlannerInfrastructure(configuration);
        configure?.Invoke(services);
        return services.BuildServiceProvider();
    }
}
