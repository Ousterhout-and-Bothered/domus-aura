using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartHome.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Devices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Location = table.Column<string>(type: "TEXT", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    DeviceType = table.Column<string>(type: "TEXT", maxLength: 13, nullable: false),
                    LockState = table.Column<int>(type: "INTEGER", nullable: true),
                    Speed = table.Column<int>(type: "INTEGER", nullable: true),
                    PowerState = table.Column<int>(type: "INTEGER", nullable: true),
                    Brightness = table.Column<int>(type: "INTEGER", nullable: true),
                    ColorHex = table.Column<string>(type: "TEXT", nullable: true),
                    State = table.Column<int>(type: "INTEGER", nullable: true),
                    Mode = table.Column<int>(type: "INTEGER", nullable: true),
                    DesiredTemperature = table.Column<int>(type: "INTEGER", nullable: true),
                    AmbientTemperature = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Devices", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Devices");
        }
    }
}
