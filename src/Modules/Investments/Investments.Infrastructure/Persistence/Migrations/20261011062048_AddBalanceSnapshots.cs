using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Investments.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBalanceSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "AK_wallets_id_household_id",
                table: "wallets",
                columns: new[] { "id", "household_id" });

            migrationBuilder.CreateTable(
                name: "balance_snapshots",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    wallet_id = table.Column<Guid>(type: "uuid", nullable: false),
                    household_id = table.Column<Guid>(type: "uuid", nullable: false),
                    as_of = table.Column<DateOnly>(type: "date", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    note = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    recorded_by_person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    recorded_by_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    recorded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_balance_snapshots", x => x.id);
                    table.CheckConstraint("ck_balance_snapshots_amount_range", "amount >= 0 AND amount <= 9999999999.99");
                    table.ForeignKey(
                        name: "FK_balance_snapshots_wallets_wallet_id_household_id",
                        columns: x => new { x.wallet_id, x.household_id },
                        principalTable: "wallets",
                        principalColumns: new[] { "id", "household_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_balance_snapshots_wallet_id_household_id",
                table: "balance_snapshots",
                columns: new[] { "wallet_id", "household_id" });

            migrationBuilder.CreateIndex(
                name: "ux_balance_snapshots_wallet_as_of",
                table: "balance_snapshots",
                columns: new[] { "wallet_id", "as_of" },
                unique: true,
                descending: new[] { false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "balance_snapshots");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_wallets_id_household_id",
                table: "wallets");
        }
    }
}
