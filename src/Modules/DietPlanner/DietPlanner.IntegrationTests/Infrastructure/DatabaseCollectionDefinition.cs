namespace DietPlanner.IntegrationTests.Infrastructure;

/// <summary>xUnit collection that shares one <see cref="DatabaseFixture"/> across every DietPlanner integration test class.</summary>
[CollectionDefinition(Name)]
public sealed class DatabaseCollectionDefinition : ICollectionFixture<DatabaseFixture>
{
    /// <summary>Collection name used in <c>[Collection]</c> attributes.</summary>
    public const string Name = "Database";
}
