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
        if (e.altKey && !e.ctrlKey && !e.metaKey) {
            switch (e.key.toLowerCase()) {
                case 'g':
                    e.preventDefault();
                    this.triggerViewMode('Graph');
                    break;
                case 'l':
                    e.preventDefault();
                    this.triggerViewMode('List');
                    break;
                case 't':
                    e.preventDefault();
                    this.triggerViewMode('Ttl');
                    break;
                case 'n':
                    e.preventDefault();
                    this.triggerViewMode('Notes');
                    break;
                case 'p':
                    e.preventDefault();
                    this.triggerViewMode('Templates');
                    break;
                case 'c':
                    e.preventDefault();
                    this.triggerAction('addConcept');
                    break;
                case 'r':
                    e.preventDefault();
                    this.triggerAction('addRelationship');
                    break;
            }
        }

        // Editing shortcuts (Ctrl/Cmd + key)
        if ((e.ctrlKey || e.metaKey) && !e.altKey && !e.shiftKey) {
            switch (e.key.toLowerCase()) {
                case 'k':
                    e.preventDefault();
                    this.triggerAction('addConcept');
                    break;
                case 'r':
                    e.preventDefault();
                    this.triggerAction('addRelationship');
                    break;
                case 'i':
                    e.preventDefault();
                    this.triggerAction('importTtl');
                    break;
                case ',':
                    e.preventDefault();
                    this.triggerAction('openSettings');
                    break;
                case 'f':
                    if (!isInputField) {
                        e.preventDefault();
                        this.triggerAction('focusSearch');
                    }
                    break;
                case 'z':
                    e.preventDefault();
                    this.triggerAction('undo');
                    break;
                case 'y':
                    e.preventDefault();
                    this.triggerAction('redo');
                    break;
                case 's':
                    // Allow default browser save in specific contexts
                    const formElement = activeElement?.closest('form');
                    if (formElement) {
                        e.preventDefault();
                        this.triggerAction('save');
                    }
                    break;
            }
        }

        // Call registered handlers
        this.handlers.forEach((callback, context) => {
            callback(e);
        });
    },

    triggerViewMode: function (mode) {
        // Find and click the view mode button
        const buttons = document.querySelectorAll('.btn-group button');
        buttons.forEach(btn => {
            if (btn.textContent.includes(mode)) {
                btn.click();
            }
        });
    },

    triggerAction: function (action) {
        // Dispatch custom event for Blazor components to handle
        document.dispatchEvent(new CustomEvent('keyboardShortcut', {
            detail: { action: action }
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
