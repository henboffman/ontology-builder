using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Eidos.Migrations
{
    /// <inheritdoc />
    public partial class AddOntologyTags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OntologyTags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OntologyId = table.Column<int>(type: "INTEGER", nullable: false),
                    Tag = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Color = table.Column<string>(type: "TEXT", maxLength: 7, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OntologyTags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OntologyTags_Ontologies_OntologyId",
                        column: x => x.OntologyId,
                        principalTable: "Ontologies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OntologyTag_OntologyId_Tag",
                table: "OntologyTags",
                columns: new[] { "OntologyId", "Tag" });

            migrationBuilder.CreateIndex(
                name: "IX_OntologyTag_Tag",
                table: "OntologyTags",
                column: "Tag");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OntologyTags");
        }
    }
}
