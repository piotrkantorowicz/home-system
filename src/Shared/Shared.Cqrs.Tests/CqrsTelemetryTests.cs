namespace Shared.Cqrs.Tests;

using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Shared.Abstractions.Core.Domain;
using Shared.Abstractions.Cqrs;
using Shared.Infrastructure.Cqrs.Extensions;

/// <summary>Spans and the duration histogram emitted by the CQRS dispatcher chain.</summary>
public sealed class CqrsTelemetryTests : IDisposable
{
    private readonly List<Activity> _spans = [];
    private readonly ActivityListener _listener;
    private readonly ActivitySource _parentSource = new("Test.Parent");

    /// <summary>Starts listening to the CQRS and parent sources.</summary>
    public CqrsTelemetryTests()
    {
        _listener = new ActivityListener
        {
            ShouldListenTo = s => s.Name is "HomeSystem.Cqrs" or "Test.Parent",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = a => { lock (_spans) _spans.Add(a); },
        };
        ActivitySource.AddActivityListener(_listener);
    }

    /// <summary>Stops the listener.</summary>
    public void Dispose()
    {
        _listener.Dispose();
        _parentSource.Dispose();
    }

    private sealed record OkCommand : ICommand;
    private sealed record BoomCommand : ICommand;
    private sealed record InvalidCommand : ICommand;
    private sealed record MissingCommand : ICommand<int>;
    private sealed record ChildCommand : ICommand;
    private sealed record OkQuery : IQuery<int>;
    private sealed record BoomQuery : IQuery<int>;

    private sealed class Handlers :
        ICommandHandler<OkCommand>,
        ICommandHandler<BoomCommand>,
        ICommandHandler<InvalidCommand>,
        ICommandHandler<MissingCommand, int>,
        ICommandHandler<ChildCommand>,
        ICommandValidator<InvalidCommand>,
        IQueryHandler<OkQuery, int>,
        IQueryHandler<BoomQuery, int>
    {
        public Task HandleAsync(OkCommand command, CancellationToken ct = default) => Task.CompletedTask;
        public Task HandleAsync(BoomCommand command, CancellationToken ct = default) => throw new InvalidOperationException("x");
        public Task HandleAsync(InvalidCommand command, CancellationToken ct = default) => Task.CompletedTask;
        public Task<int> HandleAsync(MissingCommand command, CancellationToken ct = default) => throw new NotFoundException("Thing", 1);
        public Task HandleAsync(ChildCommand command, CancellationToken ct = default) => Task.CompletedTask;
        public Task<int> HandleAsync(OkQuery query, CancellationToken ct = default) => Task.FromResult(1);
        public Task<int> HandleAsync(BoomQuery query, CancellationToken ct = default) => throw new InvalidOperationException("x");
        public IEnumerable<ValidationError> Validate(InvalidCommand command) => [new("Name", "required")];
    }

    private static ServiceProvider Build()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IMeterFactory, TestMeterFactory>();
        var h = new Handlers();
        services.AddSingleton<ICommandHandler<OkCommand>>(h);
        services.AddSingleton<ICommandHandler<BoomCommand>>(h);
        services.AddSingleton<ICommandHandler<InvalidCommand>>(h);
        services.AddSingleton<ICommandHandler<MissingCommand, int>>(h);
        services.AddSingleton<ICommandHandler<ChildCommand>>(h);
        services.AddSingleton<ICommandValidator<InvalidCommand>>(h);
        services.AddSingleton<IQueryHandler<OkQuery, int>>(h);
        services.AddSingleton<IQueryHandler<BoomQuery, int>>(h);
        services.AddCqrsDispatchers();
        return services.BuildServiceProvider();
    }

    private static MetricCollector<double> Collect(ServiceProvider sp)
        => new(sp.GetRequiredService<IMeterFactory>(), "HomeSystem.Cqrs", "homesystem.cqrs.duration");

    private Activity Span(string name) => _spans.Single(s => s.GetTagItem("cqrs.name") as string == name);

    private static string Tag(CollectedMeasurement<double> m, string key) => (string)m.Tags[key]!;

    /// <summary>A successful command is a child span with kind and name tags and one success sample.</summary>
    [Fact]
    public async Task Command_success_is_a_child_span_with_tags_and_a_success_sample()
    {
        using var sp = Build();
        using var scope = sp.CreateScope();
        using var metrics = Collect(sp);

        using (var parent = _parentSource.StartActivity("request"))
        {
            await scope.ServiceProvider.GetRequiredService<ICommandDispatcher>().SendAsync(new OkCommand(), TestContext.Current.CancellationToken);
            var span = Span(nameof(OkCommand));
            span.ParentId.ShouldBe(parent!.Id);
        }

        var s = Span(nameof(OkCommand));
        s.OperationName.ShouldBe(nameof(OkCommand));
        s.GetTagItem("cqrs.kind").ShouldBe("command");
        s.GetTagItem("cqrs.outcome").ShouldBe("success");
        s.Status.ShouldBe(ActivityStatusCode.Unset);

        var m = metrics.GetMeasurementSnapshot().Single();
        Tag(m, "cqrs.kind").ShouldBe("command");
        Tag(m, "cqrs.name").ShouldBe(nameof(OkCommand));
        Tag(m, "cqrs.outcome").ShouldBe("success");
    }

    /// <summary>A throwing handler marks the span Error and records outcome error.</summary>
    [Fact]
    public async Task Command_failure_sets_error_status_and_error_outcome()
    {
        using var sp = Build();
        using var scope = sp.CreateScope();
        using var metrics = Collect(sp);

        await Should.ThrowAsync<InvalidOperationException>(() =>
            scope.ServiceProvider.GetRequiredService<ICommandDispatcher>().SendAsync(new BoomCommand()));

        Span(nameof(BoomCommand)).Status.ShouldBe(ActivityStatusCode.Error);
        Tag(metrics.GetMeasurementSnapshot().Single(), "cqrs.outcome").ShouldBe("error");
    }

    /// <summary>A failed validator records validation_error without marking the span Error.</summary>
    [Fact]
    public async Task Validation_failure_records_validation_error()
    {
        using var sp = Build();
        using var scope = sp.CreateScope();
        using var metrics = Collect(sp);

        await Should.ThrowAsync<CommandValidationException>(() =>
            scope.ServiceProvider.GetRequiredService<ICommandDispatcher>().SendAsync(new InvalidCommand()));

        Tag(metrics.GetMeasurementSnapshot().Single(), "cqrs.outcome").ShouldBe("validation_error");
        Span(nameof(InvalidCommand)).Status.ShouldBe(ActivityStatusCode.Unset);
    }

    /// <summary>NotFoundException records not_found.</summary>
    [Fact]
    public async Task Not_found_records_not_found_for_a_command_with_result()
    {
        using var sp = Build();
        using var scope = sp.CreateScope();
        using var metrics = Collect(sp);

        await Should.ThrowAsync<NotFoundException>(() =>
            scope.ServiceProvider.GetRequiredService<ICommandDispatcher>().SendAsync<MissingCommand, int>(new MissingCommand()));

        Tag(metrics.GetMeasurementSnapshot().Single(), "cqrs.outcome").ShouldBe("not_found");
    }

    /// <summary>Queries get the same span and histogram.</summary>
    [Fact]
    public async Task Query_success_and_failure_are_traced_and_measured()
    {
        using var sp = Build();
        using var scope = sp.CreateScope();
        using var metrics = Collect(sp);
        var queries = scope.ServiceProvider.GetRequiredService<IQueryDispatcher>();

        (await queries.SendAsync<OkQuery, int>(new OkQuery(), TestContext.Current.CancellationToken)).ShouldBe(1);
        await Should.ThrowAsync<InvalidOperationException>(() => queries.SendAsync<BoomQuery, int>(new BoomQuery()));

        Span(nameof(OkQuery)).GetTagItem("cqrs.kind").ShouldBe("query");
        Span(nameof(BoomQuery)).Status.ShouldBe(ActivityStatusCode.Error);
        metrics.GetMeasurementSnapshot().Select(m => Tag(m, "cqrs.outcome")).ShouldBe(["success", "error"]);
    }
}
