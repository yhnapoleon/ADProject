using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcoLens.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddUserNickname : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Nickname",
                table: "ApplicationUsers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            // 兼容历史数据：默认将 Nickname 回填为 Username
            migrationBuilder.Sql(@"
UPDATE [ApplicationUsers]
SET [Nickname] = [Username]
WHERE [Nickname] IS NULL OR LTRIM(RTRIM([Nickname])) = '';
");

            migrationBuilder.UpdateData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 28, 8, 35, 14, 821, DateTimeKind.Utc).AddTicks(9673), new DateTime(2026, 1, 28, 8, 35, 14, 821, DateTimeKind.Utc).AddTicks(9680) });

            migrationBuilder.UpdateData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 28, 8, 35, 14, 821, DateTimeKind.Utc).AddTicks(9690), new DateTime(2026, 1, 28, 8, 35, 14, 821, DateTimeKind.Utc).AddTicks(9691) });

            migrationBuilder.UpdateData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 28, 8, 35, 14, 821, DateTimeKind.Utc).AddTicks(9693), new DateTime(2026, 1, 28, 8, 35, 14, 821, DateTimeKind.Utc).AddTicks(9693) });

            migrationBuilder.UpdateData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 28, 8, 35, 14, 821, DateTimeKind.Utc).AddTicks(9695), new DateTime(2026, 1, 28, 8, 35, 14, 821, DateTimeKind.Utc).AddTicks(9695) });

            migrationBuilder.UpdateData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 28, 8, 35, 14, 821, DateTimeKind.Utc).AddTicks(9696), new DateTime(2026, 1, 28, 8, 35, 14, 821, DateTimeKind.Utc).AddTicks(9697) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Nickname",
                table: "ApplicationUsers");

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
    }
}
