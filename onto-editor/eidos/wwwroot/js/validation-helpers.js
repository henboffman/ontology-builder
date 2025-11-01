// Validation Panel Helper Functions

/**
 * Scrolls to an element with smooth animation
 * @param {string} elementId - ID of the element to scroll to
 */
window.scrollToElement = function (elementId) {
    const element = document.getElementById(elementId);
    if (element) {
        element.scrollIntoView({
            behavior: 'smooth',
            block: 'center',
            inline: 'nearest'
        });

        // Add a highlight effect
        element.classList.add('validation-highlight');
        setTimeout(() => {
            element.classList.remove('validation-highlight');
        }, 2000);
    } else {
        console.warn(`Element with ID '${elementId}' not found`);
    }
};

/**
 * Scrolls the validation panel into view
 */
window.scrollToValidationPanel = function () {
    const panel = document.querySelector('.validation-panel');
    if (panel) {
        panel.scrollIntoView({
            behavior: 'smooth',
            block: 'start'
        });
    }
};
