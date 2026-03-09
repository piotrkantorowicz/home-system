namespace DietPlanner.Api.Domain;

public class Product
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public decimal? CaloriesPer100g { get; set; }
    public decimal? ProteinPer100g { get; set; }
    public decimal? CarbsPer100g { get; set; }
    public decimal? FatPer100g { get; set; }
    public decimal? FiberPer100g { get; set; }
    public string DefaultUnit { get; set; } = "g";
    public decimal? DensityGramsPerMl { get; set; }  // For accurate volume conversions
    public decimal? GramPerPiece { get; set; }       // For "piece" units
    public required string CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    // Navigation properties
    public ICollection<RecipeIngredient> RecipeIngredients { get; set; } = new List<RecipeIngredient>();

    // Computed property
    public bool IsDeleted => DeletedAt.HasValue;
}
