using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DietPlanner.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class LibraryVisibility : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "visibility",
                table: "recipes",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "Household"); // existing rows join the household library (#230)

            migrationBuilder.AddColumn<string>(
                name: "visibility",
                table: "products",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "Household"); // existing rows join the household library (#230)
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "visibility",
                table: "recipes");

            migrationBuilder.DropColumn(
                name: "visibility",
                table: "products");
        }
    }
}
