namespace DietPlanner.Application.OperationReceipts;

using System.Text.RegularExpressions;

/// <summary>
/// Shape of a caller-chosen idempotency key: 8 to 128 letters, digits, dots, underscores, colons or hyphens (a UUID fits).
/// </summary>
internal static partial class OperationKeyInput
{
    public const string Error = "OperationKey must be 8 to 128 letters, digits, '.', '_', ':' or '-'.";

    public static bool IsValid(string key) => KeyPattern().IsMatch(key);

    [GeneratedRegex("^[A-Za-z0-9._:-]{8,128}$")]
    private static partial Regex KeyPattern();
}
