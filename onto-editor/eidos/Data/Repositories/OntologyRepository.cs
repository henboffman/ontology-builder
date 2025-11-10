using Microsoft.EntityFrameworkCore;
using Eidos.Models;

namespace Eidos.Data.Repositories;

public class OntologyRepository : BaseRepository<Ontology>, IOntologyRepository
{
    public OntologyRepository(IDbContextFactory<OntologyDbContext> contextFactory)
        : base(contextFactory)
    {
    }

    /// <summary>
    /// Retrieves an ontology with all related data eagerly loaded.
    /// This includes concepts, relationships, individuals, and individual relationships.
    /// </summary>
    /// <param name="id">The ID of the ontology to retrieve</param>
    /// <returns>The ontology with all related data, or null if not found</returns>
    /// <remarks>
    /// Navigation properties loaded:
    /// - Concepts with their Properties (individual property values)
    /// - Concepts with their ConceptProperties (property definitions for OWL export)
    /// - Relationships with SourceConcept and TargetConcept
    /// - Individuals with their Properties (for individual visualization in graph view)
    /// - IndividualRelationships (for individual relationship edges in graph view)
    /// - LinkedOntologies
    ///
    /// Performance considerations:
    /// - Uses AsSplitQuery() to avoid cartesian explosion with multiple collections
    /// - Uses AsNoTracking() for read-only performance optimization
    /// </remarks>
    public async Task<Ontology?> GetWithAllRelatedDataAsync(int id)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Ontologies
            .AsSplitQuery()
            .AsNoTracking()
            .Include(o => o.Concepts)
                .ThenInclude(c => c.Properties)
            .Include(o => o.Concepts)
                .ThenInclude(c => c.ConceptProperties)       // Load concept property definitions for TTL export
                    .ThenInclude(cp => cp.RangeConcept)      // Load range concept for ObjectProperty types
            .Include(o => o.Concepts)
                .ThenInclude(c => c.Restrictions)
                    .ThenInclude(r => r.AllowedConcept)
            .Include(o => o.Relationships)
                .ThenInclude(r => r.SourceConcept)
            .Include(o => o.Relationships)
                .ThenInclude(r => r.TargetConcept)
            .Include(o => o.Individuals)                    // Required for individual visualization
                .ThenInclude(i => i.Concept)                 // Load concept for export
            .Include(o => o.Individuals)
                .ThenInclude(i => i.Properties)
            .Include(o => o.IndividualRelationships)        // Required for individual relationship edges
                .ThenInclude(ir => ir.SourceIndividual)
            .Include(o => o.IndividualRelationships)
                .ThenInclude(ir => ir.TargetIndividual)
            .Include(o => o.LinkedOntologies)
                .ThenInclude(link => link.LinkedOntology)    // Load linked ontology for expansion
                    .ThenInclude(lo => lo!.Concepts)         // Load concepts from linked ontology for virtualized display
                        .ThenInclude(c => c.ConceptProperties) // Load property definitions for virtual concepts
            .Include(o => o.LinkedOntologies)
                .ThenInclude(link => link.LinkedOntology)
                    .ThenInclude(lo => lo!.Relationships)    // Load relationships from linked ontology
                        .ThenInclude(r => r.SourceConcept)   // Load source concept for each relationship
            .Include(o => o.LinkedOntologies)
                .ThenInclude(link => link.LinkedOntology)
                    .ThenInclude(lo => lo!.Relationships)
                        .ThenInclude(r => r.TargetConcept)   // Load target concept for each relationship
            .Include(o => o.LinkedOntologies)
                .ThenInclude(link => link.LinkedOntology)
                    .ThenInclude(lo => lo!.LinkedOntologies) // Load nested linked ontologies
                        .ThenInclude(nestedLink => nestedLink.LinkedOntology)
                            .ThenInclude(nestedLo => nestedLo!.Concepts)
                                .ThenInclude(c => c.ConceptProperties) // Load property definitions for nested virtual concepts
            .Include(o => o.LinkedOntologies)
                .ThenInclude(link => link.LinkedOntology)
                    .ThenInclude(lo => lo!.LinkedOntologies)
                        .ThenInclude(nestedLink => nestedLink.LinkedOntology)
                            .ThenInclude(nestedLo => nestedLo!.Relationships)
                                .ThenInclude(r => r.SourceConcept)
            .Include(o => o.LinkedOntologies)
                .ThenInclude(link => link.LinkedOntology)
                    .ThenInclude(lo => lo!.LinkedOntologies)
                        .ThenInclude(nestedLink => nestedLink.LinkedOntology)
                            .ThenInclude(nestedLo => nestedLo!.Relationships)
                                .ThenInclude(r => r.TargetConcept)
            .Include(o => o.CustomTemplates)
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task<Ontology?> GetWithProgressAsync(int id, Action<ImportProgress>? onProgress = null)
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        // First, load basic ontology info
        var ontology = await context.Ontologies
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == id);

        if (ontology == null)
            return null;

        onProgress?.Invoke(new ImportProgress
        {
            Stage = "Loading Ontology",
            Current = 10,
            Total = 100,
            Message = "Loading concepts..."
        });

        // Load concepts with properties and concept property definitions
        await context.Entry(ontology)
            .Collection(o => o.Concepts)
            .Query()
            .Include(c => c.Properties)
            .Include(c => c.ConceptProperties)
                .ThenInclude(cp => cp.RangeConcept)
            .LoadAsync();

        onProgress?.Invoke(new ImportProgress
        {
            Stage = "Loading Ontology",
            Current = 50,
            Total = 100,
            Message = $"Loaded {ontology.Concepts.Count} concepts. Loading relationships..."
        });

        // Load relationships with source and target concepts
        await context.Entry(ontology)
            .Collection(o => o.Relationships)
            .Query()
            .Include(r => r.SourceConcept)
            .Include(r => r.TargetConcept)
            .LoadAsync();

        onProgress?.Invoke(new ImportProgress
        {
            Stage = "Loading Ontology",
            Current = 90,
            Total = 100,
            Message = $"Loaded {ontology.Relationships.Count} relationships. Finishing up..."
        });

        // Load linked ontologies
        await context.Entry(ontology)
            .Collection(o => o.LinkedOntologies)
            .LoadAsync();

        onProgress?.Invoke(new ImportProgress
        {
            Stage = "Loading Complete",
            Current = 100,
            Total = 100,
            Message = $"Loaded {ontology.Concepts.Count} concepts and {ontology.Relationships.Count} relationships"
        });

        return ontology;
    }

    public async Task<List<Ontology>> GetAllWithBasicDataAsync()
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Ontologies
            .AsSplitQuery()
            .AsNoTracking()
            .Include(o => o.Concepts)
            .Include(o => o.Relationships)
            .Include(o => o.OntologyTags)
            .OrderByDescending(o => o.UpdatedAt)
            .ToListAsync();
    }

    public async Task UpdateTimestampAsync(int ontologyId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var ontology = await context.Ontologies.FindAsync(ontologyId);
        if (ontology != null)
        {
            ontology.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
        }
    }

    public async Task IncrementConceptCountAsync(int ontologyId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var ontology = await context.Ontologies.FindAsync(ontologyId);
        if (ontology != null)
        {
            ontology.ConceptCount++;
            ontology.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
        }
    }

    public async Task DecrementConceptCountAsync(int ontologyId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var ontology = await context.Ontologies.FindAsync(ontologyId);
        if (ontology != null)
        {
            ontology.ConceptCount = Math.Max(0, ontology.ConceptCount - 1);
            ontology.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
        }
    }

    public async Task IncrementRelationshipCountAsync(int ontologyId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var ontology = await context.Ontologies.FindAsync(ontologyId);
        if (ontology != null)
        {
            ontology.RelationshipCount++;
            ontology.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
        }
    }

    public async Task DecrementRelationshipCountAsync(int ontologyId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var ontology = await context.Ontologies.FindAsync(ontologyId);
        if (ontology != null)
        {
            ontology.RelationshipCount = Math.Max(0, ontology.RelationshipCount - 1);
            ontology.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
        }
    }

    public override async Task<Ontology> AddAsync(Ontology ontology)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        ontology.CreatedAt = DateTime.UtcNow;
        ontology.UpdatedAt = DateTime.UtcNow;
        context.Ontologies.Add(ontology);
        await context.SaveChangesAsync();
        return ontology;
    }

    public override async Task UpdateAsync(Ontology ontology)
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        // Load the existing entity to avoid tracking conflicts
        var existingOntology = await context.Ontologies.FindAsync(ontology.Id);

        if (existingOntology == null)
        {
            throw new InvalidOperationException($"Ontology with ID {ontology.Id} not found");
        }

        // Update only the scalar properties (not navigation properties like Concepts/Relationships)
        existingOntology.Name = ontology.Name;
        existingOntology.Description = ontology.Description;
        existingOntology.Namespace = ontology.Namespace;
        existingOntology.Tags = ontology.Tags;
        existingOntology.License = ontology.License;
        existingOntology.Author = ontology.Author;
        existingOntology.Version = ontology.Version;
        existingOntology.UsesBFO = ontology.UsesBFO;
        existingOntology.UsesProvO = ontology.UsesProvO;
        existingOntology.Notes = ontology.Notes;
        existingOntology.Visibility = ontology.Visibility;
        existingOntology.AllowPublicEdit = ontology.AllowPublicEdit;
        existingOntology.RequiresApproval = ontology.RequiresApproval;
        existingOntology.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
    }
}
