using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Eidos.Migrations
{
    /// <inheritdoc />
    public partial class AddConceptGrouping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConceptGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OntologyId = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    ParentConceptId = table.Column<int>(type: "INTEGER", nullable: false),
                    ChildConceptIds = table.Column<string>(type: "TEXT", nullable: false),
                    IsCollapsed = table.Column<bool>(type: "INTEGER", nullable: false),
                    GroupName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    CollapsedPositionX = table.Column<double>(type: "REAL", nullable: true),
                    CollapsedPositionY = table.Column<double>(type: "REAL", nullable: true),
                    MaxDepth = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConceptGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConceptGroups_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ConceptGroups_Concepts_ParentConceptId",
                        column: x => x.ParentConceptId,
                        principalTable: "Concepts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ConceptGroups_Ontologies_OntologyId",
                        column: x => x.OntologyId,
                        principalTable: "Ontologies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConceptGroups_OntologyId",
                table: "ConceptGroups",
                column: "OntologyId");

            migrationBuilder.CreateIndex(
                name: "IX_ConceptGroups_ParentConceptId",
                table: "ConceptGroups",
                column: "ParentConceptId");

            migrationBuilder.CreateIndex(
                name: "IX_ConceptGroups_UserId",
                table: "ConceptGroups",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConceptGroups");
        }
    }
}
