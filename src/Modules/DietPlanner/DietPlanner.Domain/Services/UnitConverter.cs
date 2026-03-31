namespace DietPlanner.Domain.Services;

public static class UnitConverter
{
    private static readonly Dictionary<string, string> UnitAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["g"] = "g", ["gram"] = "g", ["grams"] = "g",
        ["kg"] = "kg", ["kilogram"] = "kg", ["kilograms"] = "kg",
        ["oz"] = "oz", ["ounce"] = "oz", ["ounces"] = "oz",
        ["lb"] = "lb", ["lbs"] = "lb", ["pound"] = "lb", ["pounds"] = "lb",
        ["ml"] = "ml", ["milliliter"] = "ml", ["milliliters"] = "ml",
        ["l"] = "l", ["liter"] = "l", ["liters"] = "l",
        ["cup"] = "cup", ["cups"] = "cup",
        ["tbsp"] = "tbsp", ["tablespoon"] = "tbsp", ["tablespoons"] = "tbsp",
        ["tsp"] = "tsp", ["teaspoon"] = "tsp", ["teaspoons"] = "tsp",
        ["fl oz"] = "fl oz", ["floz"] = "fl oz",
        ["piece"] = "piece", ["pieces"] = "piece", ["pcs"] = "piece", ["pc"] = "piece",
        ["unit"] = "piece", ["units"] = "piece"
    };

    private static readonly Dictionary<string, decimal> WeightToGrams = new()
    {
        ["g"] = 1m,
        ["kg"] = 1000m,
        ["oz"] = 28.3495m,
        ["lb"] = 453.592m
    };

    private static readonly Dictionary<string, decimal> VolumeToMl = new()
    {
        ["ml"] = 1m,
        ["l"] = 1000m,
        ["cup"] = 236.588m,
        ["tbsp"] = 14.787m,
        ["tsp"] = 4.929m,
        ["fl oz"] = 29.5735m
    };

    public static decimal ConvertToGrams(decimal amount, string unit, decimal? densityGramsPerMl, decimal? gramPerPiece)
    {
        var normalizedUnit = NormalizeUnit(unit);

        if (WeightToGrams.TryGetValue(normalizedUnit, out var weightFactor))
            return amount * weightFactor;

        if (VolumeToMl.TryGetValue(normalizedUnit, out var volumeFactor))
        {
            var ml = amount * volumeFactor;
            return ml * (densityGramsPerMl ?? 1.0m);
        }

        if (normalizedUnit == "piece")
            return amount * (gramPerPiece ?? 100m);

        return amount;
    }

    public static bool IsValidUnit(string unit)
        => UnitAliases.ContainsKey(unit);

    public static IEnumerable<string> GetSupportedUnits()
        => UnitAliases.Keys;

    public static string GetUnitCategory(string unit)
    {
        var normalized = NormalizeUnit(unit);
        if (WeightToGrams.ContainsKey(normalized)) return "weight";
        if (VolumeToMl.ContainsKey(normalized)) return "volume";
        if (normalized == "piece") return "piece";
        return "unknown";
    }

    private static string NormalizeUnit(string unit)
        => UnitAliases.TryGetValue(unit, out var normalized) ? normalized : unit.ToLowerInvariant();
}
