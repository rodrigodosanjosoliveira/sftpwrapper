using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SftpWrapper.Sdk.Models;

namespace SftpWrapper.Sdk.Abstractions
{
    /// <summary>
    /// Defines an asynchronous contract for SFTP file and directory operations.
    /// </summary>
    public interface ISftpService : IDisposable
#if NET8_0_OR_GREATER
        , IAsyncDisposable
#endif
    {
        /// <summary>
        /// Asynchronously uploads a local file to the remote SFTP server.
        /// </summary>
        Task UploadFileAsync(string localFilePath, string remoteDestinationPath, SftpTransferOptions? options = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously uploads a stream to a remote file path on the SFTP server.
        /// </summary>
        Task UploadStreamAsync(Stream sourceStream, string remoteDestinationPath, bool overwrite = true, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously downloads a remote SFTP file to a local destination file path.
        /// </summary>
        Task DownloadFileAsync(string remoteFilePath, string localDestinationPath, SftpTransferOptions? options = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously downloads a remote SFTP file into a destination stream.
        /// </summary>
        Task DownloadStreamAsync(string remoteFilePath, Stream destinationStream, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously lists files in a remote directory, optionally filtering by extension or glob.
        /// </summary>
        Task<IReadOnlyList<SftpItem>> ListFilesAsync(string remoteDirectoryPath, string? extensionFilter = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously checks if a file or directory exists on the remote SFTP server.
        /// </summary>
        Task<bool> ExistsAsync(string remotePath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously deletes a file on the remote SFTP server.
        /// </summary>
        Task DeleteFileAsync(string remoteFilePath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously creates a directory on the remote SFTP server if it does not already exist.
        /// </summary>
        Task CreateDirectoryAsync(string remoteDirectoryPath, CancellationToken cancellationToken = default);
    }
}
