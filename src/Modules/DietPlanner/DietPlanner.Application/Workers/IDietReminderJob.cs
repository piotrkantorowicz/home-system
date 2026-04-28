namespace DietPlanner.Application.Workers;

internal interface IDietReminderJob
{
    string Name { get; }
    Task RunAsync(DateTime nowUtc, CancellationToken ct);
}
