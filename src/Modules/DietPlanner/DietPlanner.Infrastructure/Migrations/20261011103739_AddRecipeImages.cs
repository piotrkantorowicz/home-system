using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DietPlanner.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRecipeImages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "image_height",
                table: "recipes",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "image_size",
                table: "recipes",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "image_thumbnail_height",
                table: "recipes",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "image_thumbnail_size",
                table: "recipes",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "image_thumbnail_width",
                table: "recipes",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "image_version",
                table: "recipes",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "image_width",
                table: "recipes",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "recipe_image_revisions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    recipe_id = table.Column<Guid>(type: "uuid", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    published = table.Column<bool>(type: "boolean", nullable: false),
                    retired = table.Column<bool>(type: "boolean", nullable: false),
                    next_cleanup_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_recipe_image_revisions", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_recipe_image_revisions_next_cleanup_at",
                table: "recipe_image_revisions",
                column: "next_cleanup_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "recipe_image_revisions");

            migrationBuilder.DropColumn(
                name: "image_height",
                table: "recipes");

            migrationBuilder.DropColumn(
                name: "image_size",
                table: "recipes");

            migrationBuilder.DropColumn(
                name: "image_thumbnail_height",
                table: "recipes");

            migrationBuilder.DropColumn(
                name: "image_thumbnail_size",
                table: "recipes");

            migrationBuilder.DropColumn(
                name: "image_thumbnail_width",
                table: "recipes");

            migrationBuilder.DropColumn(
                name: "image_version",
                table: "recipes");

            migrationBuilder.DropColumn(
                name: "image_width",
                table: "recipes");
        }
    }
}
