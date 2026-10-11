using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Investments.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFxRates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "fx_rates",
                columns: table => new
                {
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    effective_date = table.Column<DateOnly>(type: "date", nullable: false),
                    mid = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    table_no = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fx_rates", x => new { x.currency, x.effective_date });
                    table.CheckConstraint("ck_fx_rates_currency", "currency IN ('EUR', 'USD')");
                    table.CheckConstraint("ck_fx_rates_mid_positive", "mid > 0");
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "fx_rates");
        }
    }
}
