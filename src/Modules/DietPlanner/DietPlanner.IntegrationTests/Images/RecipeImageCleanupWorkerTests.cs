namespace DietPlanner.IntegrationTests.Images;

using DietPlanner.Infrastructure.Images;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

/// <summary>Hosted cleanup distinguishes provider cancellation from application shutdown.</summary>
public sealed class RecipeImageCleanupWorkerTests
{
    /// <summary>A timeout outside shutdown does not terminate the worker or prevent its next tick.</summary>
    [Fact]
    public async Task RunOnce_ProviderCancels_AllowsNextRunAndPropagatesShutdown()
    {
        CancellationToken ct = TestContext.Current.CancellationToken;
        var clock = new FakeTimeProvider();
        int attempts = 0;
        HostApplicationBuilder builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { EnvironmentName = "Development" });
        builder.Services.AddScoped<RecipeImageCleanup>(_ =>
        {
            attempts++;
            throw new TaskCanceledException("Provider timeout");
        });
        using IHost host = builder.Build();
        using var worker = new RecipeImageCleanupWorker(host.Services.GetRequiredService<IServiceScopeFactory>(), clock,
            NullLogger<RecipeImageCleanupWorker>.Instance, builder.Environment);
        await worker.RunOnceAsync(ct);
        await worker.RunOnceAsync(ct);
        attempts.ShouldBe(2);
        await Should.ThrowAsync<TaskCanceledException>(() => worker.RunOnceAsync(new CancellationToken(true)));
    }
}
