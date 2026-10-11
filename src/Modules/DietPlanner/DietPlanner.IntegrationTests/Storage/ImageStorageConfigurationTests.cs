namespace DietPlanner.IntegrationTests.Storage;

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

    private static ServiceProvider BuildProvider(
        Dictionary<string, string?>? options = null,
        string environment = "Development")
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
        return services.BuildServiceProvider();
    }
}
