// Concept Grouping for Graph View
// iOS-style collapsible node groups with drag-and-drop

// Store grouping state per graph instance
const groupingState = new Map();

/**
 * Initialize grouping functionality for a Cytoscape instance
 * @param {string} graphId - The graph container ID
 * @param {object} cyOrNull - Cytoscape instance (optional, will be found by graphId if null)
 * @param {object} dotNetHelper - Blazor interop object
 * @param {array} groups - Existing groups from database
 * @param {number} groupingRadius - Radius in pixels for grouping proximity (default: 100)
 */
window.initializeConceptGrouping = function(graphId, cyOrNull, dotNetHelper, groups, groupingRadius) {
    // Default to 100px if not provided
    const radius = groupingRadius || 100;


    // Find Cytoscape instance if not provided
    const cy = cyOrNull || (window.cytoscapeInstances && window.cytoscapeInstances.get(graphId));

    if (!cy) {
        console.error('❌ Cytoscape instance not found for', graphId);
        return;
    }


    // Initialize state for this graph
    const state = {
        isDragging: false,
        draggedNode: null,
        dragStartTime: null,
        hoverTarget: null,
        groups: groups || [],
        maxDepth: 5,
        groupingRadius: radius,      // Store the grouping radius for this graph
        dotNetHelper: dotNetHelper,  // Store the .NET reference for later use
        handlersInitialized: false   // Track if event handlers have been set up
    };

    groupingState.set(graphId, state);


    // Apply existing groups
    applyGroupsToGraph(graphId, cy, groups);

    // Set up drag-and-drop handlers (only once)
    if (!state.handlersInitialized) {
        setupDragHandlers(graphId, cy, dotNetHelper);
        setupGroupHandlers(graphId, cy, dotNetHelper);
        state.handlersInitialized = true;
    } else {
    }
};

/**
 * Apply existing groups to the graph visualization
 */
function applyGroupsToGraph(graphId, cy, groups) {

    groups.forEach(group => {

        if (group.isCollapsed) {
            // Hide child nodes and mark parent as grouped
            const parentNode = cy.getElementById('concept-' + group.parentConceptId);
            const childIds = JSON.parse(group.childConceptIds || '[]');
            const collapsedRelationships = group.collapsedRelationships ?
                JSON.parse(group.collapsedRelationships) : [];

            if (parentNode.length) {
                // Add grouped class to parent
                parentNode.addClass('grouped');
                parentNode.data('groupedChildren', childIds);
                parentNode.data('groupId', group.id);
                parentNode.data('collapsedRelationships', collapsedRelationships);

                // Hide child nodes
                childIds.forEach(childId => {
                    const childNode = cy.getElementById('concept-' + childId);
                    if (childNode.length) {
                        childNode.addClass('grouped-hidden');
                        childNode.style('visibility', 'hidden');

                        // Hide ALL edges connected to this child node
                        const connectedEdges = childNode.connectedEdges();
                        connectedEdges.forEach(edge => {
                            // Only hide if the edge isn't already being handled by rerouting
                            if (!edge.hasClass('rerouted-edge')) {
                                edge.addClass('grouped-edge-hidden');
                                edge.style('visibility', 'hidden');
                            }
                        });
                    }
                });

                // Handle edges: hide internal ones, reroute external ones
                handleGroupedEdges(cy, group.parentConceptId, childIds, collapsedRelationships);
            }
        }
    });

    // Apply stacked visual effect to grouped nodes
    applyStackedEffect(cy);
}

/**
 * Apply stacked visual effect to grouped nodes (iOS folder style)
 */
function applyStackedEffect(cy) {
    cy.nodes('.grouped').forEach(node => {
        const childCount = (node.data('groupedChildren') || []).length;

        // Add visual indicators for grouped nodes
        node.style({
            'border-width': '4px',
            'border-color': '#0d6efd',
            'box-shadow': '0 2px 8px rgba(13, 110, 253, 0.3), inset 0 -3px 0 rgba(13, 110, 253, 0.2)'
        });

        // Add count badge (will be rendered as pseudo-element via CSS)
        node.data('groupCount', childCount);

        // Add floating circle indicators
        addFloatingIndicators(cy, node, childCount);
    });
}

/**
 * Add floating circle indicators around a grouped node
 * Shows small circles representing the collapsed children
 */
function addFloatingIndicators(cy, node, childCount) {
    const container = cy.container();
    const nodeId = node.id();


    // Remove existing indicators for this node from the document
    const existingIndicators = document.querySelectorAll(`.group-indicator-container[data-node-id="${nodeId}"]`);
    existingIndicators.forEach(ind => {
        ind.remove();
    });

    // Don't show indicators if there are no children
    if (childCount === 0) {
        return;
    }

    // Limit to showing max 5 circles to avoid clutter
    const displayCount = Math.min(childCount, 5);

    // Create container for indicators - append to the cy container's parent to avoid clipping
    const indicatorContainer = document.createElement('div');
    indicatorContainer.className = 'group-indicator-container';
    indicatorContainer.dataset.nodeId = nodeId;
    indicatorContainer.style.position = 'absolute';
    indicatorContainer.style.pointerEvents = 'none';
    indicatorContainer.style.zIndex = '100'; // Above graph elements but below modals
    indicatorContainer.style.top = '0';
    indicatorContainer.style.left = '0';
    indicatorContainer.style.width = '100%';
    indicatorContainer.style.height = '100%';

    // Append to container (not container's parent, but ensure it's visible)
    container.appendChild(indicatorContainer);


    // Create individual circle indicators positioned around the node
    for (let i = 0; i < displayCount; i++) {
        const indicator = document.createElement('div');
        indicator.className = 'group-indicator-circle';
        indicator.dataset.index = i;
        indicator.style.position = 'absolute'; // Ensure absolute positioning
        indicatorContainer.appendChild(indicator);
    }

    // Position indicators around the node
    updateIndicatorPositions(cy, node, indicatorContainer, displayCount);

    // Update positions on pan/zoom and also on node position changes
    const updateHandler = () => {
        const container = document.querySelector(`.group-indicator-container[data-node-id="${nodeId}"]`);
        if (container && node.length) {
            updateIndicatorPositions(cy, node, container, displayCount);
        }
    };

    // Store handler for cleanup
    if (!node.data('indicatorUpdateHandler')) {
        cy.on('pan zoom position', updateHandler); // Listen to position changes too
        node.data('indicatorUpdateHandler', updateHandler);
    }

}

/**
 * Update positions of floating indicators around a node
 */
function updateIndicatorPositions(cy, node, container, count) {
    const renderedPos = node.renderedPosition();
    const nodeWidth = node.renderedWidth();
    const radius = nodeWidth / 2 + 3; // Very close orbit - just barely touching the node border

    const circles = container.querySelectorAll('.group-indicator-circle');

    // Get the node's background color
    const nodeColor = node.style('background-color') || '#0d6efd';

        nodeId: node.id(),
        renderedPos,
        nodeWidth,
        radius,
        nodeColor,
        circleCount: circles.length
    });

    circles.forEach((circle, i) => {
        // Calculate angle for this indicator (spread them around the node)
        // Start from top and go clockwise, full 360 degrees
        const angleOffset = -90; // Start at top (12 o'clock position)
        const angleSpace = 360 / count; // Evenly distribute around full circle
        const angle = (angleOffset + (i * angleSpace)) * (Math.PI / 180);

        // Calculate position
        const x = renderedPos.x + Math.cos(angle) * radius;
        const y = renderedPos.y + Math.sin(angle) * radius;

        // Set position and ensure visibility
        circle.style.left = (x - 5) + 'px'; // Center the 10px circle
        circle.style.top = (y - 5) + 'px';
        circle.style.display = 'block';
        circle.style.visibility = 'visible';
        circle.style.opacity = '1';

        // Match the node's color with a slightly lighter/more saturated version
        circle.style.width = '10px';
        circle.style.height = '10px';
        circle.style.backgroundColor = nodeColor;
        circle.style.border = '2px solid rgba(255, 255, 255, 0.9)';
        circle.style.boxShadow = `0 2px 6px rgba(0, 0, 0, 0.3), 0 0 0 2px ${nodeColor}40, 0 0 10px ${nodeColor}60`;
        circle.style.zIndex = '100';

        // Add orbit animation delay based on index
        circle.style.animationDelay = (i * 0.15) + 's';

    });
}

/**
 * Handle edges for grouped concepts: hide internal edges, reroute external edges
 * @param {object} cy - Cytoscape instance
 * @param {number} parentConceptId - The parent concept ID
 * @param {array} childIds - Array of child concept IDs
 * @param {array} collapsedRelationships - Metadata about collapsed relationships
 */
function handleGroupedEdges(cy, parentConceptId, childIds, collapsedRelationships) {
    const allGroupedIds = [parentConceptId, ...childIds];
    const reroutedEdges = new Set(); // Track which original edges we've rerouted

    // Process each collapsed relationship
    collapsedRelationships.forEach(relInfo => {
        const originalEdge = cy.getElementById('rel-' + relInfo.relationshipId);

        if (!originalEdge.length) {
            console.warn('Original edge not found:', relInfo.relationshipId);
            return;
        }

        if (relInfo.shouldBeRerouted && relInfo.externalConceptId) {
            // This edge connects to an external concept - reroute it to the parent
            const externalNode = cy.getElementById('concept-' + relInfo.externalConceptId);
            const parentNode = cy.getElementById('concept-' + parentConceptId);

            if (externalNode.length && parentNode.length) {
                // Determine direction of the rerouted edge
                const isFromGrouped = relInfo.isFromGroupedChild;
                const sourceId = isFromGrouped ? parentConceptId : relInfo.externalConceptId;
                const targetId = isFromGrouped ? relInfo.externalConceptId : parentConceptId;

                // Create a temporary rerouted edge
                const reroutedId = `rerouted-${relInfo.relationshipId}`;

                // Check if we already have this rerouted edge
                const existingRerouted = cy.getElementById(reroutedId);
                if (!existingRerouted.length) {
                    cy.add({
                        group: 'edges',
                        data: {
                            id: reroutedId,
                            source: 'concept-' + sourceId,
                            target: 'concept-' + targetId,
                            label: relInfo.relationshipType,
                            relationshipType: relInfo.relationshipType,
                            originalRelationshipId: relInfo.relationshipId,
                            isRerouted: true
                        },
                        classes: 'rerouted-edge'
                    });

                }

                // Hide the original edge
                originalEdge.addClass('grouped-edge-hidden');
                originalEdge.style('visibility', 'hidden');
                reroutedEdges.add(relInfo.relationshipId);
            }
        } else {
            // This is an internal edge (both endpoints in the group) - just hide it
            originalEdge.addClass('grouped-edge-hidden');
            originalEdge.style('visibility', 'hidden');
        }
    });

}

/**
 * Set up drag-and-drop handlers for grouping
 * Drag one node over another to create a group with visual preview
 */
function setupDragHandlers(graphId, cy, dotNetHelper) {
    const state = groupingState.get(graphId);
    let groupPreviewOverlay = null;
    let dragThrottleTimer = null;
    const DRAG_THROTTLE_MS = 50; // Throttle drag event to 20fps for performance


    // Mouse down - start tracking drag
    cy.on('mousedown', 'node', function(evt) {
        const node = evt.target;

        // Don't allow dragging of grouped-hidden nodes
        if (node.hasClass('grouped-hidden')) return;

        state.draggedNode = node;
        state.isDragging = false;
        state.hoverTarget = null;
        state.canCreateGroup = null; // Reset validation state
    });

    // Drag - check for overlap with other nodes and show preview (throttled)
    cy.on('drag', 'node', function(evt) {
        const node = evt.target;


        if (!state.draggedNode) return;

        state.isDragging = true;

        // Throttle drag processing for performance
        if (dragThrottleTimer) return;

        dragThrottleTimer = setTimeout(() => {
            dragThrottleTimer = null;

            // Find nodes under cursor (overlapping)
            const position = node.position();
            const nearbyNodes = cy.nodes().filter(n => {
                if (n.id() === node.id()) return false;
                if (n.hasClass('grouped-hidden')) return false;

                const dist = Math.sqrt(
                    Math.pow(n.position().x - position.x, 2) +
                    Math.pow(n.position().y - position.y, 2)
                );

                return dist < state.groupingRadius; // Within configured radius = nearby
            });

            // Clear previous hover effects
            cy.nodes().removeClass('group-hover-target');
            cy.nodes().removeClass('group-hover-invalid');

            if (nearbyNodes.length > 0) {
                const targetNode = nearbyNodes[0];
                state.hoverTarget = targetNode;

                // Validate if this group can be created
                validateGroupCreation(state.dotNetHelper, node, targetNode, state);
            } else {
                state.hoverTarget = null;
                state.canCreateGroup = null;
                hideGroupPreview();
            }
        }, DRAG_THROTTLE_MS);
    });

    // Mouse up - complete grouping action
    cy.on('mouseup', 'node', async function(evt) {
        const node = evt.target;

            hasHoverTarget: !!state.hoverTarget,
            hasDraggedNode: !!state.draggedNode,
            isDragging: state.isDragging,
            canCreateGroup: state.canCreateGroup,
            hasDotNetHelper: !!state.dotNetHelper
        });

        // Clear hover effects
        cy.nodes().removeClass('group-hover-target');
        cy.nodes().removeClass('group-hover-invalid');
        hideGroupPreview();

        // Clear throttle timer
        if (dragThrottleTimer) {
            clearTimeout(dragThrottleTimer);
            dragThrottleTimer = null;
        }

        // If we have a valid drop target and validation passed, create group
        if (state.hoverTarget && state.draggedNode && state.isDragging && state.canCreateGroup) {
            const draggedId = parseInt(state.draggedNode.id().replace('concept-', ''));
            const targetId = parseInt(state.hoverTarget.id().replace('concept-', ''));

            try {
                // Call Blazor to create group
                await state.dotNetHelper.invokeMethodAsync('CreateConceptGroup', targetId, [draggedId]);
                showSuccessMessage(cy, 'Group created successfully');
            } catch (error) {
                console.error('Failed to create concept group:', error);
                showErrorMessage(cy, 'Failed to create group: ' + (error.message || 'Unknown error'));
            }
        } else if (state.hoverTarget && state.draggedNode && state.isDragging && state.canCreateGroup === false) {
            // Validation failed - show error
            showErrorMessage(cy, 'Cannot create group: would create circular reference');
        }

        // Reset state
        state.isDragging = false;
        state.draggedNode = null;
        state.hoverTarget = null;
        state.canCreateGroup = null;
    });

    // Cancel on drag outside graph
    cy.on('dragfreeon', 'node', function() {
        cy.nodes().removeClass('group-hover-target');
        cy.nodes().removeClass('group-hover-invalid');
        hideGroupPreview();
        state.isDragging = false;
        state.hoverTarget = null;
        state.canCreateGroup = null;

        if (dragThrottleTimer) {
            clearTimeout(dragThrottleTimer);
            dragThrottleTimer = null;
        }
    });

    /**
     * Validate if a group can be created between two nodes
     */
    async function validateGroupCreation(dotNetHelper, draggedNode, targetNode, state) {
        const draggedId = parseInt(draggedNode.id().replace('concept-', ''));
        const targetId = parseInt(targetNode.id().replace('concept-', ''));

            draggedId,
            targetId,
            hasDotNetHelper: !!dotNetHelper
        });

        try {
            const canCreate = await dotNetHelper.invokeMethodAsync('CanCreateGroup', targetId, [draggedId]);
            state.canCreateGroup = canCreate;

            if (canCreate) {
                targetNode.addClass('group-hover-target');
                showGroupPreview(cy, draggedNode, targetNode, true);
            } else {
                targetNode.addClass('group-hover-invalid');
                showGroupPreview(cy, draggedNode, targetNode, false);
            }
        } catch (error) {
            console.error('Failed to validate group creation:', error);
            state.canCreateGroup = false;
            targetNode.addClass('group-hover-invalid');
            showGroupPreview(cy, draggedNode, targetNode, false);
        }
    }

    /**
     * Show visual preview of the group being formed
     */
    function showGroupPreview(cy, draggedNode, targetNode, isValid) {
        const container = cy.container();

        // Create overlay if it doesn't exist
        if (!groupPreviewOverlay) {
            groupPreviewOverlay = document.createElement('div');
            groupPreviewOverlay.className = 'group-preview-overlay';
            container.appendChild(groupPreviewOverlay);
        }

        // Calculate center point between the two nodes
        const draggedPos = draggedNode.renderedPosition();
        const targetPos = targetNode.renderedPosition();

        const centerX = (draggedPos.x + targetPos.x) / 2;
        const centerY = (draggedPos.y + targetPos.y) / 2;

        // Calculate radius to encompass both nodes
        const distance = Math.sqrt(
            Math.pow(draggedPos.x - targetPos.x, 2) +
            Math.pow(draggedPos.y - targetPos.y, 2)
        );
        const radius = Math.max(distance / 2 + 40, 60); // Minimum 60px radius

        // Position the preview circle
        groupPreviewOverlay.style.left = (centerX - radius) + 'px';
        groupPreviewOverlay.style.top = (centerY - radius) + 'px';
        groupPreviewOverlay.style.width = (radius * 2) + 'px';
        groupPreviewOverlay.style.height = (radius * 2) + 'px';
        groupPreviewOverlay.style.display = 'block';

        // Update inner content - show 2 dots representing the nodes and validation status
        const labelText = isValid ? 'Release to group' : 'Cannot group (circular reference)';
        const labelClass = isValid ? 'group-preview-label' : 'group-preview-label-invalid';

        groupPreviewOverlay.innerHTML = `
            <div class="group-preview-content">
                <div class="group-preview-dots">
                    <div class="group-preview-dot"></div>
                    <div class="group-preview-dot"></div>
                </div>
                <div class="${labelClass}">${labelText}</div>
            </div>
        `;

        // Add invalid class if needed
        if (isValid) {
            groupPreviewOverlay.classList.remove('group-preview-invalid');
        } else {
            groupPreviewOverlay.classList.add('group-preview-invalid');
        }
    }

    /**
     * Hide the group preview overlay
     */
    function hideGroupPreview() {
        if (groupPreviewOverlay) {
            groupPreviewOverlay.style.display = 'none';
        }
    }
}

/**
 * Start wiggle animation on a node (iOS-style)
 * Uses border pulse since Cytoscape doesn't support CSS transforms
 */
function startWiggleAnimation(node) {
    if (!node || !node.length) return;

    node.addClass('wiggling');

    // Save original border width
    const originalBorderWidth = node.style('border-width');
    node.data('originalBorderWidth', originalBorderWidth);

    // Pulse animation using border width
    let pulseUp = true;
    const minWidth = parseInt(originalBorderWidth) || 2;
    const maxWidth = minWidth + 4;

    const wiggleInterval = setInterval(() => {
        const current = parseInt(node.style('border-width')) || minWidth;

        if (pulseUp) {
            if (current >= maxWidth) {
                pulseUp = false;
                node.style('border-width', maxWidth + 'px');
            } else {
                node.style('border-width', (current + 1) + 'px');
            }
        } else {
            if (current <= minWidth) {
                pulseUp = true;
                node.style('border-width', minWidth + 'px');
            } else {
                node.style('border-width', (current - 1) + 'px');
            }
        }

        // Also pulse the border color between blue and lighter blue
        node.style('border-color', pulseUp ? '#0d6efd' : '#5fa3ff');
    }, 100);

    node.data('wiggleInterval', wiggleInterval);
}

/**
 * Stop wiggle animation on a node
 */
function stopWiggleAnimation(node) {
    if (!node || !node.length) return;

    node.removeClass('wiggling');

    const wiggleInterval = node.data('wiggleInterval');
    if (wiggleInterval) {
        clearInterval(wiggleInterval);
        node.removeData('wiggleInterval');
    }

    // Restore original border
    const originalBorderWidth = node.data('originalBorderWidth');
    if (originalBorderWidth) {
        node.style('border-width', originalBorderWidth);
        node.removeData('originalBorderWidth');
    }
    node.style('border-color', ''); // Reset to default
}

/**
 * Set up handlers for group interaction (expand/collapse)
 */
function setupGroupHandlers(graphId, cy, dotNetHelper) {
    const state = groupingState.get(graphId);

    let expandButton = null;
    let expandButtonClickHandler = null;

    // Mouse over grouped node - show expand button
    cy.on('mouseover', 'node.grouped', function(evt) {
        const node = evt.target;
        const groupId = node.data('groupId');
        const childCount = (node.data('groupedChildren') || []).length;

        if (!groupId || childCount === 0) return;

        // Get node position in rendered coordinates
        const renderedPos = node.renderedPosition();
        const zoom = cy.zoom();
        const nodeWidth = node.renderedWidth();

        // Create expand button if it doesn't exist
        if (!expandButton) {
            const container = cy.container();
            expandButton = document.createElement('button');
            expandButton.className = 'concept-group-expand-btn';
            expandButton.innerHTML = `
                <svg width="16" height="16" viewBox="0 0 16 16" fill="currentColor">
                    <path d="M8 3.5a.5.5 0 0 1 .5.5v4h4a.5.5 0 0 1 0 1h-4v4a.5.5 0 0 1-1 0v-4h-4a.5.5 0 0 1 0-1h4v-4a.5.5 0 0 1 .5-.5z"/>
                </svg>
                <span>${childCount}</span>
            `;
            expandButton.title = `Expand group (${childCount} node${childCount > 1 ? 's' : ''})`;
            expandButton.style.position = 'absolute';
            expandButton.style.pointerEvents = 'auto';
            expandButton.style.zIndex = '100';
            container.appendChild(expandButton);

            // Create and store click handler to prevent duplicates
            expandButtonClickHandler = async (e) => {
                e.stopPropagation();
                const currentGroupId = expandButton.dataset.groupId;


                try {
                    await dotNetHelper.invokeMethodAsync('ToggleConceptGroup', parseInt(currentGroupId));

                    // Remove button after expansion
                    if (expandButton) {
                        if (expandButtonClickHandler) {
                            expandButton.removeEventListener('click', expandButtonClickHandler);
                        }
                        expandButton.remove();
                        expandButton = null;
                        expandButtonClickHandler = null;
                    }
                } catch (error) {
                    console.error('❌ Failed to expand group:', error);
                }
            };

            // Add click handler
            expandButton.addEventListener('click', expandButtonClickHandler);
        }

        // Position the button
        expandButton.style.left = (renderedPos.x + nodeWidth / 2 + 10) + 'px';
        expandButton.style.top = (renderedPos.y - 12) + 'px';
        expandButton.dataset.groupId = groupId;
        expandButton.dataset.nodeId = node.id();

        // Update count
        const countSpan = expandButton.querySelector('span');
        if (countSpan) {
            countSpan.textContent = childCount;
        }
        expandButton.title = `Expand group (${childCount} node${childCount > 1 ? 's' : ''})`;
    });

    // Mouse out - hide expand button after a delay
    cy.on('mouseout', 'node.grouped', function(evt) {
        setTimeout(() => {
            // Check if mouse is over the button
            if (expandButton && !expandButton.matches(':hover')) {
                expandButton.remove();
                expandButton = null;
            }
        }, 200);
    });

    // Remove button when mouse leaves it
    document.addEventListener('mouseout', (e) => {
        if (expandButton && e.target === expandButton) {
            setTimeout(() => {
                if (expandButton && !expandButton.matches(':hover')) {
                    const nodeId = expandButton.dataset.nodeId;
                    const node = cy.getElementById(nodeId);

                    // Only remove if not hovering over the node either
                    if (!node.length || !node.data('hovered')) {
                        expandButton.remove();
                        expandButton = null;
                    }
                }
            }, 200);
        }
    });

    // Track hover state on nodes
    cy.on('mouseover', 'node', function(evt) {
        evt.target.data('hovered', true);
    });

    cy.on('mouseout', 'node', function(evt) {
        evt.target.data('hovered', false);
    });

    // Clean up button on pan/zoom
    cy.on('pan zoom', function() {
        if (expandButton) {
            expandButton.remove();
            expandButton = null;
        }
    });
}

/**
 * Expand a group (show all children)
 */
window.expandConceptGroup = function(graphId, groupId) {
    const state = groupingState.get(graphId);
    if (!state) return;

    const cy = window.cytoscapeInstances && window.cytoscapeInstances.get(graphId);
    if (!cy) return;

    const group = state.groups.find(g => g.id === groupId);
    if (!group) return;

    const parentNode = cy.getElementById('concept-' + group.parentConceptId);
    const childIds = JSON.parse(group.childConceptIds || '[]');
    const collapsedRelationships = group.collapsedRelationships ?
        JSON.parse(group.collapsedRelationships) : [];

    // Remove floating indicators from anywhere in the document
    const indicators = document.querySelectorAll(`.group-indicator-container[data-node-id="${parentNode.id()}"]`);
    indicators.forEach(ind => {
        ind.remove();
    });

    // Remove event handler for indicator updates
    const updateHandler = parentNode.data('indicatorUpdateHandler');
    if (updateHandler) {
        cy.off('pan zoom position', updateHandler);
        parentNode.removeData('indicatorUpdateHandler');
    }

    // Remove grouped class from parent
    parentNode.removeClass('grouped');
    parentNode.removeData('groupedChildren');
    parentNode.removeData('groupCount');
    parentNode.removeData('groupId');
    parentNode.removeData('collapsedRelationships');

    // Reset parent style
    parentNode.style({
        'border-width': '',
        'border-color': '',
        'box-shadow': ''
    });

    // Show child nodes and restore their edges
    // Position them intelligently to avoid overlaps with other nodes
    const parentPos = parentNode.position();
    const spawnRadius = 150; // Distance from parent to spawn children (px)
    const minNodeDistance = 80; // Minimum distance from other nodes

    // Get all other visible nodes to avoid
    const otherNodes = cy.nodes().filter(n =>
        n.id() !== parentNode.id() &&
        !childIds.includes(parseInt(n.id().replace('concept-', ''))) &&
        n.visible()
    );

    childIds.forEach((childId, index) => {
        const childNode = cy.getElementById('concept-' + childId);
        if (childNode.length) {
            childNode.removeClass('grouped-hidden');
            childNode.style('visibility', 'visible');

            // Find the best position for this child node
            const angleStep = (2 * Math.PI) / childIds.length;
            let bestAngle = index * angleStep;
            let bestPos = {
                x: parentPos.x + Math.cos(bestAngle) * spawnRadius,
                y: parentPos.y + Math.sin(bestAngle) * spawnRadius
            };
            let bestScore = -Infinity;

            // Try different angles to find the position with least overlap
            for (let angleOffset = 0; angleOffset < 2 * Math.PI; angleOffset += Math.PI / 8) {
                const testAngle = bestAngle + angleOffset;
                const testPos = {
                    x: parentPos.x + Math.cos(testAngle) * spawnRadius,
                    y: parentPos.y + Math.sin(testAngle) * spawnRadius
                };

                // Calculate score based on distance to other nodes (higher is better)
                let score = 0;
                let tooClose = false;

                otherNodes.forEach(otherNode => {
                    const otherPos = otherNode.position();
                    const dist = Math.sqrt(
                        Math.pow(testPos.x - otherPos.x, 2) +
                        Math.pow(testPos.y - otherPos.y, 2)
                    );

                    if (dist < minNodeDistance) {
                        tooClose = true;
                        score -= 1000; // Heavy penalty for being too close
                    } else {
                        score += dist; // Reward for being far away
                    }
                });

                if (score > bestScore) {
                    bestScore = score;
                    bestPos = testPos;
                }
            }


            // Animate the node to its new position
            childNode.animate({
                position: { x: bestPos.x, y: bestPos.y }
            }, {
                duration: 400,
                easing: 'ease-out'
            });

            // Restore ALL edges connected to this child node
            const connectedEdges = childNode.connectedEdges();
            connectedEdges.forEach(edge => {
                // Only restore if it's not a rerouted edge (those will be removed separately)
                if (!edge.hasClass('rerouted-edge')) {
                    edge.removeClass('grouped-edge-hidden');
                    edge.style('visibility', 'visible');
                }
            });
        }
    });

    // Remove rerouted edges
    collapsedRelationships.forEach(relInfo => {
        const reroutedId = `rerouted-${relInfo.relationshipId}`;
        const reroutedEdge = cy.getElementById(reroutedId);
        if (reroutedEdge.length) {
            cy.remove(reroutedEdge);
        }
    });

};

/**
 * Collapse a group (hide all children)
 */
window.collapseConceptGroup = function(graphId, groupId) {
    const state = groupingState.get(graphId);
    if (!state) return;

    const cy = window.cytoscapeInstances && window.cytoscapeInstances.get(graphId);
    if (!cy) return;

    const group = state.groups.find(g => g.id === groupId);
    if (!group) return;

    applyGroupsToGraph(graphId, cy, [group]);

};

/**
 * Update groups after server-side changes
 */
window.updateConceptGroups = function(graphId, groups) {

    const state = groupingState.get(graphId);
    if (!state) {
        console.error('❌ No grouping state found for', graphId);
        return;
    }

        hasDotNetHelper: !!state.dotNetHelper,
        groupCount: state.groups?.length || 0
    });

    state.groups = groups;

        hasDotNetHelper: !!state.dotNetHelper,
        groupCount: state.groups?.length || 0
    });

    const cy = window.cytoscapeInstances && window.cytoscapeInstances.get(graphId);
    if (!cy) {
        console.error('❌ No Cytoscape instance found for', graphId);
        return;
    }


    // Clear all existing group styles and remove indicators
    cy.nodes('.grouped').forEach(node => {
        // Remove floating indicators for this node
        const indicators = document.querySelectorAll(`.group-indicator-container[data-node-id="${node.id()}"]`);
        indicators.forEach(ind => {
            ind.remove();
        });

        // Remove event handler for indicator updates
        const updateHandler = node.data('indicatorUpdateHandler');
        if (updateHandler) {
            cy.off('pan zoom position', updateHandler);
            node.removeData('indicatorUpdateHandler');
        }

        node.removeClass('grouped');
        node.removeData('groupedChildren');
        node.removeData('groupCount');
        node.removeData('groupId');
        node.removeData('collapsedRelationships');
    });

    // Show previously hidden nodes and position them intelligently
    const hiddenNodes = cy.nodes('.grouped-hidden');

    hiddenNodes.forEach(hiddenNode => {
        hiddenNode.removeClass('grouped-hidden').style('visibility', 'visible');

        // Find the parent node (the grouped node this was hidden under)
        // We need to position this node away from others
        const connectedNodes = hiddenNode.connectedEdges().connectedNodes();
        let parentNode = null;

        // Try to find a parent that was recently ungrouped
        connectedNodes.forEach(node => {
            if (!node.hasClass('grouped-hidden') && node.visible()) {
                parentNode = node;
            }
        });

        if (!parentNode && connectedNodes.length > 0) {
            parentNode = connectedNodes[0];
        }

        if (parentNode) {
            const parentPos = parentNode.position();
            const spawnRadius = 150;
            const minNodeDistance = 80;

            // Get all visible nodes to avoid
            const otherNodes = cy.nodes().filter(n =>
                n.id() !== hiddenNode.id() &&
                n.id() !== parentNode.id() &&
                n.visible()
            );

            let bestPos = null;
            let bestScore = -Infinity;

            // Try 16 different angles around the parent
            for (let i = 0; i < 16; i++) {
                const angle = (i / 16) * 2 * Math.PI;
                const testPos = {
                    x: parentPos.x + Math.cos(angle) * spawnRadius,
                    y: parentPos.y + Math.sin(angle) * spawnRadius
                };

                let score = 0;

                otherNodes.forEach(otherNode => {
                    const otherPos = otherNode.position();
                    const dist = Math.sqrt(
                        Math.pow(testPos.x - otherPos.x, 2) +
                        Math.pow(testPos.y - otherPos.y, 2)
                    );

                    if (dist < minNodeDistance) {
                        score -= 1000;
                    } else {
                        score += dist;
                    }
                });

                if (score > bestScore) {
                    bestScore = score;
                    bestPos = testPos;
                }
            }

            if (bestPos) {
                hiddenNode.animate({
                    position: { x: bestPos.x, y: bestPos.y }
                }, {
                    duration: 400,
                    easing: 'ease-out'
                });
            }
        }
    });

    cy.edges('.grouped-edge-hidden').removeClass('grouped-edge-hidden').style('visibility', 'visible');

    // Remove any rerouted edges
    cy.edges('.rerouted-edge').remove();


    // Reapply groups
    applyGroupsToGraph(graphId, cy, groups);

};

/**
 * Show success message overlay
 */
function showSuccessMessage(cy, message) {
    const container = cy.container();
    const messageDiv = document.createElement('div');
    messageDiv.className = 'concept-group-message concept-group-message-success';
    messageDiv.textContent = message;
    messageDiv.style.position = 'absolute';
    messageDiv.style.top = '20px';
    messageDiv.style.left = '50%';
    messageDiv.style.transform = 'translateX(-50%)';
    messageDiv.style.padding = '12px 24px';
    messageDiv.style.backgroundColor = '#28a745';
    messageDiv.style.color = 'white';
    messageDiv.style.borderRadius = '8px';
    messageDiv.style.boxShadow = '0 4px 12px rgba(0,0,0,0.3)';
    messageDiv.style.zIndex = '100';
    messageDiv.style.fontSize = '14px';
    messageDiv.style.fontWeight = '500';
    messageDiv.style.pointerEvents = 'none';
    messageDiv.style.opacity = '0';
    messageDiv.style.transition = 'opacity 0.3s ease';

    container.appendChild(messageDiv);

    // Fade in
    setTimeout(() => {
        messageDiv.style.opacity = '1';
    }, 10);

    // Fade out and remove
    setTimeout(() => {
        messageDiv.style.opacity = '0';
        setTimeout(() => {
            if (messageDiv.parentNode) {
                messageDiv.parentNode.removeChild(messageDiv);
            }
        }, 300);
    }, 2000);
}

/**
 * Show error message overlay
 */
function showErrorMessage(cy, message) {
    const container = cy.container();
    const messageDiv = document.createElement('div');
    messageDiv.className = 'concept-group-message concept-group-message-error';
    messageDiv.textContent = message;
    messageDiv.style.position = 'absolute';
    messageDiv.style.top = '20px';
    messageDiv.style.left = '50%';
    messageDiv.style.transform = 'translateX(-50%)';
    messageDiv.style.padding = '12px 24px';
    messageDiv.style.backgroundColor = '#dc3545';
    messageDiv.style.color = 'white';
    messageDiv.style.borderRadius = '8px';
    messageDiv.style.boxShadow = '0 4px 12px rgba(0,0,0,0.3)';
    messageDiv.style.zIndex = '100';
    messageDiv.style.fontSize = '14px';
    messageDiv.style.fontWeight = '500';
    messageDiv.style.pointerEvents = 'none';
    messageDiv.style.opacity = '0';
    messageDiv.style.transition = 'opacity 0.3s ease';

    container.appendChild(messageDiv);

    // Fade in
    setTimeout(() => {
        messageDiv.style.opacity = '1';
    }, 10);

    // Fade out and remove
    setTimeout(() => {
        messageDiv.style.opacity = '0';
        setTimeout(() => {
            if (messageDiv.parentNode) {
                messageDiv.parentNode.removeChild(messageDiv);
            }
        }, 300);
    }, 3000); // Show errors a bit longer
}
