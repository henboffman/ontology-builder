using Microsoft.EntityFrameworkCore;
using Eidos.Data;
using Eidos.Models;
using Eidos.Services.Interfaces;

namespace Eidos.Services
{
    public class OntologyTemplateService
    {
        private readonly IDbContextFactory<OntologyDbContext> _contextFactory;
        private readonly IOntologyService _ontologyService;
        private readonly IUserPreferencesService _userPreferencesService;

        public OntologyTemplateService(
            IDbContextFactory<OntologyDbContext> contextFactory,
            IOntologyService ontologyService,
            IUserPreferencesService userPreferencesService)
        {
            _contextFactory = contextFactory;
            _ontologyService = ontologyService;
            _userPreferencesService = userPreferencesService;
        }

        public async Task<Ontology> CreateFromBFOTemplateAsync(string name, string? description, string? author)
        {
            // Load user preferences for color defaults
            UserPreferences prefs;
            try
            {
                prefs = await _userPreferencesService.GetCurrentUserPreferencesAsync();
            }
            catch
            {
                // Fallback to new preferences with defaults if loading fails
                prefs = new UserPreferences();
            }

            // Create the ontology
            var ontology = new Ontology
            {
                Name = name,
                Description = description ?? "Based on Basic Formal Ontology (BFO) - a top-level ontology framework",
                Author = author,
                Version = "1.0 (BFO-based)",
                UsesBFO = true
            };

            ontology = await _ontologyService.CreateOntologyAsync(ontology);

            // Create BFO top-level concepts using user preference colors
            var entity = await _ontologyService.CreateConceptAsync(new Concept
            {
                OntologyId = ontology.Id,
                Name = "Entity",
                Definition = "Anything that exists or has existed or will exist",
                SimpleExplanation = "The most general category - everything is an entity",
                Examples = "A person, a process, a quality, a location",
                Category = "Entity",
                Color = prefs.GetColorForCategory("Entity")
            });

            var continuant = await _ontologyService.CreateConceptAsync(new Concept
            {
                OntologyId = ontology.Id,
                Name = "Continuant",
                Definition = "An entity that continues to exist through time while maintaining its identity",
                SimpleExplanation = "Things that persist over time (objects, qualities)",
                Examples = "A person, a table, the color red, a temperature",
                Category = "Entity",
                Color = prefs.GetColorForCategory("Entity")
            });

            var occurrent = await _ontologyService.CreateConceptAsync(new Concept
            {
                OntologyId = ontology.Id,
                Name = "Occurrent",
                Definition = "An entity that unfolds or happens in time",
                SimpleExplanation = "Processes, events, and temporal regions",
                Examples = "Running, a concert, sleeping, the year 2024",
                Category = "Event",
                Color = prefs.GetColorForCategory("Event")
            });

            var independentContinuant = await _ontologyService.CreateConceptAsync(new Concept
            {
                OntologyId = ontology.Id,
                Name = "Independent Continuant",
                Definition = "A continuant that can exist by itself",
                SimpleExplanation = "Physical objects and spatial regions",
                Examples = "A person, a rock, a room, Earth",
                Category = "Entity",
                Color = prefs.GetColorForCategory("Entity")
            });

            var dependentContinuant = await _ontologyService.CreateConceptAsync(new Concept
            {
                OntologyId = ontology.Id,
                Name = "Dependent Continuant",
                Definition = "A continuant that depends on other entities to exist",
                SimpleExplanation = "Qualities, roles, and functions that need a bearer",
                Examples = "The color of a ball, someone's role as a teacher, the function of a heart",
                Category = "Quality",
                Color = prefs.GetColorForCategory("Quality")
            });

            var process = await _ontologyService.CreateConceptAsync(new Concept
            {
                OntologyId = ontology.Id,
                Name = "Process",
                Definition = "An occurrent that has temporal parts and happens over time",
                SimpleExplanation = "Activities and changes that take time",
                Examples = "Walking, growing, a chemical reaction, learning",
                Category = "Process",
                Color = prefs.GetColorForCategory("Process")
            });

            var temporalRegion = await _ontologyService.CreateConceptAsync(new Concept
            {
                OntologyId = ontology.Id,
                Name = "Temporal Region",
                Definition = "An occurrent that is a region of time",
                SimpleExplanation = "Time intervals and instants",
                Examples = "The year 2024, noon today, the 21st century",
                Category = "Event",
                Color = prefs.GetColorForCategory("Event")
            });

            // Create relationships
            await _ontologyService.CreateRelationshipAsync(new Relationship
            {
                OntologyId = ontology.Id,
                SourceConceptId = continuant.Id,
                TargetConceptId = entity.Id,
                RelationType = "is-a",
                Description = "Continuants are a type of entity"
            });

            await _ontologyService.CreateRelationshipAsync(new Relationship
            {
                OntologyId = ontology.Id,
                SourceConceptId = occurrent.Id,
                TargetConceptId = entity.Id,
                RelationType = "is-a",
                Description = "Occurrents are a type of entity"
            });

            await _ontologyService.CreateRelationshipAsync(new Relationship
            {
                OntologyId = ontology.Id,
                SourceConceptId = independentContinuant.Id,
                TargetConceptId = continuant.Id,
                RelationType = "is-a",
                Description = "Independent continuants are a type of continuant"
            });

            await _ontologyService.CreateRelationshipAsync(new Relationship
            {
                OntologyId = ontology.Id,
                SourceConceptId = dependentContinuant.Id,
                TargetConceptId = continuant.Id,
                RelationType = "is-a",
                Description = "Dependent continuants are a type of continuant"
            });

            await _ontologyService.CreateRelationshipAsync(new Relationship
            {
                OntologyId = ontology.Id,
                SourceConceptId = process.Id,
                TargetConceptId = occurrent.Id,
                RelationType = "is-a",
                Description = "Processes are a type of occurrent"
            });

            await _ontologyService.CreateRelationshipAsync(new Relationship
            {
                OntologyId = ontology.Id,
                SourceConceptId = temporalRegion.Id,
                TargetConceptId = occurrent.Id,
                RelationType = "is-a",
                Description = "Temporal regions are a type of occurrent"
            });

            await _ontologyService.CreateRelationshipAsync(new Relationship
            {
                OntologyId = ontology.Id,
                SourceConceptId = dependentContinuant.Id,
                TargetConceptId = independentContinuant.Id,
                RelationType = "depends-on",
                Description = "Dependent continuants require independent continuants to exist"
            });

            // Reload to get all relationships populated
            return await _ontologyService.GetOntologyAsync(ontology.Id) ?? ontology;
        }

        public async Task AddProvOConceptsAsync(Ontology ontology)
        {
            // Load user preferences for color defaults
            UserPreferences prefs;
            try
            {
                prefs = await _userPreferencesService.GetCurrentUserPreferencesAsync();
            }
            catch
            {
                // Fallback to new preferences with defaults if loading fails
                prefs = new UserPreferences();
            }

            // Create PROV-O core concepts using user preference colors
            var entity = await _ontologyService.CreateConceptAsync(new Concept
            {
                OntologyId = ontology.Id,
                Name = "prov:Entity",
                Definition = "A physical, digital, conceptual, or other kind of thing with some fixed aspects",
                SimpleExplanation = "Something that exists or existed - the subject of provenance",
                Examples = "A document, a dataset, a person, a plan",
                Category = "Entity",
                Color = prefs.GetColorForCategory("Entity"),
                SourceOntology = "PROV-O"
            });

            var activity = await _ontologyService.CreateConceptAsync(new Concept
            {
                OntologyId = ontology.Id,
                Name = "prov:Activity",
                Definition = "Something that occurs over a period of time and acts upon or with entities",
                SimpleExplanation = "An action or process that uses or generates entities",
                Examples = "Editing a document, running an analysis, publishing data",
                Category = "Process",
                Color = prefs.GetColorForCategory("Process"),
                SourceOntology = "PROV-O"
            });

            var agent = await _ontologyService.CreateConceptAsync(new Concept
            {
                OntologyId = ontology.Id,
                Name = "prov:Agent",
                Definition = "Something that bears some form of responsibility for an activity taking place, for the existence of an entity, or for another agent's activity",
                SimpleExplanation = "Who or what is responsible - person, organization, or software",
                Examples = "A person, an organization, a software application",
                Category = "Role",
                Color = prefs.GetColorForCategory("Role"),
                SourceOntology = "PROV-O"
            });

            // Create PROV-O relationships
            await _ontologyService.CreateRelationshipAsync(new Relationship
            {
                OntologyId = ontology.Id,
                SourceConceptId = entity.Id,
                TargetConceptId = activity.Id,
                RelationType = "wasGeneratedBy",
                Description = "Entity was generated by an activity"
            });

            await _ontologyService.CreateRelationshipAsync(new Relationship
            {
                OntologyId = ontology.Id,
                SourceConceptId = activity.Id,
                TargetConceptId = entity.Id,
                RelationType = "used",
                Description = "Activity used an entity"
            });

            await _ontologyService.CreateRelationshipAsync(new Relationship
            {
                OntologyId = ontology.Id,
                SourceConceptId = activity.Id,
                TargetConceptId = agent.Id,
                RelationType = "wasAssociatedWith",
                Description = "Activity was associated with an agent"
            });

            await _ontologyService.CreateRelationshipAsync(new Relationship
            {
                OntologyId = ontology.Id,
                SourceConceptId = entity.Id,
                TargetConceptId = agent.Id,
                RelationType = "wasAttributedTo",
                Description = "Entity was attributed to an agent"
            });
        }
    }
}
