using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Eidos.Models;

namespace Eidos.Data
{
    public class OntologyDbContext : IdentityDbContext<ApplicationUser>
    {
        public OntologyDbContext(DbContextOptions<OntologyDbContext> options)
            : base(options)
        {
        }

        // Keep old User table for backward compatibility during migration
        public DbSet<User> LegacyUsers { get; set; }
        public DbSet<Ontology> Ontologies { get; set; }
        public DbSet<Concept> Concepts { get; set; }
        public DbSet<Relationship> Relationships { get; set; }
        public DbSet<Property> Properties { get; set; }
        public DbSet<ConceptProperty> ConceptProperties { get; set; } // OWL property definitions
        public DbSet<CustomConceptTemplate> CustomConceptTemplates { get; set; }
        public DbSet<OntologyLink> OntologyLinks { get; set; }
        public DbSet<FeatureToggle> FeatureToggles { get; set; }

        // Individual instances (OWL Named Individuals)
        public DbSet<Individual> Individuals { get; set; }
        public DbSet<IndividualProperty> IndividualProperties { get; set; }
        public DbSet<IndividualRelationship> IndividualRelationships { get; set; }

        // Concept restrictions (OWL Restrictions)
        public DbSet<ConceptRestriction> ConceptRestrictions { get; set; }

        // Collaborative sharing tables
        public DbSet<OntologyShare> OntologyShares { get; set; }
        public DbSet<GuestSession> GuestSessions { get; set; }
        public DbSet<UserShareAccess> UserShareAccesses { get; set; }

        // User preferences
        public DbSet<UserPreferences> UserPreferences { get; set; }

        // Activity tracking and version control
        public DbSet<OntologyActivity> OntologyActivities { get; set; }

        // User groups and permissions
        public DbSet<UserGroup> UserGroups { get; set; }
        public DbSet<UserGroupMember> UserGroupMembers { get; set; }
        public DbSet<OntologyGroupPermission> OntologyGroupPermissions { get; set; }

        // Collaboration bulletin board
        public DbSet<CollaborationPost> CollaborationPosts { get; set; }
        public DbSet<CollaborationResponse> CollaborationResponses { get; set; }

        // Ontology organization (tags/folders)
        public DbSet<OntologyTag> OntologyTags { get; set; }

        // Merge request approval workflow
        public DbSet<MergeRequest> MergeRequests { get; set; }
        public DbSet<MergeRequestChange> MergeRequestChanges { get; set; }
        public DbSet<MergeRequestComment> MergeRequestComments { get; set; }

        // Ontology view tracking for "What's New" feature
        public DbSet<OntologyViewHistory> OntologyViewHistories { get; set; }

        // Entity commenting system
        public DbSet<EntityComment> EntityComments { get; set; }
        public DbSet<CommentMention> CommentMentions { get; set; }
        public DbSet<EntityCommentCount> EntityCommentCounts { get; set; }

        // Concept grouping for graph view (UI-only feature)
        public DbSet<ConceptGroup> ConceptGroups { get; set; }

        // Workspace and Notes (Obsidian-style knowledge management)
        public DbSet<Workspace> Workspaces { get; set; }
        public DbSet<Note> Notes { get; set; }
        public DbSet<NoteContent> NoteContents { get; set; }
        public DbSet<NoteLink> NoteLinks { get; set; }
        public DbSet<WorkspaceGroupPermission> WorkspaceGroupPermissions { get; set; }
        public DbSet<WorkspaceUserAccess> WorkspaceUserAccesses { get; set; }

        // Tags for organizing notes
        public DbSet<Tag> Tags { get; set; }
        public DbSet<NoteTagAssignment> NoteTagAssignments { get; set; }

        // Note attachments (images in markdown)
        public DbSet<NoteAttachment> NoteAttachments { get; set; }

        // Shared ontology user state (pin/hide/dismiss for "Shared with Me")
        public DbSet<SharedOntologyUserState> SharedOntologyUserStates { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Legacy User table
            modelBuilder.Entity<User>()
                .ToTable("Users") // Keep the old table name
                .HasIndex(u => u.Username)
                .IsUnique();

            // Configure ApplicationUser (Identity)
            modelBuilder.Entity<ApplicationUser>()
                .ToTable("AspNetUsers"); // Standard Identity table name

            // Configure FeatureToggle
            modelBuilder.Entity<FeatureToggle>()
                .HasIndex(f => f.Key)
                .IsUnique();

            // Configure Ontology - ApplicationUser relationship
            modelBuilder.Entity<Ontology>()
                .HasOne(o => o.User)
                .WithMany(u => u.Ontologies)
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure Ontology - Parent/Child provenance relationship (self-referencing)
            modelBuilder.Entity<Ontology>()
                .HasOne(o => o.ParentOntology)
                .WithMany(o => o.ChildOntologies)
                .HasForeignKey(o => o.ParentOntologyId)
                .OnDelete(DeleteBehavior.Restrict); // Don't cascade delete - keep child ontologies if parent is deleted

            // Configure Concept
            modelBuilder.Entity<Concept>()
                .HasOne(c => c.Ontology)
                .WithMany(o => o.Concepts)
                .HasForeignKey(c => c.OntologyId)
                .OnDelete(DeleteBehavior.Cascade);

            // PERFORMANCE: Add index on OntologyId for efficient concept queries
            modelBuilder.Entity<Concept>()
                .HasIndex(c => c.OntologyId)
                .HasDatabaseName("IX_Concept_OntologyId");

            // Configure Relationship - Source
            modelBuilder.Entity<Relationship>()
                .HasOne(r => r.SourceConcept)
                .WithMany(c => c.RelationshipsAsSource)
                .HasForeignKey(r => r.SourceConceptId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure Relationship - Target
            modelBuilder.Entity<Relationship>()
                .HasOne(r => r.TargetConcept)
                .WithMany(c => c.RelationshipsAsTarget)
                .HasForeignKey(r => r.TargetConceptId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure Relationship - Ontology
            modelBuilder.Entity<Relationship>()
                .HasOne(r => r.Ontology)
                .WithMany(o => o.Relationships)
                .HasForeignKey(r => r.OntologyId)
                .OnDelete(DeleteBehavior.Cascade);

            // PERFORMANCE: Add index on OntologyId for efficient relationship queries
            modelBuilder.Entity<Relationship>()
                .HasIndex(r => r.OntologyId)
                .HasDatabaseName("IX_Relationship_OntologyId");

            // Configure Relationship - Strength decimal precision for SQL Server
            modelBuilder.Entity<Relationship>()
                .Property(r => r.Strength)
                .HasPrecision(18, 2);

            // Configure Property
            modelBuilder.Entity<Property>()
                .HasOne(p => p.Concept)
                .WithMany(c => c.Properties)
                .HasForeignKey(p => p.ConceptId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure ConceptProperty - Concept (domain)
            modelBuilder.Entity<ConceptProperty>()
                .HasOne(p => p.Concept)
                .WithMany(c => c.ConceptProperties)
                .HasForeignKey(p => p.ConceptId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure ConceptProperty - RangeConcept (for ObjectProperty type)
            modelBuilder.Entity<ConceptProperty>()
                .HasOne(p => p.RangeConcept)
                .WithMany()
                .HasForeignKey(p => p.RangeConceptId)
                .OnDelete(DeleteBehavior.Restrict);

            // PERFORMANCE: Add index on ConceptId for efficient property queries
            modelBuilder.Entity<ConceptProperty>()
                .HasIndex(p => p.ConceptId)
                .HasDatabaseName("IX_ConceptProperty_ConceptId");

            // Configure CustomConceptTemplate
            modelBuilder.Entity<CustomConceptTemplate>()
                .HasOne(t => t.Ontology)
                .WithMany(o => o.CustomTemplates)
                .HasForeignKey(t => t.OntologyId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure OntologyLink
            modelBuilder.Entity<OntologyLink>(entity =>
            {
                // Parent ontology relationship
                entity.HasOne(l => l.Ontology)
                    .WithMany(o => o.LinkedOntologies)
                    .HasForeignKey(l => l.OntologyId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Linked ontology relationship (for Internal links)
                entity.HasOne(l => l.LinkedOntology)
                    .WithMany() // No inverse navigation
                    .HasForeignKey(l => l.LinkedOntologyId)
                    .OnDelete(DeleteBehavior.NoAction); // If linked ontology deleted, manual cleanup needed

                // Indexes for performance
                entity.HasIndex(l => l.OntologyId);
                entity.HasIndex(l => l.LinkedOntologyId);
                entity.HasIndex(l => new { l.OntologyId, l.LinkType });

                // Validation: must have either Uri (External) or LinkedOntologyId (Internal)
                entity.ToTable(t => t.HasCheckConstraint(
                    "CK_OntologyLink_HasTarget",
                    "(LinkType = 0 AND Uri IS NOT NULL) OR (LinkType = 1 AND LinkedOntologyId IS NOT NULL)"));

                // Prevent linking ontology to itself
                entity.ToTable(t => t.HasCheckConstraint(
                    "CK_OntologyLink_NoSelfReference",
                    "OntologyId <> LinkedOntologyId OR LinkedOntologyId IS NULL"));
            });

            // Configure Individual - Ontology
            modelBuilder.Entity<Individual>()
                .HasOne(i => i.Ontology)
                .WithMany(o => o.Individuals)
                .HasForeignKey(i => i.OntologyId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure Individual - Concept (what class this individual instantiates)
            modelBuilder.Entity<Individual>()
                .HasOne(i => i.Concept)
                .WithMany()
                .HasForeignKey(i => i.ConceptId)
                .OnDelete(DeleteBehavior.Restrict);

            // PERFORMANCE: Add index on OntologyId for efficient individual queries
            modelBuilder.Entity<Individual>()
                .HasIndex(i => i.OntologyId)
                .HasDatabaseName("IX_Individual_OntologyId");

            // Configure IndividualProperty
            modelBuilder.Entity<IndividualProperty>()
                .HasOne(p => p.Individual)
                .WithMany(i => i.Properties)
                .HasForeignKey(p => p.IndividualId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure IndividualRelationship - Source
            modelBuilder.Entity<IndividualRelationship>()
                .HasOne(r => r.SourceIndividual)
                .WithMany(i => i.RelationshipsAsSource)
                .HasForeignKey(r => r.SourceIndividualId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure IndividualRelationship - Target
            modelBuilder.Entity<IndividualRelationship>()
                .HasOne(r => r.TargetIndividual)
                .WithMany(i => i.RelationshipsAsTarget)
                .HasForeignKey(r => r.TargetIndividualId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure IndividualRelationship - Ontology
            modelBuilder.Entity<IndividualRelationship>()
                .HasOne(r => r.Ontology)
                .WithMany(o => o.IndividualRelationships)
                .HasForeignKey(r => r.OntologyId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure ConceptRestriction - Concept
            modelBuilder.Entity<ConceptRestriction>()
                .HasOne(r => r.Concept)
                .WithMany(c => c.Restrictions)
                .HasForeignKey(r => r.ConceptId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure ConceptRestriction - AllowedConcept (optional foreign key for concept type restrictions)
            modelBuilder.Entity<ConceptRestriction>()
                .HasOne(r => r.AllowedConcept)
                .WithMany()
                .HasForeignKey(r => r.AllowedConceptId)
                .OnDelete(DeleteBehavior.Restrict);

            // PERFORMANCE: Add index on ConceptId for efficient restriction queries
            modelBuilder.Entity<ConceptRestriction>()
                .HasIndex(r => r.ConceptId)
                .HasDatabaseName("IX_ConceptRestriction_ConceptId");

            // Configure OntologyShare
            modelBuilder.Entity<OntologyShare>()
                .HasOne(s => s.Ontology)
                .WithMany()
                .HasForeignKey(s => s.OntologyId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OntologyShare>()
                .HasOne(s => s.CreatedBy)
                .WithMany()
                .HasForeignKey(s => s.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Create unique index on ShareToken for security and performance
            modelBuilder.Entity<OntologyShare>()
                .HasIndex(s => s.ShareToken)
                .IsUnique();

            // PERFORMANCE: Add index on OntologyId for queries finding shares for an ontology
            modelBuilder.Entity<OntologyShare>()
                .HasIndex(s => s.OntologyId)
                .HasDatabaseName("IX_OntologyShare_OntologyId");

            // Configure GuestSession
            modelBuilder.Entity<GuestSession>()
                .HasOne(g => g.OntologyShare)
                .WithMany(s => s.GuestSessions)
                .HasForeignKey(g => g.OntologyShareId)
                .OnDelete(DeleteBehavior.Cascade);

            // Create unique index on SessionToken
            modelBuilder.Entity<GuestSession>()
                .HasIndex(g => g.SessionToken)
                .IsUnique();

            // PERFORMANCE: Add index on OntologyShareId for efficient guest session queries
            modelBuilder.Entity<GuestSession>()
                .HasIndex(g => g.OntologyShareId)
                .HasDatabaseName("IX_GuestSession_OntologyShareId");

            // Configure UserShareAccess
            modelBuilder.Entity<UserShareAccess>()
                .HasOne(u => u.User)
                .WithMany()
                .HasForeignKey(u => u.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserShareAccess>()
                .HasOne(u => u.OntologyShare)
                .WithMany()
                .HasForeignKey(u => u.OntologyShareId)
                .OnDelete(DeleteBehavior.Cascade);

            // Create composite index on UserId and OntologyShareId for efficient lookups
            modelBuilder.Entity<UserShareAccess>()
                .HasIndex(u => new { u.UserId, u.OntologyShareId })
                .IsUnique();

            // PERFORMANCE: Add index on OntologyShareId for queries filtering by share
            modelBuilder.Entity<UserShareAccess>()
                .HasIndex(u => u.OntologyShareId)
                .HasDatabaseName("IX_UserShareAccess_OntologyShareId");

            // Configure UserPreferences
            modelBuilder.Entity<UserPreferences>()
                .HasOne(p => p.User)
                .WithOne(u => u.Preferences)
                .HasForeignKey<UserPreferences>(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Create unique index on UserId for UserPreferences (one-to-one)
            modelBuilder.Entity<UserPreferences>()
                .HasIndex(p => p.UserId)
                .IsUnique();

            // Configure OntologyActivity - Ontology
            modelBuilder.Entity<OntologyActivity>()
                .HasOne(a => a.Ontology)
                .WithMany()
                .HasForeignKey(a => a.OntologyId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure OntologyActivity - User (optional, null for guest users)
            modelBuilder.Entity<OntologyActivity>()
                .HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.NoAction); // Keep activity record even if user is deleted

            // Create indexes on OntologyActivity for efficient querying
            modelBuilder.Entity<OntologyActivity>()
                .HasIndex(a => new { a.OntologyId, a.CreatedAt })
                .HasDatabaseName("IX_OntologyActivity_OntologyId_CreatedAt");

            modelBuilder.Entity<OntologyActivity>()
                .HasIndex(a => a.UserId)
                .HasDatabaseName("IX_OntologyActivity_UserId");

            modelBuilder.Entity<OntologyActivity>()
                .HasIndex(a => new { a.OntologyId, a.VersionNumber })
                .HasDatabaseName("IX_OntologyActivity_OntologyId_VersionNumber");

            // Configure UserGroup
            modelBuilder.Entity<UserGroup>(entity =>
            {
                entity.HasIndex(g => g.Name).IsUnique();

                entity.HasOne(g => g.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(g => g.CreatedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure UserGroupMember
            modelBuilder.Entity<UserGroupMember>(entity =>
            {
                // Create composite unique index to prevent duplicate memberships
                entity.HasIndex(m => new { m.UserGroupId, m.UserId })
                    .IsUnique()
                    .HasDatabaseName("IX_UserGroupMember_GroupId_UserId");

                entity.HasOne(m => m.UserGroup)
                    .WithMany(g => g.Members)
                    .HasForeignKey(m => m.UserGroupId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Changed to Restrict to avoid cascade cycles with UserGroups -> AspNetUsers
                entity.HasOne(m => m.User)
                    .WithMany()
                    .HasForeignKey(m => m.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(m => m.AddedByUser)
                    .WithMany()
                    .HasForeignKey(m => m.AddedByUserId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            // Configure OntologyGroupPermission
            modelBuilder.Entity<OntologyGroupPermission>(entity =>
            {
                // Create composite unique index to prevent duplicate permissions
                entity.HasIndex(p => new { p.OntologyId, p.UserGroupId })
                    .IsUnique()
                    .HasDatabaseName("IX_OntologyGroupPermission_OntologyId_GroupId");

                entity.HasOne(p => p.Ontology)
                    .WithMany(o => o.GroupPermissions)
                    .HasForeignKey(p => p.OntologyId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(p => p.UserGroup)
                    .WithMany(g => g.OntologyPermissions)
                    .HasForeignKey(p => p.UserGroupId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(p => p.GrantedByUser)
                    .WithMany()
                    .HasForeignKey(p => p.GrantedByUserId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            // Configure CollaborationPost
            modelBuilder.Entity<CollaborationPost>(entity =>
            {
                entity.HasOne(p => p.User)
                    .WithMany()
                    .HasForeignKey(p => p.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(p => p.Ontology)
                    .WithMany()
                    .HasForeignKey(p => p.OntologyId)
                    .OnDelete(DeleteBehavior.NoAction); // Keep post even if ontology is deleted

                entity.HasOne(p => p.CollaborationProjectGroup)
                    .WithMany()
                    .HasForeignKey(p => p.CollaborationProjectGroupId)
                    .OnDelete(DeleteBehavior.NoAction); // Keep post even if group is deleted

                // Add indexes for efficient querying
                entity.HasIndex(p => p.IsActive)
                    .HasDatabaseName("IX_CollaborationPost_IsActive");

                entity.HasIndex(p => new { p.IsActive, p.CreatedAt })
                    .HasDatabaseName("IX_CollaborationPost_IsActive_CreatedAt");

                entity.HasIndex(p => p.Domain)
                    .HasDatabaseName("IX_CollaborationPost_Domain");

                entity.HasIndex(p => p.UserId)
                    .HasDatabaseName("IX_CollaborationPost_UserId");
            });

            // Configure CollaborationResponse
            modelBuilder.Entity<CollaborationResponse>(entity =>
            {
                entity.HasOne(r => r.CollaborationPost)
                    .WithMany(p => p.Responses)
                    .HasForeignKey(r => r.CollaborationPostId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(r => r.User)
                    .WithMany()
                    .HasForeignKey(r => r.UserId)
                    .OnDelete(DeleteBehavior.Restrict); // Avoid cascade path conflict with CollaborationPost->User

                // Add index for efficient querying
                entity.HasIndex(r => r.CollaborationPostId)
                    .HasDatabaseName("IX_CollaborationResponse_PostId");

                entity.HasIndex(r => new { r.UserId, r.CollaborationPostId })
                    .HasDatabaseName("IX_CollaborationResponse_UserId_PostId");
            });

            // Configure OntologyTag
            modelBuilder.Entity<OntologyTag>(entity =>
            {
                entity.HasOne(t => t.Ontology)
                    .WithMany(o => o.OntologyTags)
                    .HasForeignKey(t => t.OntologyId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Create composite index on OntologyId and Tag for efficient queries
                entity.HasIndex(t => new { t.OntologyId, t.Tag })
                    .HasDatabaseName("IX_OntologyTag_OntologyId_Tag");

                // Create index on Tag for finding all ontologies with a specific tag/folder
                entity.HasIndex(t => t.Tag)
                    .HasDatabaseName("IX_OntologyTag_Tag");
            });

            // Configure ConceptGroup
            modelBuilder.Entity<ConceptGroup>(entity =>
            {
                // Ontology relationship - NoAction to avoid circular cascade
                entity.HasOne(cg => cg.Ontology)
                    .WithMany()
                    .HasForeignKey(cg => cg.OntologyId)
                    .OnDelete(DeleteBehavior.NoAction);

                // User relationship - NoAction to avoid circular cascade
                entity.HasOne(cg => cg.User)
                    .WithMany()
                    .HasForeignKey(cg => cg.UserId)
                    .OnDelete(DeleteBehavior.NoAction);

                // Parent Concept relationship - NoAction to avoid circular cascade
                entity.HasOne(cg => cg.ParentConcept)
                    .WithMany()
                    .HasForeignKey(cg => cg.ParentConceptId)
                    .OnDelete(DeleteBehavior.NoAction);

                // Indexes for efficient queries
                entity.HasIndex(cg => cg.OntologyId);
                entity.HasIndex(cg => cg.UserId);
                entity.HasIndex(cg => cg.ParentConceptId);
                entity.HasIndex(cg => new { cg.OntologyId, cg.UserId });
            });

            // Configure MergeRequest
            modelBuilder.Entity<MergeRequest>(entity =>
            {
                // Ontology relationship
                entity.HasOne(mr => mr.Ontology)
                    .WithMany()
                    .HasForeignKey(mr => mr.OntologyId)
                    .OnDelete(DeleteBehavior.Cascade);

                // CreatedBy relationship
                entity.HasOne(mr => mr.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(mr => mr.CreatedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                // AssignedReviewer relationship (optional)
                entity.HasOne(mr => mr.AssignedReviewer)
                    .WithMany()
                    .HasForeignKey(mr => mr.AssignedReviewerUserId)
                    .OnDelete(DeleteBehavior.NoAction);

                // ReviewedBy relationship (optional)
                entity.HasOne(mr => mr.ReviewedBy)
                    .WithMany()
                    .HasForeignKey(mr => mr.ReviewedByUserId)
                    .OnDelete(DeleteBehavior.NoAction);

                // Indexes for efficient queries
                entity.HasIndex(mr => mr.OntologyId)
                    .HasDatabaseName("IX_MergeRequest_OntologyId");

                entity.HasIndex(mr => new { mr.OntologyId, mr.Status })
                    .HasDatabaseName("IX_MergeRequest_OntologyId_Status");

                entity.HasIndex(mr => mr.CreatedByUserId)
                    .HasDatabaseName("IX_MergeRequest_CreatedByUserId");

                entity.HasIndex(mr => mr.AssignedReviewerUserId)
                    .HasDatabaseName("IX_MergeRequest_AssignedReviewerUserId");
            });

            // Configure MergeRequestChange
            modelBuilder.Entity<MergeRequestChange>(entity =>
            {
                entity.HasOne(c => c.MergeRequest)
                    .WithMany(mr => mr.Changes)
                    .HasForeignKey(c => c.MergeRequestId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Index for efficient queries
                entity.HasIndex(c => c.MergeRequestId)
                    .HasDatabaseName("IX_MergeRequestChange_MergeRequestId");
            });

            // Configure MergeRequestComment
            modelBuilder.Entity<MergeRequestComment>(entity =>
            {
                entity.HasOne(c => c.MergeRequest)
                    .WithMany(mr => mr.Comments)
                    .HasForeignKey(c => c.MergeRequestId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(c => c.User)
                    .WithMany()
                    .HasForeignKey(c => c.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Optional reference to a specific change
                entity.HasOne(c => c.MergeRequestChange)
                    .WithMany()
                    .HasForeignKey(c => c.MergeRequestChangeId)
                    .OnDelete(DeleteBehavior.NoAction);

                // Indexes for efficient queries
                entity.HasIndex(c => c.MergeRequestId)
                    .HasDatabaseName("IX_MergeRequestComment_MergeRequestId");

                entity.HasIndex(c => new { c.MergeRequestId, c.CreatedAt })
                    .HasDatabaseName("IX_MergeRequestComment_MergeRequestId_CreatedAt");
            });

            // Configure OntologyViewHistory (for "What's New" feature)
            modelBuilder.Entity<OntologyViewHistory>(entity =>
            {
                entity.HasOne(v => v.Ontology)
                    .WithMany()
                    .HasForeignKey(v => v.OntologyId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(v => v.User)
                    .WithMany()
                    .HasForeignKey(v => v.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Composite unique index: one record per user per ontology
                entity.HasIndex(v => new { v.OntologyId, v.UserId })
                    .IsUnique()
                    .HasDatabaseName("IX_OntologyViewHistory_OntologyId_UserId");

                // Index for querying by user
                entity.HasIndex(v => v.UserId)
                    .HasDatabaseName("IX_OntologyViewHistory_UserId");
            });

            // Configure EntityComment (in-context commenting)
            modelBuilder.Entity<EntityComment>(entity =>
            {
                entity.HasOne(c => c.Ontology)
                    .WithMany()
                    .HasForeignKey(c => c.OntologyId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(c => c.User)
                    .WithMany()
                    .HasForeignKey(c => c.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Self-referencing relationship for threaded comments
                entity.HasOne(c => c.ParentComment)
                    .WithMany(c => c.Replies)
                    .HasForeignKey(c => c.ParentCommentId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Composite index for entity lookup
                entity.HasIndex(c => new { c.OntologyId, c.EntityType, c.EntityId })
                    .HasDatabaseName("IX_EntityComment_OntologyId_EntityType_EntityId");

                // Index for user's comments
                entity.HasIndex(c => c.UserId)
                    .HasDatabaseName("IX_EntityComment_UserId");

                // Index for parent comment (efficient reply queries)
                entity.HasIndex(c => c.ParentCommentId)
                    .HasDatabaseName("IX_EntityComment_ParentCommentId");
            });

            // Configure CommentMention
            modelBuilder.Entity<CommentMention>(entity =>
            {
                entity.HasOne(m => m.Comment)
                    .WithMany(c => c.Mentions)
                    .HasForeignKey(m => m.CommentId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(m => m.MentionedUser)
                    .WithMany()
                    .HasForeignKey(m => m.MentionedUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Index for finding mentions by user
                entity.HasIndex(m => m.MentionedUserId)
                    .HasDatabaseName("IX_CommentMention_MentionedUserId");

                // Composite index for unread mentions
                entity.HasIndex(m => new { m.MentionedUserId, m.HasViewed })
                    .HasDatabaseName("IX_CommentMention_MentionedUserId_HasViewed");
            });

            // Configure EntityCommentCount
            modelBuilder.Entity<EntityCommentCount>(entity =>
            {
                entity.HasOne(c => c.Ontology)
                    .WithMany()
                    .HasForeignKey(c => c.OntologyId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Composite unique index: one count per entity
                entity.HasIndex(c => new { c.OntologyId, c.EntityType, c.EntityId })
                    .IsUnique()
                    .HasDatabaseName("IX_EntityCommentCount_OntologyId_EntityType_EntityId");
            });

            // Seed feature toggles only
            // User seeding will be done via Identity registration
            SeedFeatureToggles(modelBuilder);
        }

        // Note: Example ontology seeding removed - users will create their own ontologies
        // Legacy user data from old Users table can be migrated via a data migration script

        /*
        private void SeedLegacyExampleOntology_Disabled(ModelBuilder modelBuilder)
        {
            // This method is disabled during Identity migration
            // We'll re-enable example data creation after migration is complete

            // OLD CODE - Create a simple "Animals" ontology to demonstrate the tool
            modelBuilder.Entity<Ontology>().HasData(
                new Ontology
                {
                    Id = 1,
                    UserId = "sample-user-id", // Would need to be a valid Identity user ID
                    Name = "Animal Kingdom (Example)",
                    Description = "A simple example ontology showing how living creatures are classified",
                    Author = "System",
                    Version = "1.0"
                }
            );

            // Add concepts
            modelBuilder.Entity<Concept>().HasData(
                new Concept
                {
                    Id = 1,
                    OntologyId = 1,
                    Name = "Animal",
                    Definition = "A living organism that feeds on organic matter",
                    SimpleExplanation = "Any living creature that needs to eat food to survive",
                    Examples = "Dogs, cats, birds, fish, insects",
                    Category = "Root",
                    Color = "#4A90E2",
                    PositionX = 400,
                    PositionY = 100
                },
                new Concept
                {
                    Id = 2,
                    OntologyId = 1,
                    Name = "Mammal",
                    Definition = "Warm-blooded vertebrate animal with hair or fur",
                    SimpleExplanation = "Animals that have fur and feed milk to their babies",
                    Examples = "Dogs, cats, elephants, whales",
                    Category = "Classification",
                    Color = "#E27D4A",
                    PositionX = 200,
                    PositionY = 250
                },
                new Concept
                {
                    Id = 3,
                    OntologyId = 1,
                    Name = "Bird",
                    Definition = "Warm-blooded egg-laying vertebrate with feathers and wings",
                    SimpleExplanation = "Animals with feathers that (usually) can fly",
                    Examples = "Sparrow, eagle, penguin, ostrich",
                    Category = "Classification",
                    Color = "#4AE28E",
                    PositionX = 600,
                    PositionY = 250
                },
                new Concept
                {
                    Id = 4,
                    OntologyId = 1,
                    Name = "Dog",
                    Definition = "Domesticated carnivorous mammal",
                    SimpleExplanation = "A common pet that barks and wags its tail",
                    Examples = "Golden Retriever, Poodle, German Shepherd",
                    Category = "Species",
                    Color = "#E24A90",
                    PositionX = 100,
                    PositionY = 400
                },
                new Concept
                {
                    Id = 5,
                    OntologyId = 1,
                    Name = "Cat",
                    Definition = "Small carnivorous mammal",
                    SimpleExplanation = "A common pet that meows and purrs",
                    Examples = "Persian, Siamese, Maine Coon",
                    Category = "Species",
                    Color = "#E24A90",
                    PositionX = 300,
                    PositionY = 400
                }
            );

            // Add relationships
            modelBuilder.Entity<Relationship>().HasData(
                new Relationship
                {
                    Id = 1,
                    OntologyId = 1,
                    SourceConceptId = 2, // Mammal
                    TargetConceptId = 1, // Animal
                    RelationType = "is-a",
                    Description = "Mammals are a type of animal",
                    Strength = 1.0m
                },
                new Relationship
                {
                    Id = 2,
                    OntologyId = 1,
                    SourceConceptId = 3, // Bird
                    TargetConceptId = 1, // Animal
                    RelationType = "is-a",
                    Description = "Birds are a type of animal",
                    Strength = 1.0m
                },
                new Relationship
                {
                    Id = 3,
                    OntologyId = 1,
                    SourceConceptId = 4, // Dog
                    TargetConceptId = 2, // Mammal
                    RelationType = "is-a",
                    Description = "Dogs are mammals",
                    Strength = 1.0m
                },
                new Relationship
                {
                    Id = 4,
                    OntologyId = 1,
                    SourceConceptId = 5, // Cat
                    TargetConceptId = 2, // Mammal
                    RelationType = "is-a",
                    Description = "Cats are mammals",
                    Strength = 1.0m
                }
            );
        }
        */

        private void SeedFeatureToggles(ModelBuilder modelBuilder)
        {
            // Use static dates for seeding to prevent non-deterministic model changes
            var seedDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            modelBuilder.Entity<FeatureToggle>().HasData(
                new FeatureToggle
                {
                    Id = 1,
                    Key = "show-test-users",
                    Name = "Show Test Users",
                    Description = "Display additional test users in the user selector for MVP testing",
                    Category = "User Management",
                    IsEnabled = true,
                    CreatedAt = seedDate,
                    UpdatedAt = seedDate
                },
                new FeatureToggle
                {
                    Id = 2,
                    Key = "enable-collaboration",
                    Name = "Enable Collaboration (Future)",
                    Description = "Allow multiple users to collaborate on the same ontology",
                    Category = "Collaboration",
                    IsEnabled = false,
                    CreatedAt = seedDate,
                    UpdatedAt = seedDate
                },
                new FeatureToggle
                {
                    Id = 3,
                    Key = "enable-real-time-sync",
                    Name = "Enable Real-time Sync (Future)",
                    Description = "Sync changes across users in real-time using SignalR",
                    Category = "Collaboration",
                    IsEnabled = false,
                    CreatedAt = seedDate,
                    UpdatedAt = seedDate
                }
            );

            // ===== WORKSPACE AND NOTES CONFIGURATION (Obsidian-style) =====

            // Configure Workspace - User relationship
            modelBuilder.Entity<Workspace>()
                .HasOne(w => w.User)
                .WithMany()
                .HasForeignKey(w => w.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure Workspace - Ontology (1:1 relationship)
            modelBuilder.Entity<Workspace>()
                .HasOne(w => w.Ontology)
                .WithOne(o => o.Workspace)
                .HasForeignKey<Ontology>(o => o.WorkspaceId)
                .OnDelete(DeleteBehavior.NoAction); // If workspace deleted, ontology becomes orphaned (for migration safety)

            // Configure Workspace indexes
            modelBuilder.Entity<Workspace>()
                .HasIndex(w => w.UserId)
                .HasDatabaseName("IX_Workspace_UserId");

            modelBuilder.Entity<Workspace>()
                .HasIndex(w => w.Visibility)
                .HasDatabaseName("IX_Workspace_Visibility");

            modelBuilder.Entity<Workspace>()
                .HasIndex(w => new { w.UserId, w.Name })
                .HasDatabaseName("IX_Workspace_UserId_Name");

            // Configure Note - Workspace relationship
            modelBuilder.Entity<Note>()
                .HasOne(n => n.Workspace)
                .WithMany(w => w.Notes)
                .HasForeignKey(n => n.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure Note - User relationship
            modelBuilder.Entity<Note>()
                .HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure Note - Concept (1:1 for concept notes)
            modelBuilder.Entity<Note>()
                .HasOne(n => n.LinkedConcept)
                .WithOne(c => c.ConceptNote)
                .HasForeignKey<Note>(n => n.LinkedConceptId)
                .OnDelete(DeleteBehavior.NoAction); // If concept deleted, note becomes regular note

            // Configure Note indexes
            modelBuilder.Entity<Note>()
                .HasIndex(n => n.WorkspaceId)
                .HasDatabaseName("IX_Note_WorkspaceId");

            modelBuilder.Entity<Note>()
                .HasIndex(n => n.LinkedConceptId)
                .HasDatabaseName("IX_Note_LinkedConceptId");

            modelBuilder.Entity<Note>()
                .HasIndex(n => n.IsConceptNote)
                .HasDatabaseName("IX_Note_IsConceptNote");

            modelBuilder.Entity<Note>()
                .HasIndex(n => new { n.WorkspaceId, n.IsConceptNote })
                .HasDatabaseName("IX_Note_WorkspaceId_IsConceptNote");

            // Configure NoteContent - Note relationship (1:1)
            modelBuilder.Entity<NoteContent>()
                .HasKey(nc => nc.NoteId); // NoteId is the primary key

            modelBuilder.Entity<NoteContent>()
                .HasOne(nc => nc.Note)
                .WithOne(n => n.Content)
                .HasForeignKey<NoteContent>(nc => nc.NoteId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure NoteLink - Source Note relationship
            modelBuilder.Entity<NoteLink>()
                .HasOne(nl => nl.SourceNote)
                .WithMany(n => n.OutgoingLinks)
                .HasForeignKey(nl => nl.SourceNoteId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure NoteLink - Target Concept relationship (for backlinks)
            modelBuilder.Entity<NoteLink>()
                .HasOne(nl => nl.TargetConcept)
                .WithMany(c => c.IncomingNoteLinks)
                .HasForeignKey(nl => nl.TargetConceptId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure NoteLink indexes
            modelBuilder.Entity<NoteLink>()
                .HasIndex(nl => nl.SourceNoteId)
                .HasDatabaseName("IX_NoteLink_SourceNoteId");

            modelBuilder.Entity<NoteLink>()
                .HasIndex(nl => nl.TargetConceptId)
                .HasDatabaseName("IX_NoteLink_TargetConceptId");

            modelBuilder.Entity<NoteLink>()
                .HasIndex(nl => new { nl.SourceNoteId, nl.TargetConceptId })
                .HasDatabaseName("IX_NoteLink_SourceNoteId_TargetConceptId");

            // Configure WorkspaceGroupPermission - Workspace relationship
            modelBuilder.Entity<WorkspaceGroupPermission>()
                .HasOne(wgp => wgp.Workspace)
                .WithMany(w => w.GroupPermissions)
                .HasForeignKey(wgp => wgp.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure WorkspaceGroupPermission - UserGroup relationship
            modelBuilder.Entity<WorkspaceGroupPermission>()
                .HasOne(wgp => wgp.UserGroup)
                .WithMany()
                .HasForeignKey(wgp => wgp.UserGroupId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure WorkspaceGroupPermission indexes
            modelBuilder.Entity<WorkspaceGroupPermission>()
                .HasIndex(wgp => new { wgp.WorkspaceId, wgp.UserGroupId })
                .IsUnique()
                .HasDatabaseName("IX_WorkspaceGroupPermission_WorkspaceId_UserGroupId");

            // Configure WorkspaceUserAccess - Workspace relationship
            modelBuilder.Entity<WorkspaceUserAccess>()
                .HasOne(wua => wua.Workspace)
                .WithMany(w => w.UserAccesses)
                .HasForeignKey(wua => wua.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure WorkspaceUserAccess - User relationship
            modelBuilder.Entity<WorkspaceUserAccess>()
                .HasOne(wua => wua.SharedWithUser)
                .WithMany()
                .HasForeignKey(wua => wua.SharedWithUserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure WorkspaceUserAccess indexes
            modelBuilder.Entity<WorkspaceUserAccess>()
                .HasIndex(wua => new { wua.WorkspaceId, wua.SharedWithUserId })
                .IsUnique()
                .HasDatabaseName("IX_WorkspaceUserAccess_WorkspaceId_SharedWithUserId");

            // Configure Tag - Workspace relationship
            modelBuilder.Entity<Tag>()
                .HasOne(t => t.Workspace)
                .WithMany(w => w.Tags)
                .HasForeignKey(t => t.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure Tag - User (Creator) relationship
            modelBuilder.Entity<Tag>()
                .HasOne(t => t.Creator)
                .WithMany()
                .HasForeignKey(t => t.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict); // Don't cascade delete tags when user is deleted

            // Configure Tag indexes
            modelBuilder.Entity<Tag>()
                .HasIndex(t => t.WorkspaceId)
                .HasDatabaseName("IX_Tag_WorkspaceId");

            modelBuilder.Entity<Tag>()
                .HasIndex(t => new { t.WorkspaceId, t.Name })
                .IsUnique()
                .HasDatabaseName("IX_Tag_WorkspaceId_Name");

            // Configure NoteTagAssignment - Note relationship
            modelBuilder.Entity<NoteTagAssignment>()
                .HasOne(nta => nta.Note)
                .WithMany(n => n.TagAssignments)
                .HasForeignKey(nta => nta.NoteId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure NoteTagAssignment - Tag relationship
            // Use Restrict to avoid multiple cascade paths (Workspace -> Tag -> NoteTagAssignment + Workspace -> Note -> NoteTagAssignment)
            modelBuilder.Entity<NoteTagAssignment>()
                .HasOne(nta => nta.Tag)
                .WithMany(t => t.NoteAssignments)
                .HasForeignKey(nta => nta.TagId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure NoteTagAssignment - User (Assigner) relationship
            modelBuilder.Entity<NoteTagAssignment>()
                .HasOne(nta => nta.Assigner)
                .WithMany()
                .HasForeignKey(nta => nta.AssignedBy)
                .OnDelete(DeleteBehavior.Restrict); // Don't cascade delete assignments when user is deleted

            // Configure NoteTagAssignment indexes
            modelBuilder.Entity<NoteTagAssignment>()
                .HasIndex(nta => nta.NoteId)
                .HasDatabaseName("IX_NoteTagAssignment_NoteId");

            modelBuilder.Entity<NoteTagAssignment>()
                .HasIndex(nta => nta.TagId)
                .HasDatabaseName("IX_NoteTagAssignment_TagId");

            modelBuilder.Entity<NoteTagAssignment>()
                .HasIndex(nta => new { nta.NoteId, nta.TagId })
                .IsUnique()
                .HasDatabaseName("IX_NoteTagAssignment_NoteId_TagId");

            // Configure SharedOntologyUserState - User relationship
            modelBuilder.Entity<SharedOntologyUserState>()
                .HasOne(s => s.User)
                .WithMany()
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure SharedOntologyUserState - Ontology relationship
            modelBuilder.Entity<SharedOntologyUserState>()
                .HasOne(s => s.Ontology)
                .WithMany()
                .HasForeignKey(s => s.OntologyId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure SharedOntologyUserState indexes
            modelBuilder.Entity<SharedOntologyUserState>()
                .HasIndex(s => s.UserId)
                .HasDatabaseName("IX_SharedOntologyUserState_UserId");

            modelBuilder.Entity<SharedOntologyUserState>()
                .HasIndex(s => s.OntologyId)
                .HasDatabaseName("IX_SharedOntologyUserState_OntologyId");

            modelBuilder.Entity<SharedOntologyUserState>()
                .HasIndex(s => new { s.UserId, s.OntologyId })
                .IsUnique()
                .HasDatabaseName("IX_SharedOntologyUserState_UserId_OntologyId");

            // Index for pinned queries
            modelBuilder.Entity<SharedOntologyUserState>()
                .HasIndex(s => new { s.UserId, s.IsPinned })
                .HasDatabaseName("IX_SharedOntologyUserState_UserId_IsPinned");
        }
    }
}
