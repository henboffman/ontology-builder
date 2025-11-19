using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Eidos.Migrations
{
    /// <inheritdoc />
    public partial class AddSharedOntologyFeature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastAccessedAt",
                table: "OntologyGroupPermissions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SharedOntologyUserStates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    OntologyId = table.Column<int>(type: "int", nullable: false),
                    IsPinned = table.Column<bool>(type: "bit", nullable: false),
                    IsHidden = table.Column<bool>(type: "bit", nullable: false),
                    IsDismissed = table.Column<bool>(type: "bit", nullable: false),
                    PinnedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    HiddenAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DismissedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SharedOntologyUserStates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SharedOntologyUserStates_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SharedOntologyUserStates_Ontologies_OntologyId",
                        column: x => x.OntologyId,
                        principalTable: "Ontologies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SharedOntologyUserState_OntologyId",
                table: "SharedOntologyUserStates",
                column: "OntologyId");

            migrationBuilder.CreateIndex(
                name: "IX_SharedOntologyUserState_UserId",
                table: "SharedOntologyUserStates",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SharedOntologyUserState_UserId_IsPinned",
                table: "SharedOntologyUserStates",
                columns: new[] { "UserId", "IsPinned" });

            migrationBuilder.CreateIndex(
                name: "IX_SharedOntologyUserState_UserId_OntologyId",
                table: "SharedOntologyUserStates",
                columns: new[] { "UserId", "OntologyId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SharedOntologyUserStates");

            migrationBuilder.DropColumn(
                name: "LastAccessedAt",
                table: "OntologyGroupPermissions");
        }
    }
}
