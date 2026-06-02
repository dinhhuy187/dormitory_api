using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Billing.API.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class InitialBillingSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ContractTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContractTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ServicePriceTiers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TierName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RoomCapacity = table.Column<int>(type: "integer", nullable: true),
                    FromUsage = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ToUsage = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    UnitName = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServicePriceTiers", x => x.Id);
                    table.CheckConstraint("CK_ServicePriceTiers_RoomCapacity", "\"RoomCapacity\" IS NULL OR \"RoomCapacity\" > 0");
                });

            migrationBuilder.CreateTable(
                name: "Invoices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RoomId = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    BillingMonth = table.Column<short>(type: "smallint", nullable: false),
                    BillingYear = table.Column<int>(type: "integer", nullable: false),
                    ElectricityOldIndex = table.Column<int>(type: "integer", nullable: false),
                    ElectricityNewIndex = table.Column<int>(type: "integer", nullable: false),
                    ElectricityUsage = table.Column<int>(type: "integer", nullable: false),
                    ElectricityTierSnapshot = table.Column<string>(type: "jsonb", nullable: false),
                    ElectricityAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    WaterOldIndex = table.Column<int>(type: "integer", nullable: false),
                    WaterNewIndex = table.Column<int>(type: "integer", nullable: false),
                    WaterUsage = table.Column<int>(type: "integer", nullable: false),
                    WaterTierSnapshot = table.Column<string>(type: "jsonb", nullable: false),
                    WaterAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    SurchargeTotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PaidAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ContractTemplateId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invoices", x => x.Id);
                    table.CheckConstraint("CK_Invoices_ElectricityIndex", "\"ElectricityNewIndex\" >= \"ElectricityOldIndex\"");
                    table.CheckConstraint("CK_Invoices_WaterIndex", "\"WaterNewIndex\" >= \"WaterOldIndex\"");
                    table.ForeignKey(
                        name: "FK_Invoices_ContractTemplates_ContractTemplateId",
                        column: x => x.ContractTemplateId,
                        principalTable: "ContractTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Surcharges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Surcharges", x => x.Id);
                    table.CheckConstraint("CK_Surcharges_Amount", "\"Amount\" > 0");
                    table.ForeignKey(
                        name: "FK_Surcharges_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContractTemplates_Code_Version",
                table: "ContractTemplates",
                columns: new[] { "Code", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContractTemplates_IsActive_EffectiveFrom",
                table: "ContractTemplates",
                columns: new[] { "IsActive", "EffectiveFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_ContractTemplateId",
                table: "Invoices",
                column: "ContractTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_RoomId_BillingYear_BillingMonth",
                table: "Invoices",
                columns: new[] { "RoomId", "BillingYear", "BillingMonth" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_RoomId_BillingYear_BillingMonth_CreatedAt",
                table: "Invoices",
                columns: new[] { "RoomId", "BillingYear", "BillingMonth", "CreatedAt" },
                descending: new[] { false, true, true, true });

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_Status_BillingYear_BillingMonth",
                table: "Invoices",
                columns: new[] { "Status", "BillingYear", "BillingMonth" });

            migrationBuilder.CreateIndex(
                name: "IX_ServicePriceTiers_ServiceType_IsActive_EffectiveFrom_RoomCa~",
                table: "ServicePriceTiers",
                columns: new[] { "ServiceType", "IsActive", "EffectiveFrom", "RoomCapacity", "FromUsage" });

            migrationBuilder.CreateIndex(
                name: "IX_ServicePriceTiers_UQ_GlobalCapacity",
                table: "ServicePriceTiers",
                columns: new[] { "ServiceType", "EffectiveFrom", "FromUsage" },
                unique: true,
                filter: "\"RoomCapacity\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ServicePriceTiers_UQ_RoomCapacity",
                table: "ServicePriceTiers",
                columns: new[] { "ServiceType", "EffectiveFrom", "RoomCapacity", "FromUsage" },
                unique: true,
                filter: "\"RoomCapacity\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Surcharges_InvoiceId",
                table: "Surcharges",
                column: "InvoiceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ServicePriceTiers");

            migrationBuilder.DropTable(
                name: "Surcharges");

            migrationBuilder.DropTable(
                name: "Invoices");

            migrationBuilder.DropTable(
                name: "ContractTemplates");
        }
    }
}
