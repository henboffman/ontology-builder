namespace Eidos.Models;

/// <summary>
/// Application role constants for authorization
/// </summary>
public static class AppRoles
{
    /// <summary>
    /// Administrator role - full access to all features including user management
    /// </summary>
    public const string Admin = "Admin";

    /// <summary>
    /// Power user role - advanced features but no user management
    /// </summary>
    public const string PowerUser = "PowerUser";

    /// <summary>
    /// Regular user role - standard access to create/edit own ontologies
    /// </summary>
    public const string User = "User";

    /// <summary>
    /// Guest role - read-only access via share links
    /// </summary>
    public const string Guest = "Guest";

    /// <summary>
    /// All defined roles in the system
    /// </summary>
    public static readonly string[] AllRoles = { Admin, PowerUser, User, Guest };
}

/// <summary>
/// Authorization policy names
/// </summary>
public static class AppPolicies
{
    /// <summary>
    /// Requires Admin role
    /// </summary>
    public const string RequireAdmin = "RequireAdmin";

    /// <summary>
    /// Requires Admin or PowerUser role
    /// </summary>
    public const string RequirePowerUser = "RequirePowerUser";

    /// <summary>
    /// Requires any authenticated user
    /// </summary>
    public const string RequireAuthenticatedUser = "RequireAuthenticatedUser";
}
