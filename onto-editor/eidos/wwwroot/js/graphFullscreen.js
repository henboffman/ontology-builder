// Graph Fullscreen API Handler
window.graphFullscreen = {
    fullscreenElement: null,
    originalModalParents: new Map(), // Track where modals came from

    enterFullscreen: function() {
        // Get the graph container element
        const graphContainer = document.querySelector('.graph-full-screen');

        if (!graphContainer) {
            console.error('Graph container not found for fullscreen');
            return;
        }

        this.fullscreenElement = graphContainer;

        // Request fullscreen using the appropriate API
        if (graphContainer.requestFullscreen) {
            graphContainer.requestFullscreen().catch(err => {
                console.error('Error attempting to enable fullscreen:', err);
            });
        } else if (graphContainer.webkitRequestFullscreen) { // Safari
            graphContainer.webkitRequestFullscreen();
        } else if (graphContainer.mozRequestFullScreen) { // Firefox
            graphContainer.mozRequestFullScreen();
        } else if (graphContainer.msRequestFullscreen) { // IE/Edge
            graphContainer.msRequestFullscreen();
        } else {
            console.warn('Fullscreen API not supported on this browser');
        }
    },

    exitFullscreen: function() {
        // Only exit if actually in fullscreen
        if (!this.isFullscreen()) {
            console.log('[Fullscreen] Not in fullscreen mode, skipping exit');
            return;
        }

        // Exit fullscreen using the appropriate API
        if (document.exitFullscreen) {
            document.exitFullscreen().catch(err => {
                console.error('Error attempting to exit fullscreen:', err);
            });
        } else if (document.webkitExitFullscreen) { // Safari
            document.webkitExitFullscreen();
        } else if (document.mozCancelFullScreen) { // Firefox
            document.mozCancelFullScreen();
        } else if (document.msExitFullscreen) { // IE/Edge
            document.msExitFullscreen();
        }
    },

    // Check if currently in fullscreen mode
    isFullscreen: function() {
        return !!(document.fullscreenElement ||
                  document.webkitFullscreenElement ||
                  document.mozFullScreenElement ||
                  document.msFullscreenElement);
    },

    // Move a modal into the fullscreen container
    moveModalToFullscreen: function(modal) {
        console.log('[Fullscreen] moveModalToFullscreen called', {
            modal: modal,
            isFullscreen: this.isFullscreen(),
            hasFullscreenElement: !!this.fullscreenElement
        });

        if (!this.isFullscreen() || !this.fullscreenElement) {
            console.log('[Fullscreen] Skipping modal move - not in fullscreen or no fullscreen element');
            return;
        }

        // Store the original parent if not already stored
        if (!this.originalModalParents.has(modal)) {
            this.originalModalParents.set(modal, modal.parentElement);
            console.log('[Fullscreen] Stored original parent for modal');
        }

        // Move modal into fullscreen container
        this.fullscreenElement.appendChild(modal);
        console.log('[Fullscreen] Moved modal into fullscreen container');

        // Ensure modal and backdrop are visible with high z-index
        modal.style.zIndex = '10002';

        // Also move the backdrop if it exists
        const backdrop = document.querySelector('.modal-backdrop');
        if (backdrop && !this.fullscreenElement.contains(backdrop)) {
            this.fullscreenElement.appendChild(backdrop);
            backdrop.style.zIndex = '10001';
            console.log('[Fullscreen] Moved backdrop into fullscreen container');
        }
    },

    // Restore modal to its original position
    restoreModal: function(modal) {
        const originalParent = this.originalModalParents.get(modal);
        if (originalParent && modal.parentElement !== originalParent) {
            originalParent.appendChild(modal);
            modal.style.zIndex = ''; // Reset z-index

            // Also restore backdrop
            const backdrop = document.querySelector('.modal-backdrop');
            if (backdrop && this.fullscreenElement && this.fullscreenElement.contains(backdrop)) {
                document.body.appendChild(backdrop);
                backdrop.style.zIndex = '';
            }
        }
        this.originalModalParents.delete(modal);
    }
};

// Listen for fullscreen change events to handle ESC key exits
document.addEventListener('fullscreenchange', handleFullscreenChange);
document.addEventListener('webkitfullscreenchange', handleFullscreenChange);
document.addEventListener('mozfullscreenchange', handleFullscreenChange);
document.addEventListener('MSFullscreenChange', handleFullscreenChange);

function handleFullscreenChange() {
    if (!window.graphFullscreen.isFullscreen()) {
        // User exited fullscreen (probably with ESC key)
        // Restore all modals to their original positions
        const modals = document.querySelectorAll('.modal');
        modals.forEach(modal => {
            window.graphFullscreen.restoreModal(modal);
        });

        // Trigger the exit button click to sync the UI state
        const exitBtn = document.querySelector('.exit-fullscreen-btn');
        if (exitBtn) {
            exitBtn.click();
        }
    }
}

// Set up Bootstrap modal event listeners immediately
// Use event delegation to catch all modal show events
console.log('[Fullscreen] Setting up modal event listeners');

// Function to set up listeners (call immediately and on DOMContentLoaded)
function setupModalListeners() {
    console.log('[Fullscreen] setupModalListeners called');

    // Remove any existing listeners to avoid duplicates
    if (window.graphFullscreen._listenersSetup) {
        console.log('[Fullscreen] Listeners already set up, skipping');
        return;
    }
    window.graphFullscreen._listenersSetup = true;

    document.body.addEventListener('shown.bs.modal', function(event) {
        console.log('[Fullscreen] Bootstrap shown.bs.modal event fired', event.target);
        const modal = event.target;
        if (window.graphFullscreen.isFullscreen()) {
            console.log('[Fullscreen] In fullscreen, will move modal');
            // Small delay to ensure modal is fully rendered
            setTimeout(() => {
                window.graphFullscreen.moveModalToFullscreen(modal);
            }, 10);
        } else {
            console.log('[Fullscreen] Not in fullscreen, skipping modal move');
        }
    });

    // Restore modal when hidden
    document.body.addEventListener('hidden.bs.modal', function(event) {
        console.log('[Fullscreen] Bootstrap hidden.bs.modal event fired');
        const modal = event.target;
        window.graphFullscreen.restoreModal(modal);
    });
}

// Try to set up listeners immediately
if (document.body) {
    setupModalListeners();
} else {
    // If body doesn't exist yet, wait for DOMContentLoaded
    document.addEventListener('DOMContentLoaded', setupModalListeners);
}

// Also watch for modals being added to the DOM (as a fallback)
const modalObserver = new MutationObserver((mutations) => {
    mutations.forEach((mutation) => {
        mutation.addedNodes.forEach((node) => {
            if (node.nodeType === 1 && node.classList && node.classList.contains('modal')) {
                console.log('[Fullscreen] MutationObserver detected modal added to DOM', node);
                if (window.graphFullscreen.isFullscreen()) {
                    // Check if modal is being shown (has 'show' class)
                    const checkModal = () => {
                        if (node.classList.contains('show')) {
                            console.log('[Fullscreen] Modal has show class, moving to fullscreen');
                            window.graphFullscreen.moveModalToFullscreen(node);
                        } else {
                            console.log('[Fullscreen] Modal does not have show class yet, will retry');
                            setTimeout(checkModal, 50);
                        }
                    };
                    setTimeout(checkModal, 50);
                }
            }
        });
    });
});

// Start observing the document for modal additions
if (document.body) {
    modalObserver.observe(document.body, { childList: true, subtree: true });
    console.log('[Fullscreen] MutationObserver started');
} else {
    document.addEventListener('DOMContentLoaded', () => {
        modalObserver.observe(document.body, { childList: true, subtree: true });
        console.log('[Fullscreen] MutationObserver started (after DOMContentLoaded)');
    });
}
