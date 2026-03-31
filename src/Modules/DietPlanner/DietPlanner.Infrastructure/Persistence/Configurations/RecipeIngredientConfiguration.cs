namespace DietPlanner.Infrastructure.Persistence.Configurations;

using DietPlanner.Domain.Entities;
using DietPlanner.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class RecipeIngredientConfiguration : IEntityTypeConfiguration<RecipeIngredient>
{
    public void Configure(EntityTypeBuilder<RecipeIngredient> builder)
    {
        builder.ToTable("recipe_ingredients");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => RecipeIngredientId.From(value))
            .HasColumnName("id");

        builder.Property(x => x.ProductId)
            .HasConversion(id => id.Value, value => ProductId.From(value))
            .HasColumnName("product_id");

        // Shadow property for the FK to Recipe — typed as RecipeId to match the principal key type
        builder.Property<RecipeId>("RecipeId")
            .HasConversion(id => id.Value, value => RecipeId.From(value))
            .HasColumnName("recipe_id");

        builder.HasOne<Domain.Aggregates.Product>()
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.Amount)
            .HasPrecision(10, 2)
            .HasColumnName("amount");

        builder.Property(x => x.Unit)
            .HasMaxLength(50)
            .HasColumnName("unit");

        builder.HasIndex("RecipeId", "ProductId")
            .IsUnique()
            .HasDatabaseName("unique_recipe_product");

        builder.HasIndex("RecipeId").HasDatabaseName("idx_recipe_ingredients_recipe");
        builder.HasIndex(x => x.ProductId).HasDatabaseName("idx_recipe_ingredients_product");
    }
}
