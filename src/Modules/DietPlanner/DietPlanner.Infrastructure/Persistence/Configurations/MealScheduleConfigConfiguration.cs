namespace DietPlanner.Infrastructure.Persistence.Configurations;

using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class MealScheduleConfigConfiguration : IEntityTypeConfiguration<MealScheduleConfig>
{
    public void Configure(EntityTypeBuilder<MealScheduleConfig> builder)
    {
        builder.ToTable("meal_schedule_configs");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => MealScheduleConfigId.From(value))
            .HasColumnName("id");

        builder.Property(x => x.PersonId)
            .IsRequired()
            .HasColumnName("person_id");

        builder.HasIndex(x => x.PersonId)
            .IsUnique()
            .HasDatabaseName("idx_meal_schedule_configs_person");

        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

        builder.HasMany(x => x.Slots)
            .WithOne()
            .HasForeignKey("meal_schedule_config_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.Slots).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
