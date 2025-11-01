// FloatingPanelManager - Handles draggable floating panels with position persistence
class FloatingPanelManager {
    constructor() {
        this.panels = new Map();
        this.dragging = null;
    }

    initializePanel(panelId, options = {}) {
        const panel = document.getElementById(panelId);
        if (!panel) {
            console.warn(`Panel ${panelId} not found`);
            return;
        }

        // Check if already initialized
        if (this.panels.has(panelId)) {
            return;
        }

        const header = panel.querySelector('.floating-panel-header');
        if (!header) {
            console.warn(`Panel ${panelId} has no header`);
            return;
        }

        const panelData = {
            element: panel,
            header: header,
            isDragging: false,
            startX: 0,
            startY: 0,
            initialX: 0,
            initialY: 0,
            ...options
        };

        // Load saved position or center panel
        const savedPosition = this.loadPosition(panelId);
        if (savedPosition) {
            panel.style.left = savedPosition.x + 'px';
            panel.style.top = savedPosition.y + 'px';
        } else {
            this.centerPanel(panelId);
        }

        // Bind event handlers
        const onMouseDown = (e) => this.onDragStart(e, panelId);
        const onMouseMove = (e) => this.onDrag(e, panelId);
        const onMouseUp = (e) => this.onDragEnd(e, panelId);

        header.addEventListener('mousedown', onMouseDown);
        document.addEventListener('mousemove', onMouseMove);
        document.addEventListener('mouseup', onMouseUp);

        // Store handlers for cleanup
        panelData.handlers = {
            mouseDown: onMouseDown,
            mouseMove: onMouseMove,
            mouseUp: onMouseUp
        };

        // Add cursor style to header
        header.style.cursor = 'move';

        this.panels.set(panelId, panelData);
    }

    onDragStart(e, panelId) {
        const panelData = this.panels.get(panelId);
        if (!panelData || !panelData.header.contains(e.target) && !e.target.classList.contains('floating-panel-header')) {
            return;
        }

        // Don't drag if clicking close button
        if (e.target.closest('.close-btn')) {
            return;
        }

        e.preventDefault();

        const panel = panelData.element;
        const rect = panel.getBoundingClientRect();

        panelData.isDragging = true;
        panelData.startX = e.clientX;
        panelData.startY = e.clientY;
        panelData.initialX = rect.left;
        panelData.initialY = rect.top;

        this.dragging = panelId;

        // Add dragging class for visual feedback
        panel.classList.add('dragging');
    }

    onDrag(e, panelId) {
        const panelData = this.panels.get(panelId);
        if (!panelData || !panelData.isDragging || this.dragging !== panelId) {
            return;
        }

        e.preventDefault();

        const panel = panelData.element;
        const deltaX = e.clientX - panelData.startX;
        const deltaY = e.clientY - panelData.startY;

        let newX = panelData.initialX + deltaX;
        let newY = panelData.initialY + deltaY;

        // Constrain to viewport with padding
        const padding = 20;
        const rect = panel.getBoundingClientRect();
        const maxX = window.innerWidth - rect.width - padding;
        const maxY = window.innerHeight - rect.height - padding;

        newX = Math.max(padding, Math.min(newX, maxX));
        newY = Math.max(padding, Math.min(newY, maxY));

        panel.style.left = newX + 'px';
        panel.style.top = newY + 'px';
    }

    onDragEnd(e, panelId) {
        const panelData = this.panels.get(panelId);
        if (!panelData || !panelData.isDragging) {
            return;
        }

        panelData.isDragging = false;
        this.dragging = null;

        const panel = panelData.element;
        panel.classList.remove('dragging');

        // Save position to localStorage
        const rect = panel.getBoundingClientRect();
        this.savePosition(panelId, {
            x: rect.left,
            y: rect.top
        });
    }

    centerPanel(panelId) {
        const panel = document.getElementById(panelId);
        if (!panel) return;

        const rect = panel.getBoundingClientRect();
        const centerX = (window.innerWidth - rect.width) / 2;
        const centerY = (window.innerHeight - rect.height) / 2;

        panel.style.left = centerX + 'px';
        panel.style.top = centerY + 'px';
    }

    savePosition(panelId, position) {
        try {
            const key = `floating-panel-position-${panelId}`;
            localStorage.setItem(key, JSON.stringify(position));
        } catch (e) {
            console.warn('Failed to save panel position', e);
        }
    }

    loadPosition(panelId) {
        try {
            const key = `floating-panel-position-${panelId}`;
            const saved = localStorage.getItem(key);
            if (saved) {
                return JSON.parse(saved);
            }
        } catch (e) {
            console.warn('Failed to load panel position', e);
        }
        return null;
    }

    destroyPanel(panelId) {
        const panelData = this.panels.get(panelId);
        if (!panelData) return;

        // Remove event listeners
        if (panelData.handlers) {
            panelData.header.removeEventListener('mousedown', panelData.handlers.mouseDown);
            document.removeEventListener('mousemove', panelData.handlers.mouseMove);
            document.removeEventListener('mouseup', panelData.handlers.mouseUp);
        }

        // Clean up
        panelData.header.style.cursor = '';
        this.panels.delete(panelId);
    }
}

// Global instance
window.floatingPanelManager = new FloatingPanelManager();

// Global functions for Blazor JS interop
window.initializeFloatingPanel = function (panelId, options) {
    window.floatingPanelManager.initializePanel(panelId, options);
};

window.destroyFloatingPanel = function (panelId) {
    window.floatingPanelManager.destroyPanel(panelId);
};

window.centerFloatingPanel = function (panelId) {
    window.floatingPanelManager.centerPanel(panelId);
};
