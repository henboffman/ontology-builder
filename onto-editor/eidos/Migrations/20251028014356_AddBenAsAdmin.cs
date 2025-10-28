using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Eidos.Migrations
{
    /// <inheritdoc />
    public partial class AddBenAsAdmin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Assign Admin role to benbrent@gmail.com
            migrationBuilder.Sql(@"
                DECLARE @UserId NVARCHAR(450);
                DECLARE @RoleId NVARCHAR(450);

                -- Get user ID for benbrent@gmail.com
                SELECT @UserId = Id FROM AspNetUsers WHERE Email = 'benbrent@gmail.com';

                -- Get role ID for Admin
                SELECT @RoleId = Id FROM AspNetRoles WHERE Name = 'Admin';

                -- Only insert if both user and role exist and user doesn't already have the role
                IF @UserId IS NOT NULL AND @RoleId IS NOT NULL
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM AspNetUserRoles WHERE UserId = @UserId AND RoleId = @RoleId)
                    BEGIN
                        INSERT INTO AspNetUserRoles (UserId, RoleId)
                        VALUES (@UserId, @RoleId);
                    END
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove Admin role from benbrent@gmail.com
            migrationBuilder.Sql(@"
                DECLARE @UserId NVARCHAR(450);
                DECLARE @RoleId NVARCHAR(450);

                -- Get user ID for benbrent@gmail.com
                SELECT @UserId = Id FROM AspNetUsers WHERE Email = 'benbrent@gmail.com';

                -- Get role ID for Admin
                SELECT @RoleId = Id FROM AspNetRoles WHERE Name = 'Admin';

                -- Remove the role assignment if it exists
                IF @UserId IS NOT NULL AND @RoleId IS NOT NULL
                BEGIN
                    DELETE FROM AspNetUserRoles
                    WHERE UserId = @UserId AND RoleId = @RoleId;
                END
            ");
        }
    }
}
