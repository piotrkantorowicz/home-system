namespace Notifications.Infrastructure.Workers;

/// <summary>Tuning for the hosted service that retries failed deliveries, bound from <c>Notifications:RetryDelivery</c>.</summary>
public sealed class RetryDeliveryWorkerOptions
{
    /// <summary>Configuration section the options bind from.</summary>
    public const string SectionName = "Notifications:RetryDelivery";

    /// <summary>Seconds between retry ticks.</summary>
    public int PollIntervalSeconds { get; init; } = 60;
    /// <summary>Maximum failed deliveries retried per tick.</summary>
    public int BatchSize { get; init; } = 50;
    /// <summary>Deliveries with this many attempts are given up on.</summary>
    public int MaxAttempts { get; init; } = 5;
}
