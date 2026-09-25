using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Household.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddHouseholdInvitationTargetAndNickname : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "email",
                table: "household_invitations",
                type: "character varying(320)",
                maxLength: 320,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(320)",
                oldMaxLength: 320);

            migrationBuilder.AddColumn<string>(
                name: "nickname",
                table: "household_invitations",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "target_person_id",
                table: "household_invitations",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "idx_household_invitations_target_person",
                table: "household_invitations",
                column: "target_person_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_household_invitations_target_person",
                table: "household_invitations");

            migrationBuilder.DropColumn(
                name: "nickname",
                table: "household_invitations");

            migrationBuilder.DropColumn(
                name: "target_person_id",
                table: "household_invitations");

            migrationBuilder.AlterColumn<string>(
                name: "email",
                table: "household_invitations",
                type: "character varying(320)",
                maxLength: 320,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(320)",
                oldMaxLength: 320,
                oldNullable: true);
        }
    }
}
