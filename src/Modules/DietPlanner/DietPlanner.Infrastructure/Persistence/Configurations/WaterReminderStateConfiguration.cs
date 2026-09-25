namespace DietPlanner.Infrastructure.Persistence.Configurations;

using DietPlanner.Domain.Ledgers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class WaterReminderStateConfiguration : IEntityTypeConfiguration<WaterReminderState>
{
    public void Configure(EntityTypeBuilder<WaterReminderState> builder)
    {
        builder.ToTable("water_reminder_state");

        builder.HasKey(x => x.PersonId);
        builder.Property(x => x.PersonId).HasColumnName("person_id");

        builder.Property(x => x.LastWaterReminderAt)
            .HasColumnName("last_water_reminder_at");
    }
}
