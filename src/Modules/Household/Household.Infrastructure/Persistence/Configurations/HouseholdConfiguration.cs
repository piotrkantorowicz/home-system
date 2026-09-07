using Household.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HouseholdAggregate = Household.Domain.Aggregates.Household;

namespace Household.Infrastructure.Persistence.Configurations;

internal sealed class HouseholdConfiguration : IEntityTypeConfiguration<HouseholdAggregate>
{
    public void Configure(EntityTypeBuilder<HouseholdAggregate> builder)
    {
        builder.ToTable("households");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => HouseholdId.From(value))
            .HasColumnName("id");

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(120)
            .HasColumnName("name");

        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

        builder.HasMany(x => x.Members)
            .WithOne()
            .HasForeignKey("HouseholdId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.Members).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
