namespace DietPlanner.UnitTests;

using Microsoft.Extensions.Time.Testing;

/// <summary>Fixed instant every unit test treats as "now", so nothing depends on the wall clock.</summary>
internal static class TestClock
{
    /// <summary>The pinned instant: 2026-09-12 10:00 UTC.</summary>
    public static readonly DateTimeOffset Now = new(2026, 9, 12, 10, 0, 0, TimeSpan.Zero);

    /// <summary><see cref="Now"/> as a UTC <see cref="DateTime"/> — the type aggregates store.</summary>
    public static DateTime UtcNow => Now.UtcDateTime;

    /// <summary>The calendar day of <see cref="Now"/>.</summary>
    public static DateOnly Today => DateOnly.FromDateTime(UtcNow);

    /// <summary>Creates a <see cref="FakeTimeProvider"/> pinned at <see cref="Now"/>.</summary>
    public static FakeTimeProvider Create() => new(Now);
}
