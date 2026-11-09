namespace Shared.Messaging.Tests.Telemetry;

using Microsoft.Extensions.DependencyInjection;
using Shared.Abstractions.Core.Telemetry;
using Shared.Infrastructure.Messaging.Extensions;
using Shared.Infrastructure.Messaging.Telemetry;
using Shouldly;

/// <summary>A host without the REST observability wiring still resolves worker telemetry.</summary>
public sealed class WorkerTelemetryRegistrationTests
{
    /// <summary>The messaging registration provides the fallback, and repeating it adds no second descriptor.</summary>
    [Fact]
    public void AddMessagingMetrics_RegistersWorkerTelemetryFallbackOnce()
    {
        var services = new ServiceCollection();

        services.AddMessagingMetrics().AddMessagingMetrics();

        services.Count(d => d.ServiceType == typeof(IWorkerTelemetry)).ShouldBe(1);
        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<IWorkerTelemetry>().ShouldBeOfType<WorkerTelemetry>();
    }
}
