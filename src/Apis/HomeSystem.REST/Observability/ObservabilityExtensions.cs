namespace HomeSystem.REST.Observability;

using System.Reflection;
using Npgsql;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

/// <summary>
/// OpenTelemetry traces, metrics and logs for the host. The pipeline is always built; the OTLP exporter is added
/// only when <c>OTEL_EXPORTER_OTLP_ENDPOINT</c> is set, so dev, tests and CI export nothing. Conventions:
/// docs/rules/backend-observability.md.
/// </summary>
public static class ObservabilityExtensions
{
    /// <summary>The <c>service.name</c> resource attribute.</summary>
    public const string ServiceName = "home-system-api";

    /// <summary>
    /// Every HomeSystem <see cref="System.Diagnostics.ActivitySource"/> and <see cref="System.Diagnostics.Metrics.Meter"/>
    /// is named <c>HomeSystem.&lt;Area&gt;</c>; the wildcard picks up each one a later slice adds.
    /// </summary>
    public const string HomeSystemNames = "HomeSystem.*";

    /// <summary>The standard variable that switches export on.</summary>
    public const string OtlpEndpointVariable = "OTEL_EXPORTER_OTLP_ENDPOINT";

    private const string DeploymentEnvironmentAttribute = "deployment.environment.name";
    private const string RuntimeMeter = "System.Runtime";
    private const string UrlQueryAttribute = "url.query";

    /// <summary>Registers tracing, metrics and logging, plus the OTLP exporter when an endpoint is configured.</summary>
    /// <param name="builder">The host builder.</param>
    /// <returns>The same builder.</returns>
    public static IHostApplicationBuilder AddHomeSystemObservability(this IHostApplicationBuilder builder)
    {
        string? version = typeof(ObservabilityExtensions).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        var telemetry = builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(ServiceName, serviceVersion: version)
                .AddAttributes([new(DeploymentEnvironmentAttribute, builder.Environment.EnvironmentName)]))
            .WithTracing(tracing => tracing
                .AddSource(HomeSystemNames)
                .AddAspNetCoreInstrumentation(options =>
                {
                    options.Filter = context => IsTraced(context.Request.Path);
                    // Query strings can carry tokens and search terms; the route template is enough.
                    options.EnrichWithHttpRequest = (activity, _) => activity.SetTag(UrlQueryAttribute, null);
                })
                // Outgoing url.full keeps query keys only: the instrumentation redacts every value by default.
                .AddHttpClientInstrumentation()
                // Npgsql records the SQL text with $n placeholders, never parameter values. EF Core and Dapper both go
                // through it, so this is the DB span for every module.
                .AddNpgsql())
            .WithMetrics(metrics => metrics
                .AddMeter(HomeSystemNames)
                .AddMeter(RuntimeMeter)
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddNpgsqlInstrumentation())
            .WithLogging();

        if (!string.IsNullOrWhiteSpace(builder.Configuration[OtlpEndpointVariable]))
        {
            telemetry.UseOtlpExporter();
        }

        return builder;
    }

    private static bool IsTraced(PathString path)
        => !path.StartsWithSegments("/health") && !path.StartsWithSegments("/openapi");
}
