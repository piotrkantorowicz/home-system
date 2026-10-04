using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DietPlanner.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddShoppingChecks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "shopping_checks",
                columns: table => new
                {
                    scope_id = table.Column<Guid>(type: "uuid", nullable: false),
                    range_from = table.Column<DateOnly>(type: "date", nullable: false),
                    range_to = table.Column<DateOnly>(type: "date", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    checked_by_person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    checked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shopping_checks", x => new { x.scope_id, x.range_from, x.range_to, x.product_id, x.unit });
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "shopping_checks");
        }
    }
}
