using Eidos.Constants;
using Eidos.Models;
using Eidos.Services.Interfaces;
using VDS.RDF;
using VDS.RDF.Writing;
using System.IO;
using System.Linq;

namespace Eidos.Services
{
    public enum RdfFormat
    {
        Turtle,
        RdfXml,
        NTriples,
        JsonLd
    }

    public class TtlExportService : ITtlExportService
    {
        public string ExportToFormat(Ontology ontology, RdfFormat format)
        {
            var graph = BuildRdfGraph(ontology);

            using var stringWriter = new System.IO.StringWriter();

            // JsonLdWriter requires a TripleStore, not just a Graph
            if (format == RdfFormat.JsonLd)
            {
                var store = new VDS.RDF.TripleStore();
                store.Add(graph);
                var jsonLdWriter = new JsonLdWriter();

                // Write to a temp file since JsonLdWriter doesn't support TextWriter
                var tempFile = System.IO.Path.GetTempFileName();
                try
                {
                    jsonLdWriter.Save(store, tempFile);
                    return System.IO.File.ReadAllText(tempFile);
                }
                finally
                {
                    if (System.IO.File.Exists(tempFile))
                        System.IO.File.Delete(tempFile);
                }
            }

            IRdfWriter writer = format switch
            {
                RdfFormat.RdfXml => new RdfXmlWriter(),
                RdfFormat.NTriples => new NTriplesWriter(),
                _ => new CompressingTurtleWriter()
            };

            writer.Save(graph, stringWriter);
            return stringWriter.ToString();
        }

        public string ExportToTtl(Ontology ontology)
        {
            return ExportToFormat(ontology, RdfFormat.Turtle);
        }

        private IGraph BuildRdfGraph(Ontology ontology)
        {
            var graph = new Graph();
            graph.NamespaceMap.AddNamespace("rdf", UriFactory.Create(OntologyNamespaces.RdfSyntax));
            graph.NamespaceMap.AddNamespace("rdfs", UriFactory.Create(OntologyNamespaces.RdfSchema));
            graph.NamespaceMap.AddNamespace("owl", UriFactory.Create(OntologyNamespaces.Owl));
            graph.NamespaceMap.AddNamespace("dc", UriFactory.Create(OntologyNamespaces.DublinCoreElements));

            if (ontology.UsesBFO)
            {
                graph.NamespaceMap.AddNamespace("bfo", UriFactory.Create(OntologyNamespaces.BfoPrefix));
            }

            if (ontology.UsesProvO)
            {
                graph.NamespaceMap.AddNamespace("prov", UriFactory.Create(OntologyNamespaces.ProvO));
            }

            // Create base URI for this ontology - use custom namespace if provided
            var baseUri = !string.IsNullOrWhiteSpace(ontology.Namespace)
                ? ontology.Namespace
                : OntologyNamespaces.CreateDefaultNamespace(ontology.Name);

            // Ensure namespace ends with / or #
            baseUri = OntologyNamespaces.NormalizeNamespace(baseUri);

            graph.BaseUri = UriFactory.Create(baseUri);

            // Ontology metadata
            var ontologyNode = graph.CreateUriNode(UriFactory.Create(baseUri));
            var rdfType = graph.CreateUriNode("rdf:type");
            var owlOntology = graph.CreateUriNode("owl:Ontology");
            graph.Assert(ontologyNode, rdfType, owlOntology);

            if (!string.IsNullOrWhiteSpace(ontology.Description))
            {
                var dcDescription = graph.CreateUriNode("dc:description");
                var descriptionLiteral = graph.CreateLiteralNode(ontology.Description);
                graph.Assert(ontologyNode, dcDescription, descriptionLiteral);
            }

            if (!string.IsNullOrWhiteSpace(ontology.Author))
            {
                var dcCreator = graph.CreateUriNode("dc:creator");
                var authorLiteral = graph.CreateLiteralNode(ontology.Author);
                graph.Assert(ontologyNode, dcCreator, authorLiteral);
            }

            if (!string.IsNullOrWhiteSpace(ontology.Version))
            {
                var owlVersionInfo = graph.CreateUriNode("owl:versionInfo");
                var versionLiteral = graph.CreateLiteralNode(ontology.Version);
                graph.Assert(ontologyNode, owlVersionInfo, versionLiteral);
            }

            if (!string.IsNullOrWhiteSpace(ontology.License))
            {
                var dcLicense = graph.CreateUriNode("dc:license");
                var licenseLiteral = graph.CreateLiteralNode(ontology.License);
                graph.Assert(ontologyNode, dcLicense, licenseLiteral);
            }

            if (!string.IsNullOrWhiteSpace(ontology.Tags))
            {
                var dcSubject = graph.CreateUriNode("dc:subject");
                var tagsLiteral = graph.CreateLiteralNode(ontology.Tags);
                graph.Assert(ontologyNode, dcSubject, tagsLiteral);
            }

            var owlClass = graph.CreateUriNode("owl:Class");
            var rdfsLabel = graph.CreateUriNode("rdfs:label");
            var rdfsComment = graph.CreateUriNode("rdfs:comment");
            var rdfsSubClassOf = graph.CreateUriNode("rdfs:subClassOf");

            // Export concepts as OWL classes
            foreach (var concept in ontology.Concepts)
            {
                var conceptUri = CreateConceptUri(baseUri, concept);
                var conceptNode = graph.CreateUriNode(UriFactory.Create(conceptUri));

                // Type declaration
                graph.Assert(conceptNode, rdfType, owlClass);

                // Label
                var labelLiteral = graph.CreateLiteralNode(concept.Name);
                graph.Assert(conceptNode, rdfsLabel, labelLiteral);

                // Definition as comment
                if (!string.IsNullOrWhiteSpace(concept.Definition))
                {
                    var definitionLiteral = graph.CreateLiteralNode(concept.Definition);
                    graph.Assert(conceptNode, rdfsComment, definitionLiteral);
                }

                // Simple explanation
                if (!string.IsNullOrWhiteSpace(concept.SimpleExplanation))
                {
                    var explanationPredicate = graph.CreateUriNode(UriFactory.Create(baseUri + "simpleExplanation"));
                    var explanationLiteral = graph.CreateLiteralNode(concept.SimpleExplanation);
                    graph.Assert(conceptNode, explanationPredicate, explanationLiteral);
                }

                // Examples
                if (!string.IsNullOrWhiteSpace(concept.Examples))
                {
                    var examplesPredicate = graph.CreateUriNode(UriFactory.Create(baseUri + "examples"));
                    var examplesLiteral = graph.CreateLiteralNode(concept.Examples);
                    graph.Assert(conceptNode, examplesPredicate, examplesLiteral);
                }

                // Category
                if (!string.IsNullOrWhiteSpace(concept.Category))
                {
                    var categoryPredicate = graph.CreateUriNode(UriFactory.Create(baseUri + "category"));
                    var categoryLiteral = graph.CreateLiteralNode(concept.Category);
                    graph.Assert(conceptNode, categoryPredicate, categoryLiteral);
                }
            }

            // Export relationships
            foreach (var relationship in ontology.Relationships)
            {
                var sourceUri = CreateConceptUri(baseUri, relationship.SourceConcept);
                var targetUri = CreateConceptUri(baseUri, relationship.TargetConcept);

                var sourceNode = graph.CreateUriNode(UriFactory.Create(sourceUri));
                var targetNode = graph.CreateUriNode(UriFactory.Create(targetUri));

                // Handle is-a relationships as rdfs:subClassOf
                if (relationship.RelationType.ToLower() == "is-a")
                {
                    graph.Assert(sourceNode, rdfsSubClassOf, targetNode);
                }
                else
                {
                    // Create custom property for other relationship types
                    var propertyUri = CreatePropertyUri(baseUri, relationship.RelationType);
                    var propertyNode = graph.CreateUriNode(UriFactory.Create(propertyUri));
                    graph.Assert(sourceNode, propertyNode, targetNode);
                }
            }

            // Export concept restrictions as OWL restrictions
            ExportConceptRestrictions(graph, ontology, baseUri);

            // Export individuals (named instances)
            ExportIndividuals(graph, ontology, baseUri, rdfType);

            return graph;
        }

        private void ExportConceptRestrictions(IGraph graph, Ontology ontology, string baseUri)
        {
            // OWL restriction nodes
            var owlRestriction = graph.CreateUriNode("owl:Restriction");
            var owlOnProperty = graph.CreateUriNode("owl:onProperty");
            var owlAllValuesFrom = graph.CreateUriNode("owl:allValuesFrom");
            var owlSomeValuesFrom = graph.CreateUriNode("owl:someValuesFrom");
            var owlMinCardinality = graph.CreateUriNode("owl:minCardinality");
            var owlMaxCardinality = graph.CreateUriNode("owl:maxCardinality");
            var owlCardinality = graph.CreateUriNode("owl:cardinality");
            var rdfsSubClassOf = graph.CreateUriNode("rdfs:subClassOf");
            var rdfType = graph.CreateUriNode("rdf:type");

            foreach (var concept in ontology.Concepts)
            {
                if (concept.Restrictions == null || !concept.Restrictions.Any())
                    continue;

                var conceptUri = CreateConceptUri(baseUri, concept);
                var conceptNode = graph.CreateUriNode(UriFactory.Create(conceptUri));

                foreach (var restriction in concept.Restrictions)
                {
                    // Create anonymous restriction node
                    var restrictionNode = graph.CreateBlankNode();
                    graph.Assert(restrictionNode, rdfType, owlRestriction);

                    // Property being restricted
                    var propertyUri = baseUri + SanitizePropertyName(restriction.PropertyName);
                    var propertyNode = graph.CreateUriNode(UriFactory.Create(propertyUri));
                    graph.Assert(restrictionNode, owlOnProperty, propertyNode);

                    // Add restriction type-specific triples
                    switch (restriction.RestrictionType)
                    {
                        case "Cardinality":
                            if (restriction.MinCardinality.HasValue && restriction.MaxCardinality.HasValue &&
                                restriction.MinCardinality == restriction.MaxCardinality)
                            {
                                var cardinalityLiteral = graph.CreateLiteralNode(restriction.MinCardinality.Value.ToString(),
                                    UriFactory.Create(OntologyNamespaces.Xsd.NonNegativeInteger));
                                graph.Assert(restrictionNode, owlCardinality, cardinalityLiteral);
                            }
                            else
                            {
                                if (restriction.MinCardinality.HasValue)
                                {
                                    var minLiteral = graph.CreateLiteralNode(restriction.MinCardinality.Value.ToString(),
                                        UriFactory.Create(OntologyNamespaces.Xsd.NonNegativeInteger));
                                    graph.Assert(restrictionNode, owlMinCardinality, minLiteral);
                                }
                                if (restriction.MaxCardinality.HasValue)
                                {
                                    var maxLiteral = graph.CreateLiteralNode(restriction.MaxCardinality.Value.ToString(),
                                        UriFactory.Create(OntologyNamespaces.Xsd.NonNegativeInteger));
                                    graph.Assert(restrictionNode, owlMaxCardinality, maxLiteral);
                                }
                            }
                            break;

                        case "ValueType":
                        case "ConceptType":
                            if (restriction.AllowedConcept != null)
                            {
                                var targetUri = CreateConceptUri(baseUri, restriction.AllowedConcept);
                                var targetNode = graph.CreateUriNode(UriFactory.Create(targetUri));
                                graph.Assert(restrictionNode, owlAllValuesFrom, targetNode);
                            }
                            else if (!string.IsNullOrWhiteSpace(restriction.ValueType))
                            {
                                var dataTypeUri = GetXsdDataType(restriction.ValueType);
                                var dataTypeNode = graph.CreateUriNode(UriFactory.Create(dataTypeUri));
                                graph.Assert(restrictionNode, owlAllValuesFrom, dataTypeNode);
                            }
                            break;

                        case "Required":
                            // Required is expressed as minCardinality 1
                            var minOneLiteral = graph.CreateLiteralNode("1",
                                UriFactory.Create(OntologyNamespaces.Xsd.NonNegativeInteger));
                            graph.Assert(restrictionNode, owlMinCardinality, minOneLiteral);
                            break;
                    }

                    // Link concept to restriction via rdfs:subClassOf
                    graph.Assert(conceptNode, rdfsSubClassOf, restrictionNode);
                }
            }
        }

        private void ExportIndividuals(IGraph graph, Ontology ontology, string baseUri, IUriNode rdfType)
        {
            var owlNamedIndividual = graph.CreateUriNode("owl:NamedIndividual");
            var rdfsLabel = graph.CreateUriNode("rdfs:label");
            var rdfsComment = graph.CreateUriNode("rdfs:comment");

            if (ontology.Individuals == null) return;

            foreach (var individual in ontology.Individuals)
            {
                // Create individual URI
                var individualUri = !string.IsNullOrWhiteSpace(individual.Uri)
                    ? individual.Uri
                    : baseUri + "individual/" + SanitizePropertyName(individual.Name);

                var individualNode = graph.CreateUriNode(UriFactory.Create(individualUri));

                // Type as NamedIndividual
                graph.Assert(individualNode, rdfType, owlNamedIndividual);

                // Type as its concept class
                var conceptUri = CreateConceptUri(baseUri, individual.Concept);
                var conceptNode = graph.CreateUriNode(UriFactory.Create(conceptUri));
                graph.Assert(individualNode, rdfType, conceptNode);

                // Label
                if (!string.IsNullOrWhiteSpace(individual.Label))
                {
                    var labelLiteral = graph.CreateLiteralNode(individual.Label);
                    graph.Assert(individualNode, rdfsLabel, labelLiteral);
                }
                else
                {
                    var nameLiteral = graph.CreateLiteralNode(individual.Name);
                    graph.Assert(individualNode, rdfsLabel, nameLiteral);
                }

                // Description as comment
                if (!string.IsNullOrWhiteSpace(individual.Description))
                {
                    var descriptionLiteral = graph.CreateLiteralNode(individual.Description);
                    graph.Assert(individualNode, rdfsComment, descriptionLiteral);
                }

                // Export individual properties
                if (individual.Properties != null)
                {
                    foreach (var property in individual.Properties)
                    {
                        var propertyUri = baseUri + SanitizePropertyName(property.Name);
                        var propertyNode = graph.CreateUriNode(UriFactory.Create(propertyUri));

                        // Create typed literal based on data type
                        ILiteralNode valueLiteral;
                        if (!string.IsNullOrWhiteSpace(property.DataType))
                        {
                            var xsdType = GetXsdDataType(property.DataType);
                            valueLiteral = graph.CreateLiteralNode(property.Value ?? "",
                                UriFactory.Create(xsdType));
                        }
                        else
                        {
                            valueLiteral = graph.CreateLiteralNode(property.Value ?? "");
                        }

                        graph.Assert(individualNode, propertyNode, valueLiteral);
                    }
                }

                // Export individual relationships
                if (individual.RelationshipsAsSource != null)
                {
                    foreach (var relationship in individual.RelationshipsAsSource)
                    {
                        var targetIndividual = ontology.Individuals.FirstOrDefault(i => i.Id == relationship.TargetIndividualId);
                        if (targetIndividual == null) continue;

                        var targetUri = !string.IsNullOrWhiteSpace(targetIndividual.Uri)
                            ? targetIndividual.Uri
                            : baseUri + "individual/" + SanitizePropertyName(targetIndividual.Name);

                        var targetNode = graph.CreateUriNode(UriFactory.Create(targetUri));
                        var relationPropertyUri = baseUri + SanitizePropertyName(relationship.RelationType);
                        var relationPropertyNode = graph.CreateUriNode(UriFactory.Create(relationPropertyUri));

                        graph.Assert(individualNode, relationPropertyNode, targetNode);
                    }
                }
            }
        }

        private string SanitizePropertyName(string name)
        {
            return name.Replace(" ", "_")
                       .Replace("-", "_")
                       .Replace("(", "")
                       .Replace(")", "")
                       .Replace(",", "");
        }

        private string GetXsdDataType(string dataType)
        {
            return dataType.ToLower() switch
            {
                "integer" or "int" => OntologyNamespaces.Xsd.Integer,
                "decimal" or "number" => OntologyNamespaces.Xsd.Decimal,
                "boolean" or "bool" => OntologyNamespaces.Xsd.Boolean,
                "date" => OntologyNamespaces.Xsd.Date,
                "datetime" => OntologyNamespaces.Xsd.DateTime,
                "uri" or "url" => OntologyNamespaces.Xsd.AnyUri,
                _ => OntologyNamespaces.Xsd.String
            };
        }

        private string CreateConceptUri(string baseUri, Concept concept)
        {
            // Check if this is a PROV-O concept
            if (concept.Name.StartsWith("prov:"))
            {
                var provName = concept.Name.Substring(5); // Remove "prov:" prefix
                return OntologyNamespaces.CreateProvOUri(provName);
            }

            // Check if this is a BFO concept (simplified check)
            if (concept.SourceOntology == "BFO" || new[] { "Entity", "Continuant", "Occurrent", "Process", "Temporal Region", "Independent Continuant", "Dependent Continuant" }.Contains(concept.Name))
            {
                var bfoName = concept.Name.Replace(" ", "");
                return OntologyNamespaces.CreateBfoUri(bfoName);
            }

            // Regular concept
            var localName = concept.Name.Replace(" ", "_").Replace("-", "_");
            return baseUri + localName;
        }

        private string CreatePropertyUri(string baseUri, string relationType)
        {
            // Check for PROV-O relationship types
            if (new[] { "wasGeneratedBy", "used", "wasAssociatedWith", "wasAttributedTo", "wasDerivedFrom" }.Contains(relationType))
            {
                return OntologyNamespaces.CreateProvOUri(relationType);
            }

            // Check for common OWL/RDFS properties
            if (relationType.ToLower() == "depends-on")
            {
                return baseUri + "dependsOn";
            }

            var localName = relationType.Replace(" ", "_").Replace("-", "_");
            return baseUri + localName;
        }
    }
}
