using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcoLens.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTotalCarbonEmission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "TotalCarbonEmission",
                table: "ApplicationUsers",
                type: "decimal(18,4)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TotalCarbonEmission",
                table: "ApplicationUsers");
        }
    }
}
