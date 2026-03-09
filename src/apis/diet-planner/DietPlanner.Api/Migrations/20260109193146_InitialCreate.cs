using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DietPlanner.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "diet_plans",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_diet_plans", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "products",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    calories_per_100g = table.Column<decimal>(type: "numeric", nullable: true),
                    protein_per_100g = table.Column<decimal>(type: "numeric", nullable: true),
                    carbs_per_100g = table.Column<decimal>(type: "numeric", nullable: true),
                    fat_per_100g = table.Column<decimal>(type: "numeric", nullable: true),
                    default_unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    density_grams_per_ml = table.Column<decimal>(type: "numeric(6,3)", precision: 6, scale: 3, nullable: true),
                    gram_per_piece = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    created_by_user_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_products", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "recipes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    instructions = table.Column<string>(type: "text", nullable: true),
                    servings = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    prep_time_minutes = table.Column<int>(type: "integer", nullable: true),
                    created_by_user_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_recipes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "meal_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    diet_plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    meal_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    recipe_id = table.Column<Guid>(type: "uuid", nullable: false),
                    servings = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false, defaultValue: 1m),
                    notes = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_meal_entries", x => x.id);
                    table.ForeignKey(
                        name: "FK_meal_entries_diet_plans_diet_plan_id",
                        column: x => x.diet_plan_id,
                        principalTable: "diet_plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_meal_entries_recipes_recipe_id",
                        column: x => x.recipe_id,
                        principalTable: "recipes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "recipe_ingredients",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    recipe_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_recipe_ingredients", x => x.id);
                    table.ForeignKey(
                        name: "FK_recipe_ingredients_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_recipe_ingredients_recipes_recipe_id",
                        column: x => x.recipe_id,
                        principalTable: "recipes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_diet_plans_dates",
                table: "diet_plans",
                columns: new[] { "start_date", "end_date" });

            migrationBuilder.CreateIndex(
                name: "idx_diet_plans_user",
                table: "diet_plans",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "idx_meal_entries_date",
                table: "meal_entries",
                columns: new[] { "diet_plan_id", "date" });

            migrationBuilder.CreateIndex(
                name: "idx_meal_entries_recipe",
                table: "meal_entries",
                column: "recipe_id");

            migrationBuilder.CreateIndex(
                name: "idx_products_active",
                table: "products",
                column: "id",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_products_created_by",
                table: "products",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_products_name",
                table: "products",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_recipe_ingredients_product",
                table: "recipe_ingredients",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "idx_recipe_ingredients_recipe",
                table: "recipe_ingredients",
                column: "recipe_id");

            migrationBuilder.CreateIndex(
                name: "unique_recipe_product",
                table: "recipe_ingredients",
                columns: new[] { "recipe_id", "product_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_recipes_active",
                table: "recipes",
                column: "id",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_recipes_created_by",
                table: "recipes",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_recipes_name",
                table: "recipes",
                column: "name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "meal_entries");

            migrationBuilder.DropTable(
                name: "recipe_ingredients");

            migrationBuilder.DropTable(
                name: "diet_plans");

            migrationBuilder.DropTable(
                name: "products");

            migrationBuilder.DropTable(
                name: "recipes");
        }
    }
}
