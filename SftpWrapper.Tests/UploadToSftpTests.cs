using System;
using System.IO;
using SftpWrapper.Sdk.Models;
using SftpWrapper.Sdk.Services;
using Xunit;

#pragma warning disable CS0618

namespace SftpWrapper.Tests
{
    public class UploadToSftpTests : IDisposable
    {
        private const string Host = "localhost";
        private const string User = "foo";
        private const string Password = "pass";
        private const int Port = 2222;
        private const string DestinationPath = "/upload/";

        private readonly string _tempFilePath;
        private readonly ConnectionInfo _connection;

        public UploadToSftpTests()
        {
            _connection = new ConnectionInfo(User, Password, Host, Port);
            _tempFilePath = Path.Combine(Path.GetTempPath(), $"upload_test_{Guid.NewGuid():N}.txt");
            File.WriteAllText(_tempFilePath, "Sample content for SFTP upload testing");
        }

        [Fact]
        public void UploadTestWithInvalidArgument()
        {
            const string invalidPath = "non_existent_file_path_12345.ext";
            var ex = Assert.Throws<ArgumentException>(() => new Upload(_connection, invalidPath, DestinationPath));
            Assert.Equal("Invalid path or file not found.", ex.Message);
        }

        [Fact]
        public void UploadTest_SafeMode_PreservesLocalFile()
        {
            var operation = new Upload(_connection, _tempFilePath, DestinationPath, deleteSourceAfterUpload: false);
            Assert.False(operation.DeleteSourceAfterUpload);
        }

        [Fact]
        public void UploadTest_DeleteSourceMode_SetsFlagCorrectly()
        {
            var operation = new Upload(_connection, _tempFilePath, DestinationPath, deleteSourceAfterUpload: true);
            Assert.True(operation.DeleteSourceAfterUpload);
        }

        public void Dispose()
        {
            if (File.Exists(_tempFilePath))
            {
                try
                {
                    File.Delete(_tempFilePath);
                }
                catch
                {
                    // Ignore temp file cleanup errors
                }
            }
        }
    }
}
