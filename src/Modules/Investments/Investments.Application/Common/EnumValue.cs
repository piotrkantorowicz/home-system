namespace Investments.Application.Common;

/// <summary>Strict parsing of API enum strings: defined names only, case-insensitive, no numeric values.</summary>
internal static class EnumValue
{
    public static bool TryParse<TEnum>(string? value, out TEnum result)
        where TEnum : struct, Enum
        => Enum.TryParse(value?.Trim(), ignoreCase: true, out result)
           && Enum.IsDefined(result)
           && value!.Trim().All(char.IsLetter); // rejects numeric values such as "1"

    public static TEnum Parse<TEnum>(string value)
        where TEnum : struct, Enum
        => Enum.Parse<TEnum>(value.Trim(), ignoreCase: true);

    public static string Names<TEnum>()
        where TEnum : struct, Enum
        => string.Join(", ", Enum.GetNames<TEnum>());
}
