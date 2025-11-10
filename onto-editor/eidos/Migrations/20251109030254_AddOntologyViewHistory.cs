using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Eidos.Migrations
{
    /// <inheritdoc />
    public partial class AddOntologyViewHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OntologyViewHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OntologyId = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    LastViewedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastDismissedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ViewCount = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OntologyViewHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OntologyViewHistories_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OntologyViewHistories_Ontologies_OntologyId",
                        column: x => x.OntologyId,
                        principalTable: "Ontologies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OntologyViewHistory_OntologyId_UserId",
                table: "OntologyViewHistories",
                columns: new[] { "OntologyId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OntologyViewHistory_UserId",
                table: "OntologyViewHistories",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OntologyViewHistories");
        }
    }
}
