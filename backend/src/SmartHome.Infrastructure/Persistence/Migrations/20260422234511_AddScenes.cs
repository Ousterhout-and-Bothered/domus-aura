using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartHome.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddScenes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Scenes",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT COLLATE NOCASE", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Scenes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SceneActions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT COLLATE NOCASE", nullable: false),
                    DeviceId = table.Column<string>(type: "TEXT COLLATE NOCASE", nullable: true),
                    DeviceType = table.Column<string>(type: "TEXT", nullable: true),
                    Location = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Operation = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Value = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    OrderIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    SceneId = table.Column<string>(type: "TEXT COLLATE NOCASE", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SceneActions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SceneActions_Scenes_SceneId",
                        column: x => x.SceneId,
                        principalTable: "Scenes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SceneActions_SceneId_OrderIndex",
                table: "SceneActions",
                columns: new[] { "SceneId", "OrderIndex" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SceneActions");

            migrationBuilder.DropTable(
                name: "Scenes");
        }
    }
}
