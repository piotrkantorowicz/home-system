namespace DietPlanner.IntegrationTests.Infrastructure;

/// <summary>Serializes storage tests, including service stop/restart, independently of database tests.</summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class ImageStorageCollectionDefinition : ICollectionFixture<ImageStorageFixture>
{
    /// <summary>Collection name used by the real object-storage contract tests.</summary>
    public const string Name = "ImageStorage";
}
