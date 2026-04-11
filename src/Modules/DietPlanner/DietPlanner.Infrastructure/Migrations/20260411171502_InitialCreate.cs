using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DietPlanner.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
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
                    fiber_per_100g = table.Column<decimal>(type: "numeric", nullable: true),
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
                name: "user_goals",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    daily_calorie_target = table.Column<int>(type: "integer", nullable: true),
                    protein_grams = table.Column<decimal>(type: "numeric", nullable: true),
                    carbs_grams = table.Column<decimal>(type: "numeric", nullable: true),
                    fat_grams = table.Column<decimal>(type: "numeric", nullable: true),
                    fiber_grams = table.Column<decimal>(type: "numeric", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_goals", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_profiles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    date_of_birth = table.Column<DateOnly>(type: "date", nullable: true),
                    gender = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    height_cm = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: true),
                    current_weight_kg = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: true),
                    target_weight_kg = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: true),
                    activity_level = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_profiles", x => x.id);
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

            migrationBuilder.CreateTable(
                name: "meal_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    meal_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    recipe_id = table.Column<Guid>(type: "uuid", nullable: false),
                    servings = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false, defaultValue: 1m),
                    notes = table.Column<string>(type: "text", nullable: true),
                    meal_time = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    sequence_order = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_meal_entries", x => x.id);
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
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    recipe_id = table.Column<Guid>(type: "uuid", nullable: true)
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
                name: "idx_hydration_configs_user",
                table: "hydration_configs",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_meal_entries_recipe",
                table: "meal_entries",
                column: "recipe_id");

            migrationBuilder.CreateIndex(
                name: "idx_meal_entries_user_date",
                table: "meal_entries",
                columns: new[] { "user_id", "date" });

            migrationBuilder.CreateIndex(
                name: "idx_meal_schedule_configs_user",
                table: "meal_schedule_configs",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_meal_slots_meal_schedule_config_id",
                table: "meal_slots",
                column: "meal_schedule_config_id");

            migrationBuilder.CreateIndex(
                name: "idx_notification_preferences_user",
                table: "notification_preferences",
                column: "user_id",
                unique: true);

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
                unique: true,
                filter: "deleted_at IS NULL");

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
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_user_goals_user",
                table: "user_goals",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_user_profiles_user",
                table: "user_profiles",
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
                name: "meal_entries");

            migrationBuilder.DropTable(
                name: "meal_slots");

            migrationBuilder.DropTable(
                name: "notification_preferences");

            migrationBuilder.DropTable(
                name: "recipe_ingredients");

            migrationBuilder.DropTable(
                name: "user_goals");

            migrationBuilder.DropTable(
                name: "user_profiles");

            migrationBuilder.DropTable(
                name: "water_intakes");

            migrationBuilder.DropTable(
                name: "meal_schedule_configs");

            migrationBuilder.DropTable(
                name: "products");

            migrationBuilder.DropTable(
                name: "recipes");
        }
    }
}
