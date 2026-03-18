using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DietPlanner.Api.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDietPlanAddUserIdToMealEntry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_meal_entries_diet_plans_diet_plan_id",
                table: "meal_entries");

            migrationBuilder.DropTable(
                name: "diet_plans");

            migrationBuilder.DropIndex(
                name: "idx_meal_entries_date",
                table: "meal_entries");

            migrationBuilder.DropColumn(
                name: "diet_plan_id",
                table: "meal_entries");

            migrationBuilder.AddColumn<string>(
                name: "user_id",
                table: "meal_entries",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "idx_meal_entries_user_date",
                table: "meal_entries",
                columns: new[] { "user_id", "date" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_meal_entries_user_date",
                table: "meal_entries");

            migrationBuilder.DropColumn(
                name: "user_id",
                table: "meal_entries");

            migrationBuilder.AddColumn<Guid>(
                name: "diet_plan_id",
                table: "meal_entries",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "diet_plans",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    user_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_diet_plans", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "idx_meal_entries_date",
                table: "meal_entries",
                columns: new[] { "diet_plan_id", "date" });

            migrationBuilder.CreateIndex(
                name: "idx_diet_plans_dates",
                table: "diet_plans",
                columns: new[] { "start_date", "end_date" });

            migrationBuilder.CreateIndex(
                name: "idx_diet_plans_user",
                table: "diet_plans",
                column: "user_id");

            migrationBuilder.AddForeignKey(
                name: "FK_meal_entries_diet_plans_diet_plan_id",
                table: "meal_entries",
                column: "diet_plan_id",
                principalTable: "diet_plans",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
