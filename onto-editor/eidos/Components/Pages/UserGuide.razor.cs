using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Eidos.Components.Pages;

public partial class UserGuide : ComponentBase
{
    [Inject]
    private IJSRuntime JS { get; set; } = default!;

    private string activeSection = "getting-started";

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await UpdateActiveSection();
        }
    }

    private async Task ScrollToSection(string sectionId)
    {
        await JS.InvokeVoidAsync("scrollToSection", sectionId);
        activeSection = sectionId;
        StateHasChanged();
    }

    private async Task UpdateActiveSection()
    {
        // This would be called on scroll to update active section
        StateHasChanged();
    }
}
