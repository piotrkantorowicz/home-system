using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Household.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MapXminConcurrencyToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "household_invitations",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "xmin",
                table: "household_invitations");
        }
    }
}
