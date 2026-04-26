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

        builder.Property(x => x.UserId)
            .IsRequired()
            .HasMaxLength(255)
            .HasColumnName("user_id");

        builder.HasIndex(x => x.UserId)
            .IsUnique()
            .HasDatabaseName("idx_diet_reminder_settings_user");

        builder.Property(x => x.MealRemindersEnabled).HasColumnName("meal_reminders_enabled");
        builder.Property(x => x.MealReminderLeadTimeMinutes).HasColumnName("meal_reminder_lead_time_minutes");
        builder.Property(x => x.MealMissedGraceMinutes).HasColumnName("meal_missed_grace_minutes");
        builder.Property(x => x.WaterRemindersEnabled).HasColumnName("water_reminders_enabled");
        builder.Property(x => x.WaterReminderIntervalMinutes).HasColumnName("water_reminder_interval_minutes");
        builder.Property(x => x.WaterWindowStartUtc).HasColumnName("water_window_start_utc");
        builder.Property(x => x.WaterWindowEndUtc).HasColumnName("water_window_end_utc");
        builder.Property(x => x.WeeklySummaryEnabled).HasColumnName("weekly_summary_enabled");
        builder.Property(x => x.WeeklySummaryDayOfWeekUtc)
            .HasConversion<int>()
            .HasColumnName("weekly_summary_day_of_week_utc");
        builder.Property(x => x.WeeklySummaryTimeOfDayUtc).HasColumnName("weekly_summary_time_of_day_utc");
        builder.Property(x => x.GoalAlertsEnabled).HasColumnName("goal_alerts_enabled");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
    }
}
