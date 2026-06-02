using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Billing.API.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoiceRoomLocationAndCanceledStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BuildingCode",
                table: "Invoices",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Floor",
                table: "Invoices",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_BuildingCode_Floor_BillingYear_BillingMonth_Status",
                table: "Invoices",
                columns: new[] { "BuildingCode", "Floor", "BillingYear", "BillingMonth", "Status" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Invoices_Floor",
                table: "Invoices",
                sql: "\"Floor\" IS NULL OR \"Floor\" > 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Invoices_BuildingCode_Floor_BillingYear_BillingMonth_Status",
                table: "Invoices");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Invoices_Floor",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "BuildingCode",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "Floor",
                table: "Invoices");
        }
    }
}
