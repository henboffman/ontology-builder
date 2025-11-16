// Global map to store Cytoscape instances by container ID
window.cytoscapeInstances = window.cytoscapeInstances || new Map();

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
        // Check if ALL nodes have saved positions
        const allNodesHavePositions = elements.nodes &&
            elements.nodes.length > 0 &&
            elements.nodes.every(n => n.position != null && n.position.x != null && n.position.y != null);

        console.log('📊 Position check - Total nodes:', elements.nodes?.length, 'All have positions:', allNodesHavePositions);
        console.log('📋 Sample node data:', elements.nodes?.[0]);


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
                    'text-max-width': (options.nodeSize + 20) + 'px',
                    'shape': 'ellipse'
                }
            },
            // ====================================================================
            // Individual Node Styling
            // ====================================================================
            // Individuals (instances) are visually distinguished from concepts:
            // - Diamond shape (vs ellipse for concepts)
            // - Dashed border (vs solid for concepts)
            // - 1.2x larger size for better visibility with diamond shape
            // - Color with 40% opacity (set in GraphVisualization.razor)
            // ====================================================================
            {
                selector: 'node[nodeType = "individual"]',
                style: {
                    'shape': 'diamond',
                    'width': (options.nodeSize * 1.2) + 'px',
                    'height': (options.nodeSize * 1.2) + 'px',
                    'border-width': Math.max(1, Math.round(options.nodeSize * 0.04)),
                    'border-style': 'dashed'
                }
            },
            // ====================================================================
            // Ontology Link Node Styling (Virtualized Nodes)
            // ====================================================================
            // Linked ontologies are visually distinguished from regular concepts:
            // - Hexagon shape (unique visual identifier)
            // - Double border (indicates special/virtual nature)
            // - 1.3x larger size for prominence
            // - Purple default color (#9B59B6) if not customized
            // ====================================================================
            {
                selector: 'node[nodeType = "ontologyLink"]',
                style: {
                    'shape': 'hexagon',
                    'width': (options.nodeSize * 1.3) + 'px',
                    'height': (options.nodeSize * 1.3) + 'px',
                    'border-width': Math.max(2, Math.round(options.nodeSize * 0.08)),
                    'border-style': 'double'
                }
            },
            // ====================================================================
            // Virtual Concept Node Styling (Concepts from Linked Ontologies)
            // ====================================================================
            // Concepts from linked ontologies shown when expanded:
            // - Round rectangle shape (distinct from ellipse concepts)
            // - Dashed border (indicates virtual/referenced nature)
            // - Lighter purple color for visual grouping
            // ====================================================================
            {
                selector: 'node[nodeType = "virtualConcept"]',
                style: {
                    'shape': 'round-rectangle',
                    'border-width': Math.max(1, Math.round(options.nodeSize * 0.05)),
                    'border-style': 'dashed',
                    'background-opacity': 0.85
                }
            },
            {
                selector: 'edge',
                style: {
                    'width': options.edgeThickness,
                    'line-color': '#999',
                    'target-arrow-color': '#999',
                    'target-arrow-shape': 'triangle',
                    'curve-style': 'bezier',  // Simple bezier curves (original style)

                    // Enhanced label styling with text outline
                    'label': options.showEdgeLabels ? 'data(label)' : '',
                    'font-size': edgeFontSize + 'px',
                    'font-weight': '600',  // Bolder for better readability
                    'text-rotation': 'autorotate',
                    'text-margin-y': -10,
                    'color': '#333',  // Darker text

                    // White outline for readability over any background
                    'text-outline-color': '#ffffff',
                    'text-outline-width': '2.5px',

                    // Remove background (cleaner look)
                    'text-background-opacity': 0,

                    // Text wrapping
                    'text-wrap': 'wrap',
                    'text-max-width': '100px'
                }
            },
            // ====================================================================
            // Instance-of Edge Styling
            // ====================================================================
            // Edges connecting individuals to their parent concepts (type edges).
            // - Dotted line style to distinguish from regular relationships
            // - Gray color (#666) - lighter/subtler than regular edges (#999)
            // - Vee arrow shape (smaller than triangle)
            // - Slightly thinner than regular edges
            // Example: "Alice" --instance of--> "Person"
            // ====================================================================
            {
                selector: 'edge[edgeType = "instanceOf"]',
                style: {
                    'line-style': 'dotted',
                    'line-color': '#666',
                    'target-arrow-color': '#666',
                    'width': Math.max(2, options.edgeThickness - 1),
                    'target-arrow-shape': 'vee'
                }
            },
            // ====================================================================
            // Individual Relationship Edge Styling
            // ====================================================================
            // Edges connecting related individuals (instance-level relationships).
            // - Purple color (#7B68EE) to distinguish from concept relationships
            // - Solid line (not dotted like instance-of edges)
            // - Triangle arrow shape (same as regular relationships)
            // Example: "Alice" --knows--> "Bob"
            // ====================================================================
            {
                selector: 'edge[edgeType = "individualRelationship"]',
                style: {
                    'line-color': '#7B68EE',
                    'target-arrow-color': '#7B68EE'
                }
            },
            // ====================================================================
            // Virtual Link Edge Styling
            // ====================================================================
            // Edges connecting ontology link nodes to their child concepts.
            // - Dashed line to indicate virtual/container relationship
            // - Light purple color (#B19CD9) to match ontology link theme
            // - Thinner than regular edges
            // - "contains" label to indicate hierarchical relationship
            // ====================================================================
            {
                selector: 'edge[edgeType = "virtualLink"]',
                style: {
                    'line-style': 'dashed',
                    'line-color': '#B19CD9',
                    'target-arrow-color': '#B19CD9',
                    'width': Math.max(1, options.edgeThickness - 1),
                    'target-arrow-shape': 'vee',
                    'opacity': 0.6
                }
            },
            // ====================================================================
            // Virtual Relationship Edge Styling
            // ====================================================================
            // Edges representing actual relationships within the linked ontology.
            // - Solid line like regular relationships (semantic connections)
            // - Light blue color (#87CEEB) to distinguish from base ontology
            // - Triangle arrow to show directionality
            // - Shows the internal structure of the virtual ontology
            // ====================================================================
            {
                selector: 'edge[edgeType = "virtualRelationship"]',
                style: {
                    'line-style': 'solid',
                    'line-color': '#87CEEB',
                    'target-arrow-color': '#87CEEB',
                    'width': options.edgeThickness,
                    'target-arrow-shape': 'triangle',
                    'opacity': 0.7
                }
            },
            // ====================================================================
            // Bridge Edge Styling (Cross-Ontology Connections)
            // ====================================================================
            // Edges connecting imported concepts to their virtual counterparts.
            // - Dotted line to indicate equivalence relationship
            // - Gold/yellow color (#FFD700) to stand out as special connection
            // - Bidirectional (no arrow) since it represents "same as" relationship
            // - Shows how ontologies are connected through imported concepts
            // ====================================================================
            {
                selector: 'edge[edgeType = "bridge"]',
                style: {
                    'line-style': 'dotted',
                    'line-color': '#FFD700',
                    'width': Math.max(2, options.edgeThickness),
                    'target-arrow-shape': 'none', // No arrow for equivalence
                    'opacity': 0.8,
                    'line-dash-pattern': [2, 4]
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
        layout: allNodesHavePositions ? {
            // Use preset layout when all nodes have saved positions
            name: 'preset',
            fit: true,
            padding: 50,
            animate: false
        } : {
            // Use COSE layout for initial layout or when some nodes lack positions
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
            minTemp: 1.0,
            randomize: true,
            fit: true,
            padding: 50,
            // Save all positions after COSE layout finishes
            stop: function() {
                if (!allNodesHavePositions && dotNetHelper) {
                    console.log('💾 COSE layout finished - saving all node positions...');
                    const updates = [];
                    cy.nodes().forEach(node => {
                        const nodeData = node.data();
                        const pos = node.position();
                        const nodeType = nodeData.type || 'concept';
                        let nodeId;

                        if (nodeType === 'ontologyLink') {
                            nodeId = nodeData.linkId || nodeData.nodeId;
                        } else {
                            nodeId = nodeData.nodeId;
                        }

                        if (nodeId) {
                            updates.push({
                                id: parseInt(nodeId),
                                type: nodeType,
                                x: Math.round(pos.x * 100) / 100,
                                y: Math.round(pos.y * 100) / 100
                            });
                        }
                    });

                    if (updates.length > 0) {
                        dotNetHelper.invokeMethodAsync('SaveNodePositionsBatch', updates)
                            .then(() => console.log(`✓ Saved ${updates.length} node positions after layout`))
                            .catch(err => console.error('❌ Error saving positions after layout:', err));
                    }
                }
            }
        },
        userZoomingEnabled: true,
        userPanningEnabled: true,
        boxSelectionEnabled: false
    });

    // Add hover highlighting to show relationships
    // Wrap handlers in try-catch to prevent errors during graph transitions
    let hoverTimeout = null;
    let hideTimeout = null;
    let editButton = null;

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

            // Clear any existing timeouts
            if (hoverTimeout) {
                clearTimeout(hoverTimeout);
            }
            if (hideTimeout) {
                clearTimeout(hideTimeout);
                hideTimeout = null;
            }

            // Set a timeout to show the Edit button after 600ms
            hoverTimeout = setTimeout(() => {
                try {
                    // Don't show edit button for virtual/linked nodes
                    if (data.nodeType === 'virtual' || data.nodeType === 'individual') {
                        return;
                    }

                    // Get node position on screen
                    const nodePosition = node.renderedPosition();
                    const nodeWidth = node.renderedWidth();

                    // Remove any existing edit button
                    if (editButton) {
                        editButton.remove();
                    }

                    // Create edit button
                    editButton = document.createElement('button');
                    editButton.textContent = 'Edit';
                    editButton.className = 'graph-node-edit-btn';
                    editButton.style.position = 'absolute';
                    editButton.style.left = (nodePosition.x - 25) + 'px';
                    editButton.style.top = (nodePosition.y - nodeWidth / 2 - 35) + 'px';
                    editButton.style.zIndex = '9999';
                    editButton.style.padding = '4px 12px';
                    editButton.style.backgroundColor = '#4A90E2';
                    editButton.style.color = 'white';
                    editButton.style.border = 'none';
                    editButton.style.borderRadius = '4px';
                    editButton.style.cursor = 'pointer';
                    editButton.style.fontSize = '12px';
                    editButton.style.fontWeight = 'bold';
                    editButton.style.boxShadow = '0 2px 4px rgba(0,0,0,0.2)';
                    editButton.style.transition = 'background-color 0.2s';
                    editButton.style.pointerEvents = 'auto';

                    // Keep button visible when hovering over it
                    editButton.onmouseenter = () => {
                        editButton.style.backgroundColor = '#357ABD';
                        // Cancel hide timeout when mouse enters button
                        if (hideTimeout) {
                            clearTimeout(hideTimeout);
                            hideTimeout = null;
                        }
                    };
                    editButton.onmouseleave = () => {
                        editButton.style.backgroundColor = '#4A90E2';
                        // Hide button when mouse leaves it
                        if (editButton) {
                            editButton.remove();
                            editButton = null;
                        }
                    };

                    // Handle mousedown on button area - if it's not directly on the button,
                    // hide it to allow drag to work
                    editButton.onmousedown = (e) => {
                        // Only handle click events, not drag attempts
                        // The button click will be handled by onclick
                        e.stopPropagation();
                    };

                    // Handle click to open edit dialog
                    editButton.onclick = (e) => {
                        e.stopPropagation();

                        // Parse the numeric concept ID from the string (e.g., "concept-7" -> 7)
                        const conceptIdStr = data.id;
                        const conceptId = parseInt(conceptIdStr.replace('concept-', ''), 10);

                        console.log('Edit button clicked for concept:', conceptIdStr, '-> ID:', conceptId);

                        // Call the .NET helper to open the edit dialog
                        if (dotNetHelper && !isNaN(conceptId)) {
                            dotNetHelper.invokeMethodAsync('OnNodeEditClick', conceptId);
                        }

                        // Remove the button after click
                        if (editButton) {
                            editButton.remove();
                            editButton = null;
                        }
                    };

                    container.appendChild(editButton);
                } catch (err) {
                    console.error('Error showing edit button:', err);
                }
            }, 600);

        } catch (e) {
            // Silently ignore errors during graph transitions
        }
    });

    cy.on('mouseout', 'node', function (event) {
        try {
            // Clear hover timeout
            if (hoverTimeout) {
                clearTimeout(hoverTimeout);
                hoverTimeout = null;
            }

            // Delay removal of edit button and highlighting to allow moving to button
            hideTimeout = setTimeout(() => {
                // Remove edit button if not hovering over it
                if (editButton && !editButton.matches(':hover')) {
                    editButton.remove();
                    editButton = null;
                }

                // Remove all highlighting and fading
                cy.elements().removeClass('faded highlighted');
                hideTimeout = null;
            }, 700);

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

    // ====================================================================
    // Position Persistence - Save node positions when user drags them
    // ====================================================================
    // Batch position updates to reduce server calls
    let positionUpdates = [];
    let batchSaveTimeout = null;

    /**
     * Saves batched position updates to the server
     */
    const saveBatchedPositions = async () => {
        console.log('💾 saveBatchedPositions called - Updates:', positionUpdates.length, 'dotNetHelper:', !!dotNetHelper);

        if (positionUpdates.length > 0 && dotNetHelper) {
            try {
                console.log(`Saving ${positionUpdates.length} node positions...`, positionUpdates);
                await dotNetHelper.invokeMethodAsync('SaveNodePositionsBatch', positionUpdates);
                console.log('✓ Node positions saved successfully');
                positionUpdates = [];
            } catch (error) {
                console.error('❌ Error saving node positions:', error);
            }
        } else if (positionUpdates.length === 0) {
            console.log('⚠️ No position updates to save');
        } else if (!dotNetHelper) {
            console.error('❌ dotNetHelper is not available!');
        }
    };

    // Listen for mousedown to hide edit button when user tries to grab/drag
    cy.on('mousedown', 'node', function(event) {
        // Clear any hover timeout
        if (hoverTimeout) {
            clearTimeout(hoverTimeout);
            hoverTimeout = null;
        }

        // Immediately hide the edit button when user clicks/grabs node
        if (editButton) {
            editButton.remove();
            editButton = null;
        }
    });

    // Listen for drag event to hide edit button when user starts dragging
    cy.on('drag', 'node', function(event) {
        // Clear any hover timeout
        if (hoverTimeout) {
            clearTimeout(hoverTimeout);
            hoverTimeout = null;
        }

        // Immediately hide the edit button when dragging starts
        if (editButton) {
            editButton.remove();
            editButton = null;
        }
    });

    // Listen for dragfree event (fired when user releases node after dragging)
    cy.on('dragfree', 'node', function(event) {
        const node = event.target;
        const nodeData = node.data();
        const pos = node.position();

        console.log('🎯 Node dragged:', nodeData);

        // Determine node type and ID
        const nodeType = nodeData.type || 'concept';  // 'concept' or 'ontologyLink'
        let nodeId;

        if (nodeType === 'ontologyLink') {
            nodeId = nodeData.linkId || nodeData.nodeId;
        } else {
            nodeId = nodeData.nodeId;
        }

        console.log('📍 Node info - Type:', nodeType, 'ID:', nodeId, 'Position:', pos);

        // Only save if we have a valid ID
        if (nodeId) {
            // Add to batch
            const update = {
                id: parseInt(nodeId),
                type: nodeType,
                x: Math.round(pos.x * 100) / 100,  // Round to 2 decimal places
                y: Math.round(pos.y * 100) / 100
            };
            positionUpdates.push(update);
            console.log('✅ Added to batch:', update, 'Total queued:', positionUpdates.length);

            // Clear existing timeout
            if (batchSaveTimeout) {
                clearTimeout(batchSaveTimeout);
            }

            // Schedule batch save after 1 second of inactivity
            batchSaveTimeout = setTimeout(saveBatchedPositions, 1000);
            console.log('⏱️ Scheduled save in 1 second');
        } else {
            console.warn('⚠️ No valid nodeId found, cannot save position');
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
                const nodeType = node.data('nodeType');

                console.log('Node clicked:', { nodeId, nodeType });

                // Handle different node types
                if (nodeType === 'individual') {
                    // Extract the individual ID from the node ID (format: "individual-123")
                    const individualId = parseInt(nodeId.replace('individual-', ''));
                    console.log('Individual node clicked, ID:', individualId);

                    // Call the individual click handler
                    if (dotNetHelper.invokeMethodAsync) {
                        dotNetHelper.invokeMethodAsync('OnIndividualClick', individualId)
                            .then(() => console.log('OnIndividualClick succeeded'))
                            .catch(err => console.error('OnIndividualClick failed:', err));
                    }
                } else if (nodeType === 'ontologyLink') {
                    // Extract the ontology link ID from the node ID (format: "ontologylink-123")
                    const linkId = parseInt(nodeId.replace('ontologylink-', ''));
                    console.log('Ontology link node clicked, ID:', linkId);

                    // Call the ontology link click handler
                    if (dotNetHelper.invokeMethodAsync) {
                        dotNetHelper.invokeMethodAsync('OnOntologyLinkClick', linkId)
                            .then(() => console.log('OnOntologyLinkClick succeeded'))
                            .catch(err => console.error('OnOntologyLinkClick failed:', err));
                    }
                } else if (nodeType === 'virtualConcept') {
                    // Virtual concept from linked ontology
                    // Pass the full node ID to allow parsing on the server side
                    const label = node.data('label');

                    // Check if Cmd (Mac) or Ctrl (Windows/Linux) was pressed
                    if (event.originalEvent.metaKey || event.originalEvent.ctrlKey) {
                        // Ctrl/Cmd + click to create relationships with virtual concepts
                        dotNetHelper.invokeMethodAsync('OnVirtualConceptCtrlClick', nodeId, label);
                    } else {
                        // Regular click to show details
                        dotNetHelper.invokeMethodAsync('OnVirtualConceptClick', nodeId, label);
                    }
                } else {
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

            // Add click handler for background (Cmd+Shift+Click to add concept)
            cy.on('click', function (event) {
                // Check if the click was on the background (not on a node or edge)
                if (event.target === cy) {
                    // Check if Cmd+Shift (Mac) or Ctrl+Shift (Windows/Linux) was pressed
                    if ((event.originalEvent.metaKey || event.originalEvent.ctrlKey) && event.originalEvent.shiftKey) {
                        // Call back to Blazor to show add concept dialog
                        dotNetHelper.invokeMethodAsync('OnBackgroundCmdShiftClick')
                            .then(() => console.log('OnBackgroundCmdShiftClick succeeded'))
                            .catch(err => console.error('OnBackgroundCmdShiftClick failed:', err));
                    }
                }
            });
        }

        // ====================================================================
        // Multi-Edge Label Positioning
        // ====================================================================
        // Separate overlapping labels when multiple edges connect same nodes
        cy.ready(function() {
            const edgeGroups = new Map();

            // Group edges by source-target pair
            cy.edges().forEach(edge => {
                const source = edge.source().id();
                const target = edge.target().id();
                // Create bidirectional key (so A->B and B->A are in same group)
                const key = [source, target].sort().join('-');

                if (!edgeGroups.has(key)) {
                    edgeGroups.set(key, []);
                }
                edgeGroups.get(key).push(edge);
            });

            // Apply offset positioning to multi-edges
            edgeGroups.forEach(edges => {
                if (edges.length > 1) {
                    edges.forEach((edge, index) => {
                        // Calculate vertical offset from center
                        // Spreads labels evenly: for 3 edges -> offsets of -15, 0, 15
                        const offset = (index - (edges.length - 1) / 2) * 15;

                        edge.style({
                            'text-margin-y': offset
                        });
                    });
                }
            });
        });

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

        // Store in global map for access by other modules (e.g., conceptGrouping.js)
        window.cytoscapeInstances.set(containerId, cy);
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

// Check if a graph instance is fully initialized
window.isGraphInitialized = function(containerId) {
    return window.cytoscapeInstances && window.cytoscapeInstances.has(containerId);
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

// Zoom to a specific concept in the graph view
window.zoomToConcept = function (containerId, conceptId) {
    const container = document.getElementById(containerId);
    if (!container || !container.cytoscapeInstance) {
        console.warn(`Graph not found or not initialized: ${containerId}`);
        return false;
    }

    const cy = container.cytoscapeInstance;
    const node = cy.$(`#${conceptId}`);

    if (node.length === 0) {
        console.warn(`Concept node not found: ${conceptId}`);
        return false;
    }

    // Center on the node with animation and zoom
    cy.animate({
        center: {
            eles: node
        },
        zoom: 1.5,
        duration: 500,
        easing: 'ease-in-out-cubic'
    });

    // Add a temporary highlight effect to the node
    const originalBorderWidth = node.style('border-width');
    const originalBorderColor = node.style('border-color');

    node.animate({
        style: {
            'border-width': '8px',
            'border-color': '#FFD700'
        },
        duration: 300
    }).animate({
        style: {
            'border-width': originalBorderWidth,
            'border-color': originalBorderColor
        },
        duration: 300,
        delay: 500
    });

    return true;
};

// Scroll to an individual card in the list and highlight it
window.scrollToIndividual = function (individualId) {
    // Find the individual card by data attribute or ID
    const individualCard = document.querySelector(`[data-individual-id="${individualId}"]`);

    if (individualCard) {
        // Scroll into view with smooth animation
        individualCard.scrollIntoView({ behavior: 'smooth', block: 'center' });

        // Add a temporary highlight effect
        individualCard.classList.add('highlight-flash');
        setTimeout(() => {
            individualCard.classList.remove('highlight-flash');
        }, 2000);
    } else {
        console.warn(`Could not find individual card with ID: ${individualId}`);
    }
};
