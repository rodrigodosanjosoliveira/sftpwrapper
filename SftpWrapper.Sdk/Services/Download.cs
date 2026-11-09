using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using Renci.SshNet;
using Renci.SshNet.Common;
using SftpWrapper.Sdk.Models;

namespace SftpWrapper.Sdk.Services
{
    /// <summary>
    /// Service responsible for downloading files from an SFTP server.
    /// </summary>
    [Obsolete("Utilize ISftpService e SftpService para operações thread-safe e assíncronas.")]
    public class Download : IDisposable
    {
        private readonly SftpClient _client;
        private bool _disposed;

        /// <summary>
        /// Single file descriptor for download operations.
        /// </summary>
        public SftpFileInfo? File { get; set; }

        /// <summary>
        /// List of files for batch download operations.
        /// </summary>
        public List<SftpFileInfo> Files { get; set; } = new List<SftpFileInfo>();

        /// <summary>
        /// Gets or sets whether the remote file should be deleted after a successful download.
        /// Defaults to <c>false</c> to prevent accidental data loss on the remote server.
        /// </summary>
        public bool DeleteRemoteSourceAfterDownload { get; set; }

        /// <summary>
        /// Gets or sets the buffer size used for SFTP operations (default: 64 KB).
        /// </summary>
        public uint BufferSize { get; set; } = 64 * 1024;

        #region Public Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Download"/> class.
        /// </summary>
        /// <param name="info">Connection configuration.</param>
        public Download(Models.ConnectionInfo info)
        {
            if (info == null) throw new ArgumentNullException(nameof(info));
            _client = new SftpClient(info.Host, info.Port, info.UserName, info.Password);
            _client.Connect();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Download"/> class for a single file.
        /// </summary>
        /// <param name="info">Connection configuration.</param>
        /// <param name="sourcePath">Remote source file or directory path.</param>
        /// <param name="destinationPath">Local destination directory or file path.</param>
        public Download(Models.ConnectionInfo info, string sourcePath, string destinationPath)
            : this(info, sourcePath, destinationPath, deleteRemoteSourceAfterDownload: false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Download"/> class for a single file with deletion option.
        /// </summary>
        /// <param name="info">Connection configuration.</param>
        /// <param name="sourcePath">Remote source file or directory path.</param>
        /// <param name="destinationPath">Local destination directory or file path.</param>
        /// <param name="deleteRemoteSourceAfterDownload">Whether to delete the remote file after successful download.</param>
        public Download(Models.ConnectionInfo info, string sourcePath, string destinationPath, bool deleteRemoteSourceAfterDownload)
        {
            if (info == null) throw new ArgumentNullException(nameof(info));
            DeleteRemoteSourceAfterDownload = deleteRemoteSourceAfterDownload;
            _client = new SftpClient(info.Host, info.Port, info.UserName, info.Password);
            _client.Connect();

            File = new SftpFileInfo(ValidPath(sourcePath, ref destinationPath), destinationPath);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Download"/> class for batch downloading by extensions.
        /// </summary>
        /// <param name="info">Connection configuration.</param>
        /// <param name="sourcePath">Remote source directory path.</param>
        /// <param name="destinationPath">Local destination directory path.</param>
        /// <param name="validExtensions">File extensions to filter (e.g. .txt, .ret).</param>
        public Download(Models.ConnectionInfo info, string sourcePath, string destinationPath, IEnumerable<string> validExtensions)
            : this(info, sourcePath, destinationPath, validExtensions, deleteRemoteSourceAfterDownload: false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Download"/> class for batch downloading by extensions with deletion option.
        /// </summary>
        /// <param name="info">Connection configuration.</param>
        /// <param name="sourcePath">Remote source directory path.</param>
        /// <param name="destinationPath">Local destination directory path.</param>
        /// <param name="validExtensions">File extensions to filter (e.g. .txt, .ret).</param>
        /// <param name="deleteRemoteSourceAfterDownload">Whether to delete remote files after successful download.</param>
        public Download(Models.ConnectionInfo info, string sourcePath, string destinationPath, IEnumerable<string> validExtensions, bool deleteRemoteSourceAfterDownload)
        {
            if (info == null) throw new ArgumentNullException(nameof(info));
            if (validExtensions == null) throw new ArgumentNullException(nameof(validExtensions));

            DeleteRemoteSourceAfterDownload = deleteRemoteSourceAfterDownload;
            Files = new List<SftpFileInfo>();
            _client = new SftpClient(info.Host, info.Port, info.UserName, info.Password);
            _client.Connect();

            var allFiles = _client.ListDirectory(sourcePath)
                .OrderByDescending(f => f.LastWriteTime)
                .ToList();

            var filePaths = new List<string>();

            foreach (var extension in validExtensions)
            {
                filePaths.AddRange(allFiles
                    .Where(f => f.Name.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
                    .Select(f => f.FullName));
            }

            foreach (var filePath in filePaths)
            {
                var fileName = Path.GetFileName(filePath) ?? throw new InvalidOperationException($"Invalid file name: {filePath}");
                Files.Add(new SftpFileInfo(filePath, Path.Combine(destinationPath, fileName)));
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Downloads the single configured file from the SFTP server.
        /// </summary>
        public void DownloadFromSftp()
        {
            if (File == null)
                throw new InvalidOperationException("File information is not configured.");

            try
            {
                _client.BufferSize = BufferSize;
                using (var fs = System.IO.File.OpenWrite(File.DestinationPath))
                {
                    _client.DownloadFile(File.SourcePath, fs);
                }

                if (DownloadSuccess(File.DestinationPath))
                {
                    if (DeleteRemoteSourceAfterDownload)
                    {
                        DeleteSourceFolder(File);
                    }
                }
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (SocketException)
            {
                throw;
            }
            catch (SshConnectionException)
            {
                throw;
            }
            catch (SshAuthenticationException)
            {
                throw;
            }
        }

        /// <summary>
        /// Downloads all files configured in the Files collection.
        /// </summary>
        public void DownloadManyFromSftp()
        {
            try
            {
                if (!Files.Any())
                    throw new SftpPathNotFoundException("No files found to download.");

                _client.BufferSize = BufferSize;

                foreach (var file in Files)
                {
                    using (var fs = System.IO.File.OpenWrite(file.DestinationPath))
                    {
                        _client.DownloadFile(file.SourcePath, fs);
                    }

                    if (DownloadSuccess(file.DestinationPath))
                    {
                        if (DeleteRemoteSourceAfterDownload)
                        {
                            DeleteSourceFolder(file);
                        }
                    }
                    else
                    {
                        if (System.IO.File.Exists(file.DestinationPath))
                        {
                            System.IO.File.Delete(file.DestinationPath);
                        }
                    }
                }
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (SocketException)
            {
                throw;
            }
            catch (SshConnectionException)
            {
                throw;
            }
            catch (SshAuthenticationException)
            {
                throw;
            }
            catch (ApplicationException)
            {
                throw;
            }
        }

        /// <summary>
        /// Retrieves the name of the first non-hidden file in the target directory.
        /// </summary>
        public string? GetFileName()
        {
            if (File == null)
                throw new InvalidOperationException("File information is not configured.");

            try
            {
                var directory = Path.GetDirectoryName(File.SourcePath)?.Replace("\\", "/") ?? "/";
                if (!directory.EndsWith("/")) directory += "/";

                var files = _client.ListDirectory(directory);
                var sftpFiles = files.ToList();
                var first = sftpFiles.FirstOrDefault(sf => !sf.Name.StartsWith("."));

                return first?.Name;
            }
            catch (ArgumentNullException)
            {
                throw;
            }
            catch (SshConnectionException)
            {
                throw;
            }
            catch (SftpPermissionDeniedException)
            {
                throw;
            }
            catch (SshException)
            {
                throw;
            }
        }

        /// <summary>
        /// Checks whether the downloaded local file exists and is not empty.
        /// </summary>
        /// <param name="path">Local file path.</param>
        /// <returns>True if file exists and has size > 0.</returns>
        public bool DownloadSuccess(string path) => System.IO.File.Exists(path) && new FileInfo(path).Length > 0;

        #endregion

        #region Private Methods

        private string ValidPath(string sourcePath, ref string destinationPath)
        {
            var files = _client.ListDirectory(sourcePath).Where(f => !f.Name.StartsWith(".")).ToList();

            if (!files.Any()) throw new SftpPathNotFoundException("File not found.");

            var firstFile = files.First();
            var combinedPath = sourcePath.EndsWith("/") 
                ? $"{sourcePath}{firstFile.Name}" 
                : $"{sourcePath}/{firstFile.Name}";

            if (!_client.Exists(combinedPath))
                throw new SftpPathNotFoundException("File not found.");

            destinationPath = Path.Combine(destinationPath, firstFile.Name);

            return combinedPath;
        }

        private void DeleteSourceFolder(SftpFileInfo file)
        {
            if (!_client.Exists(file.SourcePath)) return;

            try
            {
                _client.Delete(file.SourcePath);
            }
            catch (ArgumentNullException)
            {
                throw;
            }
            catch (SshConnectionException)
            {
                throw;
            }
            catch (SftpPathNotFoundException)
            {
                throw;
            }
            catch (ObjectDisposedException)
            {
                throw;
            }
        }

        private void Disconnect()
        {
            if (_client != null && _client.IsConnected)
                _client.Disconnect();
        }

        #endregion

        #region IDisposable Members

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                Disconnect();
                _client?.Dispose();
            }

            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~Download()
        {
            Dispose(false);
        }

        #endregion
    }
}
