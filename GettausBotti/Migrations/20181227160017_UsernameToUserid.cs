using Microsoft.EntityFrameworkCore.Migrations;

namespace GettausBotti.Migrations
{
    public partial class UsernameToUserid : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserName",
                table: "GetAttempts");

            migrationBuilder.AddColumn<long>(
                name: "UserId",
                table: "GetAttempts",
                nullable: false,
                defaultValue: 0L);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserId",
                table: "GetAttempts");

            migrationBuilder.AddColumn<string>(
                name: "UserName",
                table: "GetAttempts",
                nullable: true);
        }
    }
}
