// Mind Map Visualization using Cytoscape.js
// Provides infinite zoom network diagram with hierarchical exploration

// Store Cytoscape instances
const mindMapInstances = new Map();

/**
 * Initialize a new mind map visualization
 * @param {string} containerId - The HTML element ID for the canvas
 * @param {Array} concepts - Array of concept nodes
 * @param {Array} relationships - Array of relationship edges
 * @param {Object} options - Configuration options
 */
window.initializeMindMap = function(containerId, concepts, relationships, options) {

    // Dispose existing instance if present
    if (mindMapInstances.has(containerId)) {
        try {
            mindMapInstances.get(containerId).destroy();
        } catch (e) {
            console.warn('Error destroying previous instance:', e);
        }
    }

    const container = document.getElementById(containerId);
    if (!container) {
        console.error('Container not found:', containerId);
        return false;
    }

    // Build node elements
    const nodes = concepts.map(c => ({
        data: {
            id: c.id,
            label: c.label,
            definition: c.definition || '',
            category: c.category || '',
            color: c.color || '#4A90E2',
            isFocused: options.focusedConcept ? (parseInt(c.id.substring(1)) === options.focusedConcept) : false
        },
        position: { x: c.x, y: c.y }
    }));

    // Build edge elements
    const edges = relationships.map(r => ({
        data: {
            id: r.id,
            source: r.source,
            target: r.target,
            label: r.label,
            type: r.type
        }
    }));

    // Initialize Cytoscape
    const cy = cytoscape({
        container: container,
        elements: [...nodes, ...edges],

        style: [
            // Node styles
            {
                selector: 'node',
                style: {
                    'label': options.showLabels ? 'data(label)' : '',
                    'background-color': 'data(color)',
                    'color': '#000',
                    'text-valign': 'center',
                    'text-halign': 'center',
                    'font-size': '14px',
                    'font-weight': '600',
                    'text-outline-width': 2,
                    'text-outline-color': '#fff',
                    'min-zoomed-font-size': 8,
                    'width': 60,
                    'height': 60,
                    'border-width': 3,
                    'border-color': '#fff',
                    'border-opacity': 0.8,
                    'opacity': 1,
                    'transition-property': 'opacity, border-color, border-width',
                    'transition-duration': '0.3s',
                    'shape': 'ellipse'
                }
            },
            // Focused node - highlighted
            {
                selector: 'node[isFocused = true]',
                style: {
                    'border-width': 5,
                    'border-color': '#0d6efd',
                    'border-opacity': 1,
                    'z-index': 999
                }
            },
            // Dimmed context nodes (when focused)
            {
                selector: 'node.dimmed',
                style: {
                    'opacity': 0.3,
                    'text-opacity': 0.3
                }
            },
            // Edge styles
            {
                selector: 'edge',
                style: {
                    'width': 2,
                    'line-color': '#999',
                    'target-arrow-color': '#999',
                    'target-arrow-shape': 'triangle',
                    'curve-style': 'bezier',
                    'label': options.showLabels ? 'data(label)' : '',
                    'font-size': '11px',
                    'text-rotation': 'autorotate',
                    'text-background-color': '#fff',
                    'text-background-opacity': 0.8,
                    'text-background-padding': '3px',
                    'color': '#555',
                    'opacity': 1,
                    'transition-property': 'opacity',
                    'transition-duration': '0.3s'
                }
            },
            // Dimmed edges
            {
                selector: 'edge.dimmed',
                style: {
                    'opacity': 0.15,
                    'text-opacity': 0.15
                }
            },
            // Hover states
            {
                selector: 'node:active',
                style: {
                    'overlay-opacity': 0.2,
                    'overlay-color': '#0d6efd'
                }
            },
            {
                selector: 'node.highlighted',
                style: {
                    'border-width': 5,
                    'border-color': '#0d6efd',
                    'z-index': 999
                }
            }
        ],

        layout: {
            name: 'preset',
            fit: true,
            padding: 50
        },

        // Enable infinite zoom
        minZoom: options.minZoom || 0.1,
        maxZoom: options.maxZoom || 100,
        wheelSensitivity: 0.2,

        // Interaction settings
        userZoomingEnabled: true,
        userPanningEnabled: true,
        boxSelectionEnabled: false,
        selectionType: 'single',
        touchTapThreshold: 8,
        desktopTapThreshold: 4,
        autoungrabify: false,
        autounselectify: false
    });

    // Apply context dimming if focused
    if (options.focusedConcept && options.dimParents) {
        applyContextDimming(cy, `c${options.focusedConcept}`);
    }

    // Event handlers
    cy.on('tap', 'node', function(evt) {
        const node = evt.target;

        // Remove previous highlights
        cy.elements().removeClass('highlighted');

        // Highlight clicked node
        node.addClass('highlighted');

        // Could trigger Blazor callback here
    });

    cy.on('mouseover', 'node', function(evt) {
        const node = evt.target;
        if (!node.hasClass('dimmed')) {
            container.style.cursor = 'pointer';
        }
    });

    cy.on('mouseout', 'node', function() {
        container.style.cursor = 'default';
    });

    // Double-click to drill into node
    cy.on('dbltap', 'node', function(evt) {
        const node = evt.target;
        // This will be handled by Blazor
    });

    // Zoom event listener to update zoom level display
    cy.on('zoom', function() {
        const zoomLevel = cy.zoom();
        // Level-of-detail could be implemented here
        applyLevelOfDetail(cy, zoomLevel, options);
    });

    // Store instance
    mindMapInstances.set(containerId, cy);

    return true;
};

/**
 * Apply context dimming to show focus
 * @param {Object} cy - Cytoscape instance
 * @param {string} focusNodeId - ID of the focused node
 */
function applyContextDimming(cy, focusNodeId) {
    const focusNode = cy.getElementById(focusNodeId);
    if (!focusNode.length) return;

    // Get all connected nodes (1 hop away)
    const neighbors = focusNode.neighborhood();
    const connectedNodes = neighbors.nodes();
    const connectedEdges = neighbors.edges();

    // Dim everything except focus and immediate neighbors
    cy.elements().addClass('dimmed');
    focusNode.removeClass('dimmed');
    connectedNodes.removeClass('dimmed');
    connectedEdges.removeClass('dimmed');
}

/**
 * Apply level-of-detail rendering based on zoom level
 * @param {Object} cy - Cytoscape instance
 * @param {number} zoomLevel - Current zoom level
 * @param {Object} options - Display options
 */
function applyLevelOfDetail(cy, zoomLevel, options) {
    // High detail at high zoom
    if (zoomLevel > 1.5) {
        cy.style()
            .selector('node')
            .style({
                'font-size': '16px',
                'width': 80,
                'height': 80
            })
            .selector('edge')
            .style({
                'font-size': '12px',
                'width': 3
            })
            .update();
    }
    // Medium detail
    else if (zoomLevel > 0.5) {
        cy.style()
            .selector('node')
            .style({
                'font-size': '14px',
                'width': 60,
                'height': 60
            })
            .selector('edge')
            .style({
                'font-size': '11px',
                'width': 2
            })
            .update();
    }
    // Low detail at low zoom (icon-only)
    else {
        cy.style()
            .selector('node')
            .style({
                'font-size': '10px',
                'width': 40,
                'height': 40
            })
            .selector('edge')
            .style({
                'font-size': '0px', // Hide edge labels
                'width': 1
            })
            .update();
    }
}

/**
 * Update mind map display options
 * @param {string} containerId - Container ID
 * @param {Object} options - New options
 */
window.updateMindMapOptions = function(containerId, options) {
    const cy = mindMapInstances.get(containerId);
    if (!cy) return false;

    // Update labels
    cy.style()
        .selector('node')
        .style({ 'label': options.showLabels ? 'data(label)' : '' })
        .selector('edge')
        .style({ 'label': options.showLabels ? 'data(label)' : '' })
        .update();

    // Reapply context dimming
    if (options.focusedConcept) {
        cy.elements().removeClass('dimmed');
        if (options.dimParents) {
            applyContextDimming(cy, `c${options.focusedConcept}`);
        }
    } else {
        cy.elements().removeClass('dimmed');
    }

    return true;
};

/**
 * Set zoom level
 * @param {string} containerId - Container ID
 * @param {number} zoomLevel - Target zoom level
 */
window.setMindMapZoom = function(containerId, zoomLevel) {
    const cy = mindMapInstances.get(containerId);
    if (!cy) return false;

    cy.animate({
        zoom: zoomLevel,
        duration: 300,
        easing: 'ease-in-out-cubic'
    });

    return true;
};

/**
 * Reset zoom to fit all nodes
 * @param {string} containerId - Container ID
 */
window.resetMindMapZoom = function(containerId) {
    const cy = mindMapInstances.get(containerId);
    if (!cy) return false;

    // Remove all dimming
    cy.elements().removeClass('dimmed');
    cy.elements().removeClass('highlighted');

    cy.animate({
        fit: {
            eles: cy.elements(),
            padding: 50
        },
        duration: 500,
        easing: 'ease-in-out-cubic'
    });

    return true;
};

/**
 * Drill into a specific node (zoom and focus)
 * @param {string} containerId - Container ID
 * @param {string} nodeId - Node to drill into
 */
window.drillIntoMindMapNode = function(containerId, nodeId) {
    const cy = mindMapInstances.get(containerId);
    if (!cy) return false;

    const node = cy.getElementById(nodeId);
    if (!node.length) return false;

    // Zoom into the node with animation
    cy.animate({
        center: {
            eles: node
        },
        zoom: 2.0,
        duration: 500,
        easing: 'ease-in-out-cubic'
    });

    // Mark as focused
    cy.nodes().removeData('isFocused');
    node.data('isFocused', true);

    // Apply dimming
    applyContextDimming(cy, nodeId);

    return true;
};

/**
 * Dispose mind map instance
 * @param {string} containerId - Container ID
 */
window.disposeMindMap = function(containerId) {
    const cy = mindMapInstances.get(containerId);
    if (cy) {
        try {
            cy.destroy();
            mindMapInstances.delete(containerId);
            return true;
        } catch (e) {
            console.error('Error disposing mind map:', e);
            return false;
        }
    }
    return false;
};
