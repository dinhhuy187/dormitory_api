using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoomService.API.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class UpdateZoneAndAmenities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HasAirConditioner",
                table: "RoomTypes");

            migrationBuilder.DropColumn(
                name: "HasPrivateBathroom",
                table: "RoomTypes");

            migrationBuilder.AddColumn<List<string>>(
                name: "Amenities",
                table: "RoomTypes",
                type: "text[]",
                nullable: false);

            migrationBuilder.AddColumn<string>(
                name: "ZoneName",
                table: "Buildings",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Amenities",
                table: "RoomTypes");

            migrationBuilder.DropColumn(
                name: "ZoneName",
                table: "Buildings");

            migrationBuilder.AddColumn<bool>(
                name: "HasAirConditioner",
                table: "RoomTypes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasPrivateBathroom",
                table: "RoomTypes",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
