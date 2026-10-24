using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DietPlanner.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ScopeCatalogNamesPerUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_recipes_name",
                table: "recipes");

            migrationBuilder.DropIndex(
                name: "IX_products_name",
                table: "products");

            migrationBuilder.CreateIndex(
                name: "IX_recipes_created_by_user_id_name",
                table: "recipes",
                columns: new[] { "created_by_user_id", "name" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_products_created_by_user_id_name",
                table: "products",
                columns: new[] { "created_by_user_id", "name" },
                unique: true,
                filter: "deleted_at IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_recipes_created_by_user_id_name",
                table: "recipes");

            migrationBuilder.DropIndex(
                name: "IX_products_created_by_user_id_name",
                table: "products");

            migrationBuilder.CreateIndex(
                name: "IX_recipes_name",
                table: "recipes",
                column: "name",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_products_name",
                table: "products",
                column: "name",
                unique: true,
                filter: "deleted_at IS NULL");
        }
    }
}
