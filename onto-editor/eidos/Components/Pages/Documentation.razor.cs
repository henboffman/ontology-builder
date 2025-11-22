using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Eidos.Components.Pages;

public partial class Documentation : ComponentBase
{
    [Inject]
    private IJSRuntime JS { get; set; } = default!;

    private string activeSection = "overview";
    private int expandedConcept = 0;

    private async Task ScrollToSection(string section)
    {
        activeSection = section;
        await JS.InvokeVoidAsync("eval", $"document.getElementById('{section}')?.scrollIntoView({{ behavior: 'smooth', block: 'start' }});");
        StateHasChanged();
    }

    private void ToggleConcept(int concept)
    {
        expandedConcept = expandedConcept == concept ? 0 : concept;
        StateHasChanged();
    }
}
