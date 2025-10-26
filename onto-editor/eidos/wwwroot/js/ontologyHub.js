// SignalR client for real-time collaborative ontology editing
window.ontologyHub = {
    connection: null,
    dotNetHelper: null,

    /**
     * Initialize SignalR connection to the OntologyHub
     * @param {any} dotNetReference - Reference to the Blazor component
     * @param {number} ontologyId - ID of the ontology to join
     */
    async init(dotNetReference, ontologyId) {
        try {
            this.dotNetHelper = dotNetReference;

            // Create SignalR connection
            this.connection = new signalR.HubConnectionBuilder()
                .withUrl("/ontologyhub")
                .withAutomaticReconnect()
                .build();

            // Subscribe to ConceptChanged events
            this.connection.on("ConceptChanged", (changeEvent) => {
                this.dotNetHelper.invokeMethodAsync('HandleConceptChanged', changeEvent);
            });

            // Subscribe to RelationshipChanged events
            this.connection.on("RelationshipChanged", (changeEvent) => {
                this.dotNetHelper.invokeMethodAsync('HandleRelationshipChanged', changeEvent);
            });

            // Subscribe to UserJoined events
            this.connection.on("UserJoined", (connectionId) => {
                // User joined - could be used for presence indicators
            });

            // Subscribe to UserLeft events
            this.connection.on("UserLeft", (connectionId) => {
                // User left - could be used for presence indicators
            });

            // Start the connection
            await this.connection.start();

            // Join the ontology group
            try {
                await this.connection.invoke("JoinOntology", ontologyId);
            } catch (joinError) {
                console.error("Error joining ontology group:", joinError);
                console.error("Join error details:", {
                    message: joinError.message,
                    stack: joinError.stack,
                    ontologyId: ontologyId
                });
                // Don't retry if it's a permission error
                if (joinError.message && joinError.message.includes("permission")) {
                    throw joinError; // Re-throw to stop retrying
                }
            }

        } catch (err) {
            console.error("Error initializing SignalR:", err);
            console.error("Full error details:", {
                message: err.message,
                stack: err.stack,
                name: err.name
            });
            // Only retry if it's not a permission error
            if (!err.message || !err.message.includes("permission")) {
                setTimeout(() => this.init(dotNetReference, ontologyId), 5000);
            }
        }
    },

    /**
     * Disconnect from SignalR hub
     * @param {number} ontologyId - ID of the ontology to leave
     */
    async disconnect(ontologyId) {
        if (this.connection) {
            try {
                await this.connection.invoke("LeaveOntology", ontologyId);
                await this.connection.stop();
            } catch (err) {
                console.error("Error disconnecting from SignalR:", err);
            }
        }
    }
};
