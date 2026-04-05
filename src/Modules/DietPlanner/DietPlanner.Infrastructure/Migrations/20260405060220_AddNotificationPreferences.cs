using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DietPlanner.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "notification_preferences",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    meal_reminder_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    meal_reminder_lead_time_minutes = table.Column<int>(type: "integer", nullable: false),
                    water_reminder_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    water_reminder_interval_minutes = table.Column<int>(type: "integer", nullable: false),
                    weekly_summary_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    goal_milestone_alerts_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notification_preferences", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "idx_notification_preferences_user",
                table: "notification_preferences",
                column: "user_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "notification_preferences");
        }
    }
}
