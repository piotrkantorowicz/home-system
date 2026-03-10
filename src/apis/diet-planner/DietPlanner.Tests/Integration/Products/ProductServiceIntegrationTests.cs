using DietPlanner.Api.Common.Exceptions;
using DietPlanner.Api.Data;
using DietPlanner.Api.Features.Products;
using DietPlanner.Tests.Builders;
using DietPlanner.Tests.Integration.Infrastructure;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace DietPlanner.Tests.Integration.Products;

[Collection("Database")]
public class ProductServiceIntegrationTests(DatabaseFixture fixture) : IAsyncLifetime
{
    private AppDbContext _db = null!;
    private IProductService _sut = null!;
    private const string UserId = "test-product-service-user";

    public async Task InitializeAsync()
    {
        _db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .Options);

        var mockEnv = new Mock<IWebHostEnvironment>();
        mockEnv.Setup(e => e.EnvironmentName).Returns("Testing");

        _sut = new ProductService(_db, NullLogger<ProductService>.Instance, mockEnv.Object);
    }

    public async Task DisposeAsync()
    {
        var products = await _db.Products
            .IgnoreQueryFilters()
            .Where(p => p.CreatedByUserId == UserId)
            .ToListAsync();

        _db.Products.RemoveRange(products);
        await _db.SaveChangesAsync();
        await _db.DisposeAsync();
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_ReturnsProductResponse()
    {
        // Arrange
        var request = new ProductBuilder()
            .WithName($"Chicken Breast {Guid.NewGuid()}")
            .WithCalories(165)
            .Build();

        // Act
        var result = await _sut.CreateAsync(request, UserId);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBeEmpty();
        result.Name.Should().Be(request.Name);
        result.CaloriesPer100g.Should().Be(request.CaloriesPer100g);
        result.ProteinPer100g.Should().Be(request.ProteinPer100g);
        result.CarbsPer100g.Should().Be(request.CarbsPer100g);
        result.FatPer100g.Should().Be(request.FatPer100g);
        result.DefaultUnit.Should().Be(request.DefaultUnit);
        result.CreatedByUserId.Should().Be(UserId);
        result.IsOwner.Should().BeTrue();
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task CreateAsync_SetsCreatedByUserId()
    {
        // Arrange
        var request = new ProductBuilder()
            .WithName($"Brown Rice {Guid.NewGuid()}")
            .Build();

        // Act
        var result = await _sut.CreateAsync(request, UserId);

        // Assert
        result.CreatedByUserId.Should().Be(UserId);

        // Verify it's persisted in the database
        var persisted = await _db.Products.FindAsync(result.Id);
        persisted.Should().NotBeNull();
        persisted!.CreatedByUserId.Should().Be(UserId);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingProduct_ReturnsIt()
    {
        // Arrange
        var request = new ProductBuilder()
            .WithName($"Oats {Guid.NewGuid()}")
            .WithCalories(389)
            .Build();
        var created = await _sut.CreateAsync(request, UserId);

        // Act
        var result = await _sut.GetByIdAsync(created.Id, UserId);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(created.Id);
        result.Name.Should().Be(request.Name);
        result.CaloriesPer100g.Should().Be(request.CaloriesPer100g);
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ThrowsNotFoundException()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var act = async () => await _sut.GetByIdAsync(nonExistentId, UserId);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"*{nonExistentId}*");
    }

    [Fact]
    public async Task UpdateAsync_ValidRequest_UpdatesProduct()
    {
        // Arrange
        var createRequest = new ProductBuilder()
            .WithName($"Salmon {Guid.NewGuid()}")
            .WithCalories(208)
            .Build();
        var created = await _sut.CreateAsync(createRequest, UserId);

        var updateRequest = new UpdateProductRequest(
            Name: $"Atlantic Salmon {Guid.NewGuid()}",
            CaloriesPer100g: 250,
            ProteinPer100g: 25,
            CarbsPer100g: 0,
            FatPer100g: 15,
            FiberPer100g: null,
            DefaultUnit: "g"
        );

        // Act
        var result = await _sut.UpdateAsync(created.Id, updateRequest, UserId);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(created.Id);
        result.Name.Should().Be(updateRequest.Name);
        result.CaloriesPer100g.Should().Be(updateRequest.CaloriesPer100g);
        result.ProteinPer100g.Should().Be(updateRequest.ProteinPer100g);
        result.FatPer100g.Should().Be(updateRequest.FatPer100g);
        result.UpdatedAt.Should().NotBeNull();
        result.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task DeleteAsync_SoftDelete_HidesProduct()
    {
        // Arrange
        var request = new ProductBuilder()
            .WithName($"Tofu {Guid.NewGuid()}")
            .Build();
        var created = await _sut.CreateAsync(request, UserId);

        // Act - soft delete (permanent = false by default)
        await _sut.DeleteAsync(created.Id, UserId, permanent: false);

        // Assert - GetByIdAsync should throw because product is soft-deleted
        var act = async () => await _sut.GetByIdAsync(created.Id, UserId);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task SearchAsync_ReturnsOnlyUserProducts_WhenOnlyMineTrue()
    {
        // Arrange
        var otherUserId = "other-user-" + Guid.NewGuid();
        var myProductName = $"MyProduct-{Guid.NewGuid()}";
        var otherProductName = $"OtherProduct-{Guid.NewGuid()}";

        await _sut.CreateAsync(
            new ProductBuilder().WithName(myProductName).Build(),
            UserId);

        await _sut.CreateAsync(
            new ProductBuilder().WithName(otherProductName).Build(),
            otherUserId);

        // Act
        var result = await _sut.SearchAsync(
            search: null,
            onlyMine: true,
            userId: UserId,
            page: 1,
            pageSize: 100);

        // Assert
        result.Items.Should().NotBeEmpty();
        result.Items.Should().AllSatisfy(p => p.CreatedByUserId.Should().Be(UserId));
        result.Items.Should().Contain(p => p.Name == myProductName);
        result.Items.Should().NotContain(p => p.Name == otherProductName);

        // Cleanup the other user's product
        var otherProduct = await _db.Products
            .FirstOrDefaultAsync(p => p.CreatedByUserId == otherUserId);
        if (otherProduct != null)
        {
            _db.Products.Remove(otherProduct);
            await _db.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task SearchAsync_FiltersByName()
    {
        // Arrange
        var uniqueToken = Guid.NewGuid().ToString("N")[..8];
        var matchingName = $"Broccoli-{uniqueToken}";
        var nonMatchingName = $"Carrot-{Guid.NewGuid()}";

        await _sut.CreateAsync(
            new ProductBuilder().WithName(matchingName).Build(),
            UserId);

        await _sut.CreateAsync(
            new ProductBuilder().WithName(nonMatchingName).Build(),
            UserId);

        // Act
        var result = await _sut.SearchAsync(
            search: uniqueToken,
            onlyMine: false,
            userId: UserId,
            page: 1,
            pageSize: 100);

        // Assert
        result.Items.Should().NotBeEmpty();
        result.Items.Should().Contain(p => p.Name == matchingName);
        result.Items.Should().NotContain(p => p.Name == nonMatchingName);
    }
}
