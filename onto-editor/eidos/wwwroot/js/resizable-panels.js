// Resizable Panels for Ontology View
// ============================================================================

let activeResizer = null;
let startPos = 0;
let startSize = 0;

/**
 * Initialize resizable panels
 */
window.initializeResizablePanels = function() {
    // Initialize vertical resizer (between main content and details panel)
    const verticalResizer = document.querySelector('.vertical-resizer');
    if (verticalResizer) {
        verticalResizer.addEventListener('mousedown', startVerticalResize);
    }

    // Initialize horizontal resizer (validation panel)
    const horizontalResizer = document.querySelector('.horizontal-resizer');
    if (horizontalResizer) {
        horizontalResizer.addEventListener('mousedown', startHorizontalResize);
    }

    // Global mouse handlers
    document.addEventListener('mousemove', handleMouseMove);
    document.addEventListener('mouseup', stopResize);
};

/**
 * Start vertical resize (details panel width)
 */
function startVerticalResize(e) {
    e.preventDefault();
    const detailsPanel = document.querySelector('.ontology-details-panel');
    if (!detailsPanel || detailsPanel.classList.contains('collapsed')) return;

    activeResizer = 'vertical';
    startPos = e.clientX;
    startSize = detailsPanel.offsetWidth;

    document.body.classList.add('resizing-in-progress');
    e.target.classList.add('resizing');
}

/**
 * Start horizontal resize (validation panel height)
 */
function startHorizontalResize(e) {
    e.preventDefault();
    const validationPanel = document.querySelector('.ontology-validation-panel');
    if (!validationPanel || validationPanel.classList.contains('collapsed')) return;

    activeResizer = 'horizontal';
    startPos = e.clientY;
    startSize = validationPanel.offsetHeight;

    document.body.classList.add('resizing-in-progress', 'horizontal');
    e.target.classList.add('resizing');
}

/**
 * Handle mouse move during resize
 */
function handleMouseMove(e) {
    if (!activeResizer) return;

    if (activeResizer === 'vertical') {
        const detailsPanel = document.querySelector('.ontology-details-panel');
        if (!detailsPanel) return;

        const delta = startPos - e.clientX;
        const newWidth = startSize + delta;

        const minWidth = parseInt(getComputedStyle(detailsPanel).minWidth) || 250;
        const maxWidth = parseInt(getComputedStyle(detailsPanel).maxWidth) || 600;

        if (newWidth >= minWidth && newWidth <= maxWidth) {
            detailsPanel.style.width = newWidth + 'px';
        }
    } else if (activeResizer === 'horizontal') {
        const validationPanel = document.querySelector('.ontology-validation-panel');
        if (!validationPanel) return;

        const delta = startPos - e.clientY;
        const newHeight = startSize + delta;

        const minHeight = parseInt(getComputedStyle(validationPanel).minHeight) || 100;
        const maxHeight = parseInt(getComputedStyle(validationPanel).maxHeight) || 600;

        if (newHeight >= minHeight && newHeight <= maxHeight) {
            validationPanel.style.height = newHeight + 'px';
        }
    }
}

/**
 * Stop resize
 */
function stopResize() {
    if (!activeResizer) return;

    const resizer = document.querySelector('.vertical-resizer, .horizontal-resizer');
    if (resizer) {
        resizer.classList.remove('resizing');
    }

    document.body.classList.remove('resizing-in-progress', 'horizontal');
    activeResizer = null;
}

/**
 * Toggle details panel visibility
 */
window.toggleDetailsPanel = function() {
    const detailsPanel = document.querySelector('.ontology-details-panel');
    if (detailsPanel) {
        detailsPanel.classList.toggle('collapsed');
    }
};

/**
 * Toggle validation panel visibility
 */
window.toggleValidationPanel = function() {
    const validationPanel = document.querySelector('.ontology-validation-panel');
    if (validationPanel) {
        validationPanel.classList.toggle('collapsed');
    }
};

/**
 * Cleanup resizable panels
 */
window.cleanupResizablePanels = function() {
    const verticalResizer = document.querySelector('.vertical-resizer');
    if (verticalResizer) {
        verticalResizer.removeEventListener('mousedown', startVerticalResize);
    }

    const horizontalResizer = document.querySelector('.horizontal-resizer');
    if (horizontalResizer) {
        horizontalResizer.removeEventListener('mousedown', startHorizontalResize);
    }

    document.removeEventListener('mousemove', handleMouseMove);
    document.removeEventListener('mouseup', stopResize);
};

// Initialize on DOM load
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', initializeResizablePanels);
} else {
    initializeResizablePanels();
}
