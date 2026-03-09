using DietPlanner.Api.Domain;

namespace DietPlanner.Api.Common.Utils;

public static class UnitConverter
{
    private static readonly Dictionary<string, decimal> WeightToGrams = new(StringComparer.OrdinalIgnoreCase)
    {
        ["g"] = 1m,
        ["gram"] = 1m,
        ["grams"] = 1m,
        ["kg"] = 1000m,
        ["kilogram"] = 1000m,
        ["kilograms"] = 1000m,
        ["oz"] = 28.3495m,
        ["ounce"] = 28.3495m,
        ["ounces"] = 28.3495m,
        ["lb"] = 453.592m,
        ["lbs"] = 453.592m,
        ["pound"] = 453.592m,
        ["pounds"] = 453.592m
    };

    private static readonly Dictionary<string, decimal> VolumeToMilliliters = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ml"] = 1m,
        ["milliliter"] = 1m,
        ["milliliters"] = 1m,
        ["l"] = 1000m,
        ["liter"] = 1000m,
        ["liters"] = 1000m,
        ["cup"] = 240m,
        ["cups"] = 240m,
        ["tbsp"] = 15m,
        ["tablespoon"] = 15m,
        ["tablespoons"] = 15m,
        ["tsp"] = 5m,
        ["teaspoon"] = 5m,
        ["teaspoons"] = 5m,
        ["fl oz"] = 29.5735m,
        ["floz"] = 29.5735m
    };

    private static readonly string[] PieceUnits = { "piece", "pieces", "pcs", "pc", "unit", "units" };

    /// <summary>
    /// Converts any unit to grams for nutrition calculation
    /// </summary>
    /// <param name="amount">Amount to convert</param>
    /// <param name="unit">Unit to convert from</param>
    /// <param name="product">Product (needed for density and piece weight)</param>
    /// <returns>Amount in grams</returns>
    public static decimal ConvertToGrams(decimal amount, string unit, Product product)
    {
        // Direct weight conversion
        if (WeightToGrams.TryGetValue(unit, out var weightFactor))
        {
            return amount * weightFactor;
        }

        // Volume to grams (requires density)
        if (VolumeToMilliliters.TryGetValue(unit, out var volumeFactor))
        {
            var milliliters = amount * volumeFactor;
            var density = product.DensityGramsPerMl ?? 1.0m; // Default to water density
            return milliliters * density;
        }

        // Piece-based units
        if (IsPieceUnit(unit))
        {
            if (product.GramPerPiece.HasValue)
            {
                return amount * product.GramPerPiece.Value;
            }

            // Fallback: assume 100g per piece
            return amount * 100m;
        }

        // Unknown unit - assume grams
        return amount;
    }

    /// <summary>
    /// Checks if a unit is valid and recognized by the converter
    /// </summary>
    public static bool IsValidUnit(string unit)
    {
        if (string.IsNullOrWhiteSpace(unit))
            return false;

        return WeightToGrams.ContainsKey(unit)
               || VolumeToMilliliters.ContainsKey(unit)
               || IsPieceUnit(unit);
    }

    /// <summary>
    /// Gets all supported units
    /// </summary>
    public static IEnumerable<string> GetSupportedUnits()
    {
        return WeightToGrams.Keys
            .Concat(VolumeToMilliliters.Keys)
            .Concat(PieceUnits)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(u => u);
    }

    /// <summary>
    /// Checks if a unit is a piece-based unit
    /// </summary>
    private static bool IsPieceUnit(string unit)
    {
        return PieceUnits.Contains(unit, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the category of a unit (weight, volume, or piece)
    /// </summary>
    public static string GetUnitCategory(string unit)
    {
        if (WeightToGrams.ContainsKey(unit))
            return "weight";

        if (VolumeToMilliliters.ContainsKey(unit))
            return "volume";

        if (IsPieceUnit(unit))
            return "piece";

        return "unknown";
    }
}
