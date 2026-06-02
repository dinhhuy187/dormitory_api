using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Billing.API.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddRoomTypeIdToContractTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "RoomTypeId",
                table: "ContractTemplates",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContractTemplates_RoomTypeId_IsActive_EffectiveFrom",
                table: "ContractTemplates",
                columns: new[] { "RoomTypeId", "IsActive", "EffectiveFrom" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ContractTemplates_RoomTypeId_IsActive_EffectiveFrom",
                table: "ContractTemplates");

            migrationBuilder.DropColumn(
                name: "RoomTypeId",
                table: "ContractTemplates");
        }
    }
}
