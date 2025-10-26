using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Eidos.Migrations
{
    /// <inheritdoc />
    public partial class AddOntologyProvenance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ParentOntologyId",
                table: "Ontologies",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProvenanceNotes",
                table: "Ontologies",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProvenanceType",
                table: "Ontologies",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Ontologies_ParentOntologyId",
                table: "Ontologies",
                column: "ParentOntologyId");

            migrationBuilder.AddForeignKey(
                name: "FK_Ontologies_Ontologies_ParentOntologyId",
                table: "Ontologies",
                column: "ParentOntologyId",
                principalTable: "Ontologies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Ontologies_Ontologies_ParentOntologyId",
                table: "Ontologies");

            migrationBuilder.DropIndex(
                name: "IX_Ontologies_ParentOntologyId",
                table: "Ontologies");

            migrationBuilder.DropColumn(
                name: "ParentOntologyId",
                table: "Ontologies");

            migrationBuilder.DropColumn(
                name: "ProvenanceNotes",
                table: "Ontologies");

            migrationBuilder.DropColumn(
                name: "ProvenanceType",
                table: "Ontologies");
        }
    }
}
