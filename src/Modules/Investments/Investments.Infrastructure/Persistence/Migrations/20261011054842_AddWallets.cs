using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Investments.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWallets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "wallets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    household_id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    institution = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    kind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    visibility = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    is_archived = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wallets", x => x.id);
                    table.CheckConstraint("ck_wallets_currency", "currency IN ('PLN', 'EUR', 'USD')");
                    table.CheckConstraint("ck_wallets_institution", "institution IN ('XTB', 'Saxo', 'Bossa', 'MBank', 'PKOBP', 'Allianz', 'Other')");
                    table.CheckConstraint("ck_wallets_kind", "kind IN ('Brokerage', 'IKE', 'IKZE', 'PPK', 'TreasuryBonds', 'Savings', 'Cash')");
                    table.CheckConstraint("ck_wallets_visibility", "visibility IN ('Shared', 'Private')");
                });

            migrationBuilder.CreateIndex(
                name: "IX_wallets_household_id_owner_person_id",
                table: "wallets",
                columns: new[] { "household_id", "owner_person_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "wallets");
        }
    }
}
