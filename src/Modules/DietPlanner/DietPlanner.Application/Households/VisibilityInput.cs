namespace DietPlanner.Application.Households;

using DietPlanner.Domain.ValueObjects;

/// <summary>Parses the optional <c>visibility</c> string of library create / update commands.</summary>
internal static class VisibilityInput
{
    public const string Error = "Visibility must be Private, Household or Public.";

    /// <summary>True for <see langword="null"/> (keep / default) or a named <see cref="Visibility"/>.</summary>
    public static bool IsValid(string? value) => value is null || Parse(value) is not null;

    /// <summary>The named <see cref="Visibility"/>, case-insensitive; <see langword="null"/> otherwise.</summary>
    public static Visibility? Parse(string? value)
        => Enum.TryParse(value, ignoreCase: true, out Visibility visibility) && Enum.IsDefined(visibility)
            ? visibility
            : null;
}
