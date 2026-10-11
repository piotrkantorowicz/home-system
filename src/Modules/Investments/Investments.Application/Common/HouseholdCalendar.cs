namespace Investments.Application.Common;

/// <summary>
/// The household's calendar: dates are Polish dates (NBP publishes on Warsaw business days), so
/// "today" is today in Europe/Warsaw, not in UTC — a balance entered just after midnight is not
/// "in the future".
/// </summary>
internal static class HouseholdCalendar
{
    private static readonly TimeZoneInfo Warsaw = TimeZoneInfo.FindSystemTimeZoneById("Europe/Warsaw");

    public static DateOnly Today(TimeProvider clock)
        => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(clock.GetUtcNow(), Warsaw).DateTime);
}
