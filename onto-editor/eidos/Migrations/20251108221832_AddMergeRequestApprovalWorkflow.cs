using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Eidos.Migrations
{
    /// <inheritdoc />
    public partial class AddMergeRequestApprovalWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "RequiresApproval",
                table: "Ontologies",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "MergeRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OntologyId = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AssignedReviewerUserId = table.Column<string>(type: "TEXT", nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ReviewedByUserId = table.Column<string>(type: "TEXT", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ReviewComments = table.Column<string>(type: "TEXT", nullable: true),
                    MergedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    BaseSnapshotJson = table.Column<string>(type: "TEXT", nullable: true),
                    ConceptsAdded = table.Column<int>(type: "INTEGER", nullable: false),
                    ConceptsModified = table.Column<int>(type: "INTEGER", nullable: false),
                    ConceptsDeleted = table.Column<int>(type: "INTEGER", nullable: false),
                    RelationshipsAdded = table.Column<int>(type: "INTEGER", nullable: false),
                    RelationshipsModified = table.Column<int>(type: "INTEGER", nullable: false),
                    RelationshipsDeleted = table.Column<int>(type: "INTEGER", nullable: false),
                    IndividualsAdded = table.Column<int>(type: "INTEGER", nullable: false),
                    IndividualsModified = table.Column<int>(type: "INTEGER", nullable: false),
                    IndividualsDeleted = table.Column<int>(type: "INTEGER", nullable: false),
                    HasConflicts = table.Column<bool>(type: "INTEGER", nullable: false),
                    Priority = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MergeRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MergeRequests_AspNetUsers_AssignedReviewerUserId",
                        column: x => x.AssignedReviewerUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_MergeRequests_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MergeRequests_AspNetUsers_ReviewedByUserId",
                        column: x => x.ReviewedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_MergeRequests_Ontologies_OntologyId",
                        column: x => x.OntologyId,
                        principalTable: "Ontologies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MergeRequestChanges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MergeRequestId = table.Column<int>(type: "INTEGER", nullable: false),
                    ChangeType = table.Column<int>(type: "INTEGER", nullable: false),
                    EntityType = table.Column<int>(type: "INTEGER", nullable: false),
                    EntityId = table.Column<int>(type: "INTEGER", nullable: false),
                    EntityName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    BeforeJson = table.Column<string>(type: "TEXT", nullable: true),
                    AfterJson = table.Column<string>(type: "TEXT", nullable: true),
                    ChangeSummary = table.Column<string>(type: "TEXT", nullable: true),
                    OrderIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    HasConflict = table.Column<bool>(type: "INTEGER", nullable: false),
                    ConflictDescription = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MergeRequestChanges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MergeRequestChanges_MergeRequests_MergeRequestId",
                        column: x => x.MergeRequestId,
                        principalTable: "MergeRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MergeRequestComments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MergeRequestId = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    Text = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EditedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsSystemComment = table.Column<bool>(type: "INTEGER", nullable: false),
                    MergeRequestChangeId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MergeRequestComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MergeRequestComments_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MergeRequestComments_MergeRequestChanges_MergeRequestChangeId",
                        column: x => x.MergeRequestChangeId,
                        principalTable: "MergeRequestChanges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_MergeRequestComments_MergeRequests_MergeRequestId",
                        column: x => x.MergeRequestId,
                        principalTable: "MergeRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MergeRequestChange_MergeRequestId",
                table: "MergeRequestChanges",
                column: "MergeRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_MergeRequestComment_MergeRequestId",
                table: "MergeRequestComments",
                column: "MergeRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_MergeRequestComment_MergeRequestId_CreatedAt",
                table: "MergeRequestComments",
                columns: new[] { "MergeRequestId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MergeRequestComments_MergeRequestChangeId",
                table: "MergeRequestComments",
                column: "MergeRequestChangeId");

            migrationBuilder.CreateIndex(
                name: "IX_MergeRequestComments_UserId",
                table: "MergeRequestComments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_MergeRequest_AssignedReviewerUserId",
                table: "MergeRequests",
                column: "AssignedReviewerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MergeRequest_CreatedByUserId",
                table: "MergeRequests",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MergeRequest_OntologyId",
                table: "MergeRequests",
                column: "OntologyId");

            migrationBuilder.CreateIndex(
                name: "IX_MergeRequest_OntologyId_Status",
                table: "MergeRequests",
                columns: new[] { "OntologyId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_MergeRequests_ReviewedByUserId",
                table: "MergeRequests",
                column: "ReviewedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MergeRequestComments");

            migrationBuilder.DropTable(
                name: "MergeRequestChanges");

            migrationBuilder.DropTable(
                name: "MergeRequests");

            migrationBuilder.DropColumn(
                name: "RequiresApproval",
                table: "Ontologies");
        }
    }
}
