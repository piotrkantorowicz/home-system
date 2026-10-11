using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Investments.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWalletArchivedOn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "archived_on",
                table: "wallets",
                type: "date",
                nullable: true);

            // Wallets archived before this column existed: treat them as archived today.
            migrationBuilder.Sql("UPDATE wallets SET archived_on = CURRENT_DATE WHERE is_archived;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "archived_on",
                table: "wallets");
        }
    }
}
