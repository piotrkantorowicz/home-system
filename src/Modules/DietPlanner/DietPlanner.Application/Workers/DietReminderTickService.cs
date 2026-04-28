namespace DietPlanner.Application.Workers;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public sealed class DietReminderTickService(
    IServiceScopeFactory scopeFactory,
    IOptions<DietReminderTickServiceOptions> options,
    ILogger<DietReminderTickService> logger) : BackgroundService
{
    private readonly DietReminderTickServiceOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            logger.LogInformation("DietReminderTickService disabled via configuration; not ticking.");
            return;
        }

        var interval = TimeSpan.FromSeconds(Math.Max(_options.TickIntervalSeconds, 1));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunOnceAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "DietReminderTickService tick failed");
            }

            try { await Task.Delay(interval, stoppingToken).ConfigureAwait(false); }
            catch (OperationCanceledException) { break; }
        }
    }

    public async Task RunOnceAsync(CancellationToken ct)
    {
        if (!_options.Enabled) return;

        await using var scope = scopeFactory.CreateAsyncScope();
        var jobs = scope.ServiceProvider.GetServices<IDietReminderJob>();
        var nowUtc = DateTime.UtcNow;

        foreach (var job in jobs)
        {
            try
            {
                await job.RunAsync(nowUtc, ct).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "DietReminderJob {JobName} failed", job.Name);
            }
        }
    }
}
