using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartHome.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixDeviceInheritance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeviceType",
                table: "Devices");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DeviceType",
                table: "Devices",
                type: "TEXT",
                maxLength: 13,
                nullable: false,
                defaultValue: "");
        }
    }
}
