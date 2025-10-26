using Microsoft.JSInterop;

namespace Eidos.Services
{
    public class TutorialStep
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? TargetSelector { get; set; }
        public string Position { get; set; } = "bottom"; // top, bottom, left, right
    }

    public class TutorialService
    {
        private const string LocalStorageKey = "ontology_builder_tutorial_seen";
        private bool _hasSeenTutorial = false;
        private bool _initialized = false;

        public event Action? OnTutorialStateChanged;

        public bool HasSeenTutorial
        {
            get => _hasSeenTutorial;
            private set
            {
                _hasSeenTutorial = value;
                OnTutorialStateChanged?.Invoke();
            }
        }

        public async Task InitializeAsync(IJSRuntime jsRuntime)
        {
            if (_initialized) return;

            try
            {
                var value = await jsRuntime.InvokeAsync<string?>("localStorage.getItem", LocalStorageKey);
                _hasSeenTutorial = value == "true";
                _initialized = true;
            }
            catch
            {
                // If localStorage is not available, default to false
                _hasSeenTutorial = false;
                _initialized = true;
            }
        }

        public async Task MarkTutorialAsCompleteAsync(IJSRuntime jsRuntime)
        {
            HasSeenTutorial = true;
            try
            {
                await jsRuntime.InvokeVoidAsync("localStorage.setItem", LocalStorageKey, "true");
            }
            catch
            {
                // Silently fail if localStorage is not available
            }
        }

        public async Task ResetTutorialAsync(IJSRuntime jsRuntime)
        {
            HasSeenTutorial = false;
            try
            {
                await jsRuntime.InvokeVoidAsync("localStorage.removeItem", LocalStorageKey);
            }
            catch
            {
                // Silently fail if localStorage is not available
            }
        }

        public List<TutorialStep> GetHomeTutorialSteps()
        {
            return new List<TutorialStep>
            {
                new TutorialStep
                {
                    Title = "Welcome to Eidos!",
                    Description = "This tutorial will guide you through the basics of creating and managing ontologies. Let's get started!"
                },
                new TutorialStep
                {
                    Title = "Create Your First Ontology",
                    Description = "Click the 'Create New Ontology' button to start building your knowledge graph. Give it a meaningful name that describes your domain.",
                    TargetSelector = ".create-ontology-btn"
                },
                new TutorialStep
                {
                    Title = "Recent Ontologies",
                    Description = "Your recent ontologies appear in the sidebar for quick access. The most recently updated ones are shown at the top.",
                    TargetSelector = ".sidebar",
                    Position = "right"
                }
            };
        }

        public List<TutorialStep> GetOntologyEditorTutorialSteps()
        {
            return new List<TutorialStep>
            {
                new TutorialStep
                {
                    Title = "View Modes",
                    Description = "Switch between different views: Graph for visual representation, List for detailed editing, TTL for export format, Notes for documentation, and Templates for reusable concept patterns.",
                    TargetSelector = ".btn-group",
                    Position = "bottom"
                },
                new TutorialStep
                {
                    Title = "Add Concepts",
                    Description = "Concepts are the building blocks of your ontology. Click 'Add Concept' or press Ctrl+K to create a new concept representing an entity, class, or idea.",
                    Position = "bottom"
                },
                new TutorialStep
                {
                    Title = "Create Relationships",
                    Description = "Connect concepts with relationships to define how they relate to each other. Click 'Add Relationship' or press Ctrl+R.",
                    Position = "bottom"
                },
                new TutorialStep
                {
                    Title = "Undo/Redo",
                    Description = "Made a mistake? Use the Undo (Ctrl+Z) and Redo (Ctrl+Y) buttons to reverse or reapply changes.",
                    TargetSelector = ".btn-group:last-child",
                    Position = "bottom"
                },
                new TutorialStep
                {
                    Title = "Search and Filter",
                    Description = "In List view, use the search box (Ctrl+F) to quickly find concepts. Results update as you type.",
                    Position = "bottom"
                },
                new TutorialStep
                {
                    Title = "Keyboard Shortcuts",
                    Description = "Press '?' anytime to see all available keyboard shortcuts for faster navigation and editing.",
                    Position = "center"
                }
            };
        }
    }
}
