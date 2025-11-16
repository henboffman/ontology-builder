using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Eidos.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkspaceAndNotesSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "WorkspaceId",
                table: "Ontologies",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Workspaces",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    Visibility = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    AllowPublicEdit = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    NoteCount = table.Column<int>(type: "INTEGER", nullable: false),
                    ConceptNoteCount = table.Column<int>(type: "INTEGER", nullable: false),
                    UserNoteCount = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Workspaces", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Workspaces_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Notes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    WorkspaceId = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    ContentLength = table.Column<int>(type: "INTEGER", nullable: false),
                    LinkCount = table.Column<int>(type: "INTEGER", nullable: false),
                    IsConceptNote = table.Column<bool>(type: "INTEGER", nullable: false),
                    LinkedConceptId = table.Column<int>(type: "INTEGER", nullable: true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notes_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Notes_Concepts_LinkedConceptId",
                        column: x => x.LinkedConceptId,
                        principalTable: "Concepts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Notes_Workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "Workspaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkspaceGroupPermissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    WorkspaceId = table.Column<int>(type: "INTEGER", nullable: false),
                    UserGroupId = table.Column<int>(type: "INTEGER", nullable: false),
                    PermissionLevel = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkspaceGroupPermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkspaceGroupPermissions_UserGroups_UserGroupId",
                        column: x => x.UserGroupId,
                        principalTable: "UserGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WorkspaceGroupPermissions_Workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "Workspaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkspaceUserAccesses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    WorkspaceId = table.Column<int>(type: "INTEGER", nullable: false),
                    SharedWithUserId = table.Column<string>(type: "TEXT", nullable: false),
                    PermissionLevel = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkspaceUserAccesses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkspaceUserAccesses_AspNetUsers_SharedWithUserId",
                        column: x => x.SharedWithUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WorkspaceUserAccesses_Workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "Workspaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NoteContents",
                columns: table => new
                {
                    NoteId = table.Column<int>(type: "INTEGER", nullable: false),
                    MarkdownContent = table.Column<string>(type: "TEXT", nullable: false),
                    RenderedHtml = table.Column<string>(type: "TEXT", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NoteContents", x => x.NoteId);
                    table.ForeignKey(
                        name: "FK_NoteContents_Notes_NoteId",
                        column: x => x.NoteId,
                        principalTable: "Notes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NoteLinks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SourceNoteId = table.Column<int>(type: "INTEGER", nullable: false),
                    TargetConceptId = table.Column<int>(type: "INTEGER", nullable: false),
                    CharacterPosition = table.Column<int>(type: "INTEGER", nullable: false),
                    ContextSnippet = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    NoteId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NoteLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NoteLinks_Concepts_TargetConceptId",
                        column: x => x.TargetConceptId,
                        principalTable: "Concepts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NoteLinks_Notes_NoteId",
                        column: x => x.NoteId,
                        principalTable: "Notes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_NoteLinks_Notes_SourceNoteId",
                        column: x => x.SourceNoteId,
                        principalTable: "Notes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Ontologies_WorkspaceId",
                table: "Ontologies",
                column: "WorkspaceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NoteLink_SourceNoteId",
                table: "NoteLinks",
                column: "SourceNoteId");

            migrationBuilder.CreateIndex(
                name: "IX_NoteLink_SourceNoteId_TargetConceptId",
                table: "NoteLinks",
                columns: new[] { "SourceNoteId", "TargetConceptId" });

            migrationBuilder.CreateIndex(
                name: "IX_NoteLink_TargetConceptId",
                table: "NoteLinks",
                column: "TargetConceptId");

            migrationBuilder.CreateIndex(
                name: "IX_NoteLinks_NoteId",
                table: "NoteLinks",
                column: "NoteId");

            migrationBuilder.CreateIndex(
                name: "IX_Note_IsConceptNote",
                table: "Notes",
                column: "IsConceptNote");

            migrationBuilder.CreateIndex(
                name: "IX_Note_LinkedConceptId",
                table: "Notes",
                column: "LinkedConceptId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Note_WorkspaceId",
                table: "Notes",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_Note_WorkspaceId_IsConceptNote",
                table: "Notes",
                columns: new[] { "WorkspaceId", "IsConceptNote" });

            migrationBuilder.CreateIndex(
                name: "IX_Notes_UserId",
                table: "Notes",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkspaceGroupPermission_WorkspaceId_UserGroupId",
                table: "WorkspaceGroupPermissions",
                columns: new[] { "WorkspaceId", "UserGroupId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkspaceGroupPermissions_UserGroupId",
                table: "WorkspaceGroupPermissions",
                column: "UserGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Workspace_UserId",
                table: "Workspaces",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Workspace_UserId_Name",
                table: "Workspaces",
                columns: new[] { "UserId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_Workspace_Visibility",
                table: "Workspaces",
                column: "Visibility");

            migrationBuilder.CreateIndex(
                name: "IX_WorkspaceUserAccess_WorkspaceId_SharedWithUserId",
                table: "WorkspaceUserAccesses",
                columns: new[] { "WorkspaceId", "SharedWithUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkspaceUserAccesses_SharedWithUserId",
                table: "WorkspaceUserAccesses",
                column: "SharedWithUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Ontologies_Workspaces_WorkspaceId",
                table: "Ontologies",
                column: "WorkspaceId",
                principalTable: "Workspaces",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Ontologies_Workspaces_WorkspaceId",
                table: "Ontologies");

            migrationBuilder.DropTable(
                name: "NoteContents");

            migrationBuilder.DropTable(
                name: "NoteLinks");

            migrationBuilder.DropTable(
                name: "WorkspaceGroupPermissions");

            migrationBuilder.DropTable(
                name: "WorkspaceUserAccesses");

            migrationBuilder.DropTable(
                name: "Notes");

            migrationBuilder.DropTable(
                name: "Workspaces");

            migrationBuilder.DropIndex(
                name: "IX_Ontologies_WorkspaceId",
                table: "Ontologies");

            migrationBuilder.DropColumn(
                name: "WorkspaceId",
                table: "Ontologies");
        }
    }
}
