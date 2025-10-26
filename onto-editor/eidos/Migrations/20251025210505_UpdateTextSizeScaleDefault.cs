using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Eidos.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTextSizeScaleDefault : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Update existing records that have TextSizeScale = 0 to the default value of 100
            migrationBuilder.Sql(@"
                UPDATE UserPreferences
                SET TextSizeScale = 100
                WHERE TextSizeScale = 0;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
