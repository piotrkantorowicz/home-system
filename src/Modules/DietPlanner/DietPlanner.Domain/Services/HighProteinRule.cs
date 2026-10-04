namespace DietPlanner.Domain.Services;

/// <summary>
/// The approved high-protein definition for a recipe (redesign v3 decision #1): per serving, protein of at
/// least 25 g, or at least 30% of the calories coming from protein (protein g × 4 kcal ÷ calories).
/// </summary>
public static class HighProteinRule
{
    /// <summary>Per-serving protein, in grams, that qualifies on its own.</summary>
    public const decimal MinProteinGramsPerServing = 25m;

    /// <summary>Share of calories from protein that qualifies on its own, as a fraction.</summary>
    public const decimal MinProteinCalorieShare = 0.30m;

    private const decimal KcalPerGramProtein = 4m;

    /// <summary>Whether one serving counts as high-protein.</summary>
    /// <param name="proteinPerServing">Protein per serving in grams; the caller excludes recipes where it is unknown.</param>
    /// <param name="caloriesPerServing">Calories per serving, or <see langword="null"/> when unknown or zero, in which case only the grams test applies.</param>
    public static bool IsHighProtein(decimal proteinPerServing, decimal? caloriesPerServing)
        => proteinPerServing >= MinProteinGramsPerServing
            || (caloriesPerServing is > 0
                && proteinPerServing * KcalPerGramProtein / caloriesPerServing.Value >= MinProteinCalorieShare);
}
