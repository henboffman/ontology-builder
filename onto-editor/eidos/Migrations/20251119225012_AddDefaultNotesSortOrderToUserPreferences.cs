using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Eidos.Migrations
{
    /// <inheritdoc />
    public partial class AddDefaultNotesSortOrderToUserPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DefaultNotesSortOrder",
                table: "UserPreferences",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DefaultNotesSortOrder",
                table: "UserPreferences");
        }
    }
}
