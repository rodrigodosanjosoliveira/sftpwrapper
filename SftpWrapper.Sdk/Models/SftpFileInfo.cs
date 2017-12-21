using System;
using System.IO;

namespace SftpWrapper.Sdk.Models
{
    /// <summary>
    /// Represents the file informations for download and upload
    /// </summary>
    public class SftpFileInfo
    {
        public string SourcePath { get; }
        public string DestinationPath { get; }

        /// <summary>
        /// Initialize a new instance of the <see="SftpClient"/> class.
        /// </summary>
        /// <param name="sourcePath">Source path of the file</param>
        /// <param name="destinationPath">Destination path of the file.</param>
        /// <exception cref="ArgumentNullException"><paramref name="sourcePath"/> is <b>null</b>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="destinationPath"/> is <b>null</b>.</exception>
        public SftpFileInfo(string sourcePath, string destinationPath)
        {
            SourcePath = sourcePath;
            DestinationPath = destinationPath;
        }

        /// <summary>
        /// 
        /// </summary>
        public string Name => Path.GetFileName(SourcePath);
    }
}
