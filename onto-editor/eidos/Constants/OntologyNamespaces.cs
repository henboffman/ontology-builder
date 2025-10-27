namespace Eidos.Constants
{
    /// <summary>
    /// Standard RDF, OWL, and ontology namespace URIs.
    /// Centralizes all hardcoded namespace strings to improve maintainability.
    /// </summary>
    public static class OntologyNamespaces
    {
        #region W3C Standards

        /// <summary>
        /// RDF Syntax namespace
        /// </summary>
        public const string RdfSyntax = "http://www.w3.org/1999/02/22-rdf-syntax-ns#";

        /// <summary>
        /// RDF Schema namespace
        /// </summary>
        public const string RdfSchema = "http://www.w3.org/2000/01/rdf-schema#";

        /// <summary>
        /// OWL (Web Ontology Language) namespace
        /// </summary>
        public const string Owl = "http://www.w3.org/2002/07/owl#";

        #endregion

        #region XML Schema Data Types

        /// <summary>
        /// XML Schema data types namespace
        /// </summary>
        public static class Xsd
        {
            private const string Base = "http://www.w3.org/2001/XMLSchema#";

            public const string String = Base + "string";
            public const string Integer = Base + "integer";
            public const string Decimal = Base + "decimal";
            public const string Boolean = Base + "boolean";
            public const string Date = Base + "date";
            public const string DateTime = Base + "dateTime";
            public const string AnyUri = Base + "anyURI";
            public const string NonNegativeInteger = Base + "nonNegativeInteger";
        }

        #endregion

        #region Dublin Core

        /// <summary>
        /// Dublin Core Elements namespace
        /// </summary>
        public const string DublinCoreElements = "http://purl.org/dc/elements/1.1/";

        /// <summary>
        /// Dublin Core Terms namespace
        /// </summary>
        public const string DublinCoreTerms = "http://purl.org/dc/terms/";

        #endregion

        #region Ontology Frameworks

        /// <summary>
        /// Basic Formal Ontology (BFO) namespace prefix
        /// Note: BFO uses individual entity URIs like "http://purl.obolibrary.org/obo/BFO_0000001"
        /// </summary>
        public const string BfoPrefix = "http://purl.obolibrary.org/obo/BFO_";

        /// <summary>
        /// Basic Formal Ontology (BFO) base namespace
        /// </summary>
        public const string BfoBase = "http://purl.obolibrary.org/obo/bfo/";

        /// <summary>
        /// Relation Ontology (RO) base namespace
        /// </summary>
        public const string RelationOntology = "http://purl.obolibrary.org/obo/ro/";

        #endregion

        #region PROV-O (Provenance Ontology)

        /// <summary>
        /// PROV-O (Provenance Ontology) namespace
        /// </summary>
        public const string ProvO = "http://www.w3.org/ns/prov#";

        #endregion

        #region SKOS (Simple Knowledge Organization System)

        /// <summary>
        /// SKOS (Simple Knowledge Organization System) namespace
        /// </summary>
        public const string Skos = "http://www.w3.org/2004/02/skos/core#";

        #endregion

        #region Default/Example Namespaces

        /// <summary>
        /// Default example.org base URI for ontologies without a custom namespace
        /// </summary>
        public const string DefaultBaseUri = "http://example.org/ontology/";

        /// <summary>
        /// Creates a default namespace URI for an ontology based on its name
        /// </summary>
        /// <param name="ontologyName">The name of the ontology</param>
        /// <returns>A URI in the format "http://example.org/ontology/{name}/"</returns>
        public static string CreateDefaultNamespace(string ontologyName)
        {
            if (string.IsNullOrWhiteSpace(ontologyName))
                return DefaultBaseUri;

            var normalizedName = ontologyName.ToLower().Replace(" ", "_");
            return $"{DefaultBaseUri}{normalizedName}/";
        }

        /// <summary>
        /// Ensures a namespace URI ends with '/' or '#' as required by RDF standards
        /// </summary>
        /// <param name="namespaceUri">The namespace URI to normalize</param>
        /// <returns>A properly terminated namespace URI</returns>
        public static string NormalizeNamespace(string namespaceUri)
        {
            if (string.IsNullOrWhiteSpace(namespaceUri))
                return string.Empty;

            if (!namespaceUri.EndsWith("/") && !namespaceUri.EndsWith("#"))
                return namespaceUri + "/";

            return namespaceUri;
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Creates a BFO entity URI from a BFO identifier
        /// </summary>
        /// <param name="bfoId">BFO identifier (e.g., "0000001" or "Entity")</param>
        /// <returns>Full BFO URI (e.g., "http://purl.obolibrary.org/obo/BFO_0000001")</returns>
        public static string CreateBfoUri(string bfoId)
        {
            if (string.IsNullOrWhiteSpace(bfoId))
                return BfoPrefix;

            return $"{BfoPrefix}{bfoId}";
        }

        /// <summary>
        /// Creates a PROV-O property URI
        /// </summary>
        /// <param name="property">Property name (e.g., "wasGeneratedBy", "wasDerivedFrom")</param>
        /// <returns>Full PROV-O URI (e.g., "http://www.w3.org/ns/prov#wasGeneratedBy")</returns>
        public static string CreateProvOUri(string property)
        {
            if (string.IsNullOrWhiteSpace(property))
                return ProvO;

            return $"{ProvO}{property}";
        }

        #endregion
    }
}
