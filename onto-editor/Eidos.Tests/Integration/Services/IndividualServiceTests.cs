using Eidos.Data;
using Eidos.Data.Repositories;
using Eidos.Hubs;
using Eidos.Models;
using Eidos.Models.Enums;
using Eidos.Models.Events;
using Eidos.Services;
using Eidos.Services.Interfaces;
using Eidos.Tests.Helpers;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Eidos.Tests.Integration.Services;

/// <summary>
/// Integration tests for IndividualService (Phase 3)
/// </summary>
public class IndividualServiceTests : IDisposable
{
    private readonly Mock<IIndividualRepository> _mockIndividualRepository;
    private readonly Mock<IOntologyRepository> _mockOntologyRepository;
    private readonly IDbContextFactory<OntologyDbContext> _contextFactory;
    private readonly Mock<IHubContext<OntologyHub>> _mockHubContext;
    private readonly Mock<IUserService> _mockUserService;
    private readonly Mock<IOntologyShareService> _mockShareService;
    private readonly IndividualService _service;
    private readonly ApplicationUser _testUser;

    public IndividualServiceTests()
    {
        _mockIndividualRepository = new Mock<IIndividualRepository>();
        _mockOntologyRepository = new Mock<IOntologyRepository>();
        _contextFactory = new TestDbContextFactory("IndividualServiceTests");
        _mockHubContext = new Mock<IHubContext<OntologyHub>>();
        _mockUserService = new Mock<IUserService>();
        _mockShareService = new Mock<IOntologyShareService>();

        _testUser = TestDataBuilder.CreateUser();
        _mockUserService.Setup(s => s.GetCurrentUserAsync()).ReturnsAsync(_testUser);

        // Setup default permission to allow operations
        _mockShareService
            .Setup(s => s.HasPermissionAsync(
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<PermissionLevel>()))
            .ReturnsAsync(true);

        // Setup SignalR hub context
        var mockClients = new Mock<IHubClients>();
        var mockClientProxy = new Mock<IClientProxy>();
        mockClients.Setup(c => c.Group(It.IsAny<string>())).Returns(mockClientProxy.Object);
        _mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);

        _service = new IndividualService(
            _mockIndividualRepository.Object,
            _mockOntologyRepository.Object,
            _contextFactory,
            _mockHubContext.Object,
            _mockUserService.Object,
            _mockShareService.Object
        );
    }

    public void Dispose()
    {
        // Cleanup if needed
    }

    [Fact]
    public async Task CreateAsync_WithValidIndividual_ShouldCreateAndBroadcast()
    {
        // Arrange
        var ontologyId = 1;
        var conceptId = 1;
        var concept = TestDataBuilder.CreateConcept(ontologyId, "Dog");
        concept.Id = conceptId;

        var individual = new Individual
        {
            OntologyId = ontologyId,
            ConceptId = conceptId,
            Name = "Fido",
            Description = "A friendly golden retriever",
            Label = "Fido the Dog"
        };

        individual.Properties = new List<IndividualProperty>
        {
            new() { Name = "age", Value = "5", DataType = "integer" },
            new() { Name = "breed", Value = "Golden Retriever", DataType = "string" }
        };

        _mockIndividualRepository.Setup(r => r.AddAsync(It.IsAny<Individual>())).ReturnsAsync(individual);

        // Act
        var result = await _service.CreateAsync(individual);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Fido", result.Name);
        _mockIndividualRepository.Verify(r => r.AddAsync(It.IsAny<Individual>()), Times.Once);
        _mockOntologyRepository.Verify(r => r.UpdateTimestampAsync(ontologyId), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithoutPermission_ShouldThrowUnauthorizedException()
    {
        // Arrange
        var individual = new Individual
        {
            OntologyId = 1,
            ConceptId = 1,
            Name = "Test Individual"
        };

        _mockShareService
            .Setup(s => s.HasPermissionAsync(1, It.IsAny<string>(), It.IsAny<string>(), PermissionLevel.ViewAndAdd))
            .ReturnsAsync(false);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.CreateAsync(individual));
    }

    [Fact]
    public async Task UpdateAsync_WithValidIndividual_ShouldUpdate()
    {
        // Arrange
        var individual = new Individual
        {
            Id = 1,
            OntologyId = 1,
            ConceptId = 1,
            Name = "Updated Name",
            Description = "Updated description"
        };

        // Act
        var result = await _service.UpdateAsync(individual);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Updated Name", result.Name);
        _mockIndividualRepository.Verify(r => r.UpdateAsync(It.IsAny<Individual>()), Times.Once);
        _mockOntologyRepository.Verify(r => r.UpdateTimestampAsync(1), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithValidId_ShouldDelete()
    {
        // Arrange
        var individualId = 1;
        var individual = new Individual
        {
            Id = individualId,
            OntologyId = 1,
            ConceptId = 1,
            Name = "Test Individual"
        };

        _mockIndividualRepository.Setup(r => r.GetByIdAsync(individualId)).ReturnsAsync(individual);

        // Act
        await _service.DeleteAsync(individualId);

        // Assert
        _mockIndividualRepository.Verify(r => r.DeleteAsync(individualId), Times.Once);
        _mockOntologyRepository.Verify(r => r.UpdateTimestampAsync(1), Times.Once);
    }

    [Fact]
    public async Task GetByOntologyIdAsync_ShouldReturnAllIndividuals()
    {
        // Arrange
        var ontologyId = 1;
        var individuals = new List<Individual>
        {
            new() { Id = 1, OntologyId = ontologyId, ConceptId = 1, Name = "Individual1" },
            new() { Id = 2, OntologyId = ontologyId, ConceptId = 1, Name = "Individual2" }
        };

        _mockIndividualRepository.Setup(r => r.GetByOntologyIdAsync(ontologyId)).ReturnsAsync(individuals);

        // Act
        var result = await _service.GetByOntologyIdAsync(ontologyId);

        // Assert
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task GetByConceptIdAsync_ShouldReturnConceptIndividuals()
    {
        // Arrange
        var conceptId = 1;
        var individuals = new List<Individual>
        {
            new() { Id = 1, OntologyId = 1, ConceptId = conceptId, Name = "Individual1" },
            new() { Id = 2, OntologyId = 1, ConceptId = conceptId, Name = "Individual2" }
        };

        _mockIndividualRepository.Setup(r => r.GetByConceptIdAsync(conceptId)).ReturnsAsync(individuals);

        // Act
        var result = await _service.GetByConceptIdAsync(conceptId);

        // Assert
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task AddPropertyAsync_WithValidProperty_ShouldAddProperty()
    {
        // Arrange
        var property = new IndividualProperty
        {
            IndividualId = 1,
            Name = "color",
            Value = "brown",
            DataType = "string"
        };

        var individual = new Individual { Id = 1, OntologyId = 1, ConceptId = 1, Name = "Test" };
        _mockIndividualRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(individual);

        // Act
        var result = await _service.AddPropertyAsync(property);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("color", result.Name);
    }

    [Fact]
    public async Task CreateRelationshipAsync_WithValidRelationship_ShouldCreateRelationship()
    {
        // Arrange
        var relationship = new IndividualRelationship
        {
            SourceIndividualId = 1,
            TargetIndividualId = 2,
            RelationType = "knows"
        };

        var sourceIndividual = new Individual { Id = 1, OntologyId = 1, ConceptId = 1, Name = "Source" };
        var targetIndividual = new Individual { Id = 2, OntologyId = 1, ConceptId = 1, Name = "Target" };

        _mockIndividualRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(sourceIndividual);
        _mockIndividualRepository.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(targetIndividual);

        // Act
        var result = await _service.CreateRelationshipAsync(relationship);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("knows", result.RelationType);
    }
}
