using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DietPlanner.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class IndexRecipeImageVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "idx_recipes_image_version",
                table: "recipes",
                column: "image_version",
                filter: "image_version IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_recipes_image_version",
                table: "recipes");
        }
    }
}
