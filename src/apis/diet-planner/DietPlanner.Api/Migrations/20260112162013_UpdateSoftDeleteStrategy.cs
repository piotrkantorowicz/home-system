using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DietPlanner.Api.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSoftDeleteStrategy : Migration
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_recipes_name",
                table: "recipes");

            migrationBuilder.DropIndex(
                name: "IX_products_name",
                table: "products");

            migrationBuilder.CreateIndex(
                name: "IX_recipes_name",
                table: "recipes",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_products_name",
                table: "products",
                column: "name",
                unique: true);
        }
    }
}
