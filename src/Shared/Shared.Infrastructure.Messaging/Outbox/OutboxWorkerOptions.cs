namespace Shared.Infrastructure.Messaging.Outbox;

public sealed class OutboxWorkerOptions
{
    public const string SectionName = "Messaging:Outbox";

    public int BatchSize { get; init; } = 50;
    public int PollIntervalMs { get; init; } = 1000;
}
