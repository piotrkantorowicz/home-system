namespace Notifications.Infrastructure.Dispatching;

using System.Text.RegularExpressions;

internal static partial class PlaceholderRenderer
{
    [GeneratedRegex(@"\{\{(\w+)\}\}")]
    private static partial Regex PlaceholderPattern();

    internal static string Render(string template, IReadOnlyDictionary<string, string>? values)
    {
        if (values is null || values.Count == 0) return template;
        return PlaceholderPattern().Replace(template, m =>
            values.TryGetValue(m.Groups[1].Value, out var v) ? v : m.Value);
    }
}
