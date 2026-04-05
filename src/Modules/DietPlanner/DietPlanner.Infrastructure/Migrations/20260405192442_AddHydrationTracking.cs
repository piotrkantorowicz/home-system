using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DietPlanner.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddHydrationTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "hydration_configs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    daily_water_target_ml = table.Column<int>(type: "integer", nullable: false),
                    glass_size_ml = table.Column<int>(type: "integer", nullable: false),
                    track_water_intake = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_hydration_configs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "water_intakes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    amount_ml = table.Column<int>(type: "integer", nullable: false),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_water_intakes", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "idx_hydration_configs_user",
                table: "hydration_configs",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_water_intakes_user_date",
                table: "water_intakes",
                columns: new[] { "user_id", "date" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "hydration_configs");

            migrationBuilder.DropTable(
                name: "water_intakes");
        }
    }
}
