using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DietPlanner.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReminderSettingsLocalTimes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "weekly_summary_time_of_day_utc",
                table: "diet_reminder_settings",
                newName: "weekly_summary_time_of_day");

            migrationBuilder.RenameColumn(
                name: "weekly_summary_day_of_week_utc",
                table: "diet_reminder_settings",
                newName: "weekly_summary_day_of_week");

            migrationBuilder.RenameColumn(
                name: "water_window_start_utc",
                table: "diet_reminder_settings",
                newName: "water_window_start");

            migrationBuilder.RenameColumn(
                name: "water_window_end_utc",
                table: "diet_reminder_settings",
                newName: "water_window_end");

            // Stored values were UTC, converted by the frontend with the January (CET, UTC+1) offset.
            // Re-derive the local wall-clock values the same way so users keep seeing what they set.
            // 'Europe/Warsaw' = the default DietPlanner:DietReminderTick:TimeZoneId.
            migrationBuilder.Sql("""
                UPDATE diet_reminder_settings SET
                    water_window_start = ((TIMESTAMP '2026-01-05' + water_window_start) AT TIME ZONE 'UTC' AT TIME ZONE 'Europe/Warsaw')::time,
                    water_window_end = ((TIMESTAMP '2026-01-05' + water_window_end) AT TIME ZONE 'UTC' AT TIME ZONE 'Europe/Warsaw')::time,
                    weekly_summary_day_of_week = EXTRACT(DOW FROM ((DATE '2026-01-04' + weekly_summary_day_of_week) + weekly_summary_time_of_day) AT TIME ZONE 'UTC' AT TIME ZONE 'Europe/Warsaw')::int,
                    weekly_summary_time_of_day = (((DATE '2026-01-04' + weekly_summary_day_of_week) + weekly_summary_time_of_day) AT TIME ZONE 'UTC' AT TIME ZONE 'Europe/Warsaw')::time;

                -- A window that ended just before midnight UTC now wraps past local midnight; cap it.
                UPDATE diet_reminder_settings SET water_window_end = TIME '23:59'
                WHERE water_window_end <= water_window_start;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE diet_reminder_settings SET
                    water_window_start = ((TIMESTAMP '2026-01-05' + water_window_start) AT TIME ZONE 'Europe/Warsaw' AT TIME ZONE 'UTC')::time,
                    water_window_end = ((TIMESTAMP '2026-01-05' + water_window_end) AT TIME ZONE 'Europe/Warsaw' AT TIME ZONE 'UTC')::time,
                    weekly_summary_day_of_week = EXTRACT(DOW FROM ((DATE '2026-01-04' + weekly_summary_day_of_week) + weekly_summary_time_of_day) AT TIME ZONE 'Europe/Warsaw' AT TIME ZONE 'UTC')::int,
                    weekly_summary_time_of_day = (((DATE '2026-01-04' + weekly_summary_day_of_week) + weekly_summary_time_of_day) AT TIME ZONE 'Europe/Warsaw' AT TIME ZONE 'UTC')::time;
                """);

            migrationBuilder.RenameColumn(
                name: "weekly_summary_time_of_day",
                table: "diet_reminder_settings",
                newName: "weekly_summary_time_of_day_utc");

            migrationBuilder.RenameColumn(
                name: "weekly_summary_day_of_week",
                table: "diet_reminder_settings",
                newName: "weekly_summary_day_of_week_utc");

            migrationBuilder.RenameColumn(
                name: "water_window_start",
                table: "diet_reminder_settings",
                newName: "water_window_start_utc");

            migrationBuilder.RenameColumn(
                name: "water_window_end",
                table: "diet_reminder_settings",
                newName: "water_window_end_utc");
        }
    }
}
