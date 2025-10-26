using Eidos.Data;
using Eidos.Data.Repositories;
using Eidos.Models;
using Eidos.Models.Enums;
using Eidos.Models.Events;
using Eidos.Services.Interfaces;
using Eidos.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Eidos.Services;

/// <summary>
/// Service for managing individual/instance operations
/// Handles CRUD operations for individuals (named instances of concepts)
/// Enforces permission checks for security
/// Broadcasts changes via SignalR for real-time collaboration
/// </summary>
public class IndividualService : IIndividualService
{
    private readonly IIndividualRepository _individualRepository;
    private readonly IOntologyRepository _ontologyRepository;
    private readonly IDbContextFactory<OntologyDbContext> _contextFactory;
    private readonly IHubContext<OntologyHub> _hubContext;
    private readonly IUserService _userService;
    private readonly IOntologyShareService _shareService;

    public IndividualService(
        IIndividualRepository individualRepository,
        IOntologyRepository ontologyRepository,
        IDbContextFactory<OntologyDbContext> contextFactory,
        IHubContext<OntologyHub> hubContext,
        IUserService userService,
        IOntologyShareService shareService)
    {
        _individualRepository = individualRepository;
        _ontologyRepository = ontologyRepository;
        _contextFactory = contextFactory;
        _hubContext = hubContext;
        _userService = userService;
        _shareService = shareService;
    }

    public async Task<Individual> CreateAsync(Individual individual)
    {
        // Verify user has permission to add individuals
        var currentUser = await _userService.GetCurrentUserAsync();
        var hasPermission = await _shareService.HasPermissionAsync(
            individual.OntologyId,
            currentUser?.Id,
            sessionToken: null,
            requiredLevel: PermissionLevel.ViewAndAdd);

        if (!hasPermission)
        {
            throw new UnauthorizedAccessException(
                $"User does not have permission to add individuals to ontology {individual.OntologyId}");
        }

        individual.CreatedAt = DateTime.UtcNow;
        individual.UpdatedAt = DateTime.UtcNow;
        var created = await _individualRepository.AddAsync(individual);
        await _ontologyRepository.UpdateTimestampAsync(individual.OntologyId);

        // Broadcast change
        await BroadcastIndividualChange(individual.OntologyId, ChangeType.Added, created);

        return created;
    }

    public async Task<Individual> UpdateAsync(Individual individual)
    {
        // Verify user has permission to edit individuals
        var currentUser = await _userService.GetCurrentUserAsync();
        var hasPermission = await _shareService.HasPermissionAsync(
            individual.OntologyId,
            currentUser?.Id,
            sessionToken: null,
            requiredLevel: PermissionLevel.ViewAddEdit);

        if (!hasPermission)
        {
            throw new UnauthorizedAccessException(
                $"User does not have permission to edit individuals in ontology {individual.OntologyId}");
        }

        individual.UpdatedAt = DateTime.UtcNow;
        await _individualRepository.UpdateAsync(individual);
        await _ontologyRepository.UpdateTimestampAsync(individual.OntologyId);

        // Broadcast change
        await BroadcastIndividualChange(individual.OntologyId, ChangeType.Updated, individual);

        return individual;
    }

    public async Task DeleteAsync(int id)
    {
        var individual = await _individualRepository.GetByIdAsync(id);
        if (individual == null)
        {
            throw new InvalidOperationException($"Individual with ID {id} not found");
        }

        // Verify user has permission to delete individuals
        var currentUser = await _userService.GetCurrentUserAsync();
        var hasPermission = await _shareService.HasPermissionAsync(
            individual.OntologyId,
            currentUser?.Id,
            sessionToken: null,
            requiredLevel: PermissionLevel.FullAccess);

        if (!hasPermission)
        {
            throw new UnauthorizedAccessException(
                $"User does not have permission to delete individuals from ontology {individual.OntologyId}");
        }

        await _individualRepository.DeleteAsync(id);
        await _ontologyRepository.UpdateTimestampAsync(individual.OntologyId);

        // Broadcast change
        await BroadcastIndividualChange(individual.OntologyId, ChangeType.Deleted, null, id);
    }

    public async Task<Individual?> GetByIdAsync(int id)
    {
        return await _individualRepository.GetByIdAsync(id);
    }

    public async Task<Individual?> GetWithPropertiesAsync(int id)
    {
        return await _individualRepository.GetWithPropertiesAsync(id);
    }

    public async Task<Individual?> GetWithDetailsAsync(int id)
    {
        return await _individualRepository.GetWithDetailsAsync(id);
    }

    public async Task<IEnumerable<Individual>> GetByOntologyIdAsync(int ontologyId)
    {
        return await _individualRepository.GetByOntologyIdAsync(ontologyId);
    }

    public async Task<IEnumerable<Individual>> GetByConceptIdAsync(int conceptId)
    {
        return await _individualRepository.GetByConceptIdAsync(conceptId);
    }

    public async Task<IEnumerable<Individual>> SearchAsync(string query, int? ontologyId = null)
    {
        return await _individualRepository.SearchAsync(query, ontologyId);
    }

    public async Task<int> GetCountByConceptIdAsync(int conceptId)
    {
        return await _individualRepository.GetCountByConceptIdAsync(conceptId);
    }

    public async Task<IndividualProperty> AddPropertyAsync(IndividualProperty property)
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        // Get the individual to check permissions
        var individual = await context.Individuals.FindAsync(property.IndividualId);
        if (individual == null)
        {
            throw new InvalidOperationException($"Individual with ID {property.IndividualId} not found");
        }

        // Verify user has permission
        var currentUser = await _userService.GetCurrentUserAsync();
        var hasPermission = await _shareService.HasPermissionAsync(
            individual.OntologyId,
            currentUser?.Id,
            sessionToken: null,
            requiredLevel: PermissionLevel.ViewAddEdit);

        if (!hasPermission)
        {
            throw new UnauthorizedAccessException(
                $"User does not have permission to modify individuals in ontology {individual.OntologyId}");
        }

        context.Set<IndividualProperty>().Add(property);
        await context.SaveChangesAsync();

        // Update individual timestamp
        individual.UpdatedAt = DateTime.UtcNow;
        context.Update(individual);
        await context.SaveChangesAsync();
        await _ontologyRepository.UpdateTimestampAsync(individual.OntologyId);

        return property;
    }

    public async Task<IndividualProperty> UpdatePropertyAsync(IndividualProperty property)
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        // Get the individual to check permissions
        var individual = await context.Individuals.FindAsync(property.IndividualId);
        if (individual == null)
        {
            throw new InvalidOperationException($"Individual with ID {property.IndividualId} not found");
        }

        // Verify user has permission
        var currentUser = await _userService.GetCurrentUserAsync();
        var hasPermission = await _shareService.HasPermissionAsync(
            individual.OntologyId,
            currentUser?.Id,
            sessionToken: null,
            requiredLevel: PermissionLevel.ViewAddEdit);

        if (!hasPermission)
        {
            throw new UnauthorizedAccessException(
                $"User does not have permission to modify individuals in ontology {individual.OntologyId}");
        }

        context.Set<IndividualProperty>().Update(property);
        await context.SaveChangesAsync();

        // Update individual timestamp
        individual.UpdatedAt = DateTime.UtcNow;
        context.Update(individual);
        await context.SaveChangesAsync();
        await _ontologyRepository.UpdateTimestampAsync(individual.OntologyId);

        return property;
    }

    public async Task DeletePropertyAsync(int propertyId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        var property = await context.Set<IndividualProperty>()
            .Include(p => p.Individual)
            .FirstOrDefaultAsync(p => p.Id == propertyId);

        if (property == null)
        {
            throw new InvalidOperationException($"Property with ID {propertyId} not found");
        }

        // Verify user has permission
        var currentUser = await _userService.GetCurrentUserAsync();
        var hasPermission = await _shareService.HasPermissionAsync(
            property.Individual.OntologyId,
            currentUser?.Id,
            sessionToken: null,
            requiredLevel: PermissionLevel.ViewAddEdit);

        if (!hasPermission)
        {
            throw new UnauthorizedAccessException(
                $"User does not have permission to modify individuals in ontology {property.Individual.OntologyId}");
        }

        context.Set<IndividualProperty>().Remove(property);
        await context.SaveChangesAsync();

        // Update individual timestamp
        property.Individual.UpdatedAt = DateTime.UtcNow;
        context.Update(property.Individual);
        await context.SaveChangesAsync();
        await _ontologyRepository.UpdateTimestampAsync(property.Individual.OntologyId);
    }

    public async Task<IndividualRelationship> CreateRelationshipAsync(IndividualRelationship relationship)
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        // Verify user has permission
        var currentUser = await _userService.GetCurrentUserAsync();
        var hasPermission = await _shareService.HasPermissionAsync(
            relationship.OntologyId,
            currentUser?.Id,
            sessionToken: null,
            requiredLevel: PermissionLevel.ViewAndAdd);

        if (!hasPermission)
        {
            throw new UnauthorizedAccessException(
                $"User does not have permission to add relationships in ontology {relationship.OntologyId}");
        }

        relationship.CreatedAt = DateTime.UtcNow;
        context.Set<IndividualRelationship>().Add(relationship);
        await context.SaveChangesAsync();
        await _ontologyRepository.UpdateTimestampAsync(relationship.OntologyId);

        return relationship;
    }

    public async Task DeleteRelationshipAsync(int relationshipId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        var relationship = await context.Set<IndividualRelationship>()
            .FirstOrDefaultAsync(r => r.Id == relationshipId);

        if (relationship == null)
        {
            throw new InvalidOperationException($"Relationship with ID {relationshipId} not found");
        }

        // Verify user has permission
        var currentUser = await _userService.GetCurrentUserAsync();
        var hasPermission = await _shareService.HasPermissionAsync(
            relationship.OntologyId,
            currentUser?.Id,
            sessionToken: null,
            requiredLevel: PermissionLevel.FullAccess);

        if (!hasPermission)
        {
            throw new UnauthorizedAccessException(
                $"User does not have permission to delete relationships from ontology {relationship.OntologyId}");
        }

        context.Set<IndividualRelationship>().Remove(relationship);
        await context.SaveChangesAsync();
        await _ontologyRepository.UpdateTimestampAsync(relationship.OntologyId);
    }

    private async Task BroadcastIndividualChange(int ontologyId, ChangeType changeType, Individual? individual, int? deletedIndividualId = null)
    {
        var groupName = $"ontology_{ontologyId}";
        var changeEvent = new IndividualChangedEvent
        {
            OntologyId = ontologyId,
            ChangeType = changeType,
            Individual = individual,
            DeletedIndividualId = deletedIndividualId
        };

        await _hubContext.Clients
            .Group(groupName)
            .SendAsync("IndividualChanged", changeEvent);
    }
}
