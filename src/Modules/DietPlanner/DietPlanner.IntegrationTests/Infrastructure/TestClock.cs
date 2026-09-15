namespace DietPlanner.IntegrationTests.Infrastructure;

using Microsoft.Extensions.Time.Testing;

/// <summary>
/// Fixed instant the test host treats as "now" — <see cref="DietPlannerWebApplicationFactory"/> replaces the
/// <see cref="TimeProvider"/> with a <see cref="FakeTimeProvider"/> pinned here, so requests and the dates
/// tests send stay on the same calendar day regardless of the wall clock.
/// </summary>
public static class TestClock
{
    /// <summary>The pinned instant: 2026-09-12 10:00 UTC.</summary>
    public static readonly DateTimeOffset Now = new(2026, 9, 12, 10, 0, 0, TimeSpan.Zero);

    /// <summary><see cref="Now"/> as a UTC <see cref="DateTime"/>.</summary>
    public static DateTime UtcNow => Now.UtcDateTime;

    /// <summary>The calendar day of <see cref="Now"/>.</summary>
    public static DateOnly Today => DateOnly.FromDateTime(UtcNow);

    /// <summary>Creates a <see cref="FakeTimeProvider"/> pinned at <see cref="Now"/>.</summary>
    public static FakeTimeProvider Create() => new(Now);
}
