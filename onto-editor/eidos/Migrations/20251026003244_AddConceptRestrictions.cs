using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Eidos.Migrations
{
    /// <inheritdoc />
    public partial class AddConceptRestrictions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConceptRestrictions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConceptId = table.Column<int>(type: "int", nullable: false),
                    PropertyName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RestrictionType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MinCardinality = table.Column<int>(type: "int", nullable: true),
                    MaxCardinality = table.Column<int>(type: "int", nullable: true),
                    ValueType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    MinValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MaxValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AllowedConceptId = table.Column<int>(type: "int", nullable: true),
                    AllowedValues = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Pattern = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsMandatory = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConceptRestrictions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConceptRestrictions_Concepts_AllowedConceptId",
                        column: x => x.AllowedConceptId,
                        principalTable: "Concepts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConceptRestrictions_Concepts_ConceptId",
                        column: x => x.ConceptId,
                        principalTable: "Concepts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConceptRestrictions_AllowedConceptId",
                table: "ConceptRestrictions",
                column: "AllowedConceptId");

            migrationBuilder.CreateIndex(
                name: "IX_ConceptRestrictions_ConceptId",
                table: "ConceptRestrictions",
                column: "ConceptId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConceptRestrictions");
        }
    }
}
