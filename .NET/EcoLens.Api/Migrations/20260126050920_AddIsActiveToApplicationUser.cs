using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcoLens.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddIsActiveToApplicationUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BarcodeReferences_CarbonReferences_CarbonReferenceId",
                table: "BarcodeReferences");

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
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 26, 5, 9, 20, 314, DateTimeKind.Utc).AddTicks(1146), new DateTime(2026, 1, 26, 5, 9, 20, 314, DateTimeKind.Utc).AddTicks(1146) });

            migrationBuilder.UpdateData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 26, 5, 9, 20, 314, DateTimeKind.Utc).AddTicks(1148), new DateTime(2026, 1, 26, 5, 9, 20, 314, DateTimeKind.Utc).AddTicks(1149) });

            migrationBuilder.UpdateData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 26, 5, 9, 20, 314, DateTimeKind.Utc).AddTicks(1150), new DateTime(2026, 1, 26, 5, 9, 20, 314, DateTimeKind.Utc).AddTicks(1151) });

            migrationBuilder.UpdateData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 26, 5, 9, 20, 314, DateTimeKind.Utc).AddTicks(1152), new DateTime(2026, 1, 26, 5, 9, 20, 314, DateTimeKind.Utc).AddTicks(1152) });

            migrationBuilder.UpdateData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 26, 5, 9, 20, 314, DateTimeKind.Utc).AddTicks(1154), new DateTime(2026, 1, 26, 5, 9, 20, 314, DateTimeKind.Utc).AddTicks(1154) });

            migrationBuilder.UpdateData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 9,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 26, 5, 9, 20, 314, DateTimeKind.Utc).AddTicks(1156), new DateTime(2026, 1, 26, 5, 9, 20, 314, DateTimeKind.Utc).AddTicks(1156) });

            migrationBuilder.UpdateData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 10,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 26, 5, 9, 20, 314, DateTimeKind.Utc).AddTicks(1158), new DateTime(2026, 1, 26, 5, 9, 20, 314, DateTimeKind.Utc).AddTicks(1158) });

            migrationBuilder.UpdateData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 11,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 26, 5, 9, 20, 314, DateTimeKind.Utc).AddTicks(1160), new DateTime(2026, 1, 26, 5, 9, 20, 314, DateTimeKind.Utc).AddTicks(1160) });

            migrationBuilder.UpdateData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 12,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 26, 5, 9, 20, 314, DateTimeKind.Utc).AddTicks(1162), new DateTime(2026, 1, 26, 5, 9, 20, 314, DateTimeKind.Utc).AddTicks(1162) });

            migrationBuilder.UpdateData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 13,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 26, 5, 9, 20, 314, DateTimeKind.Utc).AddTicks(1163), new DateTime(2026, 1, 26, 5, 9, 20, 314, DateTimeKind.Utc).AddTicks(1164) });

            migrationBuilder.UpdateData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 14,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 26, 5, 9, 20, 314, DateTimeKind.Utc).AddTicks(1165), new DateTime(2026, 1, 26, 5, 9, 20, 314, DateTimeKind.Utc).AddTicks(1166) });

            migrationBuilder.UpdateData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 15,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 26, 5, 9, 20, 314, DateTimeKind.Utc).AddTicks(1167), new DateTime(2026, 1, 26, 5, 9, 20, 314, DateTimeKind.Utc).AddTicks(1167) });

            migrationBuilder.AddForeignKey(
                name: "FK_BarcodeReferences_CarbonReferences_CarbonReferenceId",
                table: "BarcodeReferences",
                column: "CarbonReferenceId",
                principalTable: "CarbonReferences",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BarcodeReferences_CarbonReferences_CarbonReferenceId",
                table: "BarcodeReferences");

            migrationBuilder.UpdateData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 26, 5, 1, 31, 981, DateTimeKind.Utc).AddTicks(9258), new DateTime(2026, 1, 26, 5, 1, 31, 981, DateTimeKind.Utc).AddTicks(9261) });

            migrationBuilder.UpdateData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 26, 5, 1, 31, 981, DateTimeKind.Utc).AddTicks(9269), new DateTime(2026, 1, 26, 5, 1, 31, 981, DateTimeKind.Utc).AddTicks(9269) });

            migrationBuilder.UpdateData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 26, 5, 1, 31, 981, DateTimeKind.Utc).AddTicks(9271), new DateTime(2026, 1, 26, 5, 1, 31, 981, DateTimeKind.Utc).AddTicks(9272) });

            migrationBuilder.UpdateData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 26, 5, 1, 31, 981, DateTimeKind.Utc).AddTicks(9273), new DateTime(2026, 1, 26, 5, 1, 31, 981, DateTimeKind.Utc).AddTicks(9274) });

            migrationBuilder.UpdateData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 26, 5, 1, 31, 981, DateTimeKind.Utc).AddTicks(9276), new DateTime(2026, 1, 26, 5, 1, 31, 981, DateTimeKind.Utc).AddTicks(9276) });

            migrationBuilder.UpdateData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 26, 5, 1, 31, 981, DateTimeKind.Utc).AddTicks(9277), new DateTime(2026, 1, 26, 5, 1, 31, 981, DateTimeKind.Utc).AddTicks(9278) });

            migrationBuilder.UpdateData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 26, 5, 1, 31, 981, DateTimeKind.Utc).AddTicks(9279), new DateTime(2026, 1, 26, 5, 1, 31, 981, DateTimeKind.Utc).AddTicks(9279) });

            migrationBuilder.UpdateData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 26, 5, 1, 31, 981, DateTimeKind.Utc).AddTicks(9281), new DateTime(2026, 1, 26, 5, 1, 31, 981, DateTimeKind.Utc).AddTicks(9281) });

            migrationBuilder.UpdateData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 9,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 26, 5, 1, 31, 981, DateTimeKind.Utc).AddTicks(9283), new DateTime(2026, 1, 26, 5, 1, 31, 981, DateTimeKind.Utc).AddTicks(9283) });

            migrationBuilder.UpdateData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 10,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 26, 5, 1, 31, 981, DateTimeKind.Utc).AddTicks(9284), new DateTime(2026, 1, 26, 5, 1, 31, 981, DateTimeKind.Utc).AddTicks(9285) });

            migrationBuilder.UpdateData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 11,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 26, 5, 1, 31, 981, DateTimeKind.Utc).AddTicks(9286), new DateTime(2026, 1, 26, 5, 1, 31, 981, DateTimeKind.Utc).AddTicks(9287) });

            migrationBuilder.UpdateData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 12,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 26, 5, 1, 31, 981, DateTimeKind.Utc).AddTicks(9288), new DateTime(2026, 1, 26, 5, 1, 31, 981, DateTimeKind.Utc).AddTicks(9288) });

            migrationBuilder.UpdateData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 13,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 26, 5, 1, 31, 981, DateTimeKind.Utc).AddTicks(9290), new DateTime(2026, 1, 26, 5, 1, 31, 981, DateTimeKind.Utc).AddTicks(9290) });

            migrationBuilder.UpdateData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 14,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 26, 5, 1, 31, 981, DateTimeKind.Utc).AddTicks(9291), new DateTime(2026, 1, 26, 5, 1, 31, 981, DateTimeKind.Utc).AddTicks(9292) });

            migrationBuilder.UpdateData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 15,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 26, 5, 1, 31, 981, DateTimeKind.Utc).AddTicks(9293), new DateTime(2026, 1, 26, 5, 1, 31, 981, DateTimeKind.Utc).AddTicks(9293) });

            migrationBuilder.AddForeignKey(
                name: "FK_BarcodeReferences_CarbonReferences_CarbonReferenceId",
                table: "BarcodeReferences",
                column: "CarbonReferenceId",
                principalTable: "CarbonReferences",
                principalColumn: "Id");
        }
    }
}
