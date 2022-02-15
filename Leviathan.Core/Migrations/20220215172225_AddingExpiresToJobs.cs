using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leviathan.Core.Migrations
{
    public partial class AddingExpiresToJobs : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "esi_sso_expires_in",
                table: "characters");

            migrationBuilder.AddColumn<DateTime>(
                name: "expires_on",
                table: "corporations",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "esi_sso_expires_on",
                table: "characters",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "expires_on",
                table: "corporations");

            migrationBuilder.DropColumn(
                name: "esi_sso_expires_on",
                table: "characters");

            migrationBuilder.AddColumn<int>(
                name: "esi_sso_expires_in",
                table: "characters",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }
    }
}
