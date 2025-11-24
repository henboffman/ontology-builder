// Keyboard Shortcuts Manager
window.keyboardShortcuts = {
    dotNetHelper: null,
    handlers: new Map(),

    registerGlobalHandler: function (dotNetHelper) {
        this.dotNetHelper = dotNetHelper;

        document.addEventListener('keydown', (e) => {
            this.handleGlobalShortcut(e);
        });
    },

    registerHandler: function (context, callback) {
        this.handlers.set(context, callback);
    },

    unregisterHandler: function (context) {
        this.handlers.delete(context);
    },

    handleGlobalShortcut: function (e) {
        // Ignore shortcuts if user is typing in an input field
        const activeElement = document.activeElement;
        const isInputField = activeElement && (
            activeElement.tagName === 'INPUT' ||
            activeElement.tagName === 'TEXTAREA' ||
            activeElement.isContentEditable
        );

        // Show keyboard shortcuts help (? key)
        if (e.key === '?' && !e.ctrlKey && !e.altKey && !e.metaKey && !isInputField) {
            e.preventDefault();
            if (this.dotNetHelper) {
                this.dotNetHelper.invokeMethodAsync('Show');
            }
            return;
        }

        // Escape key to close modals
        if (e.key === 'Escape') {
            // Allow Blazor components to handle this
            return;
        }

        // Don't process other shortcuts when in input fields
        if (isInputField && e.key !== 'Escape') {
            return;
        }

        // Navigation and editing shortcuts (Alt + key)
        // Use e.code instead of e.key to avoid macOS special character issue
        if (e.altKey && !e.ctrlKey && !e.metaKey) {
            switch (e.code) {
                case 'KeyG':
                    e.preventDefault();
                    this.switchView('Graph');
                    break;
                case 'KeyL':
                    e.preventDefault();
                    this.switchView('List');
                    break;
                case 'KeyT':
                    e.preventDefault();
                    this.switchView('Ttl');
                    break;
                case 'KeyH':
                    e.preventDefault();
                    this.switchView('Hierarchy');
                    break;
                case 'KeyC':
                    e.preventDefault();
                    this.showAddConcept();
                    break;
                case 'KeyR':
                    e.preventDefault();
                    this.showAddRelationship();
                    break;
                case 'KeyF':
                    e.preventDefault();
                    this.toggleFullScreen();
                    break;
                case 'KeyN':
                    e.preventDefault();
                    // Check if we're in an ontology or workspace context
                    const ontologyMatch = window.location.pathname.match(/\/(ontology|workspace)\/(\d+)/);
                    if (ontologyMatch) {
                        // We're in an ontology/workspace, navigate to workspace view
                        const workspaceId = ontologyMatch[2];
                        window.location.href = `/workspace/${workspaceId}`;
                    } else {
                        // We're not in a workspace, go to workspaces dashboard
                        window.location.href = '/workspaces';
                    }
                    break;
                case 'KeyO':
                    e.preventDefault();
                    // Check if we're in an ontology or workspace context
                    const workspaceMatch = window.location.pathname.match(/\/(ontology|workspace)\/(\d+)/);
                    if (workspaceMatch) {
                        // We're in an ontology/workspace, navigate to ontology view
                        const ontologyId = workspaceMatch[2];
                        window.location.href = `/ontology/${ontologyId}`;
                    } else {
                        // We're not in a workspace, go to ontologies dashboard
                        window.location.href = '/';
                    }
                    break;
            }
        }

        // Editing shortcuts (Ctrl/Cmd + key)
        // Use e.code for letter keys, e.key for special keys like comma
        if ((e.ctrlKey || e.metaKey) && !e.altKey && !e.shiftKey) {
            switch (e.code) {
                case 'KeyZ':
                    e.preventDefault();
                    this.performUndo();
                    break;
                case 'KeyY':
                    e.preventDefault();
                    this.performRedo();
                    break;
                case 'KeyK':
                    // Command palette shortcut
                    e.preventDefault();
                    // Focus the global search input
                    const searchInput = document.querySelector('[data-global-search-input]');
                    if (searchInput) {
                        searchInput.focus();
                    }
                    break;
            }

            // Comma key - use e.key for this
            if (e.key === ',') {
                e.preventDefault();
                this.showSettings();
            }
        }

        // Redo with Shift (Ctrl/Cmd + Shift + Z for macOS compatibility)
        if ((e.ctrlKey || e.metaKey) && e.shiftKey && !e.altKey) {
            switch (e.code) {
                case 'KeyZ':
                    e.preventDefault();
                    this.performRedo();
                    break;
            }
        }

        // Call registered handlers
        this.handlers.forEach((callback, context) => {
            callback(e);
        });
    },

    switchView: function (mode) {
        this.triggerAction('switchView', { mode: mode });
    },

    showAddConcept: function () {
        this.triggerAction('addConcept');
    },

    showAddRelationship: function () {
        this.triggerAction('addRelationship');
    },

    performUndo: function () {
        this.triggerAction('undo');
    },

    performRedo: function () {
        this.triggerAction('redo');
    },

    showSettings: function () {
        this.triggerAction('settings');
    },

    toggleFullScreen: function () {
        this.triggerAction('toggleFullScreen');
    },

    triggerAction: function (action, data = {}) {
        document.dispatchEvent(new CustomEvent('keyboardShortcut', {
            detail: { action: action, ...data }
        }));
    },

    focusElement: function (selector) {
        const element = document.querySelector(selector);
        if (element) {
            element.focus();
            return true;
        }
        return false;
    }
};
