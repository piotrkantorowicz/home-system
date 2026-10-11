namespace DietPlanner.IntegrationTests.Images;

using DietPlanner.Domain.Ledgers;
using DietPlanner.Domain.ValueObjects;
using DietPlanner.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Testcontainers.PostgreSql;

/// <summary>Upgrade checks against the actual migration chain rather than EnsureCreated.</summary>
public sealed class RecipeImageMigrationTests
{
    /// <summary>Existing recipes survive the photo migration with no image; upload intents persist across contexts.</summary>
    [Fact]
    public async Task Migrate_ExistingRecipe_PreservesDataAndAddsDurableIntents()
    {
        CancellationToken ct = TestContext.Current.CancellationToken;
        await using var postgres = new PostgreSqlBuilder("postgres:17-alpine").Build();
        await postgres.StartAsync(ct);
        var options = new DbContextOptionsBuilder<DietPlannerDbContext>().UseNpgsql(postgres.GetConnectionString()).Options;
        var recipeId = Guid.CreateVersion7();
        var revisionId = Guid.CreateVersion7();
        await using (var db = new DietPlannerDbContext(options))
        {
            string[] migrations = [.. db.Database.GetMigrations()];
            var migrator = db.GetService<IMigrator>();
            int photoMigration = Array.FindIndex(migrations, m => m.EndsWith("_AddRecipeImages", StringComparison.Ordinal));
            await migrator.MigrateAsync(migrations[photoMigration - 1], ct);
            DateTime now = DateTime.UnixEpoch;
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO recipes (id, name, created_by_user_id, visibility, created_at) VALUES ({recipeId}, 'Pre-photo recipe', 'migration-user', 'Private', {now})", ct);
            await migrator.MigrateAsync(null, ct);
            var recipe = await db.Recipes.AsNoTracking().Where(r => r.Id == RecipeId.From(recipeId))
                .Select(r => new { r.Name, r.Image }).SingleAsync(ct);
            recipe.Name.ShouldBe("Pre-photo recipe");
            recipe.Image.ShouldBeNull();
            db.RecipeImageRevisions.Add(RecipeImageRevision.Create(revisionId, RecipeId.From(recipeId), DateTimeOffset.UnixEpoch));
            await db.SaveChangesAsync(ct);
        }
        await using var restarted = new DietPlannerDbContext(options);
        (await restarted.RecipeImageRevisions.SingleAsync(r => r.Id == revisionId, ct)).RecipeId.ShouldBe(RecipeId.From(recipeId));
    }
}
