namespace Investments.Infrastructure.Fx;

using Investments.Application.Common;
using Investments.Application.Fx;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
/// Keeps NBP rates current. NBP publishes table A around 11:45–12:15 Warsaw time on business days;
/// running every hour picks today's table up shortly after and retries any failed run.
/// </summary>
internal sealed partial class NbpFxWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<NbpFxOptions> options,
    TimeProvider clock,
    ILogger<NbpFxWorker> logger) : BackgroundService
{
    // ponytail: hourly instead of a 12:30 schedule; a run with nothing to fetch costs one 404 per
    // currency. Schedule precisely if NBP ever rate-limits.
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled)
            return;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunOnceAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                LogRunFailed(ex);
            }

            try { await Task.Delay(Interval, clock, stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
    }

    /// <summary>One sync of every foreign currency up to today.</summary>
    /// <param name="ct">Cancellation token.</param>
    public async Task RunOnceAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var sync = scope.ServiceProvider.GetRequiredService<FxRateSync>();
        await sync.SyncAllAsync(HouseholdCalendar.Today(clock), ct);
    }

    [LoggerMessage(EventId = 0, Level = LogLevel.Error, Message = "NBP rate sync failed; retrying next hour")]
    private partial void LogRunFailed(Exception exception);
}
