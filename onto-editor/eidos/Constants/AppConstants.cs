namespace Eidos.Constants
{
    /// <summary>
    /// Application-wide constants and configuration values.
    /// Centralizes magic numbers to improve maintainability and configurability.
    /// </summary>
    public static class AppConstants
    {
        #region Toast Notifications

        /// <summary>
        /// Toast notification display durations
        /// </summary>
        public static class Toast
        {
            /// <summary>
            /// Duration for success toast messages (in milliseconds)
            /// </summary>
            public const int SuccessDuration = 3000; // 3 seconds

            /// <summary>
            /// Duration for error toast messages (in milliseconds)
            /// </summary>
            public const int ErrorDuration = 5000; // 5 seconds

            /// <summary>
            /// Duration for warning toast messages (in milliseconds)
            /// </summary>
            public const int WarningDuration = 4000; // 4 seconds

            /// <summary>
            /// Duration for informational toast messages (in milliseconds)
            /// </summary>
            public const int InfoDuration = 3000; // 3 seconds
        }

        #endregion

        #region Pagination

        /// <summary>
        /// Pagination and data loading constants
        /// </summary>
        public static class Pagination
        {
            /// <summary>
            /// Default number of items to skip (starting page)
            /// </summary>
            public const int DefaultSkip = 0;

            /// <summary>
            /// Default page size for lists and queries
            /// </summary>
            public const int DefaultPageSize = 50;

            /// <summary>
            /// Maximum page size to prevent performance issues
            /// </summary>
            public const int MaxPageSize = 100;
        }

        #endregion

        #region Security

        /// <summary>
        /// Security-related constants
        /// </summary>
        public static class Security
        {
            /// <summary>
            /// Token length in bytes for share links and session tokens (256 bits)
            /// </summary>
            public const int TokenLengthBytes = 32;

            /// <summary>
            /// Maximum login attempts before account lockout
            /// </summary>
            public const int MaxLoginAttempts = 5;

            /// <summary>
            /// Account lockout duration in minutes
            /// </summary>
            public const int LockoutDurationMinutes = 15;

            /// <summary>
            /// Minimum password length
            /// </summary>
            public const int MinPasswordLength = 8;
        }

        #endregion

        #region History and Undo

        /// <summary>
        /// Undo/Redo and history tracking constants
        /// </summary>
        public static class History
        {
            /// <summary>
            /// Maximum number of undo/redo operations to keep in memory
            /// </summary>
            public const int MaxHistorySize = 50;

            /// <summary>
            /// Maximum number of activity records to display in timeline
            /// </summary>
            public const int MaxActivityDisplayCount = 100;
        }

        #endregion

        #region Timeouts

        /// <summary>
        /// Timeout values for various operations (in seconds)
        /// </summary>
        public static class Timeouts
        {
            /// <summary>
            /// Database connection timeout
            /// </summary>
            public const int DatabaseConnectionSeconds = 30;

            /// <summary>
            /// HTTP client timeout for external API calls
            /// </summary>
            public const int HttpClientSeconds = 30;

            /// <summary>
            /// SignalR connection timeout
            /// </summary>
            public const int SignalRConnectionSeconds = 20;

            /// <summary>
            /// Cache sliding expiration for user preferences (in minutes)
            /// </summary>
            public const int UserPreferencesCacheMinutes = 5;

            /// <summary>
            /// Rate limiting window (in seconds)
            /// </summary>
            public const int RateLimitWindowSeconds = 10;

            /// <summary>
            /// Rate limiting replenishment period (in seconds)
            /// </summary>
            public const int RateLimitReplenishmentSeconds = 3;
        }

        #endregion

        #region Rate Limiting

        /// <summary>
        /// Rate limiting configuration
        /// </summary>
        public static class RateLimiting
        {
            /// <summary>
            /// Maximum number of requests per window
            /// </summary>
            public const int PermitLimit = 100;

            /// <summary>
            /// Number of queued requests allowed
            /// </summary>
            public const int QueueLimit = 10;

            /// <summary>
            /// API-specific rate limit
            /// </summary>
            public const int ApiPermitLimit = 60;

            /// <summary>
            /// API queue limit
            /// </summary>
            public const int ApiQueueLimit = 5;
        }

        #endregion

        #region Import/Export

        /// <summary>
        /// Import and export operation constants
        /// </summary>
        public static class ImportExport
        {
            /// <summary>
            /// Progress reporting interval for large operations (percentage points)
            /// </summary>
            public const int ProgressReportingInterval = 100;

            /// <summary>
            /// Batch size for bulk insert operations
            /// </summary>
            public const int BulkInsertBatchSize = 100;

            /// <summary>
            /// Maximum file size for uploads (in MB)
            /// </summary>
            public const int MaxFileUploadSizeMB = 10;
        }

        #endregion

        #region UI and Display

        /// <summary>
        /// User interface display constants
        /// </summary>
        public static class Display
        {
            /// <summary>
            /// Number of recent ontologies to show in navigation menu
            /// </summary>
            public const int RecentOntologiesCount = 10;

            /// <summary>
            /// Default text size scale
            /// </summary>
            public const double DefaultTextSizeScale = 1.0;

            /// <summary>
            /// Minimum text size scale
            /// </summary>
            public const double MinTextSizeScale = 0.5;

            /// <summary>
            /// Maximum text size scale
            /// </summary>
            public const double MaxTextSizeScale = 2.0;

            /// <summary>
            /// Auto-hide message duration (in milliseconds)
            /// </summary>
            public const int AutoHideMessageDuration = 3000;
        }

        #endregion

        #region Caching

        /// <summary>
        /// Caching configuration constants
        /// </summary>
        public static class Cache
        {
            /// <summary>
            /// Sliding expiration for user preferences cache (in minutes)
            /// </summary>
            public const int UserPreferencesExpirationMinutes = 5;

            /// <summary>
            /// Absolute expiration for ontology metadata cache (in minutes)
            /// </summary>
            public const int OntologyMetadataExpirationMinutes = 30;
        }

        #endregion

        #region Database

        /// <summary>
        /// Database operation constants
        /// </summary>
        public static class Database
        {
            /// <summary>
            /// Command timeout for database operations (in seconds)
            /// </summary>
            public const int CommandTimeoutSeconds = 30;

            /// <summary>
            /// Batch size for EF Core SaveChanges operations
            /// </summary>
            public const int SaveChangesBatchSize = 100;
        }

        #endregion

        #region Validation

        /// <summary>
        /// Validation rules and limits
        /// </summary>
        public static class Validation
        {
            /// <summary>
            /// Maximum length for ontology name
            /// </summary>
            public const int MaxOntologyNameLength = 200;

            /// <summary>
            /// Maximum length for concept name
            /// </summary>
            public const int MaxConceptNameLength = 200;

            /// <summary>
            /// Maximum length for description fields
            /// </summary>
            public const int MaxDescriptionLength = 2000;

            /// <summary>
            /// Maximum length for note fields
            /// </summary>
            public const int MaxNoteLength = 1000;

            /// <summary>
            /// Maximum number of properties per concept
            /// </summary>
            public const int MaxPropertiesPerConcept = 100;
        }

        #endregion
    }
}
