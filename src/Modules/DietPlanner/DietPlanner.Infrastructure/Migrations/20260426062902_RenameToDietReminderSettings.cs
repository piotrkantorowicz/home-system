using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DietPlanner.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameToDietReminderSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_notification_preferences_user",
                table: "notification_preferences");

            migrationBuilder.RenameTable(
                name: "notification_preferences",
                newName: "diet_reminder_settings");

            migrationBuilder.RenameColumn(
                table: "diet_reminder_settings",
                name: "meal_reminder_enabled",
                newName: "meal_reminders_enabled");

            migrationBuilder.RenameColumn(
                table: "diet_reminder_settings",
                name: "water_reminder_enabled",
                newName: "water_reminders_enabled");

            migrationBuilder.RenameColumn(
                table: "diet_reminder_settings",
                name: "goal_milestone_alerts_enabled",
                newName: "goal_alerts_enabled");

            migrationBuilder.AddColumn<int>(
                table: "diet_reminder_settings",
                name: "meal_missed_grace_minutes",
                type: "integer",
                nullable: false,
                defaultValue: 30);

            migrationBuilder.AddColumn<TimeOnly>(
                table: "diet_reminder_settings",
                name: "water_window_start_utc",
                type: "time without time zone",
                nullable: false,
                defaultValue: new TimeOnly(6, 0));

            migrationBuilder.AddColumn<TimeOnly>(
                table: "diet_reminder_settings",
                name: "water_window_end_utc",
                type: "time without time zone",
                nullable: false,
                defaultValue: new TimeOnly(22, 0));

            migrationBuilder.AddColumn<int>(
                table: "diet_reminder_settings",
                name: "weekly_summary_day_of_week_utc",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<TimeOnly>(
                table: "diet_reminder_settings",
                name: "weekly_summary_time_of_day_utc",
                type: "time without time zone",
                nullable: false,
                defaultValue: new TimeOnly(8, 0));

            migrationBuilder.CreateIndex(
                name: "idx_diet_reminder_settings_user",
                table: "diet_reminder_settings",
                column: "user_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_diet_reminder_settings_user",
                table: "diet_reminder_settings");

            migrationBuilder.DropColumn(
                table: "diet_reminder_settings",
                name: "weekly_summary_time_of_day_utc");

            migrationBuilder.DropColumn(
                table: "diet_reminder_settings",
                name: "weekly_summary_day_of_week_utc");

            migrationBuilder.DropColumn(
                table: "diet_reminder_settings",
                name: "water_window_end_utc");

            migrationBuilder.DropColumn(
                table: "diet_reminder_settings",
                name: "water_window_start_utc");

            migrationBuilder.DropColumn(
                table: "diet_reminder_settings",
                name: "meal_missed_grace_minutes");

            migrationBuilder.RenameColumn(
                table: "diet_reminder_settings",
                name: "goal_alerts_enabled",
                newName: "goal_milestone_alerts_enabled");

            migrationBuilder.RenameColumn(
                table: "diet_reminder_settings",
                name: "water_reminders_enabled",
                newName: "water_reminder_enabled");

            migrationBuilder.RenameColumn(
                table: "diet_reminder_settings",
                name: "meal_reminders_enabled",
                newName: "meal_reminder_enabled");

            migrationBuilder.RenameTable(
                name: "diet_reminder_settings",
                newName: "notification_preferences");

            migrationBuilder.CreateIndex(
                name: "idx_notification_preferences_user",
                table: "notification_preferences",
                column: "user_id",
                unique: true);
        }
    }
}
