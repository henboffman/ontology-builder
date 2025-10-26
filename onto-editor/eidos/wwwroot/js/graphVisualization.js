window.renderOntologyGraph = function (containerId, elements, dotNetHelper, displayOptions) {
    // Clear any existing graph in this container
    const container = document.getElementById(containerId);
    if (!container) {
        console.error('Container not found:', containerId);
        return;
    }

    // Cancel any pending render operation
    if (container.renderTimeout) {
        clearTimeout(container.renderTimeout);
        container.renderTimeout = null;
    }

    // Use provided display options or defaults
    const options = displayOptions || {
        nodeSize: 40,
        edgeThickness: 2,
        showEdgeLabels: true,
        textSizeScale: 1.0
    };

    // Calculate font sizes based on node size and text scale
    // Scale font size proportionally with node size (default 40px node = 12px font)
    // Then apply text size scale (user adjustment from 50% to 150%)
    const basedNodeFontSize = Math.max(8, Math.round(options.nodeSize * 0.3));
    const baseEdgeFontSize = Math.max(7, Math.round(options.nodeSize * 0.25));

    const nodeFontSize = Math.max(6, Math.round(basedNodeFontSize * options.textSizeScale));
    const edgeFontSize = Math.max(5, Math.round(baseEdgeFontSize * options.textSizeScale));

    // Function to initialize the graph
    const initializeGraph = () => {
        // Initialize Cytoscape
        const cy = cytoscape({
        container: container,
        elements: elements,
        style: [
            {
                selector: 'node',
                style: {
                    'background-color': 'data(color)',
                    'label': 'data(label)',
                    'text-valign': 'center',
                    'text-halign': 'center',
                    'color': '#000',
                    'text-outline-width': Math.max(1, Math.round(nodeFontSize * 0.17)),
                    'text-outline-color': '#fff',
                    'font-size': nodeFontSize + 'px',
                    'font-weight': 'bold',
                    'width': options.nodeSize + 'px',
                    'height': options.nodeSize + 'px',
                    'border-width': Math.max(1, Math.round(options.nodeSize * 0.05)),
                    'border-color': '#333',
                    'text-wrap': 'wrap',
                    'text-max-width': (options.nodeSize + 20) + 'px'
                }
            },
            {
                selector: 'edge',
                style: {
                    'width': options.edgeThickness,
                    'line-color': '#999',
                    'target-arrow-color': '#999',
                    'target-arrow-shape': 'triangle',
                    'curve-style': 'bezier',
                    'label': options.showEdgeLabels ? 'data(label)' : '',
                    'font-size': edgeFontSize + 'px',
                    'text-rotation': 'autorotate',
                    'text-margin-y': -10,
                    'color': '#666',
                    'text-background-color': '#fff',
                    'text-background-opacity': 0.8,
                    'text-background-padding': '2px'
                }
            },
            {
                selector: 'node:selected',
                style: {
                    'border-width': Math.max(2, Math.round(options.nodeSize * 0.1)),
                    'border-color': '#0d6efd'
                }
            },
            {
                selector: 'edge:selected',
                style: {
                    'line-color': '#0d6efd',
                    'target-arrow-color': '#0d6efd',
                    'width': options.edgeThickness + 1
                }
            },
            {
                selector: '.highlighted',
                style: {
                    'border-width': Math.max(2, Math.round(options.nodeSize * 0.1)),
                    'border-color': '#ffc107',
                    'z-index': 999
                }
            },
            {
                selector: 'edge.highlighted',
                style: {
                    'line-color': '#ffc107',
                    'target-arrow-color': '#ffc107',
                    'width': options.edgeThickness + 2,
                    'z-index': 999
                }
            },
            {
                selector: '.faded',
                style: {
                    'opacity': 0.3
                }
            }
        ],
        layout: {
            name: 'cose',
            animate: true,
            animationDuration: 500,
            nodeRepulsion: 8000,
            idealEdgeLength: 100,
            edgeElasticity: 100,
            nestingFactor: 5,
            gravity: 80,
            numIter: 1000,
            initialTemp: 200,
            coolingFactor: 0.95,
            minTemp: 1.0
        },
        userZoomingEnabled: true,
        userPanningEnabled: true,
        boxSelectionEnabled: false
    });

    // Add hover highlighting to show relationships
    // Wrap handlers in try-catch to prevent errors during graph transitions
    cy.on('mouseover', 'node', function (event) {
        try {
            const node = event.target;
            const data = node.data();
            let tooltipText = data.label;

            if (data.explanation) {
                tooltipText += '\n' + data.explanation;
            }

            if (data.examples) {
                tooltipText += '\nExamples: ' + data.examples;
            }

            node.data('tooltip', tooltipText);

            // Highlight connected nodes and edges
            const connectedEdges = node.connectedEdges();
            const connectedNodes = connectedEdges.connectedNodes();

            // Fade all elements
            cy.elements().addClass('faded');

            // Highlight the hovered node, connected nodes, and edges
            node.removeClass('faded').addClass('highlighted');
            connectedNodes.removeClass('faded').addClass('highlighted');
            connectedEdges.removeClass('faded').addClass('highlighted');
        } catch (e) {
            // Silently ignore errors during graph transitions
        }
    });

    cy.on('mouseout', 'node', function (event) {
        try {
            // Remove all highlighting and fading
            cy.elements().removeClass('faded highlighted');
        } catch (e) {
            // Silently ignore errors during graph transitions
        }
    });

    cy.on('mouseover', 'edge', function (event) {
        try {
            const edge = event.target;
            const data = edge.data();
            let tooltipText = data.label;

            if (data.description) {
                tooltipText += '\n' + data.description;
            }

            edge.data('tooltip', tooltipText);

            // Highlight the edge and its connected nodes
            const connectedNodes = edge.connectedNodes();

            // Fade all elements
            cy.elements().addClass('faded');

            // Highlight the edge and its nodes
            edge.removeClass('faded').addClass('highlighted');
            connectedNodes.removeClass('faded').addClass('highlighted');
        } catch (e) {
            // Silently ignore errors during graph transitions
        }
    });

    cy.on('mouseout', 'edge', function (event) {
        try {
            // Remove all highlighting and fading
            cy.elements().removeClass('faded highlighted');
        } catch (e) {
            // Silently ignore errors during graph transitions
        }
    });

    // Enable fit to screen
    cy.fit(50);

        // Mobile-friendly touch support
        const isMobile = window.innerWidth <= 768;
        if (isMobile) {
            // Disable box selection on mobile (interferes with touch gestures)
            cy.boxSelectionEnabled(false);

            // Enable better touch gestures
            cy.userPanningEnabled(true);
            cy.userZoomingEnabled(true);

            // Add tap-to-center on mobile
            cy.on('tap', 'node', function (event) {
                if (!event.originalEvent.metaKey && !event.originalEvent.ctrlKey) {
                    cy.animate({
                        center: { eles: event.target },
                        zoom: Math.max(cy.zoom(), 1.2)
                    }, {
                        duration: 300
                    });
                }
            });
        }

        // Add click handlers for nodes and edges
        if (dotNetHelper) {
            cy.on('click', 'node', function (event) {
                const node = event.target;
                const nodeId = node.data('id');
                // Extract the concept ID from the node ID (format: "concept-123")
                const conceptId = parseInt(nodeId.replace('concept-', ''));

                // Check if Cmd (Mac) or Ctrl (Windows/Linux) was pressed
                if (event.originalEvent.metaKey || event.originalEvent.ctrlKey) {
                    // Ctrl/Cmd + click to create relationships
                    dotNetHelper.invokeMethodAsync('OnNodeCtrlClick', conceptId);
                } else {
                    // Regular click to show details
                    dotNetHelper.invokeMethodAsync('OnNodeClick', conceptId);
                }
            });

            // Add click handler for edges to show relationship details
            cy.on('click', 'edge', function (event) {
                const edge = event.target;
                const edgeId = edge.data('id');
                // Extract the relationship ID from the edge ID (format: "rel-123")
                const relationshipId = parseInt(edgeId.replace('rel-', ''));

                // Call back to Blazor to show relationship details
                dotNetHelper.invokeMethodAsync('OnEdgeClick', relationshipId);
            });
        }

        // Handle window resize to keep graph responsive
        const resizeObserver = new ResizeObserver(() => {
            if (cy && container.cytoscapeInstance) {
                cy.resize();
                cy.fit(50);
            }
        });
        resizeObserver.observe(container);

        // Store reference for potential future use
        container.cytoscapeInstance = cy;
        container.resizeObserver = resizeObserver;
    };

    // Clean up old instance if it exists
    if (container.cytoscapeInstance) {
        try {
            // Stop any running animations and layouts first
            container.cytoscapeInstance.stop();
            // Remove all event listeners to prevent any pending events from firing
            container.cytoscapeInstance.removeAllListeners();
            // Destroy the instance - this will clean up the DOM
            container.cytoscapeInstance.destroy();
        } catch (e) {
            // Ignore errors during cleanup - instance may already be partially destroyed
            console.debug('Graph cleanup warning (safe to ignore):', e.message);
        }
        container.cytoscapeInstance = null;
    }

    // Clean up resize observer if it exists
    if (container.resizeObserver) {
        container.resizeObserver.disconnect();
        container.resizeObserver = null;
    }

    // Clear the container
    container.innerHTML = '';

    // Use a small delay to ensure any pending operations from the old instance complete
    // before creating the new instance. requestAnimationFrame (~16ms) isn't enough for
    // Cytoscape's internal cleanup, so we use a 50ms timeout instead.
    container.renderTimeout = setTimeout(() => {
        container.renderTimeout = null;
        initializeGraph();
    }, 50);
};

// Download a text file
window.downloadTextFile = function (filename, content) {
    const blob = new Blob([content], { type: 'text/turtle;charset=utf-8' });
    const url = window.URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = filename;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    window.URL.revokeObjectURL(url);
};

// Copy text to clipboard
window.copyToClipboard = async function (text) {
    try {
        await navigator.clipboard.writeText(text);
        return true;
    } catch (err) {
        console.error('Failed to copy to clipboard:', err);
        return false;
    }
};
