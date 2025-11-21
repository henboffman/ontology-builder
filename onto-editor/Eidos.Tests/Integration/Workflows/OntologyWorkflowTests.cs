using Eidos.Data;
using Eidos.Data.Repositories;
using Eidos.Hubs;
using Eidos.Models;
using Eidos.Models.Enums;
using Eidos.Services;
using Eidos.Services.Commands;
using Eidos.Services.Interfaces;
using Eidos.Tests.Helpers;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
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
        var mockActivityService = new Mock<IOntologyActivityService>();
        var mockContextFactory = new Mock<IDbContextFactory<OntologyDbContext>>();
        var permissionService = new OntologyPermissionService(_contextFactory);
        var noteLogger = new Mock<Microsoft.Extensions.Logging.ILogger<NoteRepository>>();
        var noteRepository = new NoteRepository(_contextFactory, noteLogger.Object);
        var workspaceLogger = new Mock<Microsoft.Extensions.Logging.ILogger<WorkspaceRepository>>();
        var workspaceRepository = new WorkspaceRepository(_contextFactory, workspaceLogger.Object);
        var detectionLogger = new Mock<Microsoft.Extensions.Logging.ILogger<ConceptDetectionService>>();
        var conceptDetectionService = new ConceptDetectionService(workspaceRepository, _conceptRepository, detectionLogger.Object);
        var linkLogger = new Mock<Microsoft.Extensions.Logging.ILogger<NoteConceptLinkRepository>>();
        var noteConceptLinkRepository = new NoteConceptLinkRepository(_contextFactory, linkLogger.Object);
        var mockConceptLogger = new Mock<Microsoft.Extensions.Logging.ILogger<ConceptService>>();

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
            mockShareService.Object,
            mockActivityService.Object,
            mockContextFactory.Object,
            permissionService,
            noteRepository,
            workspaceRepository,
            conceptDetectionService,
            noteConceptLinkRepository,
            mockConceptLogger.Object);

        _relationshipService = new RelationshipService(
            _relationshipRepository,
            _ontologyRepository,
            mockCommandFactory.Object,
            mockCommandInvoker.Object,
            mockHubContext.Object,
            mockUserService.Object,
            mockShareService.Object,
            mockActivityService.Object,
            permissionService);

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

}
