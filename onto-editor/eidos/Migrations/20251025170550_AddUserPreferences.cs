using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Eidos.Migrations
{
    /// <inheritdoc />
    public partial class AddUserPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserPreferences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    EntityColor = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ProcessColor = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    QualityColor = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RoleColor = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FunctionColor = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    InformationColor = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EventColor = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DefaultConceptColor = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsARelationshipColor = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PartOfRelationshipColor = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    HasPartRelationshipColor = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RelatedToRelationshipColor = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DefaultRelationshipColor = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DefaultNodeSize = table.Column<int>(type: "int", nullable: false),
                    DefaultEdgeThickness = table.Column<int>(type: "int", nullable: false),
                    ShowEdgeLabels = table.Column<bool>(type: "bit", nullable: false),
                    AutoColorByCategory = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPreferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserPreferences_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserPreferences_UserId",
                table: "UserPreferences",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserPreferences");
        }
    }
}
