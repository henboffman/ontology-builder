// Wiki-link preview handler
// Handles clicks on rendered wiki-links in the preview pane

window.wikiLinkPreviewHandler = null;

window.handleWikiLinkClick = function (element, event) {
    console.log('Wiki-link clicked:', element);

    // Prevent default link behavior
    if (event) {
        event.preventDefault();
    }

    // Get concept name from data attribute
    const conceptName = element.getAttribute('data-concept');
    console.log('Concept name:', conceptName);

    if (!conceptName) {
        console.error('Wiki-link missing data-concept attribute');
        return false;
    }

    // Call back to Blazor
    if (window.wikiLinkPreviewHandler) {
        console.log('Calling NavigateToConcept with:', conceptName);
        window.wikiLinkPreviewHandler.invokeMethodAsync('NavigateToConcept', conceptName)
            .then(() => console.log('NavigateToConcept succeeded'))
            .catch(err => console.error('NavigateToConcept failed:', err));
    } else {
        console.error('Wiki-link preview handler not initialized! Handler is:', window.wikiLinkPreviewHandler);
    }

    return false;
};

window.initializeWikiLinkPreview = function (dotNetHelper) {
    window.wikiLinkPreviewHandler = dotNetHelper;
    console.log('Wiki-link preview handler initialized');
};

window.disposeWikiLinkPreview = function () {
    window.wikiLinkPreviewHandler = null;
};
