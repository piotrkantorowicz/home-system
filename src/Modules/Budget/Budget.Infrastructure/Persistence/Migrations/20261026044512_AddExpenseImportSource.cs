using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Budget.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddExpenseImportSource : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "import_scope",
                table: "expenses",
                type: "character varying(36)",
                maxLength: 36,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "import_source_key",
                table: "expenses",
                type: "character(64)",
                fixedLength: true,
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ux_expenses_import_source",
                table: "expenses",
                columns: new[] { "budget_id", "import_scope", "import_source_key" },
                unique: true,
                filter: "import_source_key IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_expenses_import_source_complete",
                table: "expenses",
                sql: "(import_scope IS NULL) = (import_source_key IS NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_expenses_import_source",
                table: "expenses");

            migrationBuilder.DropCheckConstraint(
                name: "ck_expenses_import_source_complete",
                table: "expenses");

            migrationBuilder.DropColumn(
                name: "import_scope",
                table: "expenses");

            migrationBuilder.DropColumn(
                name: "import_source_key",
                table: "expenses");
        }
    }
}
