using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcoLens.Api.Migrations
{
    /// <inheritdoc />
    public partial class FixUserFollowsCascade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserFollows_ApplicationUsers_FolloweeId",
                table: "UserFollows");

            migrationBuilder.DropForeignKey(
                name: "FK_UserFollows_ApplicationUsers_FollowerId",
                table: "UserFollows");

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
                name: "FK_UserFollows_ApplicationUsers_FolloweeId",
                table: "UserFollows",
                column: "FolloweeId",
                principalTable: "ApplicationUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserFollows_ApplicationUsers_FollowerId",
                table: "UserFollows",
                column: "FollowerId",
                principalTable: "ApplicationUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserFollows_ApplicationUsers_FolloweeId",
                table: "UserFollows");

            migrationBuilder.DropForeignKey(
                name: "FK_UserFollows_ApplicationUsers_FollowerId",
                table: "UserFollows");

            migrationBuilder.UpdateData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 21, 11, 43, 2, 921, DateTimeKind.Utc).AddTicks(155), new DateTime(2026, 1, 21, 11, 43, 2, 921, DateTimeKind.Utc).AddTicks(161) });

            migrationBuilder.UpdateData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 21, 11, 43, 2, 921, DateTimeKind.Utc).AddTicks(169), new DateTime(2026, 1, 21, 11, 43, 2, 921, DateTimeKind.Utc).AddTicks(170) });

            migrationBuilder.UpdateData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 21, 11, 43, 2, 921, DateTimeKind.Utc).AddTicks(172), new DateTime(2026, 1, 21, 11, 43, 2, 921, DateTimeKind.Utc).AddTicks(172) });

            migrationBuilder.UpdateData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 21, 11, 43, 2, 921, DateTimeKind.Utc).AddTicks(174), new DateTime(2026, 1, 21, 11, 43, 2, 921, DateTimeKind.Utc).AddTicks(174) });

            migrationBuilder.UpdateData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 21, 11, 43, 2, 921, DateTimeKind.Utc).AddTicks(176), new DateTime(2026, 1, 21, 11, 43, 2, 921, DateTimeKind.Utc).AddTicks(177) });

            migrationBuilder.UpdateData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 21, 11, 43, 2, 921, DateTimeKind.Utc).AddTicks(178), new DateTime(2026, 1, 21, 11, 43, 2, 921, DateTimeKind.Utc).AddTicks(178) });

            migrationBuilder.UpdateData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 21, 11, 43, 2, 921, DateTimeKind.Utc).AddTicks(180), new DateTime(2026, 1, 21, 11, 43, 2, 921, DateTimeKind.Utc).AddTicks(180) });

            migrationBuilder.UpdateData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 21, 11, 43, 2, 921, DateTimeKind.Utc).AddTicks(181), new DateTime(2026, 1, 21, 11, 43, 2, 921, DateTimeKind.Utc).AddTicks(182) });

            migrationBuilder.UpdateData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 9,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 21, 11, 43, 2, 921, DateTimeKind.Utc).AddTicks(183), new DateTime(2026, 1, 21, 11, 43, 2, 921, DateTimeKind.Utc).AddTicks(184) });

            migrationBuilder.UpdateData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 10,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 21, 11, 43, 2, 921, DateTimeKind.Utc).AddTicks(185), new DateTime(2026, 1, 21, 11, 43, 2, 921, DateTimeKind.Utc).AddTicks(185) });

            migrationBuilder.UpdateData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 11,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 21, 11, 43, 2, 921, DateTimeKind.Utc).AddTicks(187), new DateTime(2026, 1, 21, 11, 43, 2, 921, DateTimeKind.Utc).AddTicks(187) });

            migrationBuilder.UpdateData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 12,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 21, 11, 43, 2, 921, DateTimeKind.Utc).AddTicks(188), new DateTime(2026, 1, 21, 11, 43, 2, 921, DateTimeKind.Utc).AddTicks(189) });

            migrationBuilder.AddForeignKey(
                name: "FK_UserFollows_ApplicationUsers_FolloweeId",
                table: "UserFollows",
                column: "FolloweeId",
                principalTable: "ApplicationUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserFollows_ApplicationUsers_FollowerId",
                table: "UserFollows",
                column: "FollowerId",
                principalTable: "ApplicationUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
