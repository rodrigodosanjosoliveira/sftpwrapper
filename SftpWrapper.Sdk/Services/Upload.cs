using System;
using System.IO;
using System.Net.Sockets;
using Renci.SshNet;
using Renci.SshNet.Common;
using SftpWrapper.Sdk.Models;

namespace SftpWrapper.Sdk.Services
{
    /// <summary>
    /// Service responsible for uploading files to an SFTP server.
    /// </summary>
    [Obsolete("Utilize ISftpService e SftpService para operações thread-safe e assíncronas.")]
    public class Upload : IDisposable
    {
        private readonly SftpClient _client;
        private bool _disposed;

        /// <summary>
        /// Indicates whether the upload succeeded and the file exists on the destination server.
        /// </summary>
        public bool UploadSuccess { get; set; }

        /// <summary>
        /// Details of the source and destination paths for the file.
        /// </summary>
        protected SftpFileInfo File { get; set; }

        /// <summary>
        /// Gets or sets whether the local source file should be deleted after a successful upload.
        /// Default is <c>false</c> to prevent accidental data loss.
        /// </summary>
        public bool DeleteSourceAfterUpload { get; set; }

        /// <summary>
        /// Gets or sets the buffer size used for SFTP operations (default: 64 KB).
        /// </summary>
        public uint BufferSize { get; set; } = 64 * 1024;

        /// <summary>
        /// Initializes a new instance of the <see cref="Upload"/> class.
        /// </summary>
        /// <param name="info">Connection configuration.</param>
        /// <param name="sourcePath">Local source file path.</param>
        /// <param name="destinationPath">Remote destination directory path.</param>
        public Upload(Models.ConnectionInfo info, string sourcePath, string destinationPath)
            : this(info, sourcePath, destinationPath, deleteSourceAfterUpload: false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Upload"/> class with explicit deletion policy.
        /// </summary>
        /// <param name="info">Connection configuration.</param>
        /// <param name="sourcePath">Local source file path.</param>
        /// <param name="destinationPath">Remote destination directory path.</param>
        /// <param name="deleteSourceAfterUpload">Whether to delete the local file upon successful upload.</param>
        public Upload(Models.ConnectionInfo info, string sourcePath, string destinationPath, bool deleteSourceAfterUpload)
        {
            if (info == null) throw new ArgumentNullException(nameof(info));
            _client = new SftpClient(info.Host, info.Port, info.UserName, info.Password);
            File = new SftpFileInfo(Valid(sourcePath), destinationPath);
            DeleteSourceAfterUpload = deleteSourceAfterUpload;
        }

        /// <summary>
        /// Executes the upload to the SFTP server.
        /// </summary>
        public void UploadToSftp()
        {
            try
            {
                Connect();
                _client.ChangeDirectory(File.DestinationPath);
                using (var fs = new FileStream(File.SourcePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    _client.BufferSize = BufferSize;
                    _client.UploadFile(fs, File.Name);
                }

                var remotePath = File.DestinationPath.EndsWith("/") 
                    ? $"{File.DestinationPath}{File.Name}" 
                    : $"{File.DestinationPath}/{File.Name}";

                UploadSuccess = _client.Exists(remotePath);

                if (DeleteSourceAfterUpload)
                {
                    DeleteSourceFolder();
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

        #region Private Methods

        private void Connect()
        {
            if (!_client.IsConnected)
            {
                _client.Connect();
            }
        }

        private void Disconnect()
        {
            if (_client.IsConnected)
            {
                _client.Disconnect();
            }
        }

        private static string Valid(string sourcePath)
        {
            if (System.IO.File.Exists(sourcePath))
                return sourcePath;

            throw new ArgumentException("Invalid path or file not found.");
        }

        private void DeleteSourceFolder()
        {
            if (!UploadSuccess) return;
            if (System.IO.File.Exists(File.SourcePath))
                System.IO.File.Delete(File.SourcePath);
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

        ~Upload()
        {
            Dispose(false);
        }

        #endregion
    }
}
