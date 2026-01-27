using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace EcoLens.Api.Migrations
{
    /// <inheritdoc />
    public partial class AlignUtilityBillAndCascadeFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Comments_Posts_PostId",
                table: "Comments");

            migrationBuilder.DeleteData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 12);

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

            migrationBuilder.AlterColumn<decimal>(
                name: "OcrConfidence",
                table: "UtilityBills",
                type: "decimal(18,4)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,4)",
                oldNullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ElectricityCost",
                table: "UtilityBills",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "GasCost",
                table: "UtilityBills",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "WaterCost",
                table: "UtilityBills",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "YearMonth",
                table: "UtilityBills",
                type: "nvarchar(7)",
                maxLength: 7,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "BirthDate",
                table: "ApplicationUsers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SystemSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConfidenceThreshold = table.Column<int>(type: "int", nullable: false),
                    VisionModel = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    WeeklyDigest = table.Column<bool>(type: "bit", nullable: false),
                    MaintenanceMode = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemSettings", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 27, 2, 17, 6, 784, DateTimeKind.Utc).AddTicks(8767), new DateTime(2026, 1, 27, 2, 17, 6, 784, DateTimeKind.Utc).AddTicks(8771) });

            migrationBuilder.UpdateData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 27, 2, 17, 6, 784, DateTimeKind.Utc).AddTicks(8779), new DateTime(2026, 1, 27, 2, 17, 6, 784, DateTimeKind.Utc).AddTicks(8779) });

            migrationBuilder.UpdateData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 27, 2, 17, 6, 784, DateTimeKind.Utc).AddTicks(8781), new DateTime(2026, 1, 27, 2, 17, 6, 784, DateTimeKind.Utc).AddTicks(8781) });

            migrationBuilder.UpdateData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "Category", "Co2Factor", "CreatedAt", "LabelName", "Unit", "UpdatedAt" },
                values: new object[] { 2, 0.35m, new DateTime(2026, 1, 27, 2, 17, 6, 784, DateTimeKind.Utc).AddTicks(8783), "Water", "kgCO2/m3", new DateTime(2026, 1, 27, 2, 17, 6, 784, DateTimeKind.Utc).AddTicks(8783) });

            migrationBuilder.UpdateData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "Category", "Co2Factor", "CreatedAt", "LabelName", "Unit", "UpdatedAt" },
                values: new object[] { 2, 2.3m, new DateTime(2026, 1, 27, 2, 17, 6, 784, DateTimeKind.Utc).AddTicks(8785), "Gas", "kgCO2/unit", new DateTime(2026, 1, 27, 2, 17, 6, 784, DateTimeKind.Utc).AddTicks(8785) });

            migrationBuilder.InsertData(
                table: "SystemSettings",
                columns: new[] { "Id", "ConfidenceThreshold", "MaintenanceMode", "VisionModel", "WeeklyDigest" },
                values: new object[] { 1, 80, false, "default", true });

            migrationBuilder.CreateIndex(
                name: "IX_CarbonReferences_LabelName_Category_Region",
                table: "CarbonReferences",
                columns: new[] { "LabelName", "Category", "Region" },
                unique: true,
                filter: "[Region] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Comments_Posts_PostId",
                table: "Comments",
                column: "PostId",
                principalTable: "Posts",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Comments_Posts_PostId",
                table: "Comments");

            migrationBuilder.DropTable(
                name: "SystemSettings");

            migrationBuilder.DropIndex(
                name: "IX_CarbonReferences_LabelName_Category_Region",
                table: "CarbonReferences");

            migrationBuilder.DropColumn(
                name: "ElectricityCost",
                table: "UtilityBills");

            migrationBuilder.DropColumn(
                name: "GasCost",
                table: "UtilityBills");

            migrationBuilder.DropColumn(
                name: "WaterCost",
                table: "UtilityBills");

            migrationBuilder.DropColumn(
                name: "YearMonth",
                table: "UtilityBills");

            migrationBuilder.DropColumn(
                name: "BirthDate",
                table: "ApplicationUsers");

            migrationBuilder.AlterColumn<decimal>(
                name: "OcrConfidence",
                table: "UtilityBills",
                type: "decimal(5,4)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,4)",
                oldNullable: true);

            migrationBuilder.UpdateData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 26, 5, 9, 20, 314, DateTimeKind.Utc).AddTicks(1122), new DateTime(2026, 1, 26, 5, 9, 20, 314, DateTimeKind.Utc).AddTicks(1130) });

            migrationBuilder.UpdateData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 26, 5, 9, 20, 314, DateTimeKind.Utc).AddTicks(1141), new DateTime(2026, 1, 26, 5, 9, 20, 314, DateTimeKind.Utc).AddTicks(1142) });

            migrationBuilder.UpdateData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 26, 5, 9, 20, 314, DateTimeKind.Utc).AddTicks(1144), new DateTime(2026, 1, 26, 5, 9, 20, 314, DateTimeKind.Utc).AddTicks(1144) });

            migrationBuilder.UpdateData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "Category", "Co2Factor", "CreatedAt", "LabelName", "Unit", "UpdatedAt" },
                values: new object[] { 1, 0m, new DateTime(2026, 1, 26, 5, 9, 20, 314, DateTimeKind.Utc).AddTicks(1146), "Walking", "kgCO2/km", new DateTime(2026, 1, 26, 5, 9, 20, 314, DateTimeKind.Utc).AddTicks(1146) });

            migrationBuilder.UpdateData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "Category", "Co2Factor", "CreatedAt", "LabelName", "Unit", "UpdatedAt" },
                values: new object[] { 1, 0m, new DateTime(2026, 1, 26, 5, 9, 20, 314, DateTimeKind.Utc).AddTicks(1148), "Bicycle", "kgCO2/km", new DateTime(2026, 1, 26, 5, 9, 20, 314, DateTimeKind.Utc).AddTicks(1149) });

            migrationBuilder.InsertData(
                table: "CarbonReferences",
                columns: new[] { "Id", "Category", "ClimatiqActivityId", "Co2Factor", "CreatedAt", "LabelName", "Region", "Source", "Unit", "UpdatedAt" },
                values: new object[,]
                {
                    { 6, 1, null, 0.02m, new DateTime(2026, 1, 26, 5, 9, 20, 314, DateTimeKind.Utc).AddTicks(1150), "ElectricBike", null, "Local", "kgCO2/km", new DateTime(2026, 1, 26, 5, 9, 20, 314, DateTimeKind.Utc).AddTicks(1151) },
                    { 7, 1, null, 0.05m, new DateTime(2026, 1, 26, 5, 9, 20, 314, DateTimeKind.Utc).AddTicks(1152), "Bus", null, "Local", "kgCO2/km", new DateTime(2026, 1, 26, 5, 9, 20, 314, DateTimeKind.Utc).AddTicks(1152) },
                    { 8, 1, null, 0.2m, new DateTime(2026, 1, 26, 5, 9, 20, 314, DateTimeKind.Utc).AddTicks(1154), "Taxi", null, "Local", "kgCO2/km", new DateTime(2026, 1, 26, 5, 9, 20, 314, DateTimeKind.Utc).AddTicks(1154) },
                    { 9, 1, null, 0.2m, new DateTime(2026, 1, 26, 5, 9, 20, 314, DateTimeKind.Utc).AddTicks(1156), "CarGasoline", null, "Local", "kgCO2/km", new DateTime(2026, 1, 26, 5, 9, 20, 314, DateTimeKind.Utc).AddTicks(1156) },
                    { 10, 1, null, 0.05m, new DateTime(2026, 1, 26, 5, 9, 20, 314, DateTimeKind.Utc).AddTicks(1158), "CarElectric", null, "Local", "kgCO2/km", new DateTime(2026, 1, 26, 5, 9, 20, 314, DateTimeKind.Utc).AddTicks(1158) },
                    { 11, 1, null, 0.04m, new DateTime(2026, 1, 26, 5, 9, 20, 314, DateTimeKind.Utc).AddTicks(1160), "Train", null, "Local", "kgCO2/km", new DateTime(2026, 1, 26, 5, 9, 20, 314, DateTimeKind.Utc).AddTicks(1160) },
                    { 12, 1, null, 0.25m, new DateTime(2026, 1, 26, 5, 9, 20, 314, DateTimeKind.Utc).AddTicks(1162), "Plane", null, "Local", "kgCO2/km", new DateTime(2026, 1, 26, 5, 9, 20, 314, DateTimeKind.Utc).AddTicks(1162) },
                    { 13, 2, null, 0.4057m, new DateTime(2026, 1, 26, 5, 9, 20, 314, DateTimeKind.Utc).AddTicks(1163), "Electricity_SG", null, "Local", "kgCO2/kWh", new DateTime(2026, 1, 26, 5, 9, 20, 314, DateTimeKind.Utc).AddTicks(1164) },
                    { 14, 2, null, 0.419m, new DateTime(2026, 1, 26, 5, 9, 20, 314, DateTimeKind.Utc).AddTicks(1165), "Water_SG", null, "Local", "kgCO2/m³", new DateTime(2026, 1, 26, 5, 9, 20, 314, DateTimeKind.Utc).AddTicks(1166) },
                    { 15, 2, null, 0.184m, new DateTime(2026, 1, 26, 5, 9, 20, 314, DateTimeKind.Utc).AddTicks(1167), "Gas_SG", null, "Local", "kgCO2/kWh", new DateTime(2026, 1, 26, 5, 9, 20, 314, DateTimeKind.Utc).AddTicks(1167) }
                });

            migrationBuilder.AddForeignKey(
                name: "FK_Comments_Posts_PostId",
                table: "Comments",
                column: "PostId",
                principalTable: "Posts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
