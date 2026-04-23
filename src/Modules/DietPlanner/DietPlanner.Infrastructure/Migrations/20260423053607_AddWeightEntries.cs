using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DietPlanner.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWeightEntries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "weight_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    weight_kg = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_weight_entries", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "idx_weight_entries_user_date",
                table: "weight_entries",
                columns: new[] { "user_id", "date" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "weight_entries");
        }
    }
}
