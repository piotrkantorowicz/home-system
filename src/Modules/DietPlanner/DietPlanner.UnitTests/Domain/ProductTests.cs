namespace DietPlanner.UnitTests.Domain;

using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.Exceptions;
using DietPlanner.Domain.ValueObjects;

/// <summary>Unit tests for <c>Product</c> domain rules: in-memory only, no infrastructure and no mocks.</summary>
public sealed class ProductTests
{
    /// <summary>With valid data: <c>Create</c> creates product.</summary>
    [Fact]
    public void Create_WithValidData_CreatesProduct()
    {
        var id = ProductId.New();
        var nutrition = new NutritionPer100g(100m, 10m, 5m, 3m, 2m);

        var product = Product.Create(id, "Chicken Breast", nutrition, "g", null, null, "user-1", TestClock.UtcNow);

        product.Id.ShouldBe(id);
        product.Name.ShouldBe("Chicken Breast");
        product.Nutrition.ShouldBe(nutrition);
        product.DefaultUnit.ShouldBe("g");
        product.CreatedByUserId.ShouldBe("user-1");
        product.IsDeleted.ShouldBeFalse();
    }

    /// <summary>Without a visibility: <c>Create</c> shares the product with the household; <c>ChangeVisibility</c> changes it.</summary>
    [Fact]
    public void Create_WithoutVisibility_DefaultsToHousehold()
    {
        var product = Product.Create(ProductId.New(), "Rice", new NutritionPer100g(null, null, null, null, null), "g", null, null, "user-1", TestClock.UtcNow);

        product.Visibility.ShouldBe(Visibility.Household);
        product.ChangeVisibility(Visibility.Public);
        product.Visibility.ShouldBe(Visibility.Public);
    }

    /// <summary>With empty name: <c>Create</c> throws argument exception.</summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithEmptyName_ThrowsArgumentException(string? name)
    {
        var act = () => Product.Create(ProductId.New(), name!, new NutritionPer100g(null, null, null, null, null), "g", null, null, "user-1", TestClock.UtcNow);

        act.ShouldThrow<ArgumentException>();
    }

    /// <summary>With valid data: <c>Update</c> updates product.</summary>
    [Fact]
    public void Update_WithValidData_UpdatesProduct()
    {
        var product = Product.Create(ProductId.New(), "Old Name", new NutritionPer100g(null, null, null, null, null), "g", null, null, "user-1", TestClock.UtcNow);
        var newNutrition = new NutritionPer100g(200m, 20m, 10m, 6m, 3m);

        product.Update("New Name", newNutrition, "ml", 1.0m, null, TestClock.UtcNow);

        product.Name.ShouldBe("New Name");
        product.Nutrition.ShouldBe(newNutrition);
        product.DefaultUnit.ShouldBe("ml");
        product.DensityGramsPerMl.ShouldBe(1.0m);
        product.UpdatedAt.ShouldNotBeNull();
    }

    /// <summary>When not deleted: <c>SoftDelete</c> sets deleted at.</summary>
    [Fact]
    public void SoftDelete_WhenNotDeleted_SetsDeletedAt()
    {
        var product = Product.Create(ProductId.New(), "Milk", new NutritionPer100g(null, null, null, null, null), "ml", null, null, "user-1", TestClock.UtcNow);

        product.SoftDelete(TestClock.UtcNow);

        product.IsDeleted.ShouldBeTrue();
        product.DeletedAt.ShouldNotBeNull();
    }

    /// <summary>When already deleted: <c>SoftDelete</c> throws domain exception.</summary>
    [Fact]
    public void SoftDelete_WhenAlreadyDeleted_ThrowsDomainException()
    {
        var product = Product.Create(ProductId.New(), "Milk", new NutritionPer100g(null, null, null, null, null), "ml", null, null, "user-1", TestClock.UtcNow);
        product.SoftDelete(TestClock.UtcNow);

        var act = () => product.SoftDelete(TestClock.UtcNow);

        act.ShouldThrow<DietPlannerDomainException>();
    }

    /// <summary>When deleted: <c>Restore</c> clears deleted at.</summary>
    [Fact]
    public void Restore_WhenDeleted_ClearsDeletedAt()
    {
        var product = Product.Create(ProductId.New(), "Milk", new NutritionPer100g(null, null, null, null, null), "ml", null, null, "user-1", TestClock.UtcNow);
        product.SoftDelete(TestClock.UtcNow);

        product.Restore(TestClock.UtcNow);

        product.IsDeleted.ShouldBeFalse();
        product.DeletedAt.ShouldBeNull();
    }
}
