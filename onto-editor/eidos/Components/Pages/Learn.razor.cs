using Eidos.Models;
using Eidos.Services;
using Eidos.Services.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Eidos.Components.Pages;

public partial class Learn : ComponentBase
{
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private IOntologyService OntologyService { get; set; } = default!;
    [Inject] private IConceptService ConceptService { get; set; } = default!;
    [Inject] private IRelationshipService RelationshipService { get; set; } = default!;
    [Inject] private ToastService ToastService { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;

    private string activeSection = "intro";
    private HashSet<string> visitedSections = new();
    private int expandedConcept = 0;
    private int expandedPattern = 0;
    private string selectedDomain = "";
    private bool showHints = false;

    private string userConcepts = "";
    private string userRelationships = "";
    private string userProperties = "";

    private int totalSections = 6;
    private int completedSections => visitedSections.Count;
    private int progressPercent => (int)((double)completedSections / totalSections * 100);

    // Placeholder text with actual newlines
    private string conceptsPlaceholder = "e.g.,\nStudent\nProfessor\nCourse\n...";
    private string relationshipsPlaceholder = "e.g.,\nStudent enrolls-in Course\nProfessor teaches Course\n...";
    private string propertiesPlaceholder = "e.g.,\nStudent: studentID, name\nCourse: courseCode, credits\n...";

    protected override void OnInitialized()
    {
        // Auto-mark intro as visited on page load
        visitedSections.Add("intro");
    }

    private async Task ScrollToSection(string section)
    {
        activeSection = section;
        if (!visitedSections.Contains(section))
        {
            visitedSections.Add(section);
        }

        // Scroll to the section using JavaScript
        await JS.InvokeVoidAsync("eval", $"document.getElementById('{section}')?.scrollIntoView({{ behavior: 'smooth', block: 'start' }});");

        StateHasChanged();
    }

    private void MarkSectionComplete(string section)
    {
        if (!visitedSections.Contains(section))
        {
            visitedSections.Add(section);
        }

        // Auto-scroll to next section
        var sections = new[] { "intro", "concepts", "example", "practice", "usecases", "bestpractices" };
        var currentIndex = Array.IndexOf(sections, section);
        if (currentIndex >= 0 && currentIndex < sections.Length - 1)
        {
            activeSection = sections[currentIndex + 1];
        }

        StateHasChanged();
    }

    private void ToggleConcept(int concept)
    {
        expandedConcept = expandedConcept == concept ? 0 : concept;
        StateHasChanged();
    }

    private void TogglePattern(int pattern)
    {
        expandedPattern = expandedPattern == pattern ? 0 : pattern;
        StateHasChanged();
    }

    private void SelectDomain(string domain)
    {
        selectedDomain = domain;
        userConcepts = "";
        userRelationships = "";
        userProperties = "";
        showHints = false;
        StateHasChanged();
    }

    private string GetDomainName()
    {
        return selectedDomain switch
        {
            "university" => "University System",
            "restaurant" => "Restaurant Management",
            "hospital" => "Hospital Operations",
            "custom" => "Your Custom Domain",
            _ => ""
        };
    }

    private string GetDomainDescription()
    {
        return selectedDomain switch
        {
            "university" => "Model the relationships between students, courses, professors, and departments.",
            "restaurant" => "Design a system for orders, menu items, ingredients, and staff.",
            "hospital" => "Create an ontology for patients, doctors, treatments, and departments.",
            "custom" => "Choose any domain you're interested in and design your own ontology!",
            _ => ""
        };
    }

    private string GetDomainHints()
    {
        return selectedDomain switch
        {
            "university" => @"
                <strong>Concepts:</strong> Student, Professor, Course, Department, Degree, Classroom<br/>
                <strong>Relationships:</strong> Student enrolls-in Course, Professor teaches Course, Department offers Course<br/>
                <strong>Properties:</strong> Student: studentID, GPA; Course: courseCode, credits, semester
            ",
            "restaurant" => @"
                <strong>Concepts:</strong> Customer, Order, MenuItem, Chef, Table, Ingredient<br/>
                <strong>Relationships:</strong> Customer places Order, Chef prepares MenuItem, MenuItem contains Ingredient<br/>
                <strong>Properties:</strong> MenuItem: name, price, calories; Order: orderNumber, timestamp, totalCost
            ",
            "hospital" => @"
                <strong>Concepts:</strong> Patient, Doctor, Nurse, Department, Treatment, Prescription<br/>
                <strong>Relationships:</strong> Doctor treats Patient, Patient receives Treatment, Doctor prescribes Prescription<br/>
                <strong>Properties:</strong> Patient: patientID, age, bloodType; Treatment: name, duration, cost
            ",
            _ => ""
        };
    }

    private void ToggleHints()
    {
        showHints = !showHints;
        StateHasChanged();
    }

    private async Task CreatePracticeOntology()
    {
        try
        {
            // Validate that user has entered something
            if (string.IsNullOrWhiteSpace(userConcepts))
            {
                ToastService.ShowWarning("Please list at least one concept before creating your ontology.");
                return;
            }

            // Create the ontology
            var ontologyName = $"My {GetDomainName()} Ontology";
            var description = $"A practice ontology created from the Learn page for {GetDomainName().ToLower()}.";

            var newOntology = new Models.Ontology
            {
                Name = ontologyName,
                Description = description
            };

            var ontology = await OntologyService.CreateOntologyAsync(newOntology);

            // Parse and create concepts
            var conceptLines = userConcepts.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var createdConcepts = new Dictionary<string, Models.Concept>();

            foreach (var line in conceptLines)
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    var conceptName = line.Trim();
                    var concept = new Models.Concept
                    {
                        OntologyId = ontology.Id,
                        Name = conceptName,
                        Category = "Entity", // Default category
                        Definition = $"A {conceptName.ToLower()} in the {GetDomainName().ToLower()} domain.",
                        Color = GenerateRandomColor()
                    };

                    var created = await ConceptService.CreateAsync(concept);
                    createdConcepts[conceptName.ToLower()] = created;
                }
            }

            // Parse and create relationships if provided
            if (!string.IsNullOrWhiteSpace(userRelationships))
            {
                var relationshipLines = userRelationships.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                foreach (var line in relationshipLines)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        // Try to parse "Source relationship Target" format
                        var parts = line.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 3)
                        {
                            var sourceName = parts[0].ToLower();
                            var targetName = parts[^1].ToLower();
                            var relationType = string.Join(" ", parts.Skip(1).Take(parts.Length - 2));

                            // Find matching concepts
                            var source = createdConcepts.Values.FirstOrDefault(c => c.Name.ToLower().Contains(sourceName));
                            var target = createdConcepts.Values.FirstOrDefault(c => c.Name.ToLower().Contains(targetName));

                            if (source != null && target != null)
                            {
                                var relationship = new Models.Relationship
                                {
                                    OntologyId = ontology.Id,
                                    SourceConceptId = source.Id,
                                    TargetConceptId = target.Id,
                                    RelationType = relationType,
                                    Description = $"{source.Name} {relationType} {target.Name}"
                                };

                                await RelationshipService.CreateAsync(relationship);
                            }
                        }
                    }
                }
            }

            // Show success message
            ToastService.ShowSuccess($"Created '{ontologyName}' with {createdConcepts.Count} concepts!");

            // Navigate to the newly created ontology
            Navigation.NavigateTo($"ontology/{ontology.Id}");
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"Error creating ontology: {ex.Message}");
        }
    }

    private string GenerateRandomColor()
    {
        var colors = new[]
        {
            "#4A90E2", "#E67E22", "#6BCF7F", "#9B59B6", "#E74C3C",
            "#3498DB", "#F39C12", "#1ABC9C", "#E91E63", "#9C27B0"
        };
        return colors[Random.Shared.Next(colors.Length)];
    }

    private void NavigateToHome()
    {
        Navigation.NavigateTo("");
    }

    private void NavigateToTemplates()
    {
        Navigation.NavigateTo("");
    }
}
