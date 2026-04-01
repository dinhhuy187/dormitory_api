using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoomService.API.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddOccupiedCountToRoom : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OccupiedCount",
                table: "Rooms",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OccupiedCount",
                table: "Rooms");
        }
    }
}
