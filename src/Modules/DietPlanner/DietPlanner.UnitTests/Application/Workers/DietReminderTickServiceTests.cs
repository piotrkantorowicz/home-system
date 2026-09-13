namespace DietPlanner.UnitTests.Application.Workers;

#pragma warning disable IDE0005
using DietPlanner.Application.Workers;
#pragma warning restore IDE0005
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;

public sealed class DietReminderTickServiceTests
{
    [Fact]
    public async Task RunOnceAsync_InvokesEachRegisteredJob_WithUtcNowApproximately()
    {
        var jobA = new RecordingJob("A");
        var jobB = new RecordingJob("B");
        var services = new ServiceCollection();
        services.AddScoped<IDietReminderJob>(_ => jobA);
        services.AddScoped<IDietReminderJob>(_ => jobB);
        var sp = services.BuildServiceProvider();

        var sut = new DietReminderTickService(
            sp.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new DietReminderTickServiceOptions()),
            NullLogger<DietReminderTickService>.Instance);

        var before = DateTime.UtcNow;
        await sut.RunOnceAsync(CancellationToken.None);
        var after = DateTime.UtcNow;

        jobA.Calls.Count.ShouldBe(1);
        jobB.Calls.Count.ShouldBe(1);
        jobA.Calls[0].ShouldBeInRange(before, after);
    }

    [Fact]
    public async Task RunOnceAsync_OneJobThrows_OtherStillRuns()
    {
        var failing = new ThrowingJob();
        var ok = new RecordingJob("ok");
        var services = new ServiceCollection();
        services.AddScoped<IDietReminderJob>(_ => failing);
        services.AddScoped<IDietReminderJob>(_ => ok);
        var sp = services.BuildServiceProvider();

        var sut = new DietReminderTickService(
            sp.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new DietReminderTickServiceOptions()),
            NullLogger<DietReminderTickService>.Instance);

        await sut.RunOnceAsync(CancellationToken.None);

        ok.Calls.Count.ShouldBe(1);
    }

    [Fact]
    public async Task RunOnceAsync_OneJobThrows_LogsOneErrorNamingTheJob()
    {
        var failing = new ThrowingJob();
        var services = new ServiceCollection();
        services.AddScoped<IDietReminderJob>(_ => failing);
        var sp = services.BuildServiceProvider();
        var logger = new FakeLogger<DietReminderTickService>();

        var sut = new DietReminderTickService(
            sp.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new DietReminderTickServiceOptions()),
            logger);

        await sut.RunOnceAsync(CancellationToken.None);

        var record = logger.Collector.GetSnapshot().ShouldHaveSingleItem();
        record.Level.ShouldBe(LogLevel.Error);
        record.Exception.ShouldBeOfType<InvalidOperationException>();
        record.StructuredState.ShouldNotBeNull()
            .ShouldContain(kv => kv.Key == "JobName" && kv.Value == failing.Name);
    }

    [Fact]
    public async Task RunOnceAsync_WhenDisabled_DoesNothing()
    {
        var job = new RecordingJob("J");
        var services = new ServiceCollection();
        services.AddScoped<IDietReminderJob>(_ => job);
        var sp = services.BuildServiceProvider();

        var sut = new DietReminderTickService(
            sp.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new DietReminderTickServiceOptions { Enabled = false }),
            NullLogger<DietReminderTickService>.Instance);

        await sut.RunOnceAsync(CancellationToken.None);

        job.Calls.ShouldBeEmpty();
    }

    private sealed class RecordingJob(string name) : IDietReminderJob
    {
        public string Name { get; } = name;
        public List<DateTime> Calls { get; } = [];
        public Task RunAsync(DateTime nowUtc, CancellationToken ct)
        {
            Calls.Add(nowUtc);
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingJob : IDietReminderJob
    {
        public string Name => "throw";
        public Task RunAsync(DateTime nowUtc, CancellationToken ct)
            => throw new InvalidOperationException("boom");
    }
}
