namespace DietPlanner.Infrastructure.Persistence.Configurations;

using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => ProductId.From(value))
            .HasColumnName("id");

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200)
            .HasColumnName("name");

        builder.HasIndex(x => x.Name)
            .IsUnique()
            .HasFilter("deleted_at IS NULL");

        builder.OwnsOne(x => x.Nutrition, n =>
        {
            n.Property(x => x.Calories).HasColumnName("calories_per_100g");
            n.Property(x => x.Protein).HasColumnName("protein_per_100g");
            n.Property(x => x.Carbs).HasColumnName("carbs_per_100g");
            n.Property(x => x.Fat).HasColumnName("fat_per_100g");
            n.Property(x => x.Fiber).HasColumnName("fiber_per_100g");
        });

        builder.Property(x => x.DefaultUnit)
            .HasMaxLength(50)
            .HasColumnName("default_unit");

        builder.Property(x => x.DensityGramsPerMl)
            .HasPrecision(6, 3)
            .HasColumnName("density_grams_per_ml");

        builder.Property(x => x.GramPerPiece)
            .HasPrecision(10, 2)
            .HasColumnName("gram_per_piece");

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

        builder.HasQueryFilter(p => p.DeletedAt == null);

        builder.HasIndex(x => x.CreatedByUserId).HasDatabaseName("idx_products_created_by");
        builder.HasIndex(x => x.Id)
            .HasDatabaseName("idx_products_active")
            .HasFilter("deleted_at IS NULL");
    }
}
