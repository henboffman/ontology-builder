// Wiki-link preview handler
// Handles clicks on rendered wiki-links in the preview pane

window.wikiLinkPreviewHandler = null;

window.handleWikiLinkClick = function (element, event) {

    // Prevent default link behavior
    if (event) {
        event.preventDefault();
    }

    // Get concept name from data attribute
    const conceptName = element.getAttribute('data-concept');

    if (!conceptName) {
        console.error('Wiki-link missing data-concept attribute');
        return false;
    }

    // Call back to Blazor
    if (window.wikiLinkPreviewHandler) {
        window.wikiLinkPreviewHandler.invokeMethodAsync('NavigateToConcept', conceptName)
            .catch(err => console.error('NavigateToConcept failed:', err));
    } else {
        console.error('Wiki-link preview handler not initialized! Handler is:', window.wikiLinkPreviewHandler);
    }

    return false;
};

window.initializeWikiLinkPreview = function (dotNetHelper) {
    window.wikiLinkPreviewHandler = dotNetHelper;
};

window.disposeWikiLinkPreview = function () {
    window.wikiLinkPreviewHandler = null;
};
