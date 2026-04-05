using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DietPlanner.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMealScheduleConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "meal_schedule_configs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_meal_schedule_configs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "meal_slots",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    default_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    meal_schedule_config_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_meal_slots", x => x.id);
                    table.ForeignKey(
                        name: "FK_meal_slots_meal_schedule_configs_meal_schedule_config_id",
                        column: x => x.meal_schedule_config_id,
                        principalTable: "meal_schedule_configs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_meal_schedule_configs_user",
                table: "meal_schedule_configs",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_meal_slots_meal_schedule_config_id",
                table: "meal_slots",
                column: "meal_schedule_config_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "meal_slots");

            migrationBuilder.DropTable(
                name: "meal_schedule_configs");
        }
    }
}
