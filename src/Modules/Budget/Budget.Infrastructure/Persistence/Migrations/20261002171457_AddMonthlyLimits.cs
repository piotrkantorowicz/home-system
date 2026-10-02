using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Budget.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMonthlyLimits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "monthly_limits",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    budget_id = table.Column<Guid>(type: "uuid", nullable: false),
                    budget_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    month_start = table.Column<DateOnly>(type: "date", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    revision = table.Column<int>(type: "integer", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_monthly_limits", x => x.id);
                    table.CheckConstraint("ck_monthly_limits_amount_range", "amount >= 0 AND amount <= 9999999999.99");
                    table.CheckConstraint("ck_monthly_limits_month_start", "EXTRACT(DAY FROM month_start) = 1");
                    table.ForeignKey(
                        name: "FK_monthly_limits_budget_accounts_budget_account_id_budget_id",
                        columns: x => new { x.budget_account_id, x.budget_id },
                        principalTable: "budget_accounts",
                        principalColumns: new[] { "id", "budget_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_monthly_limits_budget_account_id_budget_id",
                table: "monthly_limits",
                columns: new[] { "budget_account_id", "budget_id" });

            migrationBuilder.CreateIndex(
                name: "ux_monthly_limits_account_month",
                table: "monthly_limits",
                columns: new[] { "budget_account_id", "month_start" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "monthly_limits");
        }
    }
}
