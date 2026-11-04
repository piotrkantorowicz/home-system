namespace Shared.Web.IntegrationTests;

using System.Diagnostics;
using HomeSystem.REST.Observability;
using Microsoft.AspNetCore.Builder;
using Shared.Web.IntegrationTests.Infrastructure;

/// <summary>
/// The host's OpenTelemetry wiring: a request yields a server span named by its route with a child Npgsql span,
/// probes are not traced, logs carry the trace ID, nothing user-supplied reaches a tag, and the OTLP exporter is
/// registered only when <c>OTEL_EXPORTER_OTLP_ENDPOINT</c> is set.
/// </summary>
public sealed class ObservabilityTests(ObservabilityDatabaseFixture database) : IClassFixture<ObservabilityDatabaseFixture>
{
    private const string Route = "/items/{id}";
    private const string QueryValue = "query-value-must-not-leak";

    /// <summary>The server span carries the route template; the DB span is its child; neither leaks input.</summary>
    [Fact]
    public async Task Request_WithQueryString_ProducesRouteSpanWithChildDbSpanAndNoUserInput()
    {
        await using var host = await ObservabilityHost.StartAsync(database.ConnectionString);
        using var client = host.CreateClient();

        using var response = await client.GetAsync($"/items/7?search={QueryValue}", TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();

        var server = host.WaitForSpan(IsItemsServerSpan);
        server.DisplayName.ShouldBe($"GET {Route}");
        server.GetTagItem("url.query").ShouldBeNull();

        var db = host.WaitForSpan(span => span.Source.Name == "Npgsql" && span.ParentSpanId == server.SpanId);
        db.TraceId.ShouldBe(server.TraceId);

        foreach (var span in new[] { server, db })
        {
            span.TagObjects.ShouldNotContain(tag => Leaks(tag.Value));
        }
    }

    /// <summary>Probes are filtered out before a span starts.</summary>
    [Fact]
    public async Task HealthProbe_ProducesNoSpan()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var host = await ObservabilityHost.StartAsync(database.ConnectionString);
        using var client = host.CreateClient();

        using var probe = await client.GetAsync("/health/live", ct);
        probe.EnsureSuccessStatusCode();
        // A traced request after the probe: once its span is in, the probe's would be too.
        using var request = await client.GetAsync("/items/1", ct);
        request.EnsureSuccessStatusCode();
        host.WaitForSpan(IsItemsServerSpan);

        host.Spans.ShouldNotContain(span => span.GetTagItem("url.path") as string == "/health/live");
    }

    /// <summary>A log record written while handling a request is stamped with that request's trace.</summary>
    [Fact]
    public async Task Log_WrittenInsideRequest_CarriesItsTraceId()
    {
        await using var host = await ObservabilityHost.StartAsync(database.ConnectionString);
        using var client = host.CreateClient();

        using var response = await client.GetAsync("/items/3", TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();

        var server = host.WaitForSpan(IsItemsServerSpan);
        host.Logs.ShouldContain(log => log.Category == ObservabilityHost.LogCategory && log.TraceId == server.TraceId);
    }

    /// <summary>Without an endpoint nothing is exported; with one, the OTLP exporter is registered.</summary>
    /// <param name="endpoint">The <c>OTEL_EXPORTER_OTLP_ENDPOINT</c> value.</param>
    /// <param name="exported">Whether an OTLP exporter is expected.</param>
    [Theory]
    [InlineData("", false)]
    [InlineData("http://localhost:4317", true)]
    public void Exporter_IsRegisteredOnlyWhenTheEndpointIsSet(string endpoint, bool exported)
    {
        var builder = WebApplication.CreateBuilder();
        // Set explicitly, so an endpoint in the developer's shell does not leak into the "unset" case.
        builder.Configuration[ObservabilityExtensions.OtlpEndpointVariable] = endpoint;

        builder.AddHomeSystemObservability();

        builder.Services
            .Any(descriptor => (descriptor.ImplementationType ?? descriptor.ServiceType).FullName?.Contains("Otlp", StringComparison.Ordinal) == true)
            .ShouldBe(exported);
    }

    private static bool IsItemsServerSpan(Activity span)
        => span.Kind == ActivityKind.Server && span.GetTagItem("http.route") as string == Route;

    private static bool Leaks(object? value)
        => value?.ToString() is { } text
           && (text.Contains(QueryValue, StringComparison.Ordinal)
               || text.Contains(ObservabilityHost.ParameterValue, StringComparison.Ordinal));
}
