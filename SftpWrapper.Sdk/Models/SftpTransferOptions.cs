namespace SftpWrapper.Sdk.Models
{
    /// <summary>
    /// Configures behavior for upload and download operations.
    /// </summary>
    public class SftpTransferOptions
    {
        /// <summary>
        /// Action to take on the source file after a successful transfer (default: KeepSource).
        /// </summary>
        public PostTransferAction PostTransferAction { get; set; } = PostTransferAction.KeepSource;

        /// <summary>
        /// Indicates whether existing destination files should be overwritten. Default is true.
        /// </summary>
        public bool Overwrite { get; set; } = true;

        /// <summary>
        /// Optional custom buffer size override in bytes. If null, the client default buffer size is used.
        /// </summary>
        public uint? BufferSize { get; set; }
    }
}
