using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Eidos.Migrations
{
    /// <inheritdoc />
    public partial class SetDefaultThemeToLight : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Update existing UserPreferences records with empty Theme to "light"
            migrationBuilder.Sql(
                "UPDATE UserPreferences SET Theme = 'light' WHERE Theme = '' OR Theme IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
