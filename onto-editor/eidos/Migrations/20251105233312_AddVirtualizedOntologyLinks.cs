using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Eidos.Migrations
{
    /// <inheritdoc />
    public partial class AddVirtualizedOntologyLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Uri",
                table: "OntologyLinks",
                type: "TEXT",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 500);

            migrationBuilder.AddColumn<string>(
                name: "Color",
                table: "OntologyLinks",
                type: "TEXT",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastSyncedAt",
                table: "OntologyLinks",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LinkType",
                table: "OntologyLinks",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LinkedOntologyId",
                table: "OntologyLinks",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "PositionX",
                table: "OntologyLinks",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "PositionY",
                table: "OntologyLinks",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "UpdateAvailable",
                table: "OntologyLinks",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "OntologyLinks",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateIndex(
                name: "IX_OntologyLinks_LinkedOntologyId",
                table: "OntologyLinks",
                column: "LinkedOntologyId");

            migrationBuilder.CreateIndex(
                name: "IX_OntologyLinks_OntologyId_LinkType",
                table: "OntologyLinks",
                columns: new[] { "OntologyId", "LinkType" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_OntologyLink_HasTarget",
                table: "OntologyLinks",
                sql: "(LinkType = 0 AND Uri IS NOT NULL) OR (LinkType = 1 AND LinkedOntologyId IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_OntologyLink_NoSelfReference",
                table: "OntologyLinks",
                sql: "OntologyId <> LinkedOntologyId OR LinkedOntologyId IS NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_OntologyLinks_Ontologies_LinkedOntologyId",
                table: "OntologyLinks",
                column: "LinkedOntologyId",
                principalTable: "Ontologies",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OntologyLinks_Ontologies_LinkedOntologyId",
                table: "OntologyLinks");

            migrationBuilder.DropIndex(
                name: "IX_OntologyLinks_LinkedOntologyId",
                table: "OntologyLinks");

            migrationBuilder.DropIndex(
                name: "IX_OntologyLinks_OntologyId_LinkType",
                table: "OntologyLinks");

            migrationBuilder.DropCheckConstraint(
                name: "CK_OntologyLink_HasTarget",
                table: "OntologyLinks");

            migrationBuilder.DropCheckConstraint(
                name: "CK_OntologyLink_NoSelfReference",
                table: "OntologyLinks");

            migrationBuilder.DropColumn(
                name: "Color",
                table: "OntologyLinks");

            migrationBuilder.DropColumn(
                name: "LastSyncedAt",
                table: "OntologyLinks");

            migrationBuilder.DropColumn(
                name: "LinkType",
                table: "OntologyLinks");

            migrationBuilder.DropColumn(
                name: "LinkedOntologyId",
                table: "OntologyLinks");

            migrationBuilder.DropColumn(
                name: "PositionX",
                table: "OntologyLinks");

            migrationBuilder.DropColumn(
                name: "PositionY",
                table: "OntologyLinks");

            migrationBuilder.DropColumn(
                name: "UpdateAvailable",
                table: "OntologyLinks");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "OntologyLinks");

            migrationBuilder.AlterColumn<string>(
                name: "Uri",
                table: "OntologyLinks",
                type: "TEXT",
                maxLength: 500,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 500,
                oldNullable: true);
        }
    }
}
