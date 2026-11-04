namespace Shared.Web.IntegrationTests.Infrastructure;

using System.Collections.Concurrent;
using OpenTelemetry;

/// <summary>
/// Records what the OpenTelemetry pipeline finished, as <typeparamref name="TCaptured"/>. Log records are pooled
/// and reused, so the capture copies the fields a test needs instead of keeping the record.
/// </summary>
/// <typeparam name="T">The telemetry item (span or log record).</typeparam>
/// <typeparam name="TCaptured">What the test keeps of it.</typeparam>
/// <param name="capture">Copies the item when it ends.</param>
internal sealed class CapturingProcessor<T, TCaptured>(Func<T, TCaptured> capture) : BaseProcessor<T>
{
    private readonly ConcurrentQueue<TCaptured> _items = new();

    /// <summary>Everything captured so far, in completion order.</summary>
    public IReadOnlyList<TCaptured> Items => [.. _items];

    /// <inheritdoc />
    public override void OnEnd(T data) => _items.Enqueue(capture(data));
}
