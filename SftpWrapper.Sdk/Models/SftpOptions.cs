using System;

namespace SftpWrapper.Sdk.Models
{
    /// <summary>
    /// Configuration options for establishing an SFTP connection.
    /// </summary>
    public class SftpOptions
    {
        /// <summary>
        /// SFTP server hostname or IP address.
        /// </summary>
        public string Host { get; set; } = string.Empty;

        /// <summary>
        /// SFTP server port (default: 22).
        /// </summary>
        public int Port { get; set; } = 22;

        /// <summary>
        /// Username for authentication.
        /// </summary>
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// Password for password-based authentication.
        /// </summary>
        public string? Password { get; set; }

        /// <summary>
        /// Local file path to an SSH private key file (e.g. id_rsa, id_ed25519).
        /// </summary>
        public string? PrivateKeyFilePath { get; set; }

        /// <summary>
        /// Raw private key content in PEM/OpenSSH format (useful for secrets retrieved from Vault/KeyVault).
        /// </summary>
        public string? PrivateKeyContent { get; set; }

        /// <summary>
        /// Optional passphrase protecting the private key.
        /// </summary>
        public string? PrivateKeyPassphrase { get; set; }

        /// <summary>
        /// Buffer size used for transfer operations (default: 64 KB).
        /// </summary>
        public uint BufferSize { get; set; } = 64 * 1024;

        /// <summary>
        /// Connection timeout. Defaults to 30 seconds.
        /// </summary>
        public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Maximum retry attempts for transient network failures during connection and transfer (default: 3). Set to 0 to disable retries.
        /// </summary>
        public int MaxRetryAttempts { get; set; } = 3;

        /// <summary>
        /// Initial delay between retry attempts (default: 1 second). Uses exponential backoff.
        /// </summary>
        public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Initializes a new instance of the <see cref="SftpOptions"/> class.
        /// </summary>
        public SftpOptions()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SftpOptions"/> class with required connection parameters.
        /// </summary>
        public SftpOptions(string host, string userName, string? password = null, int port = 22)
        {
            Host = host ?? throw new ArgumentNullException(nameof(host));
            UserName = userName ?? throw new ArgumentNullException(nameof(userName));
            Password = password;
            Port = port;
        }
    }
}
