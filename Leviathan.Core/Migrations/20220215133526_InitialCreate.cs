using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leviathan.Core.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "alliances",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    name = table.Column<string>(type: "TEXT", nullable: false),
                    alliance_id = table.Column<int>(type: "INTEGER", nullable: false),
                    ticker = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_alliances", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "characters",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    auth_state = table.Column<string>(type: "TEXT", nullable: false),
                    esi_sso_status = table.Column<bool>(type: "INTEGER", nullable: false),
                    esi_sso_access_token = table.Column<string>(type: "TEXT", nullable: false),
                    esi_sso_expires_in = table.Column<int>(type: "INTEGER", nullable: false),
                    esi_sso_refresh_token = table.Column<string>(type: "TEXT", nullable: false),
                    esi_character_id = table.Column<int>(type: "INTEGER", nullable: false),
                    esi_character_name = table.Column<string>(type: "TEXT", nullable: false),
                    esi_alliance_id = table.Column<int>(type: "INTEGER", nullable: false),
                    esi_corporation_id = table.Column<int>(type: "INTEGER", nullable: false),
                    discord_user_id = table.Column<ulong>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_characters", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "corporations",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    alliance_id = table.Column<int>(type: "INTEGER", nullable: false),
                    name = table.Column<string>(type: "TEXT", nullable: false),
                    corporation_id = table.Column<int>(type: "INTEGER", nullable: false),
                    ticker = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_corporations", x => x.id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "alliances");

            migrationBuilder.DropTable(
                name: "characters");

            migrationBuilder.DropTable(
                name: "corporations");
        }
    }
}
