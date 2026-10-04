namespace DietPlanner.IntegrationTests.Infrastructure;

using System.Collections.Concurrent;
using System.Reflection;
using Xunit.v3;

/// <summary>
/// Remembers every <see cref="DietPlannerWebApplicationFactory"/> a test boots, so they can be disposed
/// when that test ends instead of living (with their hosted services) until the process exits.
/// </summary>
internal static class TestHostTracker
{
    private static readonly ConcurrentDictionary<string, ConcurrentBag<IDisposable>> Hosts = new();

    internal static void Track(IDisposable host)
    {
        if (TestContext.Current.Test is { } test)
            Hosts.GetOrAdd(test.UniqueID, _ => []).Add(host);
    }

    internal static void DisposeAll(string testId)
    {
        if (!Hosts.TryRemove(testId, out var hosts))
            return;

        foreach (var host in hosts)
            host.Dispose();
    }
}

/// <summary>Assembly-wide hook: disposes the hosts a test created once the test has finished.</summary>
[AttributeUsage(AttributeTargets.Assembly)]
public sealed class DisposeTestHostsAttribute : BeforeAfterTestAttribute
{
    /// <inheritdoc />
    public override void After(MethodInfo methodUnderTest, IXunitTest test) => TestHostTracker.DisposeAll(test.UniqueID);
}
