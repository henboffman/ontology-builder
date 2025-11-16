# 2025-11-14-notes-functionality

## Purpose

Defines functionality to be added to the Eidos application in the next feature release, version 1.1. This update modifies the application such that the ontology is no longer the central entity in the application. Instead, we have an eidos base entity (name to be determined), each of which can contain an ontology (i.e. everything in the current application version), and also notes, which are a basic flat collection of notes that render in either rich text or markdown. The notes can use a tagging syntax: [[]] then something is wrapped in [[]], it references a concept. the concept should be created in the graph when the [[]] tag is discovered. additionally, each concept in the ontology should have a supporting note (named <concept-name>-note.md).

I have attempted to document the proposed high level schema using the application:
{
  "name": "eidos design",
  "version": "1.1",
  "usesBFO": false,
  "usesProvO": false,
  "visibility": "private",
  "allowPublicEdit": false,
  "createdAt": "2025-11-15T00:00:45.802469",
  "updatedAt": "2025-11-15T00:30:19.367411",
  "conceptCount": 16,
  "relationshipCount": 21,
  "concepts": [
    {
      "name": "Ontology",
      "color": "#85f7ff",
      "positionX": 105.73,
      "positionY": 251.69,
      "createdAt": "2025-11-15T00:00:57.452686",
      "properties": [],
      "conceptProperties": [],
      "restrictions": []
    },
    {
      "name": "Concept",
      "color": "#85f7ff",
      "positionX": 211.68,
      "positionY": 219.64,
      "createdAt": "2025-11-15T00:01:13.898033",
      "properties": [],
      "conceptProperties": [
        {
          "name": "version",
          "propertyType": "DataProperty",
          "dataType": "string",
          "isRequired": true,
          "isFunctional": false,
          "createdAt": "2025-11-15T00:04:54.710869",
          "updatedAt": "2025-11-15T00:04:54.710914"
        }
      ],
      "restrictions": []
    },
    {
      "name": "Relationship",
      "color": "#bb00ff",
      "positionX": 77.83,
      "positionY": 82.52,
      "createdAt": "2025-11-15T00:01:26.625136",
      "properties": [],
      "conceptProperties": [],
      "restrictions": []
    },
    {
      "name": "Individual",
      "simpleExplanation": "Also known as Instances",
      "color": "#9bf6fd",
      "positionX": 251.64,
      "positionY": 328.53,
      "createdAt": "2025-11-15T00:01:53.928237",
      "properties": [],
      "conceptProperties": [],
      "restrictions": []
    },
    {
      "name": "Graph View",
      "color": "#c79bfd",
      "positionX": 167.75,
      "positionY": 328.71,
      "createdAt": "2025-11-15T00:03:03.760906",
      "properties": [],
      "conceptProperties": [],
      "restrictions": []
    },
    {
      "name": "List View",
      "color": "#c79bfd",
      "positionX": 29.22,
      "positionY": 161.78,
      "createdAt": "2025-11-15T00:03:09.491031",
      "properties": [],
      "conceptProperties": [],
      "restrictions": []
    },
    {
      "name": "Collapsable Groups",
      "category": "Feature",
      "color": "#da8185",
      "positionX": 21,
      "positionY": 251.55,
      "createdAt": "2025-11-15T00:06:08.74138",
      "properties": [],
      "conceptProperties": [],
      "restrictions": []
    },
    {
      "name": "Enhanced Grouping",
      "simpleExplanation": "Additional functionality for creating groups of nodes and manipulating them in the application.",
      "color": "#da8185",
      "positionX": 82.59,
      "positionY": 334.77,
      "createdAt": "2025-11-15T00:08:51.988153",
      "properties": [],
      "conceptProperties": [],
      "restrictions": []
    },
    {
      "name": "Notes",
      "color": "#bc8590",
      "positionX": 358.17,
      "positionY": 115.8,
      "createdAt": "2025-11-15T00:10:00.055622",
      "properties": [],
      "conceptProperties": [],
      "restrictions": []
    },
    {
      "name": "Eidos base entity",
      "simpleExplanation": "Name/term TBD",
      "color": "#bc8590",
      "positionX": 306.71,
      "positionY": 71.26,
      "createdAt": "2025-11-15T00:11:41.205209",
      "properties": [],
      "conceptProperties": [],
      "restrictions": []
    },
    {
      "name": "Note",
      "simpleExplanation": "Individual note document",
      "color": "#bc8590",
      "positionX": 441.01,
      "positionY": 103.66,
      "createdAt": "2025-11-15T00:13:40.638813",
      "properties": [],
      "conceptProperties": [],
      "restrictions": []
    },
    {
      "name": "Text body",
      "color": "#bc8985",
      "positionX": 436.59,
      "positionY": 203.17,
      "createdAt": "2025-11-15T00:14:25.739271",
      "properties": [],
      "conceptProperties": [],
      "restrictions": []
    },
    {
      "name": "Render Format",
      "color": "#b685bc",
      "positionX": 263.85,
      "positionY": 25.79,
      "createdAt": "2025-11-15T00:15:44.586176",
      "properties": [],
      "conceptProperties": [],
      "restrictions": []
    },
    {
      "name": "Markdown",
      "color": "#b685bc",
      "positionX": 161.53,
      "positionY": 21,
      "createdAt": "2025-11-15T00:16:18.336479",
      "properties": [],
      "conceptProperties": [],
      "restrictions": []
    },
    {
      "name": "Rich text format",
      "color": "#b37575",
      "positionX": 201.18,
      "positionY": -14.05,
      "createdAt": "2025-11-15T00:23:18.694062",
      "properties": [],
      "conceptProperties": [],
      "restrictions": []
    },
    {
      "name": "version 1.1",
      "color": "#95A5A6",
      "positionX": 410.49,
      "positionY": 21,
      "createdAt": "2025-11-15T00:29:01.895987",
      "properties": [],
      "conceptProperties": [],
      "restrictions": []
    }
  ],
  "relationships": [
    {
      "sourceConcept": "Ontology",
      "relationType": "has-many",
      "targetConcept": "Relationship",
      "createdAt": "2025-11-15T00:02:06.369387"
    },
    {
      "sourceConcept": "Ontology",
      "relationType": "has-many",
      "targetConcept": "Concept",
      "createdAt": "2025-11-15T00:02:13.192486"
    },
    {
      "sourceConcept": "Ontology",
      "relationType": "has-many",
      "targetConcept": "Individual",
      "createdAt": "2025-11-15T00:02:19.04458"
    },
    {
      "sourceConcept": "Ontology",
      "relationType": "renders-in",
      "targetConcept": "Graph View",
      "createdAt": "2025-11-15T00:03:21.007452"
    },
    {
      "sourceConcept": "Ontology",
      "relationType": "renders-in",
      "targetConcept": "List View",
      "createdAt": "2025-11-15T00:03:29.425244"
    },
    {
      "sourceConcept": "Collapsable Groups",
      "relationType": "v1-feature",
      "targetConcept": "Graph View",
      "createdAt": "2025-11-15T00:06:30.341005"
    },
    {
      "sourceConcept": "Enhanced Grouping",
      "relationType": "v1-feature",
      "targetConcept": "Graph View",
      "createdAt": "2025-11-15T00:09:02.761104"
    },
    {
      "sourceConcept": "Ontology",
      "relationType": "part-of",
      "targetConcept": "Eidos base entity",
      "createdAt": "2025-11-15T00:12:14.822325"
    },
    {
      "sourceConcept": "Notes",
      "relationType": "part-of",
      "targetConcept": "Eidos base entity",
      "createdAt": "2025-11-15T00:12:30.43703"
    },
    {
      "sourceConcept": "Notes",
      "relationType": "has-many",
      "targetConcept": "Note",
      "createdAt": "2025-11-15T00:13:58.167359"
    },
    {
      "sourceConcept": "Note",
      "relationType": "has-a",
      "targetConcept": "Text body",
      "createdAt": "2025-11-15T00:14:42.271973"
    },
    {
      "sourceConcept": "Text body",
      "relationType": "has-a",
      "targetConcept": "Render Format",
      "createdAt": "2025-11-15T00:16:08.020267"
    },
    {
      "sourceConcept": "Markdown",
      "relationType": "instance-of",
      "targetConcept": "Render Format",
      "createdAt": "2025-11-15T00:16:32.968888"
    },
    {
      "sourceConcept": "Rich text format",
      "relationType": "instance-of",
      "targetConcept": "Render Format",
      "createdAt": "2025-11-15T00:23:33.607033"
    },
    {
      "sourceConcept": "Text body",
      "relationType": "references-many",
      "targetConcept": "Concept",
      "createdAt": "2025-11-15T00:25:44.430897"
    },
    {
      "sourceConcept": "Concept",
      "relationType": "has-a",
      "targetConcept": "Note",
      "description": "Each concept has a note that is automatically created and is available for users to view and modify. The note is captured in the database and associated with the concept",
      "createdAt": "2025-11-15T00:28:25.659252"
    },
    {
      "sourceConcept": "Eidos base entity",
      "relationType": "added-in",
      "targetConcept": "version 1.1",
      "createdAt": "2025-11-15T00:29:17.578116"
    },
    {
      "sourceConcept": "Notes",
      "relationType": "added-in",
      "targetConcept": "version 1.1",
      "createdAt": "2025-11-15T00:29:26.428785"
    },
    {
      "sourceConcept": "Render Format",
      "relationType": "added-in",
      "targetConcept": "version 1.1",
      "createdAt": "2025-11-15T00:29:40.492214"
    },
    {
      "sourceConcept": "Enhanced Grouping",
      "relationType": "added-in",
      "targetConcept": "version 1.1",
      "createdAt": "2025-11-15T00:30:11.794957"
    },
    {
      "sourceConcept": "Collapsable Groups",
      "relationType": "added-in",
      "targetConcept": "version 1.1",
      "createdAt": "2025-11-15T00:30:19.361665"
    }
  ],
  "individuals": [],
  "individualRelationships": [],
  "linkedOntologies": [],
  "customTemplates": []
}
