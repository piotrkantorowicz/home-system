namespace Budget.UnitTests;

using Microsoft.Extensions.Time.Testing;

/// <summary>Fixed instant the unit tests treat as "now": 2026-10-02 10:00 UTC.</summary>
internal static class TestClock
{
    public static readonly DateTimeOffset Now = new(2026, 10, 2, 10, 0, 0, TimeSpan.Zero);

    public static DateTime UtcNow => Now.UtcDateTime;

    public static FakeTimeProvider Create() => new(Now);
}
