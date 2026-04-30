using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartHome.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixGuidCaseSensitivity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "Devices",
                type: "TEXT COLLATE NOCASE",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "TEXT");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "Devices",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT COLLATE NOCASE");
        }
    }
}
