using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcoLens.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddUtilityBill : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Comments_ApplicationUsers_UserId",
                table: "Comments");

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

            migrationBuilder.CreateIndex(
                name: "IX_UtilityBills_UserId",
                table: "UtilityBills",
                column: "UserId");

            migrationBuilder.InsertData(
                table: "CarbonReferences",
                columns: new[] { "Id", "LabelName", "Category", "Co2Factor", "Unit", "CreatedAt", "UpdatedAt" },
                values: new object[,]
                {
                    { 13, "Electricity_SG", 2, 0.4057m, "kgCO2/kWh", new DateTime(2026, 1, 23, 6, 59, 49, 200, DateTimeKind.Utc).AddTicks(416), new DateTime(2026, 1, 23, 6, 59, 49, 200, DateTimeKind.Utc).AddTicks(416) },
                    { 14, "Water_SG", 2, 0.419m, "kgCO2/m³", new DateTime(2026, 1, 23, 6, 59, 49, 200, DateTimeKind.Utc).AddTicks(418), new DateTime(2026, 1, 23, 6, 59, 49, 200, DateTimeKind.Utc).AddTicks(418) },
                    { 15, "Gas_SG", 2, 0.184m, "kgCO2/kWh", new DateTime(2026, 1, 23, 6, 59, 49, 200, DateTimeKind.Utc).AddTicks(420), new DateTime(2026, 1, 23, 6, 59, 49, 200, DateTimeKind.Utc).AddTicks(420) }
                });

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

            migrationBuilder.AddForeignKey(
                name: "FK_Comments_ApplicationUsers_UserId",
                table: "Comments",
                column: "UserId",
                principalTable: "ApplicationUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Comments_ApplicationUsers_UserId",
                table: "Comments");

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

            migrationBuilder.DropTable(
                name: "UtilityBills");

            migrationBuilder.UpdateData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 21, 12, 11, 6, 379, DateTimeKind.Utc).AddTicks(9834), new DateTime(2026, 1, 21, 12, 11, 6, 379, DateTimeKind.Utc).AddTicks(9846) });

            migrationBuilder.UpdateData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 21, 12, 11, 6, 379, DateTimeKind.Utc).AddTicks(9857), new DateTime(2026, 1, 21, 12, 11, 6, 379, DateTimeKind.Utc).AddTicks(9857) });

            migrationBuilder.UpdateData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 21, 12, 11, 6, 379, DateTimeKind.Utc).AddTicks(9860), new DateTime(2026, 1, 21, 12, 11, 6, 379, DateTimeKind.Utc).AddTicks(9860) });

            migrationBuilder.UpdateData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 21, 12, 11, 6, 379, DateTimeKind.Utc).AddTicks(9862), new DateTime(2026, 1, 21, 12, 11, 6, 379, DateTimeKind.Utc).AddTicks(9862) });

            migrationBuilder.UpdateData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 21, 12, 11, 6, 379, DateTimeKind.Utc).AddTicks(9864), new DateTime(2026, 1, 21, 12, 11, 6, 379, DateTimeKind.Utc).AddTicks(9865) });

            migrationBuilder.UpdateData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 21, 12, 11, 6, 379, DateTimeKind.Utc).AddTicks(9866), new DateTime(2026, 1, 21, 12, 11, 6, 379, DateTimeKind.Utc).AddTicks(9866) });

            migrationBuilder.UpdateData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 21, 12, 11, 6, 379, DateTimeKind.Utc).AddTicks(9868), new DateTime(2026, 1, 21, 12, 11, 6, 379, DateTimeKind.Utc).AddTicks(9868) });

            migrationBuilder.UpdateData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 21, 12, 11, 6, 379, DateTimeKind.Utc).AddTicks(9870), new DateTime(2026, 1, 21, 12, 11, 6, 379, DateTimeKind.Utc).AddTicks(9870) });

            migrationBuilder.UpdateData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 9,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 21, 12, 11, 6, 379, DateTimeKind.Utc).AddTicks(9872), new DateTime(2026, 1, 21, 12, 11, 6, 379, DateTimeKind.Utc).AddTicks(9872) });

            migrationBuilder.UpdateData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 10,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 21, 12, 11, 6, 379, DateTimeKind.Utc).AddTicks(9874), new DateTime(2026, 1, 21, 12, 11, 6, 379, DateTimeKind.Utc).AddTicks(9874) });

            migrationBuilder.UpdateData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 11,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 21, 12, 11, 6, 379, DateTimeKind.Utc).AddTicks(9876), new DateTime(2026, 1, 21, 12, 11, 6, 379, DateTimeKind.Utc).AddTicks(9876) });

            migrationBuilder.UpdateData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 12,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 21, 12, 11, 6, 379, DateTimeKind.Utc).AddTicks(9878), new DateTime(2026, 1, 21, 12, 11, 6, 379, DateTimeKind.Utc).AddTicks(9878) });

            migrationBuilder.AddForeignKey(
                name: "FK_Comments_ApplicationUsers_UserId",
                table: "Comments",
                column: "UserId",
                principalTable: "ApplicationUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
