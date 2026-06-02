using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Billing.API.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class MakeInvoiceStudentNullableForRoomMonthly : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "StudentId",
                table: "Invoices",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Invoices_BookingRegistrationScope",
                table: "Invoices",
                sql: "\"InvoiceType\" <> 'BookingRegistration' OR (\"BookingId\" IS NOT NULL AND \"StudentId\" IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Invoices_MonthlyUtilityScope",
                table: "Invoices",
                sql: "\"InvoiceType\" <> 'MonthlyUtility' OR (\"BookingId\" IS NULL AND \"StudentId\" IS NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Invoices_BookingRegistrationScope",
                table: "Invoices");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Invoices_MonthlyUtilityScope",
                table: "Invoices");

            migrationBuilder.AlterColumn<Guid>(
                name: "StudentId",
                table: "Invoices",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
