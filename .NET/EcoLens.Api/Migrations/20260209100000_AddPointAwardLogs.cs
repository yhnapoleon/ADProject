using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcoLens.Api.Migrations
{
	/// <inheritdoc />
	public partial class AddPointAwardLogs : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.CreateTable(
				name: "PointAwardLogs",
				columns: table => new
				{
					Id = table.Column<int>(type: "int", nullable: false)
						.Annotation("SqlServer:Identity", "1, 1"),
					UserId = table.Column<int>(type: "int", nullable: false),
					Points = table.Column<int>(type: "int", nullable: false),
					AwardedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
					Source = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
					CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_PointAwardLogs", x => x.Id);
					table.ForeignKey(
						name: "FK_PointAwardLogs_ApplicationUsers_UserId",
						column: x => x.UserId,
						principalTable: "ApplicationUsers",
						principalColumn: "Id",
						onDelete: ReferentialAction.Cascade);
				});

			migrationBuilder.CreateIndex(
				name: "IX_PointAwardLogs_UserId_AwardedAt",
				table: "PointAwardLogs",
				columns: new[] { "UserId", "AwardedAt" });
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropTable(name: "PointAwardLogs");
		}
	}
}
