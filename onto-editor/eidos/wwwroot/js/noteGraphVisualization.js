// Note Graph Visualization using Cytoscape.js
// Renders interactive network of notes connected by shared concepts

let noteGraphInstances = {};

window.renderNoteGraph = function (elementId, graphDataJson, layout = 'cose') {
    try {
        const graphData = JSON.parse(graphDataJson);
        const container = document.getElementById(elementId);

        if (!container) {
            console.error(`Note graph container not found: ${elementId}`);
            return;
        }

        // Destroy existing instance if it exists
        if (noteGraphInstances[elementId]) {
            noteGraphInstances[elementId].destroy();
            delete noteGraphInstances[elementId];
        }

        // Initialize Cytoscape
        const cy = cytoscape({
            container: container,
            elements: [...graphData.nodes, ...graphData.edges],
            style: getNoteGraphStyle(),
            layout: getNoteGraphLayout(layout),
            minZoom: 0.5,
            maxZoom: 3,
            wheelSensitivity: 0.1
        });

        // Store instance
        noteGraphInstances[elementId] = cy;

        // Add interactivity
        setupNoteGraphInteractivity(cy, elementId);

        // Fit graph to view
        cy.fit(null, 50);

    } catch (error) {
        console.error('Error rendering note graph:', error);
    }
};

function getNoteGraphStyle() {
    return [
        // Note nodes - larger, blue
        {
            selector: 'node[nodeType = "note"]',
            style: {
                'background-color': function (ele) {
                    return ele.data('isConceptNote') ? '#10b981' : '#3b82f6';
                },
                'border-width': 2,
                'border-color': function (ele) {
                    return ele.data('isConceptNote') ? '#059669' : '#2563eb';
                },
                'label': 'data(label)',
                'width': function (ele) {
                    // Size based on concept count (40px to 80px for notes)
                    const conceptCount = ele.data('conceptCount') || 0;
                    return Math.min(80, 40 + conceptCount * 5);
                },
                'height': function (ele) {
                    const conceptCount = ele.data('conceptCount') || 0;
                    return Math.min(80, 40 + conceptCount * 5);
                },
                'shape': 'roundrectangle',
                'text-valign': 'center',
                'text-halign': 'center',
                'font-size': '11px',
                'font-weight': '600',
                'color': '#1f2937',
                'text-outline-color': '#ffffff',
                'text-outline-width': 2,
                'text-wrap': 'wrap',
                'text-max-width': '100px'
            }
        },
        // Concept nodes - smaller, different colors by category
        {
            selector: 'node[nodeType = "concept"]',
            style: {
                'background-color': function (ele) {
                    // Color based on category or default
                    const category = ele.data('conceptCategory');
                    const colorMap = {
                        'Entity': '#f59e0b',
                        'Process': '#8b5cf6',
                        'Property': '#ec4899',
                        'Relation': '#14b8a6',
                        'Event': '#f97316'
                    };
                    return colorMap[category] || '#6b7280';
                },
                'border-width': 2,
                'border-color': '#374151',
                'label': 'data(label)',
                'width': 30,
                'height': 30,
                'shape': 'ellipse',
                'text-valign': 'bottom',
                'text-halign': 'center',
                'text-margin-y': 5,
                'font-size': '9px',
                'font-weight': '500',
                'color': '#1f2937',
                'text-outline-color': '#ffffff',
                'text-outline-width': 2,
                'text-wrap': 'wrap',
                'text-max-width': '60px'
            }
        },
        {
            selector: 'node:selected',
            style: {
                'border-width': 4,
                'border-color': '#f59e0b',
                'overlay-color': '#f59e0b',
                'overlay-padding': 8,
                'overlay-opacity': 0.2
            }
        },
        // "Contains" edges - note to concept (dashed, lighter)
        {
            selector: 'edge[edgeType = "contains"]',
            style: {
                'width': 1,
                'line-color': '#cbd5e1',
                'line-style': 'dashed',
                'target-arrow-shape': 'triangle',
                'target-arrow-color': '#cbd5e1',
                'curve-style': 'bezier',
                'opacity': 0.5
            }
        },
        // Relationship edges - concept to concept (solid, with labels)
        {
            selector: 'edge[edgeType != "contains"]',
            style: {
                'width': function (ele) {
                    return 2;
                },
                'line-color': function (ele) {
                    // Color based on relationship type
                    const type = ele.data('edgeType');
                    const colorMap = {
                        'is-a': '#10b981',
                        'part-of': '#3b82f6',
                        'related-to': '#6b7280',
                        'causes': '#ef4444',
                        'depends-on': '#f59e0b'
                    };
                    return colorMap[type] || '#94a3b8';
                },
                'target-arrow-shape': 'triangle',
                'target-arrow-color': function (ele) {
                    const type = ele.data('edgeType');
                    const colorMap = {
                        'is-a': '#10b981',
                        'part-of': '#3b82f6',
                        'related-to': '#6b7280',
                        'causes': '#ef4444',
                        'depends-on': '#f59e0b'
                    };
                    return colorMap[type] || '#94a3b8';
                },
                'curve-style': 'bezier',
                'opacity': 0.7,
                'label': function (ele) {
                    return ele.data('label') || ele.data('edgeType');
                },
                'font-size': '8px',
                'text-rotation': 'autorotate',
                'text-background-color': '#ffffff',
                'text-background-opacity': 0.8,
                'text-background-padding': '2px'
            }
        },
        {
            selector: 'edge:selected',
            style: {
                'line-color': '#f59e0b',
                'target-arrow-color': '#f59e0b',
                'opacity': 1,
                'width': 3
            }
        }
    ];
}

function getNoteGraphLayout(layoutName) {
    const layouts = {
        'cose': {
            name: 'cose',
            animate: true,
            animationDuration: 500,
            nodeRepulsion: 8000,
            nodeOverlap: 20,
            idealEdgeLength: 100,
            edgeElasticity: 100,
            nestingFactor: 5,
            gravity: 80,
            numIter: 1000,
            initialTemp: 200,
            coolingFactor: 0.95,
            minTemp: 1.0
        },
        'circle': {
            name: 'circle',
            animate: true,
            animationDuration: 500,
            avoidOverlap: true,
            spacingFactor: 1.5
        },
        'grid': {
            name: 'grid',
            animate: true,
            animationDuration: 500,
            avoidOverlap: true,
            avoidOverlapPadding: 10,
            rows: undefined,
            cols: undefined
        },
        'concentric': {
            name: 'concentric',
            animate: true,
            animationDuration: 500,
            avoidOverlap: true,
            spacingFactor: 1.5,
            concentric: function (node) {
                return node.data('conceptCount') || 1;
            },
            levelWidth: function () {
                return 2;
            }
        }
    };

    return layouts[layoutName] || layouts['cose'];
}

function setupNoteGraphInteractivity(cy, elementId) {
    // Node tap event
    cy.on('tap', 'node', function (evt) {
        const node = evt.target;
        const noteId = node.data('noteId');
        const label = node.data('label');
        const conceptCount = node.data('conceptCount');
        const isConceptNote = node.data('isConceptNote');
        const tags = node.data('tags') || [];

        showNoteDetails(noteId, label, conceptCount, isConceptNote, tags);
    });

    // Node double-tap to navigate
    cy.on('dbltap', 'node', function (evt) {
        const node = evt.target;
        const noteId = node.data('noteId');

        // Trigger Blazor callback to navigate to note
        if (typeof DotNet !== 'undefined') {
            // This will be handled by Blazor component's OnNoteSelected callback
        }
    });

    // Edge tap event
    cy.on('tap', 'edge', function (evt) {
        const edge = evt.target;
        const weight = edge.data('weight');
        const source = edge.source().data('label');
        const target = edge.target().data('label');

        showEdgeDetails(source, target, weight);
    });

    // Highlight connected edges on node hover
    cy.on('mouseover', 'node', function (evt) {
        const node = evt.target;
        const connectedEdges = node.connectedEdges();

        connectedEdges.style({
            'line-color': '#6366f1',
            'opacity': 0.9
        });
    });

    cy.on('mouseout', 'node', function (evt) {
        const node = evt.target;
        const connectedEdges = node.connectedEdges();

        connectedEdges.style({
            'line-color': '#94a3b8',
            'opacity': 0.6
        });
    });
}

function showNoteDetails(noteId, label, conceptCount, isConceptNote, tags) {
    const noteType = isConceptNote ? 'Concept Note' : 'User Note';
    const tagList = tags.length > 0 ? tags.join(', ') : 'None';

    // TODO: Could enhance this with a tooltip or popup panel
}

function showEdgeDetails(source, target, weight) {
    // TODO: Could enhance this with a tooltip showing which concepts are shared
}

window.fitNoteGraph = function (elementId) {
    const cy = noteGraphInstances[elementId];
    if (cy) {
        cy.fit(null, 50);
        cy.center();
    }
};

window.updateNoteGraphLayout = function (elementId, layoutName) {
    const cy = noteGraphInstances[elementId];
    if (cy) {
        const layout = cy.layout(getNoteGraphLayout(layoutName));
        layout.run();
    }
};

window.destroyNoteGraph = function (elementId) {
    const cy = noteGraphInstances[elementId];
    if (cy) {
        cy.destroy();
        delete noteGraphInstances[elementId];
    }
};

// Dark mode support
function updateNoteGraphTheme(elementId, isDarkMode) {
    const cy = noteGraphInstances[elementId];
    if (!cy) return;

    const textColor = isDarkMode ? '#f3f4f6' : '#1f2937';
    const textOutline = isDarkMode ? '#1f2937' : '#ffffff';

    cy.style()
        .selector('node')
        .style({
            'color': textColor,
            'text-outline-color': textOutline
        })
        .update();
}

// Listen for theme changes
if (window.matchMedia) {
    const darkModeQuery = window.matchMedia('(prefers-color-scheme: dark)');
    darkModeQuery.addListener(function (e) {
        const isDarkMode = e.matches;
        Object.keys(noteGraphInstances).forEach(elementId => {
            updateNoteGraphTheme(elementId, isDarkMode);
        });
    });
}

// Cleanup on page unload
window.addEventListener('beforeunload', function () {
    Object.keys(noteGraphInstances).forEach(elementId => {
        destroyNoteGraph(elementId);
    });
});
