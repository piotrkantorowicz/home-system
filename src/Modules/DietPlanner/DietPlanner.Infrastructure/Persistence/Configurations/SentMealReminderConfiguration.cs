namespace DietPlanner.Infrastructure.Persistence.Configurations;

using DietPlanner.Domain.Ledgers;
using DietPlanner.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class SentMealReminderConfiguration : IEntityTypeConfiguration<SentMealReminder>
{
    public void Configure(EntityTypeBuilder<SentMealReminder> builder)
    {
        builder.ToTable("sent_meal_reminders");

        builder.HasKey(x => new { x.MealEntryId, x.Kind });

        builder.Property(x => x.MealEntryId)
            .HasConversion(id => id.Value, value => MealEntryId.From(value))
            .HasColumnName("meal_entry_id");

        builder.Property(x => x.Kind)
            .HasConversion<string>()
            .HasMaxLength(16)
            .HasColumnName("kind");

        builder.Property(x => x.SentAt)
            .HasColumnName("sent_at");
    }
}
