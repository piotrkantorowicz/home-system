namespace DietPlanner.UnitTests.Domain;

using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.Exceptions;
using DietPlanner.Domain.ValueObjects;

public sealed class ProductTests
{
    [Fact]
    public void Create_WithValidData_CreatesProduct()
    {
        var id = ProductId.New();
        var nutrition = new NutritionPer100g(100m, 10m, 5m, 3m, 2m);

        var product = Product.Create(id, "Chicken Breast", nutrition, "g", null, null, "user-1");

        product.Id.ShouldBe(id);
        product.Name.ShouldBe("Chicken Breast");
        product.Nutrition.ShouldBe(nutrition);
        product.DefaultUnit.ShouldBe("g");
        product.CreatedByUserId.ShouldBe("user-1");
        product.IsDeleted.ShouldBeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithEmptyName_ThrowsArgumentException(string? name)
    {
        var act = () => Product.Create(ProductId.New(), name!, new NutritionPer100g(null, null, null, null, null), "g", null, null, "user-1");

        act.ShouldThrow<ArgumentException>();
    }

    [Fact]
    public void Update_WithValidData_UpdatesProduct()
    {
        var product = Product.Create(ProductId.New(), "Old Name", new NutritionPer100g(null, null, null, null, null), "g", null, null, "user-1");
        var newNutrition = new NutritionPer100g(200m, 20m, 10m, 6m, 3m);

        product.Update("New Name", newNutrition, "ml", 1.0m, null);

        product.Name.ShouldBe("New Name");
        product.Nutrition.ShouldBe(newNutrition);
        product.DefaultUnit.ShouldBe("ml");
        product.DensityGramsPerMl.ShouldBe(1.0m);
        product.UpdatedAt.ShouldNotBeNull();
    }

    [Fact]
    public void SoftDelete_WhenNotDeleted_SetsDeletedAt()
    {
        var product = Product.Create(ProductId.New(), "Milk", new NutritionPer100g(null, null, null, null, null), "ml", null, null, "user-1");

        product.SoftDelete();

        product.IsDeleted.ShouldBeTrue();
        product.DeletedAt.ShouldNotBeNull();
    }

    [Fact]
    public void SoftDelete_WhenAlreadyDeleted_ThrowsDomainException()
    {
        var product = Product.Create(ProductId.New(), "Milk", new NutritionPer100g(null, null, null, null, null), "ml", null, null, "user-1");
        product.SoftDelete();

        var act = () => product.SoftDelete();

        act.ShouldThrow<DietPlannerDomainException>();
    }

    [Fact]
    public void Restore_WhenDeleted_ClearsDeletedAt()
    {
        var product = Product.Create(ProductId.New(), "Milk", new NutritionPer100g(null, null, null, null, null), "ml", null, null, "user-1");
        product.SoftDelete();

        product.Restore();

        product.IsDeleted.ShouldBeFalse();
        product.DeletedAt.ShouldBeNull();
    }
}
