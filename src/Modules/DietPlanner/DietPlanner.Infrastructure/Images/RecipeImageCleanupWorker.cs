namespace DietPlanner.Infrastructure.Images;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared.Abstractions.Core.Telemetry;

internal sealed partial class RecipeImageCleanupWorker(IServiceScopeFactory scopes, TimeProvider clock,
    ILogger<RecipeImageCleanupWorker> logger, IHostEnvironment environment, IWorkerTelemetry telemetry) : BackgroundService
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
            // Tracked inside the try: the failure is swallowed below, and the tick must still count as an error.
            await telemetry.TrackTickAsync(nameof(RecipeImageCleanupWorker), async token =>
            {
                await using AsyncServiceScope scope = scopes.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<RecipeImageCleanup>().RunOnceAsync(token);
            }, ct);
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
