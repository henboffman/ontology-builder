// Note Grid Keyboard Handler
window.noteGridKeyboard = {
    dotNetRef: null,
    isGridMode: false,

    /**
     * Register keyboard handlers with a .NET object reference
     * @param {any} dotNetReference - The .NET object reference for callbacks
     */
    register: function(dotNetReference) {
        console.log('[NoteGridKeyboard] Registering keyboard handlers');
        this.dotNetRef = dotNetReference;

        // Remove any existing listener
        document.removeEventListener('keydown', this.handleKeyDown);

        // Add new listener
        document.addEventListener('keydown', this.handleKeyDown.bind(this));

        console.log('[NoteGridKeyboard] Handlers registered successfully');
        return true;
    },

    /**
     * Update grid mode state
     * @param {boolean} isEnabled - Whether grid mode is enabled
     */
    setGridMode: function(isEnabled) {
        this.isGridMode = isEnabled;
        console.log('[NoteGridKeyboard] Grid mode:', isEnabled ? 'enabled' : 'disabled');
    },

    /**
     * Handle keydown events
     * @param {KeyboardEvent} event - The keyboard event
     */
    handleKeyDown: function(event) {
        // Check if we're in an input/textarea (allow normal shortcuts there)
        const isInEditor = event.target.tagName === 'TEXTAREA' || event.target.tagName === 'INPUT';

        // Alt/Option key shortcuts
        const isAlt = event.altKey && !event.ctrlKey && !event.shiftKey && !event.metaKey;
        const isAltShift = event.altKey && event.shiftKey && !event.ctrlKey && !event.metaKey;

        // Alt + G: Toggle grid mode
        if (isAlt && event.key.toLowerCase() === 'g' && !isInEditor) {
            event.preventDefault();
            this.invokeMethod('ToggleGridMode');
            return;
        }

        // Grid mode specific shortcuts (only when grid is enabled)
        if (this.isGridMode) {
            // Alt + N: Add new note to grid
            if (isAlt && event.key.toLowerCase() === 'n' && !isInEditor) {
                event.preventDefault();
                this.invokeMethod('AddNoteToGrid');
                return;
            }

            // Alt + W: Close currently focused note
            if (isAlt && event.key.toLowerCase() === 'w') {
                event.preventDefault();
                this.invokeMethod('CloseCurrentNote');
                return;
            }

            // Alt + 1/2/3/4: Focus specific note
            if (isAlt && !isInEditor) {
                const num = parseInt(event.key);
                if (num >= 1 && num <= 4) {
                    event.preventDefault();
                    this.invokeMethod('FocusNote', num - 1); // 0-indexed
                    return;
                }
            }

            // Alt + Tab: Cycle through notes
            if (isAlt && event.key === 'Tab') {
                event.preventDefault();
                const direction = event.shiftKey ? -1 : 1;
                this.invokeMethod('CycleNoteFocus', direction);
                return;
            }

            // Alt + Shift + F: Toggle full screen
            if (isAltShift && event.key.toLowerCase() === 'f' && !isInEditor) {
                event.preventDefault();
                this.invokeMethod('ToggleFullScreen');
                return;
            }
        }
    },

    /**
     * Invoke a method on the .NET object
     * @param {string} methodName - The method name to invoke
     * @param {any[]} args - Arguments to pass to the method
     */
    invokeMethod: function(methodName, ...args) {
        if (this.dotNetRef) {
            try {
                this.dotNetRef.invokeMethodAsync(methodName, ...args)
                    .catch(error => {
                        console.error(`[NoteGridKeyboard] Error invoking ${methodName}:`, error);
                    });
            } catch (error) {
                console.error(`[NoteGridKeyboard] Failed to invoke ${methodName}:`, error);
            }
        } else {
            console.warn('[NoteGridKeyboard] dotNetRef not set, cannot invoke', methodName);
        }
    },

    /**
     * Unregister keyboard handlers
     */
    unregister: function() {
        document.removeEventListener('keydown', this.handleKeyDown);
        this.dotNetRef = null;
        console.log('[NoteGridKeyboard] Handlers unregistered');
    },

    /**
     * Focus a specific note by index
     * @param {number} noteIndex - The index of the note to focus (0-based)
     */
    focusNoteByIndex: function(noteIndex) {
        const notes = document.querySelectorAll('.grid-note');
        if (notes[noteIndex]) {
            const textarea = notes[noteIndex].querySelector('textarea');
            if (textarea) {
                textarea.focus();
                return true;
            }
        }
        return false;
    },

    /**
     * Get the currently focused note index
     * @returns {number} The index of the focused note, or -1 if none
     */
    getFocusedNoteIndex: function() {
        const notes = document.querySelectorAll('.grid-note');
        for (let i = 0; i < notes.length; i++) {
            if (notes[i].classList.contains('focused')) {
                return i;
            }
        }
        return -1;
    }
};

// Initialize when DOM is ready
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', () => {
        console.log('[NoteGridKeyboard] DOM ready');
    });
}

// Clean up on Blazor disposal
if (window.Blazor) {
    window.Blazor.addEventListener('beforeDispose', () => {
        window.noteGridKeyboard.unregister();
    });
}
