namespace DietPlanner.Infrastructure.Persistence.Configurations;

using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.Entities;
using DietPlanner.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class MealEntryActualProductConfiguration : IEntityTypeConfiguration<MealEntryActualProduct>
{
    public void Configure(EntityTypeBuilder<MealEntryActualProduct> builder)
    {
        builder.ToTable("meal_entry_actual_products");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => MealEntryActualProductId.From(value))
            .HasColumnName("id");

        builder.Property(x => x.ProductId)
            .HasConversion(id => id.Value, value => ProductId.From(value))
            .HasColumnName("product_id");

        builder.Property(x => x.Amount)
            .HasPrecision(10, 3)
            .HasColumnName("amount");

        builder.Property(x => x.Unit)
            .IsRequired()
            .HasMaxLength(32)
            .HasColumnName("unit");

        // Block product deletion when actual-product references exist.
        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.ProductId).HasDatabaseName("idx_meal_entry_actual_products_product");
    }
}
