namespace DietPlanner.Infrastructure.Persistence.Configurations;

using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class DietReminderSettingsConfiguration : IEntityTypeConfiguration<DietReminderSettings>
{
    public void Configure(EntityTypeBuilder<DietReminderSettings> builder)
    {
        builder.ToTable("diet_reminder_settings");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => DietReminderSettingsId.From(value))
            .HasColumnName("id");

        builder.Property(x => x.PersonId)
            .IsRequired()
            .HasColumnName("person_id");

        builder.HasIndex(x => x.PersonId)
            .IsUnique()
            .HasDatabaseName("idx_diet_reminder_settings_person");

        builder.Property(x => x.MealRemindersEnabled).HasColumnName("meal_reminders_enabled");
        builder.Property(x => x.MealReminderLeadTimeMinutes).HasColumnName("meal_reminder_lead_time_minutes");
        builder.Property(x => x.MealMissedGraceMinutes).HasColumnName("meal_missed_grace_minutes");
        builder.Property(x => x.WaterRemindersEnabled).HasColumnName("water_reminders_enabled");
        builder.Property(x => x.WaterReminderIntervalMinutes).HasColumnName("water_reminder_interval_minutes");
        builder.Property(x => x.WaterWindowStart).HasColumnName("water_window_start");
        builder.Property(x => x.WaterWindowEnd).HasColumnName("water_window_end");
        builder.Property(x => x.WeeklySummaryEnabled).HasColumnName("weekly_summary_enabled");
        builder.Property(x => x.WeeklySummaryDayOfWeek)
            .HasConversion<int>()
            .HasColumnName("weekly_summary_day_of_week");
        builder.Property(x => x.WeeklySummaryTimeOfDay).HasColumnName("weekly_summary_time_of_day");
        builder.Property(x => x.GoalAlertsEnabled).HasColumnName("goal_alerts_enabled");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
    }
}
