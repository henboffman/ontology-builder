namespace Eidos.Models
{
    /// <summary>
    /// Status of auto-save operation for a note
    /// </summary>
    public enum AutoSaveStatus
    {
        /// <summary>
        /// No save operation in progress or pending
        /// </summary>
        Idle,

        /// <summary>
        /// Save operation currently in progress
        /// </summary>
        Saving,

        /// <summary>
        /// Save operation completed successfully
        /// </summary>
        Saved,

        /// <summary>
        /// Save operation failed with error
        /// </summary>
        Error
    }
}
