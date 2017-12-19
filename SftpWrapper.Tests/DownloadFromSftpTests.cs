using System;
using System.IO;
using Renci.SshNet.Common;
using SftpWrapper.Sdk.Models;
using SftpWrapper.Sdk.Services;
using Xunit;

namespace SftpWrapper.Tests
{
    public class DownloadFromSftpTests
    {
        private const string Host = "localhost";
        private const string User = "foo";
        private const string Password = "pass";
        private const string SourcePath = "/upload/filename.ext";
        private readonly string _destinationPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "filename.ext");
        private const int Port = 2222;
        private readonly ConnectionInfo _connection;

        public DownloadFromSftpTests()
        {
            _connection = new ConnectionInfo(User, Password, Host, Port);
        }

        [Fact]
        public void DownloadTest()
        {
            var operation = new Download(_connection, SourcePath, _destinationPath);
            operation.DownloadFromSftp();
            Assert.True(operation.DownloadSuccess);
        }

        [Fact]
        public void UploadTestWithInvalidArgument()
        {
            const string invalidPath = "path";
            var ex = Assert.Throws<SftpPathNotFoundException>(() => new Download(_connection, invalidPath, _destinationPath));
            Assert.Equal("File not found.", ex.Message);
        }
    }
}