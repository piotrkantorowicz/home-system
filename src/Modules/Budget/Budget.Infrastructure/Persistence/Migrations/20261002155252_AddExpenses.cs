using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Budget.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddExpenses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "AK_budget_accounts_id_budget_id",
                table: "budget_accounts",
                columns: new[] { "id", "budget_id" });

            migrationBuilder.CreateTable(
                name: "expenses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    budget_id = table.Column<Guid>(type: "uuid", nullable: false),
                    budget_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    category = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    occurred_on = table.Column<DateOnly>(type: "date", nullable: false),
                    funding_source = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    paid_by_person_id = table.Column<Guid>(type: "uuid", nullable: true),
                    paid_by_display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    added_by_person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    added_by_display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    revision = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_expenses", x => x.id);
                    table.CheckConstraint("ck_expenses_amount_range", "amount > 0 AND amount <= 9999999999.99");
                    table.CheckConstraint("ck_expenses_payer_matches_funding", "(funding_source = 'HouseholdFunds') = (paid_by_person_id IS NULL)");
                    table.ForeignKey(
                        name: "FK_expenses_budget_accounts_budget_account_id_budget_id",
                        columns: x => new { x.budget_account_id, x.budget_id },
                        principalTable: "budget_accounts",
                        principalColumns: new[] { "id", "budget_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "expense_revisions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    expense_id = table.Column<Guid>(type: "uuid", nullable: false),
                    budget_id = table.Column<Guid>(type: "uuid", nullable: false),
                    revision_number = table.Column<int>(type: "integer", nullable: false),
                    operation = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    actor_person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    request = table.Column<string>(type: "jsonb", nullable: false),
                    snapshot = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_expense_revisions", x => x.id);
                    table.ForeignKey(
                        name: "FK_expense_revisions_expenses_expense_id",
                        column: x => x.expense_id,
                        principalTable: "expenses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "expense_shares",
                columns: table => new
                {
                    expense_id = table.Column<Guid>(type: "uuid", nullable: false),
                    person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    person_display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_expense_shares", x => new { x.expense_id, x.person_id });
                    table.CheckConstraint("ck_expense_shares_amount", "amount >= 0");
                    table.ForeignKey(
                        name: "FK_expense_shares_expenses_expense_id",
                        column: x => x.expense_id,
                        principalTable: "expenses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_expense_revisions_expense_id_revision_number",
                table: "expense_revisions",
                columns: new[] { "expense_id", "revision_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_expense_revisions_request_identity",
                table: "expense_revisions",
                columns: new[] { "budget_id", "actor_person_id", "client_request_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_expense_shares_person_id",
                table: "expense_shares",
                column: "person_id");

            migrationBuilder.CreateIndex(
                name: "IX_expenses_budget_account_id_budget_id",
                table: "expenses",
                columns: new[] { "budget_account_id", "budget_id" });

            migrationBuilder.CreateIndex(
                name: "ix_expenses_budget_date_id",
                table: "expenses",
                columns: new[] { "budget_id", "occurred_on", "id" },
                descending: new[] { false, true, true });

            migrationBuilder.CreateIndex(
                name: "ix_expenses_duplicate_lookup",
                table: "expenses",
                columns: new[] { "budget_id", "category", "amount", "occurred_on" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "expense_revisions");

            migrationBuilder.DropTable(
                name: "expense_shares");

            migrationBuilder.DropTable(
                name: "expenses");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_budget_accounts_id_budget_id",
                table: "budget_accounts");
        }
    }
}
