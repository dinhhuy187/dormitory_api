using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoomService.API.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddBuildingBankAccountInfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AccountName",
                table: "Buildings",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AccountNumber",
                table: "Buildings",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BankCode",
                table: "Buildings",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccountName",
                table: "Buildings");

            migrationBuilder.DropColumn(
                name: "AccountNumber",
                table: "Buildings");

            migrationBuilder.DropColumn(
                name: "BankCode",
                table: "Buildings");
        }
    }
}
