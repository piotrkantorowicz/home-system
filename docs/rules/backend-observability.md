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
| `HomeSystem.Cqrs` | ActivitySource | `Shared.Infrastructure.Cqrs` (`CqrsTelemetry`) | #804 |
| `HomeSystem.Cqrs` | Meter | `Shared.Infrastructure.Cqrs` (`CqrsTelemetry`) | #804 |
| `HomeSystem.Messaging` | Meter | `Shared.Infrastructure.Messaging` (`MessagingMetrics`) | #805 |
| `HomeSystem.Messaging` | ActivitySource | `Shared.Infrastructure.Messaging` (`MessagingTracing`) | #806 |

`HomeSystem.Cqrs` — one span per dispatched command or query (outermost dispatcher decorator, so validation
failures are included), named after the request type, plus histogram `homesystem.cqrs.duration` (`s`). Span tags
and metric tags are the same: `cqrs.kind` (`command` / `query`), `cqrs.name` (request type name),
`cqrs.outcome` (`success`, `validation_error`, `not_found`, `forbidden`, `conflict`, `rejected` for a domain
rule, `cancelled`, `error`). Only `error` sets the span status to Error; the others are 4xx rejections or client
aborts, not faults.

`HomeSystem.Messaging` instruments (tags: `homesystem.module` = publishing module, `homesystem.event.type` and
`homesystem.handler.type` = short type names, `outcome`):

| Instrument | Type, unit | Tags |
|---|---|---|
| `homesystem.outbox.pending` | observable gauge, `{message}` | module |
| `homesystem.outbox.oldest_pending_age` | observable gauge, `s` | module |
| `homesystem.outbox.publish.duration` | histogram, `s` | event type, outcome (`published`, `failed`) |
| `homesystem.outbox.dead_letters` | counter, `{message}` | module, event type |
| `homesystem.inbox.handle.duration` | histogram, `s` | event type, handler type, outcome (`handled`, `duplicate`, `error`) |

The gauges report the backlog the module's `OutboxWorker` stored at the end of its last tick (dead letters are
excluded); a module without a running worker reports nothing. A handler cancelled by shutdown records no inbox
measurement, so deploys do not show up as `error`.

`HomeSystem.Messaging` spans: `<Event> publish` (producer, at enqueue, child of the caller's span), `outbox tick`
(worker, one per non-empty batch) and `<Event> process` (consumer). The producer's W3C `traceparent` is stored in
the outbox row's nullable `trace_parent` column; the consumer span is its child and links to the tick span. A row
without `trace_parent` (enqueued untraced or before the column) is handled under a new root span. Attributes:
event type name, event id, outbox message id. `tracestate` and baggage are not propagated.

Instrument names: lower-case, dot-separated, prefixed by the area — `homesystem.cqrs.duration`,
`homesystem.outbox.pending`. Units follow UCUM (`s`, `By`, `{message}`). Durations are histograms in
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
        _duration = meter.CreateHistogram<double>("homesystem.cqrs.duration", unit: "s");
    }

    public void RecordDispatch(string requestType, string outcome, TimeSpan elapsed)
        => _duration.Record(elapsed.TotalSeconds, new("cqrs.name", requestType), new("cqrs.outcome", outcome));
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
