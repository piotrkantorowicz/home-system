namespace DietPlanner.Infrastructure.Persistence.Configurations;

using DietPlanner.Domain.Ledgers;
using DietPlanner.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class ShoppingCheckConfiguration : IEntityTypeConfiguration<ShoppingCheck>
{
    public void Configure(EntityTypeBuilder<ShoppingCheck> builder)
    {
        builder.ToTable("shopping_checks");

        builder.HasKey(x => new { x.ScopeId, x.RangeFrom, x.RangeTo, x.ProductId, x.Unit });

        builder.Property(x => x.ScopeId).HasColumnName("scope_id");
        builder.Property(x => x.RangeFrom).HasColumnName("range_from");
        builder.Property(x => x.RangeTo).HasColumnName("range_to");

        builder.Property(x => x.ProductId)
            .HasConversion(id => id.Value, value => ProductId.From(value))
            .HasColumnName("product_id");

        builder.Property(x => x.Unit)
            .HasMaxLength(50)
            .HasColumnName("unit");

        builder.Property(x => x.CheckedByPersonId).HasColumnName("checked_by_person_id");
        builder.Property(x => x.CheckedAt).HasColumnName("checked_at");
    }
}
