namespace DietPlanner.IntegrationTests.Infrastructure;

[CollectionDefinition(Name)]
public sealed class DatabaseCollectionDefinition : ICollectionFixture<DatabaseFixture>
{
    public const string Name = "Database";
}
