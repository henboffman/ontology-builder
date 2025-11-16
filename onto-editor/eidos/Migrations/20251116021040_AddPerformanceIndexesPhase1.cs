using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Eidos.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexesPhase1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Note: Some indexes may already exist from previous migrations or model conventions.
            // Some tables may not exist in the current database (e.g., OntologyActivity).
            // We use raw SQL with IF EXISTS for table check and IF NOT EXISTS for index check.

            // 1. IndividualProperty.IndividualId - Foreign key index (may already exist)
            migrationBuilder.Sql(@"
                IF OBJECT_ID('IndividualProperties', 'U') IS NOT NULL
                   AND NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_IndividualProperties_IndividualId' AND object_id = OBJECT_ID('IndividualProperties'))
                BEGIN
                    CREATE INDEX IX_IndividualProperties_IndividualId ON IndividualProperties (IndividualId);
                END
            ");

            // 2. Property.ConceptId - Foreign key index (may already exist)
            migrationBuilder.Sql(@"
                IF OBJECT_ID('Properties', 'U') IS NOT NULL
                   AND NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Properties_ConceptId' AND object_id = OBJECT_ID('Properties'))
                BEGIN
                    CREATE INDEX IX_Properties_ConceptId ON Properties (ConceptId);
                END
            ");

            // 3. Note.Title - Frequently searched column
            migrationBuilder.Sql(@"
                IF OBJECT_ID('Notes', 'U') IS NOT NULL
                   AND NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Notes_Title' AND object_id = OBJECT_ID('Notes'))
                BEGIN
                    CREATE INDEX IX_Notes_Title ON Notes (Title);
                END
            ");

            // 4. OntologyActivity composite index for activity queries (skip if table doesn't exist)
            migrationBuilder.Sql(@"
                IF OBJECT_ID('OntologyActivity', 'U') IS NOT NULL
                   AND NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_OntologyActivity_EntityType_EntityId_OntologyId' AND object_id = OBJECT_ID('OntologyActivity'))
                BEGIN
                    CREATE INDEX IX_OntologyActivity_EntityType_EntityId_OntologyId ON OntologyActivity (EntityType, EntityId, OntologyId);
                END
            ");

            // 5. Workspace covering index for list queries
            migrationBuilder.Sql(@"
                IF OBJECT_ID('Workspaces', 'U') IS NOT NULL
                   AND NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Workspaces_UserId_Visibility_UpdatedAt' AND object_id = OBJECT_ID('Workspaces'))
                BEGIN
                    CREATE INDEX IX_Workspaces_UserId_Visibility_UpdatedAt ON Workspaces (UserId, Visibility, UpdatedAt);
                END
            ");

            // 6. Note filtered index for concept notes
            migrationBuilder.Sql(@"
                IF OBJECT_ID('Notes', 'U') IS NOT NULL
                   AND NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Notes_WorkspaceId_LinkedConceptId_IsConceptNote' AND object_id = OBJECT_ID('Notes'))
                BEGIN
                    CREATE INDEX IX_Notes_WorkspaceId_LinkedConceptId_IsConceptNote ON Notes (WorkspaceId, LinkedConceptId) WHERE IsConceptNote = 1;
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop indexes in reverse order (with IF EXISTS for safety, check table exists too)
            migrationBuilder.Sql(@"
                IF OBJECT_ID('Notes', 'U') IS NOT NULL
                   AND EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Notes_WorkspaceId_LinkedConceptId_IsConceptNote' AND object_id = OBJECT_ID('Notes'))
                    DROP INDEX IX_Notes_WorkspaceId_LinkedConceptId_IsConceptNote ON Notes;
            ");

            migrationBuilder.Sql(@"
                IF OBJECT_ID('Workspaces', 'U') IS NOT NULL
                   AND EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Workspaces_UserId_Visibility_UpdatedAt' AND object_id = OBJECT_ID('Workspaces'))
                    DROP INDEX IX_Workspaces_UserId_Visibility_UpdatedAt ON Workspaces;
            ");

            migrationBuilder.Sql(@"
                IF OBJECT_ID('OntologyActivity', 'U') IS NOT NULL
                   AND EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_OntologyActivity_EntityType_EntityId_OntologyId' AND object_id = OBJECT_ID('OntologyActivity'))
                    DROP INDEX IX_OntologyActivity_EntityType_EntityId_OntologyId ON OntologyActivity;
            ");

            migrationBuilder.Sql(@"
                IF OBJECT_ID('Notes', 'U') IS NOT NULL
                   AND EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Notes_Title' AND object_id = OBJECT_ID('Notes'))
                    DROP INDEX IX_Notes_Title ON Notes;
            ");

            migrationBuilder.Sql(@"
                IF OBJECT_ID('Properties', 'U') IS NOT NULL
                   AND EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Properties_ConceptId' AND object_id = OBJECT_ID('Properties'))
                    DROP INDEX IX_Properties_ConceptId ON Properties;
            ");

            migrationBuilder.Sql(@"
                IF OBJECT_ID('IndividualProperties', 'U') IS NOT NULL
                   AND EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_IndividualProperties_IndividualId' AND object_id = OBJECT_ID('IndividualProperties'))
                    DROP INDEX IX_IndividualProperties_IndividualId ON IndividualProperties;
            ");
        }
    }
}
