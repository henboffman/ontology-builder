using Eidos.Models;

namespace Eidos.Services.Interfaces;

public interface IOntologyService
{
    // Ontology operations
    Task<List<Ontology>> GetAllOntologiesAsync();
    Task<List<Ontology>> GetOntologiesForCurrentUserAsync();
    Task<Ontology?> GetOntologyAsync(int id);
    Task<Ontology?> GetOntologyAsync(int id, Action<ImportProgress>? onProgress = null);
    Task<Ontology> CreateOntologyAsync(Ontology ontology);
    Task<Ontology> UpdateOntologyAsync(Ontology ontology);
    Task DeleteOntologyAsync(int id);

    // Concept operations
    Task<Concept> CreateConceptAsync(Concept concept, bool recordUndo = true);
    Task<Concept> UpdateConceptAsync(Concept concept, bool recordUndo = true);
    Task DeleteConceptAsync(int id, bool recordUndo = true);

    // Relationship operations
    Task<Relationship> CreateRelationshipAsync(Relationship relationship, bool recordUndo = true);
    Task<Relationship> UpdateRelationshipAsync(Relationship relationship, bool recordUndo = true);
    Task DeleteRelationshipAsync(int id, bool recordUndo = true);

    // Property operations
    Task<Property> CreatePropertyAsync(Property property);
    Task<Property> UpdatePropertyAsync(Property property);
    Task DeletePropertyAsync(int id);

    // Validation helpers
    Task<bool> CanCreateRelationshipAsync(int sourceId, int targetId, string relationType);
    Task<List<string>> GetSuggestedRelationshipsAsync(int conceptId);

    // Undo/Redo operations
    Task<bool> UndoAsync();
    Task<bool> RedoAsync();
    bool CanUndo();
    bool CanRedo();

    // Fork/Clone/Provenance operations
    Task<Ontology> CloneOntologyAsync(int sourceOntologyId, string newName, string? provenanceNotes = null);
    Task<Ontology> ForkOntologyAsync(int sourceOntologyId, string newName, string? provenanceNotes = null);
    Task<List<Ontology>> GetOntologyLineageAsync(int ontologyId); // Get parent chain
    Task<List<Ontology>> GetOntologyDescendantsAsync(int ontologyId); // Get all children/forks
}
