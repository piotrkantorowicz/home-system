using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DietPlanner.Api.Migrations
{
    /// <inheritdoc />
    public partial class DropUserGoals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_goals");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "user_goals",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    carbs_grams = table.Column<decimal>(type: "numeric", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    daily_calorie_target = table.Column<int>(type: "integer", nullable: true),
                    fat_grams = table.Column<decimal>(type: "numeric", nullable: true),
                    fiber_grams = table.Column<decimal>(type: "numeric", nullable: true),
                    protein_grams = table.Column<decimal>(type: "numeric", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    user_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_goals", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "idx_user_goals_user",
                table: "user_goals",
                column: "user_id",
                unique: true);
        }
    }
}
