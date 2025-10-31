using System.ComponentModel.DataAnnotations;

namespace Eidos.Models;

/// <summary>
/// Represents a tag/folder for organizing ontologies.
/// Tags enable flexible categorization where ontologies can belong to multiple folders.
/// </summary>
public class OntologyTag
{
    public int Id { get; set; }

    public int OntologyId { get; set; }
    public Ontology Ontology { get; set; } = null!;

    /// <summary>
    /// The tag name (e.g., "Personal", "Work", "Archive")
    /// Presented to users as "folders" in the UI
    /// </summary>
    [Required]
    [StringLength(100)]
    public string Tag { get; set; } = string.Empty;

    /// <summary>
    /// Optional color for the tag/folder (hex format)
    /// </summary>
    [StringLength(7)]
    public string? Color { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
