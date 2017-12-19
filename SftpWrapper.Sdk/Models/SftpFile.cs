using System.IO;

namespace SftpWrapper.Sdk.Models
{
    public class SftpFile
    {
        public string SourcePath { get; }
        public string DestinationPath { get; }

        public SftpFile(string sourcePath, string destinationPath)
        {
            SourcePath = sourcePath;
            DestinationPath = destinationPath;
        }

        public string Name => Path.GetFileName(SourcePath);
    }
}
