using Eidos.Data.Repositories;
using Eidos.Hubs;
using Eidos.Models;
using Eidos.Models.Enums;
using Eidos.Services;
using Eidos.Services.Commands;
using Eidos.Services.Interfaces;
using Eidos.Tests.Helpers;
using Microsoft.AspNetCore.SignalR;
using Moq;
using Xunit;

namespace Eidos.Tests.Integration.Workflows;

/// <summary>
/// End-to-end workflow tests that verify complete user scenarios work from start to finish
/// These tests use REAL repositories and services to test the full application stack
/// </summary>
public class OntologyWorkflowTests : IDisposable
{
    private readonly TestDbContextFactory _contextFactory;
    private readonly OntologyService _ontologyService;
    private readonly ConceptService _conceptService;
    private readonly RelationshipService _relationshipService;
    private readonly IOntologyRepository _ontologyRepository;
    private readonly IConceptRepository _conceptRepository;
    private readonly IRelationshipRepository _relationshipRepository;
    private readonly ApplicationUser _testUser;

    public OntologyWorkflowTests()
    {
        var dbName = $"TestDb_{Guid.NewGuid()}";
        _contextFactory = new TestDbContextFactory(dbName);

        // Create REAL repositories
        _ontologyRepository = new OntologyRepository(_contextFactory);
        _conceptRepository = new ConceptRepository(_contextFactory);
        _relationshipRepository = new RelationshipRepository(_contextFactory);

        // Setup mocks for external concerns
        var mockUserService = new Mock<IUserService>();
        var mockShareService = new Mock<IOntologyShareService>();
        var mockCommandFactory = new Mock<ICommandFactory>();
        var mockCommandInvoker = new Mock<CommandInvoker>();
        var mockHubContext = new Mock<IHubContext<OntologyHub>>();

        _testUser = TestDataBuilder.CreateUser();
        mockUserService.Setup(s => s.GetCurrentUserAsync()).ReturnsAsync(_testUser);
        mockShareService
            .Setup(s => s.HasPermissionAsync(
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<PermissionLevel>()))
            .ReturnsAsync(true);

        // Setup SignalR
        var mockClients = new Mock<IHubClients>();
        var mockClientProxy = new Mock<IClientProxy>();
        mockClients.Setup(c => c.Group(It.IsAny<string>())).Returns(mockClientProxy.Object);
        mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);

        // Create REAL services
        _conceptService = new ConceptService(
            _conceptRepository,
            _ontologyRepository,
            _relationshipRepository,
            mockCommandFactory.Object,
            mockCommandInvoker.Object,
            mockHubContext.Object,
            mockUserService.Object,
            mockShareService.Object);

        _relationshipService = new RelationshipService(
            _relationshipRepository,
            _ontologyRepository,
            mockCommandFactory.Object,
            mockCommandInvoker.Object,
            mockHubContext.Object,
            mockUserService.Object,
            mockShareService.Object);

        _ontologyService = new OntologyService(
            _contextFactory,
            _ontologyRepository,
            _conceptService,
            _relationshipService,
            new Mock<IPropertyService>().Object,
            new Mock<IRelationshipSuggestionService>().Object,
            mockCommandInvoker.Object,
            mockUserService.Object,
            mockShareService.Object);
    }

    public void Dispose()
    {
        // In-memory database will be cleaned up automatically
    }

    [Fact]
    public async Task CompleteOntologyWorkflow_CreateOntologyWithConceptsAndRelationships_ShouldWork()
    {
        // SCENARIO: User creates a complete ontology from scratch

        // Step 1: Create an ontology
        var ontology = await _ontologyService.CreateOntologyAsync(new Ontology
        {
            Name = "Organization Ontology",
            Description = "Model of an organization",
            UserId = _testUser.Id
        });

        Assert.NotNull(ontology);
        Assert.True(ontology.Id > 0);

        // Step 2: Add concepts
        var person = await _conceptService.CreateAsync(new Concept
        {
            OntologyId = ontology.Id,
            Name = "Person",
            Definition = "An individual human being",
            Category = "Entity"
        }, recordUndo: false);

        var organization = await _conceptService.CreateAsync(new Concept
        {
            OntologyId = ontology.Id,
            Name = "Organization",
            Definition = "A structured group of people",
            Category = "Entity"
        }, recordUndo: false);

        var department = await _conceptService.CreateAsync(new Concept
        {
            OntologyId = ontology.Id,
            Name = "Department",
            Definition = "A division of an organization",
            Category = "Entity"
        }, recordUndo: false);

        Assert.True(person.Id > 0);
        Assert.True(organization.Id > 0);
        Assert.True(department.Id > 0);

        // Step 3: Create relationships
        var worksFor = await _relationshipService.CreateAsync(new Relationship
        {
            OntologyId = ontology.Id,
            SourceConceptId = person.Id,
            TargetConceptId = organization.Id,
            RelationType = "works-for",
            Description = "Person works for Organization"
        }, recordUndo: false);

        var hasPart = await _relationshipService.CreateAsync(new Relationship
        {
            OntologyId = ontology.Id,
            SourceConceptId = organization.Id,
            TargetConceptId = department.Id,
            RelationType = "has-part",
            Description = "Organization has Department"
        }, recordUndo: false);

        Assert.True(worksFor.Id > 0);
        Assert.True(hasPart.Id > 0);

        // Step 4: Load the complete ontology and verify everything is connected
        var loadedOntology = await _ontologyService.GetOntologyAsync(ontology.Id);

        Assert.NotNull(loadedOntology);
        Assert.Equal("Organization Ontology", loadedOntology.Name);
        Assert.Equal(3, loadedOntology.Concepts.Count);
        Assert.Equal(2, loadedOntology.Relationships.Count);

        // Verify concepts
        Assert.Contains(loadedOntology.Concepts, c => c.Name == "Person");
        Assert.Contains(loadedOntology.Concepts, c => c.Name == "Organization");
        Assert.Contains(loadedOntology.Concepts, c => c.Name == "Department");

        // Verify relationships
        Assert.Contains(loadedOntology.Relationships, r => r.RelationType == "works-for");
        Assert.Contains(loadedOntology.Relationships, r => r.RelationType == "has-part");
    }

    [Fact]
    public async Task ForkOntologyWorkflow_ShouldCopyAllDataAndMaintainProvenance()
    {
        // SCENARIO: User creates an ontology, then forks it for experimentation

        // Step 1: Create original ontology with concepts and relationships
        var original = await _ontologyService.CreateOntologyAsync(new Ontology
        {
            Name = "Original Biology Ontology",
            UserId = _testUser.Id
        });

        var mammal = await _conceptService.CreateAsync(new Concept
        {
            OntologyId = original.Id,
            Name = "Mammal",
            Definition = "Warm-blooded vertebrate"
        }, recordUndo: false);

        var dog = await _conceptService.CreateAsync(new Concept
        {
            OntologyId = original.Id,
            Name = "Dog",
            Definition = "Domestic canine"
        }, recordUndo: false);

        await _relationshipService.CreateAsync(new Relationship
        {
            OntologyId = original.Id,
            SourceConceptId = dog.Id,
            TargetConceptId = mammal.Id,
            RelationType = "is-a"
        }, recordUndo: false);

        // Step 2: Fork the ontology
        var forked = await _ontologyService.ForkOntologyAsync(
            original.Id,
            "Forked Biology Ontology",
            "Testing new classification");

        Assert.NotNull(forked);
        Assert.NotEqual(original.Id, forked.Id);
        Assert.Equal("fork", forked.ProvenanceType);
        Assert.Equal(original.Id, forked.ParentOntologyId);

        // Step 3: Verify all data was copied
        var forkedWithData = await _ontologyService.GetOntologyAsync(forked.Id);
        Assert.Equal(2, forkedWithData.Concepts.Count);
        Assert.Single(forkedWithData.Relationships);

        // Verify concepts were copied
        Assert.Contains(forkedWithData.Concepts, c => c.Name == "Mammal");
        Assert.Contains(forkedWithData.Concepts, c => c.Name == "Dog");

        // Verify relationship was copied
        var copiedRelationship = forkedWithData.Relationships.First();
        Assert.Equal("is-a", copiedRelationship.RelationType);

        // Step 4: Verify lineage is tracked
        var lineage = await _ontologyService.GetOntologyLineageAsync(forked.Id);
        Assert.Equal(2, lineage.Count);
        Assert.Equal(forked.Id, lineage[0].Id);
        Assert.Equal(original.Id, lineage[1].Id);

        // Step 5: Verify original shows forked ontology as descendant
        var descendants = await _ontologyService.GetOntologyDescendantsAsync(original.Id);
        Assert.Single(descendants);
        Assert.Equal(forked.Id, descendants.First().Id);
    }

    [Fact]
    public async Task MultipleUsersWorkflow_ShouldIsolateDataByUser()
    {
        // SCENARIO: Two users create separate ontologies

        var user1Ontology = await _ontologyService.CreateOntologyAsync(new Ontology
        {
            Name = "User 1 Ontology",
            UserId = _testUser.Id
        });

        var user2Ontology = await _ontologyService.CreateOntologyAsync(new Ontology
        {
            Name = "User 2 Ontology",
            UserId = "other-user-id"
        });

        // Add concepts to user 1's ontology
        await _conceptService.CreateAsync(new Concept
        {
            OntologyId = user1Ontology.Id,
            Name = "User 1 Concept"
        }, recordUndo: false);

        // Add concepts to user 2's ontology (using repository directly since we don't have their service)
        await _conceptRepository.AddAsync(new Concept
        {
            OntologyId = user2Ontology.Id,
            Name = "User 2 Concept"
        });

        // Verify isolation: user 1's ontology should only have their concepts
        var user1Data = await _ontologyService.GetOntologyAsync(user1Ontology.Id);
        Assert.Single(user1Data.Concepts);
        Assert.Equal("User 1 Concept", user1Data.Concepts.First().Name);

        // Verify user 2's ontology has their own concepts
        var user2Data = await _ontologyService.GetOntologyAsync(user2Ontology.Id);
        Assert.Single(user2Data.Concepts);
        Assert.Equal("User 2 Concept", user2Data.Concepts.First().Name);
    }

    [Fact]
    public async Task UpdateWorkflow_ModifyConceptAndRelationship_ShouldPersist()
    {
        // SCENARIO: User creates ontology, then modifies concepts and relationships

        // Create initial ontology
        var ontology = await _ontologyService.CreateOntologyAsync(new Ontology
        {
            Name = "Test Ontology",
            UserId = _testUser.Id
        });

        var concept1 = await _conceptService.CreateAsync(new Concept
        {
            OntologyId = ontology.Id,
            Name = "Original Name",
            Definition = "Original definition"
        }, recordUndo: false);

        var concept2 = await _conceptService.CreateAsync(new Concept
        {
            OntologyId = ontology.Id,
            Name = "Concept 2"
        }, recordUndo: false);

        var relationship = await _relationshipService.CreateAsync(new Relationship
        {
            OntologyId = ontology.Id,
            SourceConceptId = concept1.Id,
            TargetConceptId = concept2.Id,
            RelationType = "original-type"
        }, recordUndo: false);

        // Update concept
        concept1.Name = "Updated Name";
        concept1.Definition = "Updated definition";
        await _conceptService.UpdateAsync(concept1, recordUndo: false);

        // Update relationship
        relationship.RelationType = "updated-type";
        relationship.Description = "New description";
        await _relationshipService.UpdateAsync(relationship, recordUndo: false);

        // Verify updates persisted
        var loaded = await _ontologyService.GetOntologyAsync(ontology.Id);
        var loadedConcept = loaded.Concepts.First(c => c.Id == concept1.Id);
        var loadedRelationship = loaded.Relationships.First();

        Assert.Equal("Updated Name", loadedConcept.Name);
        Assert.Equal("Updated definition", loadedConcept.Definition);
        Assert.Equal("updated-type", loadedRelationship.RelationType);
        Assert.Equal("New description", loadedRelationship.Description);
    }

    [Fact]
    public async Task DeleteWorkflow_RemoveConceptsAndRelationships_ShouldWork()
    {
        // SCENARIO: User creates ontology with data, then removes some of it

        var ontology = await _ontologyService.CreateOntologyAsync(new Ontology
        {
            Name = "Test Ontology",
            UserId = _testUser.Id
        });

        var concept1 = await _conceptService.CreateAsync(new Concept
        {
            OntologyId = ontology.Id,
            Name = "Concept 1"
        }, recordUndo: false);

        var concept2 = await _conceptService.CreateAsync(new Concept
        {
            OntologyId = ontology.Id,
            Name = "Concept 2"
        }, recordUndo: false);

        var concept3 = await _conceptService.CreateAsync(new Concept
        {
            OntologyId = ontology.Id,
            Name = "Concept 3"
        }, recordUndo: false);

        var relationship = await _relationshipService.CreateAsync(new Relationship
        {
            OntologyId = ontology.Id,
            SourceConceptId = concept1.Id,
            TargetConceptId = concept2.Id,
            RelationType = "related-to"
        }, recordUndo: false);

        // Delete relationship
        await _relationshipService.DeleteAsync(relationship.Id, recordUndo: false);

        // Delete concept
        await _conceptService.DeleteAsync(concept3.Id, recordUndo: false);

        // Verify deletions
        var loaded = await _ontologyService.GetOntologyAsync(ontology.Id);
        Assert.Equal(2, loaded.Concepts.Count); // concept3 deleted
        Assert.Empty(loaded.Relationships); // relationship deleted
        Assert.DoesNotContain(loaded.Concepts, c => c.Name == "Concept 3");
    }

    [Fact]
    public async Task ComplexLineageWorkflow_MultipleGenerations_ShouldTrackCorrectly()
    {
        // SCENARIO: User creates ontology, clones it, then forks the clone

        // Generation 1: Original
        var original = await _ontologyService.CreateOntologyAsync(new Ontology
        {
            Name = "Original",
            UserId = _testUser.Id
        });

        await _conceptService.CreateAsync(new Concept
        {
            OntologyId = original.Id,
            Name = "Base Concept"
        }, recordUndo: false);

        // Generation 2: Clone of original
        var clone = await _ontologyService.CloneOntologyAsync(
            original.Id, "Clone", "First generation clone");

        // Generation 3: Fork of clone
        var fork = await _ontologyService.ForkOntologyAsync(
            clone.Id, "Fork", "Second generation fork");

        // Verify lineage from fork traces back to original
        var lineage = await _ontologyService.GetOntologyLineageAsync(fork.Id);
        Assert.Equal(3, lineage.Count);
        Assert.Equal("Fork", lineage[0].Name);
        Assert.Equal("Clone", lineage[1].Name);
        Assert.Equal("Original", lineage[2].Name);

        // Verify original shows all descendants
        var descendants = await _ontologyService.GetOntologyDescendantsAsync(original.Id);
        Assert.Equal(2, descendants.Count);
        Assert.Contains(descendants, d => d.Name == "Clone");
        Assert.Contains(descendants, d => d.Name == "Fork");

        // Verify clone shows fork as descendant
        var cloneDescendants = await _ontologyService.GetOntologyDescendantsAsync(clone.Id);
        Assert.Single(cloneDescendants);
        Assert.Equal("Fork", cloneDescendants.First().Name);
    }
}
