using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Eidos.Migrations
{
    /// <inheritdoc />
    public partial class AddConceptAndRelationshipCounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ConceptCount",
                table: "Ontologies",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RelationshipCount",
                table: "Ontologies",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // Populate counts for existing ontologies
            migrationBuilder.Sql(@"
                UPDATE Ontologies
                SET ConceptCount = (
                    SELECT COUNT(*)
                    FROM Concepts
                    WHERE Concepts.OntologyId = Ontologies.Id
                )
            ");

            migrationBuilder.Sql(@"
                UPDATE Ontologies
                SET RelationshipCount = (
                    SELECT COUNT(*)
                    FROM Relationships
                    WHERE Relationships.OntologyId = Ontologies.Id
                )
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConceptCount",
                table: "Ontologies");

            migrationBuilder.DropColumn(
                name: "RelationshipCount",
                table: "Ontologies");
        }
    }
}
