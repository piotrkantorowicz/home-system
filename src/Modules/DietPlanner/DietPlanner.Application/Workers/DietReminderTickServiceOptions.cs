namespace DietPlanner.Application.Workers;

public sealed class DietReminderTickServiceOptions
{
    public const string SectionName = "DietPlanner:DietReminderTick";
    public bool Enabled { get; init; } = true;
    public int TickIntervalSeconds { get; init; } = 60;
}
