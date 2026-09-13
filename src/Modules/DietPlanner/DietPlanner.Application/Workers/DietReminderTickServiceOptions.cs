namespace DietPlanner.Application.Workers;

/// <summary>
/// Tuning for the hosted service that runs the reminder jobs (meal, water, weekly summary), bound
/// from the <c>DietPlanner:DietReminderTick</c> configuration section.
/// </summary>
public sealed class DietReminderTickServiceOptions
{
    /// <summary>Configuration section the options bind from.</summary>
    public const string SectionName = "DietPlanner:DietReminderTick";
    /// <summary>Whether the service ticks at all; integration tests turn it off.</summary>
    public bool Enabled { get; init; } = true;
    /// <summary>Seconds between ticks; clamped to at least 1.</summary>
    public int TickIntervalSeconds { get; init; } = 60;
}
