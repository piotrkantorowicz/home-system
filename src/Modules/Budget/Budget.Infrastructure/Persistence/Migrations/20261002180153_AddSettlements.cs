using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Budget.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSettlements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "settlements",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    budget_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    to_person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    to_display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    paid_on = table.Column<DateOnly>(type: "date", nullable: false),
                    note = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    added_by_person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    added_by_display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    client_request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    revision = table.Column<int>(type: "integer", nullable: false),
                    is_voided = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    voided_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    void_reason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    voided_by_person_id = table.Column<Guid>(type: "uuid", nullable: true),
                    voided_by_display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_settlements", x => x.id);
                    table.CheckConstraint("ck_settlements_amount_range", "amount > 0 AND amount <= 9999999999.99");
                    table.CheckConstraint("ck_settlements_distinct_people", "from_person_id <> to_person_id");
                    table.ForeignKey(
                        name: "FK_settlements_budgets_budget_id",
                        column: x => x.budget_id,
                        principalTable: "budgets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_settlements_budget_date",
                table: "settlements",
                columns: new[] { "budget_id", "paid_on" });

            migrationBuilder.CreateIndex(
                name: "ux_settlements_request_identity",
                table: "settlements",
                columns: new[] { "budget_id", "added_by_person_id", "client_request_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "settlements");
        }
    }
}
