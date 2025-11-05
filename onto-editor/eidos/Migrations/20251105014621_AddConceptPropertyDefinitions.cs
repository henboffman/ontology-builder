using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Eidos.Migrations
{
    /// <inheritdoc />
    public partial class AddConceptPropertyDefinitions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CollaborationProjectGroupId",
                table: "CollaborationPosts",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ConceptProperties",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ConceptId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    PropertyType = table.Column<int>(type: "INTEGER", nullable: false),
                    DataType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    RangeConceptId = table.Column<int>(type: "INTEGER", nullable: true),
                    IsRequired = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsFunctional = table.Column<bool>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Uri = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConceptProperties", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConceptProperties_Concepts_ConceptId",
                        column: x => x.ConceptId,
                        principalTable: "Concepts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ConceptProperties_Concepts_RangeConceptId",
                        column: x => x.RangeConceptId,
                        principalTable: "Concepts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CollaborationPosts_CollaborationProjectGroupId",
                table: "CollaborationPosts",
                column: "CollaborationProjectGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_ConceptProperties_RangeConceptId",
                table: "ConceptProperties",
                column: "RangeConceptId");

            migrationBuilder.CreateIndex(
                name: "IX_ConceptProperty_ConceptId",
                table: "ConceptProperties",
                column: "ConceptId");

            migrationBuilder.AddForeignKey(
                name: "FK_CollaborationPosts_UserGroups_CollaborationProjectGroupId",
                table: "CollaborationPosts",
                column: "CollaborationProjectGroupId",
                principalTable: "UserGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CollaborationPosts_UserGroups_CollaborationProjectGroupId",
                table: "CollaborationPosts");

            migrationBuilder.DropTable(
                name: "ConceptProperties");

            migrationBuilder.DropIndex(
                name: "IX_CollaborationPosts_CollaborationProjectGroupId",
                table: "CollaborationPosts");

            migrationBuilder.DropColumn(
                name: "CollaborationProjectGroupId",
                table: "CollaborationPosts");
        }
    }
}
