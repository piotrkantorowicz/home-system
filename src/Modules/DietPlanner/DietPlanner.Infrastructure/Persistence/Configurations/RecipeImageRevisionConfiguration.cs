namespace DietPlanner.Infrastructure.Persistence.Configurations;

using DietPlanner.Domain.Ledgers;
using DietPlanner.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class RecipeImageRevisionConfiguration : IEntityTypeConfiguration<RecipeImageRevision>
{
    public void Configure(EntityTypeBuilder<RecipeImageRevision> builder)
    {
        builder.ToTable("recipe_image_revisions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.RecipeId).HasColumnName("recipe_id")
            .HasConversion(id => id.Value, value => RecipeId.From(value));
        builder.Property(x => x.ExpiresAt).HasColumnName("expires_at");
        builder.Property(x => x.Published).HasColumnName("published");
        builder.Property(x => x.Retired).HasColumnName("retired");
        builder.Property(x => x.NextCleanupAt).HasColumnName("next_cleanup_at");
        builder.HasIndex(x => x.NextCleanupAt);
    }
}
