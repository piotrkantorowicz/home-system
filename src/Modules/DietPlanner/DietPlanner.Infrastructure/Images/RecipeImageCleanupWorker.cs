namespace DietPlanner.Infrastructure.Images;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

internal sealed partial class RecipeImageCleanupWorker(IServiceScopeFactory scopes, TimeProvider clock,
    ILogger<RecipeImageCleanupWorker> logger, IHostEnvironment environment) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        if (environment.IsEnvironment("Testing"))
            return;
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1), clock);
        while (await timer.WaitForNextTickAsync(ct))
            await RunOnceAsync(ct);
    }

    internal async Task RunOnceAsync(CancellationToken ct)
    {
        try
        {
            await using AsyncServiceScope scope = scopes.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<RecipeImageCleanup>().RunOnceAsync(ct);
        }
        catch (Exception) when (!ct.IsCancellationRequested)
        {
            // Provider keys and failure details must not enter application logs.
            LogCleanupFailure(logger);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Recipe image cleanup failed; durable records will be retried.")]
    private static partial void LogCleanupFailure(ILogger logger);
}
