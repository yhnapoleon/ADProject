using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcoLens.Api.Migrations
{
    /// <inheritdoc />
    public partial class CleanUpUnknownFoodReferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 22, 7, 22, 37, 387, DateTimeKind.Utc).AddTicks(6049), new DateTime(2026, 1, 22, 7, 22, 37, 387, DateTimeKind.Utc).AddTicks(6060) });

            migrationBuilder.UpdateData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 22, 7, 22, 37, 387, DateTimeKind.Utc).AddTicks(6072), new DateTime(2026, 1, 22, 7, 22, 37, 387, DateTimeKind.Utc).AddTicks(6073) });

            migrationBuilder.UpdateData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 22, 7, 22, 37, 387, DateTimeKind.Utc).AddTicks(6074), new DateTime(2026, 1, 22, 7, 22, 37, 387, DateTimeKind.Utc).AddTicks(6075) });
            // 1. 删除所有引用 CarbonReferenceId = 4 的 BarcodeReferences 记录
        migrationBuilder.DeleteData(
            table: "BarcodeReferences",
            keyColumn: "CarbonReferenceId", // 指定外键列名
            keyValue: 4 // 指定要删除的CarbonReferenceId
        );

        // 2. 删除 CarbonReference ID 为 4 的 "Unknown Food" 记录
        migrationBuilder.DeleteData(
            table: "CarbonReferences",
            keyColumn: "Id", // 指定主键列名
            keyValue: 4 // 指定要删除的Id
        );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 22, 6, 3, 10, 301, DateTimeKind.Utc).AddTicks(3135), new DateTime(2026, 1, 22, 6, 3, 10, 301, DateTimeKind.Utc).AddTicks(3138) });

            migrationBuilder.UpdateData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 22, 6, 3, 10, 301, DateTimeKind.Utc).AddTicks(3145), new DateTime(2026, 1, 22, 6, 3, 10, 301, DateTimeKind.Utc).AddTicks(3146) });

            migrationBuilder.UpdateData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 22, 6, 3, 10, 301, DateTimeKind.Utc).AddTicks(3148), new DateTime(2026, 1, 22, 6, 3, 10, 301, DateTimeKind.Utc).AddTicks(3148) });
        }
    }
}
