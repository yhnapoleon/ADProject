using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace EcoLens.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Comments_ApplicationUsers_UserId",
                table: "Comments");

            migrationBuilder.DropForeignKey(
                name: "FK_Comments_Posts_PostId",
                table: "Comments");

            migrationBuilder.DropForeignKey(
                name: "FK_UserFollows_ApplicationUsers_FolloweeId",
                table: "UserFollows");

            migrationBuilder.DropForeignKey(
                name: "FK_UserFollows_ApplicationUsers_FollowerId",
                table: "UserFollows");

            migrationBuilder.DropIndex(
                name: "IX_BarcodeReferences_Barcode",
                table: "BarcodeReferences");

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Posts",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "BirthDate",
                table: "ApplicationUsers",
                type: "datetime2",
                nullable: true);

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

            migrationBuilder.CreateTable(
                name: "UtilityBills",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    YearMonth = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: false),
                    ElectricityUsage = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    ElectricityCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    WaterUsage = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    WaterCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GasUsage = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    GasCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
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
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 26, 9, 26, 23, 701, DateTimeKind.Utc).AddTicks(170), new DateTime(2026, 1, 26, 9, 26, 23, 701, DateTimeKind.Utc).AddTicks(172) });

            migrationBuilder.UpdateData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 26, 9, 26, 23, 701, DateTimeKind.Utc).AddTicks(178), new DateTime(2026, 1, 26, 9, 26, 23, 701, DateTimeKind.Utc).AddTicks(179) });

            migrationBuilder.UpdateData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 26, 9, 26, 23, 701, DateTimeKind.Utc).AddTicks(180), new DateTime(2026, 1, 26, 9, 26, 23, 701, DateTimeKind.Utc).AddTicks(180) });

            migrationBuilder.InsertData(
                table: "CarbonReferences",
                columns: new[] { "Id", "Category", "ClimatiqActivityId", "Co2Factor", "CreatedAt", "LabelName", "Region", "Source", "Unit", "UpdatedAt" },
                values: new object[,]
                {
                    { 4, 2, null, 0.35m, new DateTime(2026, 1, 26, 9, 26, 23, 701, DateTimeKind.Utc).AddTicks(181), "Water", null, "Local", "kgCO2/m3", new DateTime(2026, 1, 26, 9, 26, 23, 701, DateTimeKind.Utc).AddTicks(182) },
                    { 5, 2, null, 2.3m, new DateTime(2026, 1, 26, 9, 26, 23, 701, DateTimeKind.Utc).AddTicks(183), "Gas", null, "Local", "kgCO2/unit", new DateTime(2026, 1, 26, 9, 26, 23, 701, DateTimeKind.Utc).AddTicks(183) }
                });

            migrationBuilder.InsertData(
                table: "SystemSettings",
                columns: new[] { "Id", "ConfidenceThreshold", "MaintenanceMode", "VisionModel", "WeeklyDigest" },
                values: new object[] { 1, 80, false, "default", true });

            migrationBuilder.CreateIndex(
                name: "IX_DietTemplateItems_DietTemplateId",
                table: "DietTemplateItems",
                column: "DietTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_UtilityBills_UserId",
                table: "UtilityBills",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Comments_ApplicationUsers_UserId",
                table: "Comments",
                column: "UserId",
                principalTable: "ApplicationUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Comments_Posts_PostId",
                table: "Comments",
                column: "PostId",
                principalTable: "Posts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserFollows_ApplicationUsers_FolloweeId",
                table: "UserFollows",
                column: "FolloweeId",
                principalTable: "ApplicationUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserFollows_ApplicationUsers_FollowerId",
                table: "UserFollows",
                column: "FollowerId",
                principalTable: "ApplicationUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Comments_ApplicationUsers_UserId",
                table: "Comments");

            migrationBuilder.DropForeignKey(
                name: "FK_Comments_Posts_PostId",
                table: "Comments");

            migrationBuilder.DropForeignKey(
                name: "FK_UserFollows_ApplicationUsers_FolloweeId",
                table: "UserFollows");

            migrationBuilder.DropForeignKey(
                name: "FK_UserFollows_ApplicationUsers_FollowerId",
                table: "UserFollows");

            migrationBuilder.DropTable(
                name: "DietTemplateItems");

            migrationBuilder.DropTable(
                name: "SystemSettings");

            migrationBuilder.DropTable(
                name: "UtilityBills");

            migrationBuilder.DropTable(
                name: "DietTemplates");

            migrationBuilder.DeleteData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "CarbonReferences",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "BirthDate",
                table: "ApplicationUsers");

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

            migrationBuilder.CreateIndex(
                name: "IX_BarcodeReferences_Barcode",
                table: "BarcodeReferences",
                column: "Barcode",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Comments_ApplicationUsers_UserId",
                table: "Comments",
                column: "UserId",
                principalTable: "ApplicationUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Comments_Posts_PostId",
                table: "Comments",
                column: "PostId",
                principalTable: "Posts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserFollows_ApplicationUsers_FolloweeId",
                table: "UserFollows",
                column: "FolloweeId",
                principalTable: "ApplicationUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserFollows_ApplicationUsers_FollowerId",
                table: "UserFollows",
                column: "FollowerId",
                principalTable: "ApplicationUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
