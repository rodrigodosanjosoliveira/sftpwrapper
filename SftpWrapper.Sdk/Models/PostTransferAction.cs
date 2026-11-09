namespace SftpWrapper.Sdk.Models
{
    /// <summary>
    /// Specifies the action to be taken on the source file after a successful transfer operation.
    /// </summary>
    public enum PostTransferAction
    {
        /// <summary>
        /// Keeps the source file intact (default, safe behavior).
        /// </summary>
        KeepSource = 0,

        /// <summary>
        /// Deletes the source file upon successful transfer verification.
        /// </summary>
        DeleteSource = 1
    }
}
