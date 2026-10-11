namespace DietPlanner.UnitTests.Domain;

using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.Exceptions;
using DietPlanner.Domain.Ledgers;
using DietPlanner.Domain.ValueObjects;

/// <summary>Photo lifecycle invariants.</summary>
public sealed class RecipeImageRevisionTests
{
    /// <summary>Cleanup permanently fences abandoned uploads.</summary>
    [Fact]
    public void Publish_RetiredRevision_RejectsReuse()
    {
        var revision = RecipeImageRevision.Create(Guid.CreateVersion7(), RecipeId.New(), DateTimeOffset.UnixEpoch);
        revision.Retire();
        Should.Throw<InvalidOperationException>(revision.Publish);
        revision.Published.ShouldBeFalse();
    }

    /// <summary>Repeated deletion catches delayed storage writes.</summary>
    [Fact]
    public void Cleaned_Always_KeepsTombstoneForLateWrites()
    {
        var revision = RecipeImageRevision.Create(Guid.CreateVersion7(), RecipeId.New(), DateTimeOffset.UnixEpoch);
        revision.ExpiresAt.ShouldBe(DateTimeOffset.UnixEpoch.AddMinutes(15));
        revision.Retire();
        revision.Cleaned(DateTimeOffset.UnixEpoch.AddHours(1));
        revision.Retired.ShouldBeTrue();
        revision.NextCleanupAt.ShouldBe(DateTimeOffset.UnixEpoch.AddHours(25));
    }

    /// <summary>Recipe deletion revokes its image and prevents resurrection.</summary>
    [Fact]
    public void SoftDelete_WithPhoto_ClearsMetadataAndRejectsNewPhoto()
    {
        var recipe = Recipe.Create(RecipeId.New(), "Photo recipe", null, null, 1, null, "creator", DateTime.UnixEpoch);
        var image = new RecipeImage(Guid.CreateVersion7(), 10, 10, 100, 10, 10, 100);
        recipe.ChangeImage(image);
        recipe.SoftDelete(DateTime.UnixEpoch);
        recipe.Image.ShouldBeNull();
        Should.Throw<DietPlannerDomainException>(() => recipe.ChangeImage(image));
    }

    /// <summary>Invalid normalized metadata cannot enter an aggregate.</summary>
    [Theory]
    [InlineData(0, 10, 100, 10, 10, 100)]
    [InlineData(10, 1601, 100, 10, 10, 100)]
    [InlineData(10, 10, 1048577, 10, 10, 100)]
    [InlineData(10, 10, 100, 321, 10, 100)]
    [InlineData(10, 10, 100, 10, 0, 100)]
    [InlineData(10, 10, 100, 10, 10, 102401)]
    public void Metadata_InvalidBounds_Rejects(int width, int height, int size, int thumbnailWidth, int thumbnailHeight, int thumbnailSize)
        => Should.Throw<DietPlannerDomainException>(() => new RecipeImage(Guid.CreateVersion7(), width, height, size,
            thumbnailWidth, thumbnailHeight, thumbnailSize));

    /// <summary>Every active photo has an opaque nonempty revision.</summary>
    [Fact]
    public void Metadata_EmptyVersion_Rejects()
        => Should.Throw<DietPlannerDomainException>(() => new RecipeImage(Guid.Empty, 10, 10, 100, 10, 10, 100));
}
