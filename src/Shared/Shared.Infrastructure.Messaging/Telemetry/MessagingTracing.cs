namespace Shared.Infrastructure.Messaging.Telemetry;

using System.Diagnostics;

/// <summary>
/// The <c>HomeSystem.Messaging</c> <see cref="ActivitySource"/>: producer span at enqueue, worker tick
/// span, consumer span at handling. Tags carry type names and ids only — never payload data.
/// </summary>
internal static class MessagingTracing
{
    /// <summary>Source name; the host subscribes to <c>HomeSystem.*</c>.</summary>
    public const string SourceName = "HomeSystem.Messaging";

    public const string EventTypeTag = "homesystem.event.type";
    public const string EventIdTag = "homesystem.event.id";
    public const string MessageIdTag = "messaging.message.id";

    public static readonly ActivitySource Source = new(SourceName);
}
