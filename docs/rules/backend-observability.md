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
| `HomeSystem.Cqrs` | ActivitySource | `Shared.Infrastructure.Cqrs` (`CqrsTelemetry`) | #804 |
| `HomeSystem.Cqrs` | Meter | `Shared.Infrastructure.Cqrs` (`CqrsTelemetry`) | #804 |
| `HomeSystem.Messaging` | Meter | `Shared.Infrastructure.Messaging` (`MessagingMetrics`) | #805 |
| `HomeSystem.Messaging` | ActivitySource | `Shared.Infrastructure.Messaging` (`MessagingTracing`) | #806 |
| `HomeSystem.Assistant` | ActivitySource | `Assistant.Api` (`AssistantTelemetry`; chat spans from the `UseOpenTelemetry` wrapper) | #807 |
| `HomeSystem.Assistant` | Meter | `Assistant.Api` (`AssistantTelemetry`; GenAI metrics from the wrapper) | #807 |

`WorkerTelemetry` lives in `Shared.Abstractions.Core` deliberately: Application-layer workers (`DietReminderTickService`) may only reference `Shared.Abstractions.*`.

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
error result or a throwing tool, `cancelled`).

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

`HomeSystem.Messaging` spans: `<Event> publish` (producer, at enqueue, child of the caller's span) and `<Event> process` (consumer). The producer's W3C `traceparent` is stored in
the outbox row's nullable `trace_parent` column; the consumer span is its child and links to the worker's `OutboxWorker.<module> tick` span (`HomeSystem.Workers`), which carries `homesystem.module` and `homesystem.outbox.batch_size` for a non-empty batch. A row
without `trace_parent` (enqueued untraced or before the column) is handled under a new root span. Attributes:
event type name, event id, outbox message id. `tracestate` and baggage are not propagated.

`HomeSystem.Assistant` — the chat client is wrapped with `UseOpenTelemetry(sourceName: "HomeSystem.Assistant")` and
`EnableSensitiveData = false` for every provider (done where `AssistantAgent` is registered), which gives chat
spans and the GenAI metrics `gen_ai.client.token.usage` and `gen_ai.client.operation.duration` tagged with the
model and provider. On top: one `execute_tool {name}` span per tool call (tags `gen_ai.tool.name`,
`homesystem.assistant.outcome`) and counters `homesystem.assistant.tool_calls` (`{call}`; tags `tool`,
`outcome` = `ok`, `error`, `refused`) and `homesystem.assistant.turns` (`{turn}`; tag `outcome` = `completed`,
`failed`, `limit_reached`; a cancelled turn or tool call records nothing). Never a prompt, reply, tool argument or result.
A failed chat span carries `error.type` and the provider's exception message as its status description: provider
error text, not request content.

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
