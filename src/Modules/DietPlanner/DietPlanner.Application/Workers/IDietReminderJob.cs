namespace DietPlanner.Application.Workers;

public interface IDietReminderJob
{
    string Name { get; }
    Task RunAsync(DateTime nowUtc, CancellationToken ct);
}
