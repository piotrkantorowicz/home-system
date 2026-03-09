namespace DietPlanner.Api.Domain;

public class RecipeIngredient
{
    public Guid Id { get; set; }
    public Guid RecipeId { get; set; }
    public Guid ProductId { get; set; }
    public decimal Amount { get; set; }
    public string Unit { get; set; } = "g";

    // Navigation properties
    public Recipe Recipe { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
