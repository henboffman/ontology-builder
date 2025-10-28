using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Eidos.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "IX_UserShareAccesses_OntologyShareId",
                table: "UserShareAccesses",
                newName: "IX_UserShareAccess_OntologyShareId");

            migrationBuilder.RenameIndex(
                name: "IX_Relationships_OntologyId",
                table: "Relationships",
                newName: "IX_Relationship_OntologyId");

            migrationBuilder.RenameIndex(
                name: "IX_OntologyShares_OntologyId",
                table: "OntologyShares",
                newName: "IX_OntologyShare_OntologyId");

            migrationBuilder.RenameIndex(
                name: "IX_Individuals_OntologyId",
                table: "Individuals",
                newName: "IX_Individual_OntologyId");

            migrationBuilder.RenameIndex(
                name: "IX_GuestSessions_OntologyShareId",
                table: "GuestSessions",
                newName: "IX_GuestSession_OntologyShareId");

            migrationBuilder.RenameIndex(
                name: "IX_Concepts_OntologyId",
                table: "Concepts",
                newName: "IX_Concept_OntologyId");

            migrationBuilder.RenameIndex(
                name: "IX_ConceptRestrictions_ConceptId",
                table: "ConceptRestrictions",
                newName: "IX_ConceptRestriction_ConceptId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "IX_UserShareAccess_OntologyShareId",
                table: "UserShareAccesses",
                newName: "IX_UserShareAccesses_OntologyShareId");

            migrationBuilder.RenameIndex(
                name: "IX_Relationship_OntologyId",
                table: "Relationships",
                newName: "IX_Relationships_OntologyId");

            migrationBuilder.RenameIndex(
                name: "IX_OntologyShare_OntologyId",
                table: "OntologyShares",
                newName: "IX_OntologyShares_OntologyId");

            migrationBuilder.RenameIndex(
                name: "IX_Individual_OntologyId",
                table: "Individuals",
                newName: "IX_Individuals_OntologyId");

            migrationBuilder.RenameIndex(
                name: "IX_GuestSession_OntologyShareId",
                table: "GuestSessions",
                newName: "IX_GuestSessions_OntologyShareId");

            migrationBuilder.RenameIndex(
                name: "IX_Concept_OntologyId",
                table: "Concepts",
                newName: "IX_Concepts_OntologyId");

            migrationBuilder.RenameIndex(
                name: "IX_ConceptRestriction_ConceptId",
                table: "ConceptRestrictions",
                newName: "IX_ConceptRestrictions_ConceptId");
        }
    }
}
