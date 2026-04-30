using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartHome.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueThermostatPerLocationIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Devices_Location",
                table: "Devices",
                column: "Location",
                unique: true,
                filter: "\"Type\" = 3");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Devices_Location",
                table: "Devices");
        }
    }
}
