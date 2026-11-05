# Backend — Observability (OpenTelemetry)

The host emits OpenTelemetry traces, metrics and logs. Wiring lives in one place:
`src/Apis/HomeSystem.REST/Observability/ObservabilityExtensions.cs` (`builder.AddHomeSystemObservability()`).
Modules and `Shared.*` projects never configure OpenTelemetry; they only create an `ActivitySource` or a
`Meter` with a name the host already listens to.

## Export — off unless configured

| Variable | Effect |
|---|---|
| `OTEL_EXPORTER_OTLP_ENDPOINT` | **The switch.** Unset or empty → no exporter is registered; nothing leaves the process. Set → OTLP export of all three signals. |
| `OTEL_EXPORTER_OTLP_PROTOCOL` | `grpc` (default, `:4317`) or `http/protobuf` (`:4318`). |
| `OTEL_EXPORTER_OTLP_HEADERS` | Auth headers for a remote collector. |
| `OTEL_RESOURCE_ATTRIBUTES` | Extra resource attributes (e.g. `k8s.namespace.name=…`). |
| `OTEL_METRIC_EXPORT_INTERVAL` | Metric push interval in ms (default 60000). |

Only the standard `OTEL_*` variables: no `appsettings` section, no custom options class. `launchSettings.json`
and `appsettings.*.json` never set the endpoint; dev, tests and CI therefore export nothing.

Resource: `service.name=home-system-api`, `service.version` = the assembly informational version (includes the
commit SHA), `deployment.environment.name` = the ASP.NET Core environment name.

Local viewer: `docker compose --profile observability up -d` (Aspire dashboard, UI at `http://localhost:18888`),
then run the API with `OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317`. See `infrastructure/README.md`.

## What is instrumented

| Signal | Sources |
|---|---|
| Traces | ASP.NET Core (server spans named `METHOD route-template`), HttpClient, Npgsql (DB spans for EF Core *and* Dapper — both run on Npgsql), every `HomeSystem.*` `ActivitySource` |
| Metrics | ASP.NET Core / Kestrel, HttpClient, Npgsql, `System.Runtime`, every `HomeSystem.*` `Meter` |
| Logs | Every `ILogger` record, exported with the active `TraceId` / `SpanId` |

Not traced: `/health*` (probes) and `/openapi*`.

## Naming — `HomeSystem.<Area>`

Every `ActivitySource` and `Meter` is named `HomeSystem.<Area>`. The host subscribes with the wildcard
`HomeSystem.*`, so a new source needs **no host change** — only the name. Add the name to this table in the PR
that introduces it.

| Name | Kind | Owner | Slice |
|---|---|---|---|
| `HomeSystem.Workers` | ActivitySource | `Shared.Abstractions.Core` (`WorkerTelemetry`) | #808 |
| `HomeSystem.Workers` | Meter | `Shared.Abstractions.Core` (`WorkerTelemetry`) | #808 |
| `HomeSystem.Mcp` | Meter | `Shared.Infrastructure.Web` (`McpToolMetrics`) | #808 |

`HomeSystem.Workers` — every background worker runs its tick through `WorkerTelemetry.TrackTickAsync`: one **root**
span per tick named `<worker> tick` (current for the whole tick, so the tick's DB and HTTP spans are its children),
plus counter `homesystem.worker.ticks` (`{tick}`) and histogram `homesystem.worker.tick.duration` (`s`). Tags on
both: `homesystem.worker` (the type name; the outbox adds the module, `OutboxWorker.DietPlanner`) and `outcome`
(`success`, `error` when the tick throws, `cancelled` on shutdown). A new worker calls the helper from its
`RunOnceAsync` and registers it with `services.TryAddSingleton<WorkerTelemetry>()`; a worker that swallows its own
failures calls it *inside* the `try`, so the tick still counts as `error`.

`HomeSystem.Mcp` — histogram `homesystem.mcp.tool.duration` (`s`) for every tool call, on `/mcp`
(`McpCallLogging`) and in-process (`McpToolInvoker`). Tags: `homesystem.mcp.tool` (the tool name; a name outside
the registered catalog is `unknown`, so callers cannot grow the series) and `outcome` (`success`, `error` for an
error result, `exception`, `cancelled`).

Instrument names: lower-case, dot-separated, prefixed by the area — `homesystem.cqrs.dispatch.duration`,
`homesystem.messaging.outbox.pending`. Units follow UCUM (`s`, `By`, `{message}`). Durations are histograms in
seconds.

## Attributes — IDs only

A span, metric or log attribute may carry:

- identifiers: user ID (`sub`), household ID, aggregate IDs, message IDs, correlation IDs;
- types and outcomes: command / query / event type name, status code, error type, retry count;
- route templates (`/api/households/{id}`), never the concrete URL with its query string.

It must **never** carry user content: prompts or LLM replies, product / recipe / expense names, amounts,
free-text descriptions, e-mail addresses, request or response bodies, query strings, SQL parameter values.

How the host enforces its part:

- the server span drops `url.query`; on HttpClient spans the .NET runtime sets `url.full` and replaces the whole query with `?*`. Never switch that off (`System.Net.Http.DisableUriRedaction` / `DOTNET_SYSTEM_NET_HTTP_DISABLEURIREDACTION`);
- Npgsql records SQL text with `$n` placeholders only — never parameter values. Do not turn on parameter
  logging (`EnableSensitiveDataLogging`, Npgsql `IncludeErrorDetail`, parameter tags) in shipped code;
- request and response bodies are never recorded.

Logs follow `backend-coding-standards.md` → Logging: structured placeholders for IDs, no tokens, e-mails or
bodies. A placeholder value becomes an exported attribute, so the IDs-only rule applies to it too.

A slice that adds telemetry on a path handling user content (assistant, imports, MCP tools) adds a test that
the content does not reach a tag.

## Adding a metric — `IMeterFactory`

Create meters through DI so they are disposed with the host and isolated per test host. One meter class per
area, registered as a singleton:

```csharp
namespace Shared.Infrastructure.Cqrs.Telemetry;

using System.Diagnostics.Metrics;

internal sealed class CqrsMetrics
{
    public const string MeterName = "HomeSystem.Cqrs";

    private readonly Histogram<double> _duration;

    public CqrsMetrics(IMeterFactory meters)
    {
        var meter = meters.Create(MeterName);
        _duration = meter.CreateHistogram<double>("homesystem.cqrs.dispatch.duration", unit: "s");
    }

    public void RecordDispatch(string requestType, string outcome, TimeSpan elapsed)
        => _duration.Record(elapsed.TotalSeconds, new("homesystem.request.type", requestType), new("outcome", outcome));
}

// registration (module / shared Extensions): services.AddSingleton<CqrsMetrics>();
```

- Tags are low-cardinality: type names and outcomes, never IDs (IDs belong on spans, not metric tags).
- Traces: `private static readonly ActivitySource Source = new("HomeSystem.<Area>");` — a static source is fine;
  `StartActivity` returns `null` when nothing listens, so guard with `?.`.

## Testing

`Shared.Web.IntegrationTests/ObservabilityTests.cs` runs `AddHomeSystemObservability` on a minimal host with a
capturing processor and a real PostgreSQL: route-named server span, child Npgsql span, probes untraced,
`TraceId` on logs, no query string or parameter value on any tag, exporter only with the endpoint set. Copy that
pattern (`ConfigureOpenTelemetryTracerProvider(t => t.AddProcessor(...))`) for new sources; capture fields
from a `LogRecord` in `OnEnd`, because log records are pooled.
