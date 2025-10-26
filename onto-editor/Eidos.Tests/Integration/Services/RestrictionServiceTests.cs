using Eidos.Data.Repositories;
using Eidos.Models;
using Eidos.Models.Enums;
using Eidos.Services;
using Eidos.Services.Interfaces;
using Eidos.Tests.Helpers;
using Moq;
using Xunit;

namespace Eidos.Tests.Integration.Services;

/// <summary>
/// Integration tests for RestrictionService (Phase 3)
/// </summary>
public class RestrictionServiceTests : IDisposable
{
    private readonly Mock<IRestrictionRepository> _mockRestrictionRepository;
    private readonly Mock<IConceptRepository> _mockConceptRepository;
    private readonly Mock<IOntologyRepository> _mockOntologyRepository;
    private readonly Mock<IUserService> _mockUserService;
    private readonly Mock<IOntologyShareService> _mockShareService;
    private readonly RestrictionService _service;
    private readonly ApplicationUser _testUser;

    public RestrictionServiceTests()
    {
        _mockRestrictionRepository = new Mock<IRestrictionRepository>();
        _mockConceptRepository = new Mock<IConceptRepository>();
        _mockOntologyRepository = new Mock<IOntologyRepository>();
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

        _service = new RestrictionService(
            _mockRestrictionRepository.Object,
            _mockConceptRepository.Object,
            _mockOntologyRepository.Object,
            _mockUserService.Object,
            _mockShareService.Object
        );
    }

    public void Dispose()
    {
        // Cleanup if needed
    }

    [Fact]
    public async Task CreateAsync_WithValidRestriction_ShouldCreate()
    {
        // Arrange
        var restriction = new ConceptRestriction
        {
            ConceptId = 1,
            PropertyName = "age",
            RestrictionType = RestrictionTypes.ValueType,
            ValueType = "integer",
            IsMandatory = true
        };

        var concept = TestDataBuilder.CreateConcept(1, "Person");
        _mockConceptRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(concept);
        _mockRestrictionRepository.Setup(r => r.AddAsync(It.IsAny<ConceptRestriction>())).ReturnsAsync(restriction);

        // Act
        var result = await _service.CreateAsync(restriction);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("age", result.PropertyName);
        _mockRestrictionRepository.Verify(r => r.AddAsync(It.IsAny<ConceptRestriction>()), Times.Once);
        _mockOntologyRepository.Verify(r => r.UpdateTimestampAsync(1), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithoutPermission_ShouldThrowUnauthorizedException()
    {
        // Arrange
        var restriction = new ConceptRestriction
        {
            ConceptId = 1,
            PropertyName = "test",
            RestrictionType = RestrictionTypes.Required
        };

        var concept = TestDataBuilder.CreateConcept(1, "Test");
        _mockConceptRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(concept);

        _mockShareService
            .Setup(s => s.HasPermissionAsync(1, It.IsAny<string>(), It.IsAny<string>(), PermissionLevel.ViewAddEdit))
            .ReturnsAsync(false);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.CreateAsync(restriction));
    }

    [Fact]
    public async Task ValidatePropertyAsync_WithRequiredRestriction_ShouldValidateCorrectly()
    {
        // Arrange
        var conceptId = 1;
        var restrictions = new List<ConceptRestriction>
        {
            new()
            {
                ConceptId = conceptId,
                PropertyName = "name",
                RestrictionType = RestrictionTypes.Required,
                IsMandatory = true
            }
        };

        _mockRestrictionRepository.Setup(r => r.GetByConceptIdAsync(conceptId)).ReturnsAsync(restrictions);

        // Act
        var resultValid = await _service.ValidatePropertyAsync(conceptId, "name", "John Doe");
        var resultInvalid = await _service.ValidatePropertyAsync(conceptId, "name", "");

        // Assert
        Assert.True(resultValid.IsValid);
        Assert.Null(resultValid.ErrorMessage);

        Assert.False(resultInvalid.IsValid);
        Assert.NotNull(resultInvalid.ErrorMessage);
        Assert.Contains("required", resultInvalid.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ValidatePropertyAsync_WithValueTypeRestriction_ShouldValidateDataType()
    {
        // Arrange
        var conceptId = 1;
        var restrictions = new List<ConceptRestriction>
        {
            new()
            {
                ConceptId = conceptId,
                PropertyName = "age",
                RestrictionType = RestrictionTypes.ValueType,
                ValueType = "integer"
            }
        };

        _mockRestrictionRepository.Setup(r => r.GetByConceptIdAsync(conceptId)).ReturnsAsync(restrictions);

        // Act
        var resultValid = await _service.ValidatePropertyAsync(conceptId, "age", "25");
        var resultInvalid = await _service.ValidatePropertyAsync(conceptId, "age", "twenty-five");

        // Assert
        Assert.True(resultValid.IsValid);
        Assert.False(resultInvalid.IsValid);
        Assert.Contains("type", resultInvalid.ErrorMessage!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ValidatePropertyAsync_WithRangeRestriction_ShouldValidateRange()
    {
        // Arrange
        var conceptId = 1;
        var restrictions = new List<ConceptRestriction>
        {
            new()
            {
                ConceptId = conceptId,
                PropertyName = "age",
                RestrictionType = RestrictionTypes.Range,
                MinValue = "0",
                MaxValue = "120"
            }
        };

        _mockRestrictionRepository.Setup(r => r.GetByConceptIdAsync(conceptId)).ReturnsAsync(restrictions);

        // Act
        var resultValid = await _service.ValidatePropertyAsync(conceptId, "age", "25");
        var resultTooLow = await _service.ValidatePropertyAsync(conceptId, "age", "-5");
        var resultTooHigh = await _service.ValidatePropertyAsync(conceptId, "age", "150");

        // Assert
        Assert.True(resultValid.IsValid);
        Assert.False(resultTooLow.IsValid);
        Assert.False(resultTooHigh.IsValid);
        Assert.Contains("between", resultTooLow.ErrorMessage!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ValidatePropertyAsync_WithEnumerationRestriction_ShouldValidateAllowedValues()
    {
        // Arrange
        var conceptId = 1;
        var restrictions = new List<ConceptRestriction>
        {
            new()
            {
                ConceptId = conceptId,
                PropertyName = "status",
                RestrictionType = RestrictionTypes.Enumeration,
                AllowedValues = "draft, published, archived"
            }
        };

        _mockRestrictionRepository.Setup(r => r.GetByConceptIdAsync(conceptId)).ReturnsAsync(restrictions);

        // Act
        var resultValid = await _service.ValidatePropertyAsync(conceptId, "status", "published");
        var resultInvalid = await _service.ValidatePropertyAsync(conceptId, "status", "deleted");

        // Assert
        Assert.True(resultValid.IsValid);
        Assert.False(resultInvalid.IsValid);
        Assert.Contains("one of", resultInvalid.ErrorMessage!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ValidatePropertyAsync_WithPatternRestriction_ShouldValidateRegex()
    {
        // Arrange
        var conceptId = 1;
        var restrictions = new List<ConceptRestriction>
        {
            new()
            {
                ConceptId = conceptId,
                PropertyName = "email",
                RestrictionType = RestrictionTypes.Pattern,
                Pattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$"
            }
        };

        _mockRestrictionRepository.Setup(r => r.GetByConceptIdAsync(conceptId)).ReturnsAsync(restrictions);

        // Act
        var resultValid = await _service.ValidatePropertyAsync(conceptId, "email", "test@example.com");
        var resultInvalid = await _service.ValidatePropertyAsync(conceptId, "email", "invalid-email");

        // Assert
        Assert.True(resultValid.IsValid);
        Assert.False(resultInvalid.IsValid);
        Assert.Contains("pattern", resultInvalid.ErrorMessage!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ValidatePropertyAsync_WithNoRestrictions_ShouldReturnValid()
    {
        // Arrange
        var conceptId = 1;
        var restrictions = new List<ConceptRestriction>();

        _mockRestrictionRepository.Setup(r => r.GetByConceptIdAsync(conceptId)).ReturnsAsync(restrictions);

        // Act
        var result = await _service.ValidatePropertyAsync(conceptId, "anyProperty", "anyValue");

        // Assert
        Assert.True(result.IsValid);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public async Task DeleteAsync_WithValidId_ShouldDelete()
    {
        // Arrange
        var restrictionId = 1;
        var restriction = new ConceptRestriction
        {
            Id = restrictionId,
            ConceptId = 1,
            PropertyName = "test",
            RestrictionType = RestrictionTypes.Required
        };

        var concept = TestDataBuilder.CreateConcept(1, "Test");
        _mockRestrictionRepository.Setup(r => r.GetByIdAsync(restrictionId)).ReturnsAsync(restriction);
        _mockConceptRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(concept);

        // Act
        await _service.DeleteAsync(restrictionId);

        // Assert
        _mockRestrictionRepository.Verify(r => r.DeleteAsync(restrictionId), Times.Once);
        _mockOntologyRepository.Verify(r => r.UpdateTimestampAsync(1), Times.Once);
    }

    [Fact]
    public async Task GetByConceptIdAsync_ShouldReturnAllRestrictions()
    {
        // Arrange
        var conceptId = 1;
        var restrictions = new List<ConceptRestriction>
        {
            new() { Id = 1, ConceptId = conceptId, PropertyName = "prop1", RestrictionType = RestrictionTypes.Required },
            new() { Id = 2, ConceptId = conceptId, PropertyName = "prop2", RestrictionType = RestrictionTypes.ValueType }
        };

        _mockRestrictionRepository.Setup(r => r.GetByConceptIdAsync(conceptId)).ReturnsAsync(restrictions);

        // Act
        var result = await _service.GetByConceptIdAsync(conceptId);

        // Assert
        Assert.Equal(2, result.Count());
    }
}
