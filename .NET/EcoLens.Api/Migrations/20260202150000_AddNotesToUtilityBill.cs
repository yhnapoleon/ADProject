using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcoLens.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddNotesToUtilityBill : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "UtilityBills",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Notes",
                table: "UtilityBills");
        }
    }
}
