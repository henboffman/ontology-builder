using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Eidos.Data;
using Eidos.Models;

namespace Eidos;

public static class SeedDevUsers
{
    public static async Task SeedAsync(OntologyDbContext context)
    {
        Console.WriteLine("🌱 Seeding development users...");

        // Add dev user
        var devUserId = "cb7c6b4d-af5d-4ff5-88d0-ba9fc88239fa";
        if (!await context.Users.AnyAsync(u => u.Id == devUserId))
        {
            var devUser = new ApplicationUser
            {
                Id = devUserId,
                UserName = "dev@localhost.local",
                NormalizedUserName = "DEV@LOCALHOST.LOCAL",
                Email = "dev@localhost.local",
                NormalizedEmail = "DEV@LOCALHOST.LOCAL",
                EmailConfirmed = true,
                SecurityStamp = "X7ORP6OMFRJHDQ2LZNDYCF5LKTRF7DVD",
                ConcurrencyStamp = "4099628a-eafa-44cf-ace5-943ca01371c1",
                LockoutEnabled = true,
                CreatedAt = DateTime.UtcNow
            };
            context.Users.Add(devUser);
            Console.WriteLine("  ✅ Added dev@localhost.local");

            // Add to roles
            var adminRole = await context.Roles.FirstOrDefaultAsync(r => r.NormalizedName == "ADMIN");
            var userRole = await context.Roles.FirstOrDefaultAsync(r => r.NormalizedName == "USER");

            if (adminRole != null)
            {
                context.UserRoles.Add(new IdentityUserRole<string> { UserId = devUserId, RoleId = adminRole.Id });
            }
            if (userRole != null)
            {
                context.UserRoles.Add(new IdentityUserRole<string> { UserId = devUserId, RoleId = userRole.Id });
            }

            // Add minimal preferences
            var devPrefs = new UserPreferences
            {
                UserId = devUserId,
                Theme = "dark",
                GroupingRadius = 100,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            context.UserPreferences.Add(devPrefs);
        }

        // Add test user
        var testUserId = "test-user-id-123";
        if (!await context.Users.AnyAsync(u => u.Id == testUserId))
        {
            var testUser = new ApplicationUser
            {
                Id = testUserId,
                UserName = "test@test.com",
                NormalizedUserName = "TEST@TEST.COM",
                Email = "test@test.com",
                NormalizedEmail = "TEST@TEST.COM",
                EmailConfirmed = true,
                DisplayName = "Test User",
                SecurityStamp = "NEWSTAMP123",
                ConcurrencyStamp = "NEWCONCURRENCYSTAMP123",
                LockoutEnabled = true,
                CreatedAt = DateTime.UtcNow
            };
            context.Users.Add(testUser);
            Console.WriteLine("  ✅ Added test@test.com");

            var userRole = await context.Roles.FirstOrDefaultAsync(r => r.NormalizedName == "USER");
            if (userRole != null)
            {
                context.UserRoles.Add(new IdentityUserRole<string> { UserId = testUserId, RoleId = userRole.Id });
            }

            var testPrefs = new UserPreferences
            {
                UserId = testUserId,
                Theme = "light",
                GroupingRadius = 100,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            context.UserPreferences.Add(testPrefs);
        }

        // Add collab user
        var collabUserId = "collab-user-id-456";
        if (!await context.Users.AnyAsync(u => u.Id == collabUserId))
        {
            var collabUser = new ApplicationUser
            {
                Id = collabUserId,
                UserName = "collab@test.com",
                NormalizedUserName = "COLLAB@TEST.COM",
                Email = "collab@test.com",
                NormalizedEmail = "COLLAB@TEST.COM",
                EmailConfirmed = true,
                DisplayName = "Collaborator User",
                SecurityStamp = "STAMP456",
                ConcurrencyStamp = "CONCURRENCY456",
                LockoutEnabled = true,
                CreatedAt = DateTime.UtcNow
            };
            context.Users.Add(collabUser);
            Console.WriteLine("  ✅ Added collab@test.com");

            var userRole = await context.Roles.FirstOrDefaultAsync(r => r.NormalizedName == "USER");
            if (userRole != null)
            {
                context.UserRoles.Add(new IdentityUserRole<string> { UserId = collabUserId, RoleId = userRole.Id });
            }

            var collabPrefs = new UserPreferences
            {
                UserId = collabUserId,
                Theme = "light",
                GroupingRadius = 100,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            context.UserPreferences.Add(collabPrefs);
        }

        await context.SaveChangesAsync();
        Console.WriteLine("✅ Dev users seeded successfully!\n");
    }
}
