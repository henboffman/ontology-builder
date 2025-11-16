using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Eidos.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FeatureToggles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeatureToggles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Color = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserGroups_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserPreferences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    EntityColor = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ProcessColor = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    QualityColor = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RoleColor = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FunctionColor = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    InformationColor = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EventColor = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DefaultConceptColor = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsARelationshipColor = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PartOfRelationshipColor = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    HasPartRelationshipColor = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RelatedToRelationshipColor = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DefaultRelationshipColor = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DefaultNodeSize = table.Column<int>(type: "int", nullable: false),
                    DefaultEdgeThickness = table.Column<int>(type: "int", nullable: false),
                    ShowEdgeLabels = table.Column<bool>(type: "bit", nullable: false),
                    AutoColorByCategory = table.Column<bool>(type: "bit", nullable: false),
                    TextSizeScale = table.Column<int>(type: "int", nullable: false),
                    GroupingRadius = table.Column<int>(type: "int", nullable: false),
                    Theme = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    LayoutStyle = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ShowKeyboardShortcuts = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPreferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserPreferences_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Workspaces",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Visibility = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AllowPublicEdit = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NoteCount = table.Column<int>(type: "int", nullable: false),
                    ConceptNoteCount = table.Column<int>(type: "int", nullable: false),
                    UserNoteCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Workspaces", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Workspaces_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserGroupMembers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserGroupId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AddedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    IsGroupAdmin = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserGroupMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserGroupMembers_AspNetUsers_AddedByUserId",
                        column: x => x.AddedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UserGroupMembers_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserGroupMembers_UserGroups_UserGroupId",
                        column: x => x.UserGroupId,
                        principalTable: "UserGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Ontologies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkspaceId = table.Column<int>(type: "int", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Namespace = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    NamespacePrefixes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Tags = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    License = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Author = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Version = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConceptCount = table.Column<int>(type: "int", nullable: false),
                    RelationshipCount = table.Column<int>(type: "int", nullable: false),
                    UsesBFO = table.Column<bool>(type: "bit", nullable: false),
                    UsesProvO = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ParentOntologyId = table.Column<int>(type: "int", nullable: true),
                    ProvenanceType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ProvenanceNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Visibility = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AllowPublicEdit = table.Column<bool>(type: "bit", nullable: false),
                    RequiresApproval = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ontologies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Ontologies_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Ontologies_Ontologies_ParentOntologyId",
                        column: x => x.ParentOntologyId,
                        principalTable: "Ontologies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Ontologies_Workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "Workspaces",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "WorkspaceGroupPermissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkspaceId = table.Column<int>(type: "int", nullable: false),
                    UserGroupId = table.Column<int>(type: "int", nullable: false),
                    PermissionLevel = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkspaceGroupPermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkspaceGroupPermissions_UserGroups_UserGroupId",
                        column: x => x.UserGroupId,
                        principalTable: "UserGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WorkspaceGroupPermissions_Workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "Workspaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkspaceUserAccesses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkspaceId = table.Column<int>(type: "int", nullable: false),
                    SharedWithUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PermissionLevel = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkspaceUserAccesses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkspaceUserAccesses_AspNetUsers_SharedWithUserId",
                        column: x => x.SharedWithUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WorkspaceUserAccesses_Workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "Workspaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CollaborationPosts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    OntologyId = table.Column<int>(type: "int", nullable: true),
                    CollaborationProjectGroupId = table.Column<int>(type: "int", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Domain = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Tags = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    LookingFor = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TimeCommitment = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SkillLevel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    ViewCount = table.Column<int>(type: "int", nullable: false),
                    ResponseCount = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastBumpedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CollaborationPosts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CollaborationPosts_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CollaborationPosts_Ontologies_OntologyId",
                        column: x => x.OntologyId,
                        principalTable: "Ontologies",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CollaborationPosts_UserGroups_CollaborationProjectGroupId",
                        column: x => x.CollaborationProjectGroupId,
                        principalTable: "UserGroups",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Concepts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OntologyId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Definition = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SimpleExplanation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Examples = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PositionX = table.Column<double>(type: "float", nullable: true),
                    PositionY = table.Column<double>(type: "float", nullable: true),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Color = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SourceOntology = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Concepts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Concepts_Ontologies_OntologyId",
                        column: x => x.OntologyId,
                        principalTable: "Ontologies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CustomConceptTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OntologyId = table.Column<int>(type: "int", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Examples = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Color = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomConceptTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomConceptTemplates_Ontologies_OntologyId",
                        column: x => x.OntologyId,
                        principalTable: "Ontologies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EntityCommentCounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OntologyId = table.Column<int>(type: "int", nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EntityId = table.Column<int>(type: "int", nullable: false),
                    TotalComments = table.Column<int>(type: "int", nullable: false),
                    UnresolvedThreads = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntityCommentCounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EntityCommentCounts_Ontologies_OntologyId",
                        column: x => x.OntologyId,
                        principalTable: "Ontologies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EntityComments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OntologyId = table.Column<int>(type: "int", nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EntityId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Text = table.Column<string>(type: "nvarchar(max)", maxLength: 5000, nullable: false),
                    ParentCommentId = table.Column<int>(type: "int", nullable: true),
                    IsResolved = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EditedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntityComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EntityComments_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EntityComments_EntityComments_ParentCommentId",
                        column: x => x.ParentCommentId,
                        principalTable: "EntityComments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EntityComments_Ontologies_OntologyId",
                        column: x => x.OntologyId,
                        principalTable: "Ontologies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MergeRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OntologyId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AssignedReviewerUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewComments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MergedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BaseSnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConceptsAdded = table.Column<int>(type: "int", nullable: false),
                    ConceptsModified = table.Column<int>(type: "int", nullable: false),
                    ConceptsDeleted = table.Column<int>(type: "int", nullable: false),
                    RelationshipsAdded = table.Column<int>(type: "int", nullable: false),
                    RelationshipsModified = table.Column<int>(type: "int", nullable: false),
                    RelationshipsDeleted = table.Column<int>(type: "int", nullable: false),
                    IndividualsAdded = table.Column<int>(type: "int", nullable: false),
                    IndividualsModified = table.Column<int>(type: "int", nullable: false),
                    IndividualsDeleted = table.Column<int>(type: "int", nullable: false),
                    HasConflicts = table.Column<bool>(type: "bit", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MergeRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MergeRequests_AspNetUsers_AssignedReviewerUserId",
                        column: x => x.AssignedReviewerUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MergeRequests_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MergeRequests_AspNetUsers_ReviewedByUserId",
                        column: x => x.ReviewedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MergeRequests_Ontologies_OntologyId",
                        column: x => x.OntologyId,
                        principalTable: "Ontologies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OntologyActivities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OntologyId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    GuestSessionToken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ActorName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ActivityType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EntityId = table.Column<int>(type: "int", nullable: true),
                    EntityName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BeforeSnapshot = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AfterSnapshot = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    VersionNumber = table.Column<int>(type: "int", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OntologyActivities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OntologyActivities_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OntologyActivities_Ontologies_OntologyId",
                        column: x => x.OntologyId,
                        principalTable: "Ontologies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OntologyGroupPermissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OntologyId = table.Column<int>(type: "int", nullable: false),
                    UserGroupId = table.Column<int>(type: "int", nullable: false),
                    PermissionLevel = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    GrantedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GrantedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OntologyGroupPermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OntologyGroupPermissions_AspNetUsers_GrantedByUserId",
                        column: x => x.GrantedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OntologyGroupPermissions_Ontologies_OntologyId",
                        column: x => x.OntologyId,
                        principalTable: "Ontologies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OntologyGroupPermissions_UserGroups_UserGroupId",
                        column: x => x.UserGroupId,
                        principalTable: "UserGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OntologyLinks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OntologyId = table.Column<int>(type: "int", nullable: false),
                    LinkType = table.Column<int>(type: "int", nullable: false),
                    Uri = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    LinkedOntologyId = table.Column<int>(type: "int", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Prefix = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PositionX = table.Column<double>(type: "float", nullable: true),
                    PositionY = table.Column<double>(type: "float", nullable: true),
                    Color = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ConceptsImported = table.Column<bool>(type: "bit", nullable: false),
                    ImportedConceptCount = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastSyncedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdateAvailable = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OntologyLinks", x => x.Id);
                    table.CheckConstraint("CK_OntologyLink_HasTarget", "(LinkType = 0 AND Uri IS NOT NULL) OR (LinkType = 1 AND LinkedOntologyId IS NOT NULL)");
                    table.CheckConstraint("CK_OntologyLink_NoSelfReference", "OntologyId <> LinkedOntologyId OR LinkedOntologyId IS NULL");
                    table.ForeignKey(
                        name: "FK_OntologyLinks_Ontologies_LinkedOntologyId",
                        column: x => x.LinkedOntologyId,
                        principalTable: "Ontologies",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OntologyLinks_Ontologies_OntologyId",
                        column: x => x.OntologyId,
                        principalTable: "Ontologies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OntologyShares",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OntologyId = table.Column<int>(type: "int", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    ShareToken = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PermissionLevel = table.Column<int>(type: "int", nullable: false),
                    AllowGuestAccess = table.Column<bool>(type: "bit", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    AccessCount = table.Column<int>(type: "int", nullable: false),
                    LastAccessedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OntologyShares", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OntologyShares_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OntologyShares_Ontologies_OntologyId",
                        column: x => x.OntologyId,
                        principalTable: "Ontologies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OntologyTags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OntologyId = table.Column<int>(type: "int", nullable: false),
                    Tag = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Color = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OntologyTags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OntologyTags_Ontologies_OntologyId",
                        column: x => x.OntologyId,
                        principalTable: "Ontologies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OntologyViewHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OntologyId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    LastViewedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastDismissedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CurrentSessionId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LastDismissedSessionId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ViewCount = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OntologyViewHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OntologyViewHistories_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OntologyViewHistories_Ontologies_OntologyId",
                        column: x => x.OntologyId,
                        principalTable: "Ontologies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CollaborationResponses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CollaborationPostId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ContactInfo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CollaborationResponses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CollaborationResponses_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CollaborationResponses_CollaborationPosts_CollaborationPostId",
                        column: x => x.CollaborationPostId,
                        principalTable: "CollaborationPosts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConceptGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OntologyId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ParentConceptId = table.Column<int>(type: "int", nullable: false),
                    ChildConceptIds = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsCollapsed = table.Column<bool>(type: "bit", nullable: false),
                    CollapsedRelationships = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GroupName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CollapsedPositionX = table.Column<double>(type: "float", nullable: true),
                    CollapsedPositionY = table.Column<double>(type: "float", nullable: true),
                    MaxDepth = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConceptGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConceptGroups_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ConceptGroups_Concepts_ParentConceptId",
                        column: x => x.ParentConceptId,
                        principalTable: "Concepts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ConceptGroups_Ontologies_OntologyId",
                        column: x => x.OntologyId,
                        principalTable: "Ontologies",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ConceptProperties",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConceptId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PropertyType = table.Column<int>(type: "int", nullable: false),
                    DataType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RangeConceptId = table.Column<int>(type: "int", nullable: true),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    IsFunctional = table.Column<bool>(type: "bit", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Uri = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "Individuals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OntologyId = table.Column<int>(type: "int", nullable: false),
                    ConceptId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Label = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Uri = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Individuals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Individuals_Concepts_ConceptId",
                        column: x => x.ConceptId,
                        principalTable: "Concepts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Individuals_Ontologies_OntologyId",
                        column: x => x.OntologyId,
                        principalTable: "Ontologies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Notes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkspaceId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ContentLength = table.Column<int>(type: "int", nullable: false),
                    LinkCount = table.Column<int>(type: "int", nullable: false),
                    IsConceptNote = table.Column<bool>(type: "bit", nullable: false),
                    LinkedConceptId = table.Column<int>(type: "int", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notes_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Notes_Concepts_LinkedConceptId",
                        column: x => x.LinkedConceptId,
                        principalTable: "Concepts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Notes_Workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "Workspaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Properties",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConceptId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DataType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Properties", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Properties_Concepts_ConceptId",
                        column: x => x.ConceptId,
                        principalTable: "Concepts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Relationships",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OntologyId = table.Column<int>(type: "int", nullable: false),
                    SourceConceptId = table.Column<int>(type: "int", nullable: false),
                    TargetConceptId = table.Column<int>(type: "int", nullable: false),
                    RelationType = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Label = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OntologyUri = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Strength = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Relationships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Relationships_Concepts_SourceConceptId",
                        column: x => x.SourceConceptId,
                        principalTable: "Concepts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Relationships_Concepts_TargetConceptId",
                        column: x => x.TargetConceptId,
                        principalTable: "Concepts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Relationships_Ontologies_OntologyId",
                        column: x => x.OntologyId,
                        principalTable: "Ontologies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CommentMentions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CommentId = table.Column<int>(type: "int", nullable: false),
                    MentionedUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    HasViewed = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ViewedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommentMentions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommentMentions_AspNetUsers_MentionedUserId",
                        column: x => x.MentionedUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CommentMentions_EntityComments_CommentId",
                        column: x => x.CommentId,
                        principalTable: "EntityComments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MergeRequestChanges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MergeRequestId = table.Column<int>(type: "int", nullable: false),
                    ChangeType = table.Column<int>(type: "int", nullable: false),
                    EntityType = table.Column<int>(type: "int", nullable: false),
                    EntityId = table.Column<int>(type: "int", nullable: false),
                    EntityName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    BeforeJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AfterJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ChangeSummary = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OrderIndex = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    HasConflict = table.Column<bool>(type: "bit", nullable: false),
                    ConflictDescription = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MergeRequestChanges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MergeRequestChanges_MergeRequests_MergeRequestId",
                        column: x => x.MergeRequestId,
                        principalTable: "MergeRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GuestSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OntologyShareId = table.Column<int>(type: "int", nullable: false),
                    SessionToken = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    GuestName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastActivityAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuestSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GuestSessions_OntologyShares_OntologyShareId",
                        column: x => x.OntologyShareId,
                        principalTable: "OntologyShares",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserShareAccesses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    OntologyShareId = table.Column<int>(type: "int", nullable: false),
                    FirstAccessedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastAccessedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AccessCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserShareAccesses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserShareAccesses_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserShareAccesses_OntologyShares_OntologyShareId",
                        column: x => x.OntologyShareId,
                        principalTable: "OntologyShares",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IndividualProperties",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IndividualId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DataType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConceptPropertyId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IndividualProperties", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IndividualProperties_Individuals_IndividualId",
                        column: x => x.IndividualId,
                        principalTable: "Individuals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IndividualRelationships",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OntologyId = table.Column<int>(type: "int", nullable: false),
                    SourceIndividualId = table.Column<int>(type: "int", nullable: false),
                    TargetIndividualId = table.Column<int>(type: "int", nullable: false),
                    RelationType = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Label = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OntologyUri = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IndividualRelationships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IndividualRelationships_Individuals_SourceIndividualId",
                        column: x => x.SourceIndividualId,
                        principalTable: "Individuals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IndividualRelationships_Individuals_TargetIndividualId",
                        column: x => x.TargetIndividualId,
                        principalTable: "Individuals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IndividualRelationships_Ontologies_OntologyId",
                        column: x => x.OntologyId,
                        principalTable: "Ontologies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NoteContents",
                columns: table => new
                {
                    NoteId = table.Column<int>(type: "int", nullable: false),
                    MarkdownContent = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RenderedHtml = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NoteContents", x => x.NoteId);
                    table.ForeignKey(
                        name: "FK_NoteContents_Notes_NoteId",
                        column: x => x.NoteId,
                        principalTable: "Notes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NoteLinks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SourceNoteId = table.Column<int>(type: "int", nullable: false),
                    TargetConceptId = table.Column<int>(type: "int", nullable: false),
                    CharacterPosition = table.Column<int>(type: "int", nullable: false),
                    ContextSnippet = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NoteId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NoteLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NoteLinks_Concepts_TargetConceptId",
                        column: x => x.TargetConceptId,
                        principalTable: "Concepts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NoteLinks_Notes_NoteId",
                        column: x => x.NoteId,
                        principalTable: "Notes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_NoteLinks_Notes_SourceNoteId",
                        column: x => x.SourceNoteId,
                        principalTable: "Notes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MergeRequestComments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MergeRequestId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Text = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EditedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsSystemComment = table.Column<bool>(type: "bit", nullable: false),
                    MergeRequestChangeId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MergeRequestComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MergeRequestComments_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MergeRequestComments_MergeRequestChanges_MergeRequestChangeId",
                        column: x => x.MergeRequestChangeId,
                        principalTable: "MergeRequestChanges",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MergeRequestComments_MergeRequests_MergeRequestId",
                        column: x => x.MergeRequestId,
                        principalTable: "MergeRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "FeatureToggles",
                columns: new[] { "Id", "Category", "CreatedAt", "Description", "IsEnabled", "Key", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "User Management", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Display additional test users in the user selector for MVP testing", true, "show-test-users", "Show Test Users", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 2, "Collaboration", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Allow multiple users to collaborate on the same ontology", false, "enable-collaboration", "Enable Collaboration (Future)", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 3, "Collaboration", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Sync changes across users in real-time using SignalR", false, "enable-real-time-sync", "Enable Real-time Sync (Future)", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CollaborationPost_Domain",
                table: "CollaborationPosts",
                column: "Domain");

            migrationBuilder.CreateIndex(
                name: "IX_CollaborationPost_IsActive",
                table: "CollaborationPosts",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_CollaborationPost_IsActive_CreatedAt",
                table: "CollaborationPosts",
                columns: new[] { "IsActive", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CollaborationPost_UserId",
                table: "CollaborationPosts",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_CollaborationPosts_CollaborationProjectGroupId",
                table: "CollaborationPosts",
                column: "CollaborationProjectGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_CollaborationPosts_OntologyId",
                table: "CollaborationPosts",
                column: "OntologyId");

            migrationBuilder.CreateIndex(
                name: "IX_CollaborationResponse_PostId",
                table: "CollaborationResponses",
                column: "CollaborationPostId");

            migrationBuilder.CreateIndex(
                name: "IX_CollaborationResponse_UserId_PostId",
                table: "CollaborationResponses",
                columns: new[] { "UserId", "CollaborationPostId" });

            migrationBuilder.CreateIndex(
                name: "IX_CommentMention_MentionedUserId",
                table: "CommentMentions",
                column: "MentionedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CommentMention_MentionedUserId_HasViewed",
                table: "CommentMentions",
                columns: new[] { "MentionedUserId", "HasViewed" });

            migrationBuilder.CreateIndex(
                name: "IX_CommentMentions_CommentId",
                table: "CommentMentions",
                column: "CommentId");

            migrationBuilder.CreateIndex(
                name: "IX_ConceptGroups_OntologyId",
                table: "ConceptGroups",
                column: "OntologyId");

            migrationBuilder.CreateIndex(
                name: "IX_ConceptGroups_OntologyId_UserId",
                table: "ConceptGroups",
                columns: new[] { "OntologyId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_ConceptGroups_ParentConceptId",
                table: "ConceptGroups",
                column: "ParentConceptId");

            migrationBuilder.CreateIndex(
                name: "IX_ConceptGroups_UserId",
                table: "ConceptGroups",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ConceptProperties_RangeConceptId",
                table: "ConceptProperties",
                column: "RangeConceptId");

            migrationBuilder.CreateIndex(
                name: "IX_ConceptProperty_ConceptId",
                table: "ConceptProperties",
                column: "ConceptId");

            migrationBuilder.CreateIndex(
                name: "IX_ConceptRestriction_ConceptId",
                table: "ConceptRestrictions",
                column: "ConceptId");

            migrationBuilder.CreateIndex(
                name: "IX_ConceptRestrictions_AllowedConceptId",
                table: "ConceptRestrictions",
                column: "AllowedConceptId");

            migrationBuilder.CreateIndex(
                name: "IX_Concept_OntologyId",
                table: "Concepts",
                column: "OntologyId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomConceptTemplates_OntologyId",
                table: "CustomConceptTemplates",
                column: "OntologyId");

            migrationBuilder.CreateIndex(
                name: "IX_EntityCommentCount_OntologyId_EntityType_EntityId",
                table: "EntityCommentCounts",
                columns: new[] { "OntologyId", "EntityType", "EntityId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EntityComment_OntologyId_EntityType_EntityId",
                table: "EntityComments",
                columns: new[] { "OntologyId", "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_EntityComment_ParentCommentId",
                table: "EntityComments",
                column: "ParentCommentId");

            migrationBuilder.CreateIndex(
                name: "IX_EntityComment_UserId",
                table: "EntityComments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_FeatureToggles_Key",
                table: "FeatureToggles",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GuestSession_OntologyShareId",
                table: "GuestSessions",
                column: "OntologyShareId");

            migrationBuilder.CreateIndex(
                name: "IX_GuestSessions_SessionToken",
                table: "GuestSessions",
                column: "SessionToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IndividualProperties_IndividualId",
                table: "IndividualProperties",
                column: "IndividualId");

            migrationBuilder.CreateIndex(
                name: "IX_IndividualRelationships_OntologyId",
                table: "IndividualRelationships",
                column: "OntologyId");

            migrationBuilder.CreateIndex(
                name: "IX_IndividualRelationships_SourceIndividualId",
                table: "IndividualRelationships",
                column: "SourceIndividualId");

            migrationBuilder.CreateIndex(
                name: "IX_IndividualRelationships_TargetIndividualId",
                table: "IndividualRelationships",
                column: "TargetIndividualId");

            migrationBuilder.CreateIndex(
                name: "IX_Individual_OntologyId",
                table: "Individuals",
                column: "OntologyId");

            migrationBuilder.CreateIndex(
                name: "IX_Individuals_ConceptId",
                table: "Individuals",
                column: "ConceptId");

            migrationBuilder.CreateIndex(
                name: "IX_MergeRequestChange_MergeRequestId",
                table: "MergeRequestChanges",
                column: "MergeRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_MergeRequestComment_MergeRequestId",
                table: "MergeRequestComments",
                column: "MergeRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_MergeRequestComment_MergeRequestId_CreatedAt",
                table: "MergeRequestComments",
                columns: new[] { "MergeRequestId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MergeRequestComments_MergeRequestChangeId",
                table: "MergeRequestComments",
                column: "MergeRequestChangeId");

            migrationBuilder.CreateIndex(
                name: "IX_MergeRequestComments_UserId",
                table: "MergeRequestComments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_MergeRequest_AssignedReviewerUserId",
                table: "MergeRequests",
                column: "AssignedReviewerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MergeRequest_CreatedByUserId",
                table: "MergeRequests",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MergeRequest_OntologyId",
                table: "MergeRequests",
                column: "OntologyId");

            migrationBuilder.CreateIndex(
                name: "IX_MergeRequest_OntologyId_Status",
                table: "MergeRequests",
                columns: new[] { "OntologyId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_MergeRequests_ReviewedByUserId",
                table: "MergeRequests",
                column: "ReviewedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_NoteLink_SourceNoteId",
                table: "NoteLinks",
                column: "SourceNoteId");

            migrationBuilder.CreateIndex(
                name: "IX_NoteLink_SourceNoteId_TargetConceptId",
                table: "NoteLinks",
                columns: new[] { "SourceNoteId", "TargetConceptId" });

            migrationBuilder.CreateIndex(
                name: "IX_NoteLink_TargetConceptId",
                table: "NoteLinks",
                column: "TargetConceptId");

            migrationBuilder.CreateIndex(
                name: "IX_NoteLinks_NoteId",
                table: "NoteLinks",
                column: "NoteId");

            migrationBuilder.CreateIndex(
                name: "IX_Note_IsConceptNote",
                table: "Notes",
                column: "IsConceptNote");

            migrationBuilder.CreateIndex(
                name: "IX_Note_LinkedConceptId",
                table: "Notes",
                column: "LinkedConceptId",
                unique: true,
                filter: "[LinkedConceptId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Note_WorkspaceId",
                table: "Notes",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_Note_WorkspaceId_IsConceptNote",
                table: "Notes",
                columns: new[] { "WorkspaceId", "IsConceptNote" });

            migrationBuilder.CreateIndex(
                name: "IX_Notes_UserId",
                table: "Notes",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Ontologies_ParentOntologyId",
                table: "Ontologies",
                column: "ParentOntologyId");

            migrationBuilder.CreateIndex(
                name: "IX_Ontologies_UserId",
                table: "Ontologies",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Ontologies_WorkspaceId",
                table: "Ontologies",
                column: "WorkspaceId",
                unique: true,
                filter: "[WorkspaceId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OntologyActivity_OntologyId_CreatedAt",
                table: "OntologyActivities",
                columns: new[] { "OntologyId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_OntologyActivity_OntologyId_VersionNumber",
                table: "OntologyActivities",
                columns: new[] { "OntologyId", "VersionNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_OntologyActivity_UserId",
                table: "OntologyActivities",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_OntologyGroupPermission_OntologyId_GroupId",
                table: "OntologyGroupPermissions",
                columns: new[] { "OntologyId", "UserGroupId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OntologyGroupPermissions_GrantedByUserId",
                table: "OntologyGroupPermissions",
                column: "GrantedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OntologyGroupPermissions_UserGroupId",
                table: "OntologyGroupPermissions",
                column: "UserGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_OntologyLinks_LinkedOntologyId",
                table: "OntologyLinks",
                column: "LinkedOntologyId");

            migrationBuilder.CreateIndex(
                name: "IX_OntologyLinks_OntologyId",
                table: "OntologyLinks",
                column: "OntologyId");

            migrationBuilder.CreateIndex(
                name: "IX_OntologyLinks_OntologyId_LinkType",
                table: "OntologyLinks",
                columns: new[] { "OntologyId", "LinkType" });

            migrationBuilder.CreateIndex(
                name: "IX_OntologyShare_OntologyId",
                table: "OntologyShares",
                column: "OntologyId");

            migrationBuilder.CreateIndex(
                name: "IX_OntologyShares_CreatedByUserId",
                table: "OntologyShares",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OntologyShares_ShareToken",
                table: "OntologyShares",
                column: "ShareToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OntologyTag_OntologyId_Tag",
                table: "OntologyTags",
                columns: new[] { "OntologyId", "Tag" });

            migrationBuilder.CreateIndex(
                name: "IX_OntologyTag_Tag",
                table: "OntologyTags",
                column: "Tag");

            migrationBuilder.CreateIndex(
                name: "IX_OntologyViewHistory_OntologyId_UserId",
                table: "OntologyViewHistories",
                columns: new[] { "OntologyId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OntologyViewHistory_UserId",
                table: "OntologyViewHistories",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Properties_ConceptId",
                table: "Properties",
                column: "ConceptId");

            migrationBuilder.CreateIndex(
                name: "IX_Relationship_OntologyId",
                table: "Relationships",
                column: "OntologyId");

            migrationBuilder.CreateIndex(
                name: "IX_Relationships_SourceConceptId",
                table: "Relationships",
                column: "SourceConceptId");

            migrationBuilder.CreateIndex(
                name: "IX_Relationships_TargetConceptId",
                table: "Relationships",
                column: "TargetConceptId");

            migrationBuilder.CreateIndex(
                name: "IX_UserGroupMember_GroupId_UserId",
                table: "UserGroupMembers",
                columns: new[] { "UserGroupId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserGroupMembers_AddedByUserId",
                table: "UserGroupMembers",
                column: "AddedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserGroupMembers_UserId",
                table: "UserGroupMembers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserGroups_CreatedByUserId",
                table: "UserGroups",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserGroups_Name",
                table: "UserGroups",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserPreferences_UserId",
                table: "UserPreferences",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserShareAccess_OntologyShareId",
                table: "UserShareAccesses",
                column: "OntologyShareId");

            migrationBuilder.CreateIndex(
                name: "IX_UserShareAccesses_UserId_OntologyShareId",
                table: "UserShareAccesses",
                columns: new[] { "UserId", "OntologyShareId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkspaceGroupPermission_WorkspaceId_UserGroupId",
                table: "WorkspaceGroupPermissions",
                columns: new[] { "WorkspaceId", "UserGroupId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkspaceGroupPermissions_UserGroupId",
                table: "WorkspaceGroupPermissions",
                column: "UserGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Workspace_UserId",
                table: "Workspaces",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Workspace_UserId_Name",
                table: "Workspaces",
                columns: new[] { "UserId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_Workspace_Visibility",
                table: "Workspaces",
                column: "Visibility");

            migrationBuilder.CreateIndex(
                name: "IX_WorkspaceUserAccess_WorkspaceId_SharedWithUserId",
                table: "WorkspaceUserAccesses",
                columns: new[] { "WorkspaceId", "SharedWithUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkspaceUserAccesses_SharedWithUserId",
                table: "WorkspaceUserAccesses",
                column: "SharedWithUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "CollaborationResponses");

            migrationBuilder.DropTable(
                name: "CommentMentions");

            migrationBuilder.DropTable(
                name: "ConceptGroups");

            migrationBuilder.DropTable(
                name: "ConceptProperties");

            migrationBuilder.DropTable(
                name: "ConceptRestrictions");

            migrationBuilder.DropTable(
                name: "CustomConceptTemplates");

            migrationBuilder.DropTable(
                name: "EntityCommentCounts");

            migrationBuilder.DropTable(
                name: "FeatureToggles");

            migrationBuilder.DropTable(
                name: "GuestSessions");

            migrationBuilder.DropTable(
                name: "IndividualProperties");

            migrationBuilder.DropTable(
                name: "IndividualRelationships");

            migrationBuilder.DropTable(
                name: "MergeRequestComments");

            migrationBuilder.DropTable(
                name: "NoteContents");

            migrationBuilder.DropTable(
                name: "NoteLinks");

            migrationBuilder.DropTable(
                name: "OntologyActivities");

            migrationBuilder.DropTable(
                name: "OntologyGroupPermissions");

            migrationBuilder.DropTable(
                name: "OntologyLinks");

            migrationBuilder.DropTable(
                name: "OntologyTags");

            migrationBuilder.DropTable(
                name: "OntologyViewHistories");

            migrationBuilder.DropTable(
                name: "Properties");

            migrationBuilder.DropTable(
                name: "Relationships");

            migrationBuilder.DropTable(
                name: "UserGroupMembers");

            migrationBuilder.DropTable(
                name: "UserPreferences");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "UserShareAccesses");

            migrationBuilder.DropTable(
                name: "WorkspaceGroupPermissions");

            migrationBuilder.DropTable(
                name: "WorkspaceUserAccesses");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "CollaborationPosts");

            migrationBuilder.DropTable(
                name: "EntityComments");

            migrationBuilder.DropTable(
                name: "Individuals");

            migrationBuilder.DropTable(
                name: "MergeRequestChanges");

            migrationBuilder.DropTable(
                name: "Notes");

            migrationBuilder.DropTable(
                name: "OntologyShares");

            migrationBuilder.DropTable(
                name: "UserGroups");

            migrationBuilder.DropTable(
                name: "MergeRequests");

            migrationBuilder.DropTable(
                name: "Concepts");

            migrationBuilder.DropTable(
                name: "Ontologies");

            migrationBuilder.DropTable(
                name: "Workspaces");

            migrationBuilder.DropTable(
                name: "AspNetUsers");
        }
    }
}
