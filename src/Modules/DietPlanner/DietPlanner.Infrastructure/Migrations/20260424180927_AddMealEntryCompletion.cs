using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DietPlanner.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMealEntryCompletion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "actual_recipe_id",
                table: "meal_entries",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "meal_entries",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Planned");

            migrationBuilder.CreateTable(
                name: "meal_entry_actual_products",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(10,3)", precision: 10, scale: 3, nullable: false),
                    unit = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    meal_entry_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_meal_entry_actual_products", x => x.id);
                    table.ForeignKey(
                        name: "FK_meal_entry_actual_products_meal_entries_meal_entry_id",
                        column: x => x.meal_entry_id,
                        principalTable: "meal_entries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_meal_entry_actual_products_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "idx_meal_entries_actual_recipe",
                table: "meal_entries",
                column: "actual_recipe_id");

            migrationBuilder.CreateIndex(
                name: "idx_meal_entry_actual_products_product",
                table: "meal_entry_actual_products",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_meal_entry_actual_products_meal_entry_id",
                table: "meal_entry_actual_products",
                column: "meal_entry_id");

            migrationBuilder.AddForeignKey(
                name: "FK_meal_entries_recipes_actual_recipe_id",
                table: "meal_entries",
                column: "actual_recipe_id",
                principalTable: "recipes",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_meal_entries_recipes_actual_recipe_id",
                table: "meal_entries");

            migrationBuilder.DropTable(
                name: "meal_entry_actual_products");

            migrationBuilder.DropIndex(
                name: "idx_meal_entries_actual_recipe",
                table: "meal_entries");

            migrationBuilder.DropColumn(
                name: "actual_recipe_id",
                table: "meal_entries");

            migrationBuilder.DropColumn(
                name: "status",
                table: "meal_entries");
        }
    }
}
