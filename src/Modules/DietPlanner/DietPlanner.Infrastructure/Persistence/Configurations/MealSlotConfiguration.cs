namespace DietPlanner.Infrastructure.Persistence.Configurations;

using DietPlanner.Domain.Entities;
using DietPlanner.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class MealSlotConfiguration : IEntityTypeConfiguration<MealSlot>
{
    public void Configure(EntityTypeBuilder<MealSlot> builder)
    {
        builder.ToTable("meal_slots");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => MealSlotId.From(value))
            .HasColumnName("id");

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnName("name");

        builder.Property(x => x.DefaultTime)
            .HasColumnName("default_time");

        builder.Property(x => x.SortOrder)
            .HasColumnName("sort_order");
    }
}
