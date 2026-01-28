using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcoLens.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTreeStateFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CurrentTreeProgress",
                table: "ApplicationUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TreesTotalCount",
                table: "ApplicationUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 28, 7, 30, 7, 89, DateTimeKind.Utc).AddTicks(6397), new DateTime(2026, 1, 28, 7, 30, 7, 89, DateTimeKind.Utc).AddTicks(6402) });

            migrationBuilder.UpdateData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 28, 7, 30, 7, 89, DateTimeKind.Utc).AddTicks(6412), new DateTime(2026, 1, 28, 7, 30, 7, 89, DateTimeKind.Utc).AddTicks(6412) });

            migrationBuilder.UpdateData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 28, 7, 30, 7, 89, DateTimeKind.Utc).AddTicks(6415), new DateTime(2026, 1, 28, 7, 30, 7, 89, DateTimeKind.Utc).AddTicks(6415) });

            migrationBuilder.UpdateData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 28, 7, 30, 7, 89, DateTimeKind.Utc).AddTicks(6417), new DateTime(2026, 1, 28, 7, 30, 7, 89, DateTimeKind.Utc).AddTicks(6418) });

            migrationBuilder.UpdateData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 28, 7, 30, 7, 89, DateTimeKind.Utc).AddTicks(6420), new DateTime(2026, 1, 28, 7, 30, 7, 89, DateTimeKind.Utc).AddTicks(6421) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentTreeProgress",
                table: "ApplicationUsers");

            migrationBuilder.DropColumn(
                name: "TreesTotalCount",
                table: "ApplicationUsers");

            migrationBuilder.UpdateData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 28, 4, 8, 35, 161, DateTimeKind.Utc).AddTicks(1382), new DateTime(2026, 1, 28, 4, 8, 35, 161, DateTimeKind.Utc).AddTicks(1388) });

            migrationBuilder.UpdateData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 28, 4, 8, 35, 161, DateTimeKind.Utc).AddTicks(1399), new DateTime(2026, 1, 28, 4, 8, 35, 161, DateTimeKind.Utc).AddTicks(1400) });

            migrationBuilder.UpdateData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 28, 4, 8, 35, 161, DateTimeKind.Utc).AddTicks(1402), new DateTime(2026, 1, 28, 4, 8, 35, 161, DateTimeKind.Utc).AddTicks(1402) });

            migrationBuilder.UpdateData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 28, 4, 8, 35, 161, DateTimeKind.Utc).AddTicks(1404), new DateTime(2026, 1, 28, 4, 8, 35, 161, DateTimeKind.Utc).AddTicks(1405) });

            migrationBuilder.UpdateData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 28, 4, 8, 35, 161, DateTimeKind.Utc).AddTicks(1407), new DateTime(2026, 1, 28, 4, 8, 35, 161, DateTimeKind.Utc).AddTicks(1407) });
        }
    }
}
