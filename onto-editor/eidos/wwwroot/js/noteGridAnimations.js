// Note Grid Animations Handler
window.noteGridAnimations = {
    // Track animation states
    animatingNodes: new Set(),

    /**
     * Initialize animation observers for the grid
     */
    initialize: function() {
        console.log('[NoteGridAnimations] Initializing...');

        // Observe grid container for note additions/removals
        const observer = new MutationObserver((mutations) => {
            mutations.forEach((mutation) => {
                mutation.addedNodes.forEach((node) => {
                    if (node.nodeType === 1 && node.classList && node.classList.contains('grid-note')) {
                        this.triggerEnterAnimation(node);
                    }
                });

                mutation.removedNodes.forEach((node) => {
                    if (node.nodeType === 1 && node.classList && node.classList.contains('grid-note')) {
                        this.triggerExitAnimation(node);
                    }
                });
            });
        });

        // Observe all grid containers
        const containers = document.querySelectorAll('.note-grid-container');
        containers.forEach(container => {
            observer.observe(container, {
                childList: true,
                subtree: false
            });
        });

        console.log('[NoteGridAnimations] Initialized successfully');
        return true;
    },

    /**
     * Trigger entry animation for a note
     */
    triggerEnterAnimation: function(noteElement) {
        if (this.animatingNodes.has(noteElement)) {
            return; // Already animating
        }

        this.animatingNodes.add(noteElement);

        // Add entering class
        noteElement.classList.add('entering');

        // Remove class after animation completes
        setTimeout(() => {
            noteElement.classList.remove('entering');
            this.animatingNodes.delete(noteElement);
        }, 350); // Match animation duration
    },

    /**
     * Trigger exit animation for a note
     */
    triggerExitAnimation: function(noteElement) {
        if (this.animatingNodes.has(noteElement)) {
            return; // Already animating
        }

        this.animatingNodes.add(noteElement);

        // Add exiting class
        noteElement.classList.add('exiting');

        // Clean up after animation
        setTimeout(() => {
            this.animatingNodes.delete(noteElement);
        }, 300); // Match animation duration
    },

    /**
     * Trigger reflow animation when grid layout changes
     */
    triggerReflowAnimation: function() {
        const notes = document.querySelectorAll('.grid-note:not(.entering):not(.exiting)');

        notes.forEach((note, index) => {
            // Skip notes that are currently animating
            if (this.animatingNodes.has(note)) {
                return;
            }

            // Add reflow class with stagger
            setTimeout(() => {
                note.classList.add('reflowing');

                // Remove after animation
                setTimeout(() => {
                    note.classList.remove('reflowing');
                }, 400);
            }, index * 50); // Stagger by 50ms
        });
    },

    /**
     * Trigger focus pulse animation
     */
    triggerFocusAnimation: function(noteId) {
        const note = document.querySelector(`.grid-note[data-note-id="${noteId}"]`);
        if (note && !this.animatingNodes.has(note)) {
            // The CSS handles the focus animation via the .focused class
            // This is just a helper to manually trigger it if needed
            note.classList.remove('focused');
            // Force reflow
            void note.offsetWidth;
            note.classList.add('focused');
        }
    },

    /**
     * Add data attributes to notes for easier targeting
     */
    attachNoteIds: function() {
        const notes = document.querySelectorAll('.grid-note');
        notes.forEach((note, index) => {
            if (!note.hasAttribute('data-note-id')) {
                note.setAttribute('data-note-id', `note-${index}`);
            }
        });
    },

    /**
     * Clean up animations
     */
    cleanup: function() {
        this.animatingNodes.clear();
        console.log('[NoteGridAnimations] Cleaned up');
    }
};

// Initialize when DOM is ready
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', () => {
        window.noteGridAnimations.initialize();
    });
} else {
    window.noteGridAnimations.initialize();
}

// Re-initialize on Blazor reconnection
if (window.Blazor) {
    window.Blazor.addEventListener('reconnected', () => {
        console.log('[NoteGridAnimations] Blazor reconnected, reinitializing...');
        window.noteGridAnimations.initialize();
    });
}
