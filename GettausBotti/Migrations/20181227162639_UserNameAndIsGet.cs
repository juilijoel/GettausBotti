using Microsoft.EntityFrameworkCore.Migrations;

namespace GettausBotti.Migrations
{
    public partial class UserNameAndIsGet : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsGet",
                table: "GetAttempts",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "UserName",
                table: "GetAttempts",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsGet",
                table: "GetAttempts");

            migrationBuilder.DropColumn(
                name: "UserName",
                table: "GetAttempts");
        }
    }
}
