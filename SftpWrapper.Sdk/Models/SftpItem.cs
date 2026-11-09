using System;

namespace SftpWrapper.Sdk.Models
{
    /// <summary>
    /// Represents a remote item (file or directory) in an SFTP server.
    /// </summary>
    public class SftpItem
    {
        /// <summary>
        /// Name of the item.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Full absolute path of the remote item.
        /// </summary>
        public string FullName { get; }

        /// <summary>
        /// Size of the file in bytes (0 for directories).
        /// </summary>
        public long Length { get; }

        /// <summary>
        /// Indicates whether this item is a directory.
        /// </summary>
        public bool IsDirectory { get; }

        /// <summary>
        /// Date and time when the item was last modified.
        /// </summary>
        public DateTime LastWriteTime { get; }

        public SftpItem(string name, string fullName, long length, bool isDirectory, DateTime lastWriteTime)
        {
            Name = name;
            FullName = fullName;
            Length = length;
            IsDirectory = isDirectory;
            LastWriteTime = lastWriteTime;
        }
    }
}
