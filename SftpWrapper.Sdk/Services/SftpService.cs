using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Renci.SshNet;
using Renci.SshNet.Common;
using SftpWrapper.Sdk.Abstractions;
using SftpWrapper.Sdk.Models;

namespace SftpWrapper.Sdk.Services
{
    /// <summary>
    /// Thread-safe, resilient and asynchronous implementation of <see cref="ISftpService"/>.
    /// </summary>
    public class SftpService : ISftpService
    {
        private readonly SftpOptions _options;
        private readonly ILogger<SftpService> _logger;
        private readonly SemaphoreSlim _connectionLock = new SemaphoreSlim(1, 1);
        private SftpClient? _client;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="SftpService"/> class with strongly-typed options and logger.
        /// </summary>
        public SftpService(IOptions<SftpOptions> options, ILogger<SftpService>? logger = null)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            _options = options.Value ?? throw new ArgumentException("Options value cannot be null.", nameof(options));
            _logger = logger ?? NullLogger<SftpService>.Instance;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SftpService"/> class with direct options.
        /// </summary>
        public SftpService(SftpOptions options, ILogger<SftpService>? logger = null)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? NullLogger<SftpService>.Instance;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SftpService"/> class using legacy <see cref="Models.ConnectionInfo"/>.
        /// </summary>
        public SftpService(Models.ConnectionInfo info, ILogger<SftpService>? logger = null)
        {
            if (info == null) throw new ArgumentNullException(nameof(info));
            _options = new SftpOptions(info.Host, info.UserName, info.Password, info.Port);
            _logger = logger ?? NullLogger<SftpService>.Instance;
        }

        private async Task<SftpClient> GetConnectedClientAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (_client != null && _client.IsConnected)
            {
                return _client;
            }

            await _connectionLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (_client != null && _client.IsConnected)
                {
                    return _client;
                }

                _client?.Dispose();
                _client = CreateClient();

                _logger.LogInformation("Connecting to SFTP server {Host}:{Port} as user '{UserName}'", _options.Host, _options.Port, _options.UserName);
                await _client.ConnectAsync(cancellationToken).ConfigureAwait(false);
                _logger.LogInformation("Successfully connected to SFTP server {Host}:{Port}", _options.Host, _options.Port);

                return _client;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to SFTP server {Host}:{Port}", _options.Host, _options.Port);
                throw;
            }
            finally
            {
                _connectionLock.Release();
            }
        }

        private SftpClient CreateClient()
        {
            var authMethods = new List<AuthenticationMethod>();

            if (!string.IsNullOrEmpty(_options.Password))
            {
                authMethods.Add(new PasswordAuthenticationMethod(_options.UserName, _options.Password));
            }

            if (!string.IsNullOrEmpty(_options.PrivateKeyFilePath))
            {
                var keyFile = string.IsNullOrEmpty(_options.PrivateKeyPassphrase)
                    ? new PrivateKeyFile(_options.PrivateKeyFilePath!)
                    : new PrivateKeyFile(_options.PrivateKeyFilePath!, _options.PrivateKeyPassphrase);

                authMethods.Add(new PrivateKeyAuthenticationMethod(_options.UserName, keyFile));
            }
            else if (!string.IsNullOrEmpty(_options.PrivateKeyContent))
            {
                using var keyStream = new MemoryStream(Encoding.UTF8.GetBytes(_options.PrivateKeyContent!));
                var keyFile = string.IsNullOrEmpty(_options.PrivateKeyPassphrase)
                    ? new PrivateKeyFile(keyStream)
                    : new PrivateKeyFile(keyStream, _options.PrivateKeyPassphrase);

                authMethods.Add(new PrivateKeyAuthenticationMethod(_options.UserName, keyFile));
            }

            if (authMethods.Count == 0)
            {
                throw new InvalidOperationException("No authentication method configured. Provide a password or private key.");
            }

            var connectionInfo = new Renci.SshNet.ConnectionInfo(
                _options.Host,
                _options.Port,
                _options.UserName,
                authMethods.ToArray())
            {
                Timeout = _options.ConnectionTimeout
            };

            return new SftpClient(connectionInfo)
            {
                BufferSize = _options.BufferSize,
                OperationTimeout = _options.ConnectionTimeout
            };
        }

        private async Task ExecuteWithRetryAsync(Func<SftpClient, Task> operation, CancellationToken cancellationToken)
        {
            var attempts = 0;
            var maxAttempts = Math.Max(1, _options.MaxRetryAttempts);

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                attempts++;

                try
                {
                    var client = await GetConnectedClientAsync(cancellationToken).ConfigureAwait(false);
                    await operation(client).ConfigureAwait(false);
                    return;
                }
                catch (Exception ex) when (IsTransientException(ex) && attempts < maxAttempts)
                {
                    var delay = TimeSpan.FromMilliseconds(_options.RetryDelay.TotalMilliseconds * Math.Pow(2, attempts - 1));
                    _logger.LogWarning(ex, "Transient error during SFTP operation (attempt {Attempt}/{MaxAttempts}). Retrying in {Delay}ms...",
                        attempts, maxAttempts, delay.TotalMilliseconds);

                    await ResetConnectionAsync().ConfigureAwait(false);
                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        private async Task<T> ExecuteWithRetryAsync<T>(Func<SftpClient, Task<T>> operation, CancellationToken cancellationToken)
        {
            var attempts = 0;
            var maxAttempts = Math.Max(1, _options.MaxRetryAttempts);

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                attempts++;

                try
                {
                    var client = await GetConnectedClientAsync(cancellationToken).ConfigureAwait(false);
                    return await operation(client).ConfigureAwait(false);
                }
                catch (Exception ex) when (IsTransientException(ex) && attempts < maxAttempts)
                {
                    var delay = TimeSpan.FromMilliseconds(_options.RetryDelay.TotalMilliseconds * Math.Pow(2, attempts - 1));
                    _logger.LogWarning(ex, "Transient error during SFTP operation (attempt {Attempt}/{MaxAttempts}). Retrying in {Delay}ms...",
                        attempts, maxAttempts, delay.TotalMilliseconds);

                    await ResetConnectionAsync().ConfigureAwait(false);
                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        private static bool IsTransientException(Exception ex)
        {
            return ex is SocketException ||
                   ex is SshConnectionException ||
                   ex is TimeoutException ||
                   (ex is IOException && !(ex is FileNotFoundException || ex is DirectoryNotFoundException));
        }

        private async Task ResetConnectionAsync()
        {
            await _connectionLock.WaitAsync().ConfigureAwait(false);
            try
            {
                if (_client != null)
                {
                    if (_client.IsConnected)
                    {
                        try { _client.Disconnect(); } catch { }
                    }
                    _client.Dispose();
                    _client = null;
                }
            }
            finally
            {
                _connectionLock.Release();
            }
        }

        /// <inheritdoc />
        public async Task UploadFileAsync(string localFilePath, string remoteDestinationPath, SftpTransferOptions? options = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(localFilePath)) throw new ArgumentException("Local path cannot be null or empty.", nameof(localFilePath));
            if (string.IsNullOrWhiteSpace(remoteDestinationPath)) throw new ArgumentException("Remote destination cannot be null or empty.", nameof(remoteDestinationPath));
            if (!File.Exists(localFilePath)) throw new FileNotFoundException("Local file not found.", localFilePath);

            options ??= new SftpTransferOptions();
            var resolvedRemotePath = NormalizeRemotePath(remoteDestinationPath, Path.GetFileName(localFilePath));

            _logger.LogInformation("Uploading local file {LocalPath} to remote path {RemotePath}", localFilePath, resolvedRemotePath);

            await ExecuteWithRetryAsync(async client =>
            {
                using var fs = new FileStream(localFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                if (options.BufferSize.HasValue)
                {
                    client.BufferSize = options.BufferSize.Value;
                }

                await client.UploadFileAsync(fs, resolvedRemotePath, options.Overwrite, null, cancellationToken).ConfigureAwait(false);
            }, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Successfully uploaded {LocalPath} to {RemotePath}", localFilePath, resolvedRemotePath);

            if (options.PostTransferAction == PostTransferAction.DeleteSource)
            {
                if (File.Exists(localFilePath))
                {
                    File.Delete(localFilePath);
                    _logger.LogInformation("Deleted local source file {LocalPath} after transfer", localFilePath);
                }
            }
        }

        /// <inheritdoc />
        public async Task UploadStreamAsync(Stream sourceStream, string remoteDestinationPath, bool overwrite = true, CancellationToken cancellationToken = default)
        {
            if (sourceStream == null) throw new ArgumentNullException(nameof(sourceStream));
            if (string.IsNullOrWhiteSpace(remoteDestinationPath)) throw new ArgumentException("Remote path cannot be null or empty.", nameof(remoteDestinationPath));

            var normalizedPath = remoteDestinationPath.Replace('\\', '/');
            _logger.LogInformation("Uploading stream to remote path {RemotePath}", normalizedPath);

            await ExecuteWithRetryAsync(async client =>
            {
                await client.UploadFileAsync(sourceStream, normalizedPath, overwrite, null, cancellationToken).ConfigureAwait(false);
            }, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Successfully uploaded stream to {RemotePath}", normalizedPath);
        }

        /// <inheritdoc />
        public async Task DownloadFileAsync(string remoteFilePath, string localDestinationPath, SftpTransferOptions? options = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(remoteFilePath)) throw new ArgumentException("Remote path cannot be null or empty.", nameof(remoteFilePath));
            if (string.IsNullOrWhiteSpace(localDestinationPath)) throw new ArgumentException("Local destination cannot be null or empty.", nameof(localDestinationPath));

            options ??= new SftpTransferOptions();
            var normalizedRemotePath = remoteFilePath.Replace('\\', '/');
            var resolvedLocalPath = Directory.Exists(localDestinationPath)
                ? Path.Combine(localDestinationPath, Path.GetFileName(normalizedRemotePath))
                : localDestinationPath;

            var localDir = Path.GetDirectoryName(resolvedLocalPath);
            if (!string.IsNullOrEmpty(localDir) && !Directory.Exists(localDir))
            {
                Directory.CreateDirectory(localDir);
            }

            _logger.LogInformation("Downloading remote file {RemotePath} to {LocalPath}", normalizedRemotePath, resolvedLocalPath);

            await ExecuteWithRetryAsync(async client =>
            {
                using var fs = new FileStream(resolvedLocalPath, FileMode.Create, FileAccess.Write, FileShare.None);
                if (options.BufferSize.HasValue)
                {
                    client.BufferSize = options.BufferSize.Value;
                }

                await client.DownloadFileAsync(normalizedRemotePath, fs, null, cancellationToken).ConfigureAwait(false);
            }, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Successfully downloaded {RemotePath} to {LocalPath}", normalizedRemotePath, resolvedLocalPath);

            if (options.PostTransferAction == PostTransferAction.DeleteSource)
            {
                await DeleteFileAsync(normalizedRemotePath, cancellationToken).ConfigureAwait(false);
                _logger.LogInformation("Deleted remote file {RemotePath} after download", normalizedRemotePath);
            }
        }

        /// <inheritdoc />
        public async Task DownloadStreamAsync(string remoteFilePath, Stream destinationStream, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(remoteFilePath)) throw new ArgumentException("Remote path cannot be null or empty.", nameof(remoteFilePath));
            if (destinationStream == null) throw new ArgumentNullException(nameof(destinationStream));

            var normalizedRemotePath = remoteFilePath.Replace('\\', '/');
            _logger.LogInformation("Downloading remote file {RemotePath} to stream", normalizedRemotePath);

            await ExecuteWithRetryAsync(async client =>
            {
                await client.DownloadFileAsync(normalizedRemotePath, destinationStream, null, cancellationToken).ConfigureAwait(false);
            }, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Successfully downloaded {RemotePath} to stream", normalizedRemotePath);
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<SftpItem>> ListFilesAsync(string remoteDirectoryPath, string? extensionFilter = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(remoteDirectoryPath)) throw new ArgumentException("Directory path cannot be null or empty.", nameof(remoteDirectoryPath));

            var normalizedDir = remoteDirectoryPath.Replace('\\', '/');

            return await ExecuteWithRetryAsync(async client =>
            {
                var files = new List<SftpItem>();
                await foreach (var item in client.ListDirectoryAsync(normalizedDir, cancellationToken).ConfigureAwait(false))
                {
                    if (item.Name == "." || item.Name == "..")
                        continue;

                    if (!string.IsNullOrEmpty(extensionFilter) &&
                        !item.Name.EndsWith(extensionFilter, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    files.Add(new SftpItem(item.Name, item.FullName, item.Length, item.IsDirectory, item.LastWriteTime));
                }

                return (IReadOnlyList<SftpItem>)files;
            }, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<bool> ExistsAsync(string remotePath, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(remotePath)) return false;

            var normalized = remotePath.Replace('\\', '/');
            return await ExecuteWithRetryAsync(async client =>
            {
                return await client.ExistsAsync(normalized, cancellationToken).ConfigureAwait(false);
            }, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task DeleteFileAsync(string remoteFilePath, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(remoteFilePath)) throw new ArgumentException("Path cannot be null or empty.", nameof(remoteFilePath));

            var normalized = remoteFilePath.Replace('\\', '/');
            _logger.LogInformation("Deleting remote file {RemotePath}", normalized);

            await ExecuteWithRetryAsync(async client =>
            {
                await client.DeleteFileAsync(normalized, cancellationToken).ConfigureAwait(false);
            }, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task CreateDirectoryAsync(string remoteDirectoryPath, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(remoteDirectoryPath)) throw new ArgumentException("Path cannot be null or empty.", nameof(remoteDirectoryPath));

            var normalized = remoteDirectoryPath.Replace('\\', '/');
            _logger.LogInformation("Creating remote directory {RemotePath}", normalized);

            await ExecuteWithRetryAsync(async client =>
            {
                await client.CreateDirectoryAsync(normalized, cancellationToken).ConfigureAwait(false);
            }, cancellationToken).ConfigureAwait(false);
        }

        private static string NormalizeRemotePath(string destinationPath, string fileName)
        {
            var normalized = destinationPath.Replace('\\', '/');
            return normalized.EndsWith("/") ? $"{normalized}{fileName}" : $"{normalized}/{fileName}";
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(SftpService));
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _connectionLock.Dispose();
            if (_client != null)
            {
                if (_client.IsConnected)
                {
                    _client.Disconnect();
                }
                _client.Dispose();
            }

            GC.SuppressFinalize(this);
        }

#if NET8_0_OR_GREATER
        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;
            _disposed = true;

            _connectionLock.Dispose();
            if (_client != null)
            {
                if (_client.IsConnected)
                {
                    _client.Disconnect();
                }
                _client.Dispose();
            }

            GC.SuppressFinalize(this);
            await Task.CompletedTask;
        }
#endif
    }
}
