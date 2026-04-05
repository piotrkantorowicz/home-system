namespace DietPlanner.Infrastructure.Persistence.Configurations;

using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class NotificationPreferencesConfiguration : IEntityTypeConfiguration<NotificationPreferences>
{
    public void Configure(EntityTypeBuilder<NotificationPreferences> builder)
    {
        builder.ToTable("notification_preferences");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => NotificationPreferencesId.From(value))
            .HasColumnName("id");

        builder.Property(x => x.UserId)
            .IsRequired()
            .HasMaxLength(255)
            .HasColumnName("user_id");

        builder.HasIndex(x => x.UserId)
            .IsUnique()
            .HasDatabaseName("idx_notification_preferences_user");

        builder.Property(x => x.MealReminderEnabled).HasColumnName("meal_reminder_enabled");
        builder.Property(x => x.MealReminderLeadTimeMinutes).HasColumnName("meal_reminder_lead_time_minutes");
        builder.Property(x => x.WaterReminderEnabled).HasColumnName("water_reminder_enabled");
        builder.Property(x => x.WaterReminderIntervalMinutes).HasColumnName("water_reminder_interval_minutes");
        builder.Property(x => x.WeeklySummaryEnabled).HasColumnName("weekly_summary_enabled");
        builder.Property(x => x.GoalMilestoneAlertsEnabled).HasColumnName("goal_milestone_alerts_enabled");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
    }
}
