using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace EcoLens.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddBarcodeReference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Posts",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ClimatiqActivityId",
                table: "CarbonReferences",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Region",
                table: "CarbonReferences",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "CarbonReferences",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "ApplicationUsers",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateTable(
                name: "BarcodeReferences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Barcode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CarbonReferenceId = table.Column<int>(type: "int", nullable: true),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Brand = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BarcodeReferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BarcodeReferences_CarbonReferences_CarbonReferenceId",
                        column: x => x.CarbonReferenceId,
                        principalTable: "CarbonReferences",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "DietTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TemplateName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DietTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UtilityBills",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    BillType = table.Column<int>(type: "int", nullable: false),
                    BillPeriodStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BillPeriodEnd = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ElectricityUsage = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    WaterUsage = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    GasUsage = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    ElectricityCarbonEmission = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    WaterCarbonEmission = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    GasCarbonEmission = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    TotalCarbonEmission = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    OcrRawText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OcrConfidence = table.Column<decimal>(type: "decimal(5,4)", nullable: true),
                    InputMethod = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UtilityBills", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UtilityBills_ApplicationUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "ApplicationUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DietTemplateItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DietTemplateId = table.Column<int>(type: "int", nullable: false),
                    FoodId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<double>(type: "float", nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DietTemplateItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DietTemplateItems_DietTemplates_DietTemplateId",
                        column: x => x.DietTemplateId,
                        principalTable: "DietTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ClimatiqActivityId", "CreatedAt", "Region", "Source", "UpdatedAt" },
                values: new object[] { null, new DateTime(2026, 1, 26, 5, 1, 31, 981, DateTimeKind.Utc).AddTicks(9258), null, "Local", new DateTime(2026, 1, 26, 5, 1, 31, 981, DateTimeKind.Utc).AddTicks(9261) });

            migrationBuilder.UpdateData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "ClimatiqActivityId", "CreatedAt", "Region", "Source", "UpdatedAt" },
                values: new object[] { null, new DateTime(2026, 1, 26, 5, 1, 31, 981, DateTimeKind.Utc).AddTicks(9269), null, "Local", new DateTime(2026, 1, 26, 5, 1, 31, 981, DateTimeKind.Utc).AddTicks(9269) });

            migrationBuilder.UpdateData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "ClimatiqActivityId", "CreatedAt", "Region", "Source", "UpdatedAt" },
                values: new object[] { null, new DateTime(2026, 1, 26, 5, 1, 31, 981, DateTimeKind.Utc).AddTicks(9271), null, "Local", new DateTime(2026, 1, 26, 5, 1, 31, 981, DateTimeKind.Utc).AddTicks(9272) });

            migrationBuilder.UpdateData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "ClimatiqActivityId", "CreatedAt", "Region", "Source", "UpdatedAt" },
                values: new object[] { null, new DateTime(2026, 1, 26, 5, 1, 31, 981, DateTimeKind.Utc).AddTicks(9273), null, "Local", new DateTime(2026, 1, 26, 5, 1, 31, 981, DateTimeKind.Utc).AddTicks(9274) });

            migrationBuilder.UpdateData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "ClimatiqActivityId", "CreatedAt", "Region", "Source", "UpdatedAt" },
                values: new object[] { null, new DateTime(2026, 1, 26, 5, 1, 31, 981, DateTimeKind.Utc).AddTicks(9276), null, "Local", new DateTime(2026, 1, 26, 5, 1, 31, 981, DateTimeKind.Utc).AddTicks(9276) });

            migrationBuilder.UpdateData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "ClimatiqActivityId", "CreatedAt", "Region", "Source", "UpdatedAt" },
                values: new object[] { null, new DateTime(2026, 1, 26, 5, 1, 31, 981, DateTimeKind.Utc).AddTicks(9277), null, "Local", new DateTime(2026, 1, 26, 5, 1, 31, 981, DateTimeKind.Utc).AddTicks(9278) });

            migrationBuilder.UpdateData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "ClimatiqActivityId", "CreatedAt", "Region", "Source", "UpdatedAt" },
                values: new object[] { null, new DateTime(2026, 1, 26, 5, 1, 31, 981, DateTimeKind.Utc).AddTicks(9279), null, "Local", new DateTime(2026, 1, 26, 5, 1, 31, 981, DateTimeKind.Utc).AddTicks(9279) });

            migrationBuilder.UpdateData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "ClimatiqActivityId", "CreatedAt", "Region", "Source", "UpdatedAt" },
                values: new object[] { null, new DateTime(2026, 1, 26, 5, 1, 31, 981, DateTimeKind.Utc).AddTicks(9281), null, "Local", new DateTime(2026, 1, 26, 5, 1, 31, 981, DateTimeKind.Utc).AddTicks(9281) });

            migrationBuilder.UpdateData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 9,
                columns: new[] { "ClimatiqActivityId", "CreatedAt", "Region", "Source", "UpdatedAt" },
                values: new object[] { null, new DateTime(2026, 1, 26, 5, 1, 31, 981, DateTimeKind.Utc).AddTicks(9283), null, "Local", new DateTime(2026, 1, 26, 5, 1, 31, 981, DateTimeKind.Utc).AddTicks(9283) });

            migrationBuilder.UpdateData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 10,
                columns: new[] { "ClimatiqActivityId", "CreatedAt", "Region", "Source", "UpdatedAt" },
                values: new object[] { null, new DateTime(2026, 1, 26, 5, 1, 31, 981, DateTimeKind.Utc).AddTicks(9284), null, "Local", new DateTime(2026, 1, 26, 5, 1, 31, 981, DateTimeKind.Utc).AddTicks(9285) });

            migrationBuilder.UpdateData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 11,
                columns: new[] { "ClimatiqActivityId", "CreatedAt", "Region", "Source", "UpdatedAt" },
                values: new object[] { null, new DateTime(2026, 1, 26, 5, 1, 31, 981, DateTimeKind.Utc).AddTicks(9286), null, "Local", new DateTime(2026, 1, 26, 5, 1, 31, 981, DateTimeKind.Utc).AddTicks(9287) });

            migrationBuilder.UpdateData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 12,
                columns: new[] { "ClimatiqActivityId", "CreatedAt", "Region", "Source", "UpdatedAt" },
                values: new object[] { null, new DateTime(2026, 1, 26, 5, 1, 31, 981, DateTimeKind.Utc).AddTicks(9288), null, "Local", new DateTime(2026, 1, 26, 5, 1, 31, 981, DateTimeKind.Utc).AddTicks(9288) });

            migrationBuilder.InsertData(
                table: "CarbonReferences",
                columns: new[] { "Id", "Category", "ClimatiqActivityId", "Co2Factor", "CreatedAt", "LabelName", "Region", "Source", "Unit", "UpdatedAt" },
                values: new object[,]
                {
                    { 13, 2, null, 0.4057m, new DateTime(2026, 1, 26, 5, 1, 31, 981, DateTimeKind.Utc).AddTicks(9290), "Electricity_SG", null, "Local", "kgCO2/kWh", new DateTime(2026, 1, 26, 5, 1, 31, 981, DateTimeKind.Utc).AddTicks(9290) },
                    { 14, 2, null, 0.419m, new DateTime(2026, 1, 26, 5, 1, 31, 981, DateTimeKind.Utc).AddTicks(9291), "Water_SG", null, "Local", "kgCO2/m³", new DateTime(2026, 1, 26, 5, 1, 31, 981, DateTimeKind.Utc).AddTicks(9292) },
                    { 15, 2, null, 0.184m, new DateTime(2026, 1, 26, 5, 1, 31, 981, DateTimeKind.Utc).AddTicks(9293), "Gas_SG", null, "Local", "kgCO2/kWh", new DateTime(2026, 1, 26, 5, 1, 31, 981, DateTimeKind.Utc).AddTicks(9293) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_BarcodeReferences_CarbonReferenceId",
                table: "BarcodeReferences",
                column: "CarbonReferenceId");

            migrationBuilder.CreateIndex(
                name: "IX_DietTemplateItems_DietTemplateId",
                table: "DietTemplateItems",
                column: "DietTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_UtilityBills_UserId",
                table: "UtilityBills",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BarcodeReferences");

            migrationBuilder.DropTable(
                name: "DietTemplateItems");

            migrationBuilder.DropTable(
                name: "UtilityBills");

            migrationBuilder.DropTable(
                name: "DietTemplates");

            migrationBuilder.DeleteData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 15);

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "ClimatiqActivityId",
                table: "CarbonReferences");

            migrationBuilder.DropColumn(
                name: "Region",
                table: "CarbonReferences");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "CarbonReferences");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "ApplicationUsers");

            migrationBuilder.UpdateData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 23, 6, 59, 49, 200, DateTimeKind.Utc).AddTicks(374), new DateTime(2026, 1, 23, 6, 59, 49, 200, DateTimeKind.Utc).AddTicks(379) });

            migrationBuilder.UpdateData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 23, 6, 59, 49, 200, DateTimeKind.Utc).AddTicks(387), new DateTime(2026, 1, 23, 6, 59, 49, 200, DateTimeKind.Utc).AddTicks(387) });

            migrationBuilder.UpdateData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 23, 6, 59, 49, 200, DateTimeKind.Utc).AddTicks(390), new DateTime(2026, 1, 23, 6, 59, 49, 200, DateTimeKind.Utc).AddTicks(391) });

            migrationBuilder.UpdateData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 23, 6, 59, 49, 200, DateTimeKind.Utc).AddTicks(393), new DateTime(2026, 1, 23, 6, 59, 49, 200, DateTimeKind.Utc).AddTicks(394) });

            migrationBuilder.UpdateData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 23, 6, 59, 49, 200, DateTimeKind.Utc).AddTicks(396), new DateTime(2026, 1, 23, 6, 59, 49, 200, DateTimeKind.Utc).AddTicks(397) });

            migrationBuilder.UpdateData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 23, 6, 59, 49, 200, DateTimeKind.Utc).AddTicks(398), new DateTime(2026, 1, 23, 6, 59, 49, 200, DateTimeKind.Utc).AddTicks(399) });

            migrationBuilder.UpdateData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 23, 6, 59, 49, 200, DateTimeKind.Utc).AddTicks(401), new DateTime(2026, 1, 23, 6, 59, 49, 200, DateTimeKind.Utc).AddTicks(402) });

            migrationBuilder.UpdateData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 23, 6, 59, 49, 200, DateTimeKind.Utc).AddTicks(404), new DateTime(2026, 1, 23, 6, 59, 49, 200, DateTimeKind.Utc).AddTicks(404) });

            migrationBuilder.UpdateData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 9,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 23, 6, 59, 49, 200, DateTimeKind.Utc).AddTicks(406), new DateTime(2026, 1, 23, 6, 59, 49, 200, DateTimeKind.Utc).AddTicks(407) });

            migrationBuilder.UpdateData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 10,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 23, 6, 59, 49, 200, DateTimeKind.Utc).AddTicks(409), new DateTime(2026, 1, 23, 6, 59, 49, 200, DateTimeKind.Utc).AddTicks(409) });

            migrationBuilder.UpdateData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 11,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 23, 6, 59, 49, 200, DateTimeKind.Utc).AddTicks(411), new DateTime(2026, 1, 23, 6, 59, 49, 200, DateTimeKind.Utc).AddTicks(412) });

            migrationBuilder.UpdateData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 12,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 23, 6, 59, 49, 200, DateTimeKind.Utc).AddTicks(414), new DateTime(2026, 1, 23, 6, 59, 49, 200, DateTimeKind.Utc).AddTicks(414) });
        }
    }
}
