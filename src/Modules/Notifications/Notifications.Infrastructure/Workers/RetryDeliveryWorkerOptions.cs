namespace Notifications.Infrastructure.Workers;

public sealed class RetryDeliveryWorkerOptions
{
    public const string SectionName = "Notifications:RetryDelivery";

    public int PollIntervalSeconds { get; init; } = 60;
    public int BatchSize { get; init; } = 50;
    public int MaxAttempts { get; init; } = 5;
}
