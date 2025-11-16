// Workspace keyboard handler for global shortcuts
window.WorkspaceKeyboardHandler = {
    dotNetHelper: null,

    initialize: function (dotNetHelper) {
        this.dotNetHelper = dotNetHelper;
        document.addEventListener('keydown', this.handleKeyDown.bind(this));
        console.log('[WorkspaceKeyboardHandler] Initialized');
    },

    handleKeyDown: function (event) {
        // Cmd+K or Ctrl+K to open quick switcher
        if ((event.metaKey || event.ctrlKey) && event.key === 'k') {
            event.preventDefault();
            console.log('[WorkspaceKeyboardHandler] Cmd/Ctrl+K detected, invoking ShowQuickSwitcher');
            if (this.dotNetHelper) {
                this.dotNetHelper.invokeMethodAsync('ShowQuickSwitcher');
            }
        }

        // Cmd+E or Ctrl+E to toggle preview
        if ((event.metaKey || event.ctrlKey) && event.key === 'e') {
            event.preventDefault();
            console.log('[WorkspaceKeyboardHandler] Cmd/Ctrl+E detected, invoking TogglePreview');
            if (this.dotNetHelper) {
                this.dotNetHelper.invokeMethodAsync('TogglePreview');
            }
        }
    },

    dispose: function () {
        document.removeEventListener('keydown', this.handleKeyDown.bind(this));
        this.dotNetHelper = null;
        console.log('[WorkspaceKeyboardHandler] Disposed');
    }
};
