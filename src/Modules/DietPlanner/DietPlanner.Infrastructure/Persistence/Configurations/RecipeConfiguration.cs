namespace DietPlanner.Infrastructure.Persistence.Configurations;

using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class RecipeConfiguration : IEntityTypeConfiguration<Recipe>
{
    public void Configure(EntityTypeBuilder<Recipe> builder)
    {
        builder.ToTable("recipes");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => RecipeId.From(value))
            .HasColumnName("id");

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200)
            .HasColumnName("name");

        builder.HasIndex(x => x.Name)
            .IsUnique()
            .HasFilter("deleted_at IS NULL");

        builder.Property(x => x.Description).HasColumnName("description");
        builder.Property(x => x.Instructions).HasColumnName("instructions");
        builder.Property(x => x.Servings)
            .HasColumnName("servings")
            .HasDefaultValue(1);
        builder.Property(x => x.PrepTimeMinutes).HasColumnName("prep_time_minutes");

        builder.Property(x => x.CreatedByUserId)
            .IsRequired()
            .HasMaxLength(255)
            .HasColumnName("created_by_user_id");

        builder.Property(x => x.Visibility)
            .HasConversion<string>()
            .HasMaxLength(16)
            .HasColumnName("visibility");

        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");

        builder.HasQueryFilter(r => r.DeletedAt == null);

        builder.HasIndex(x => x.CreatedByUserId).HasDatabaseName("idx_recipes_created_by");
        builder.HasIndex(x => x.Id)
            .HasDatabaseName("idx_recipes_active")
            .HasFilter("deleted_at IS NULL");

        builder.HasMany(r => r.Ingredients)
            .WithOne()
            .HasForeignKey("RecipeId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(r => r.Ingredients).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
