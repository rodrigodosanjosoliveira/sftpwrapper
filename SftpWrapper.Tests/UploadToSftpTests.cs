using System;
using System.IO;
using SftpWrapper.Sdk.Models;
using SftpWrapper.Sdk.Services;
using Xunit;

namespace SftpWrapper.Tests
{
    public class UploadToSftpTests
    {
        const string Host = "localhost";
        const string User = "foo";
        const string Password = "pass";
        const string DestinationPath = "/upload/";
        readonly string _sourcePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), 
            "filename.ext");
        const int Port = 2222;
        readonly ConnectionInfo _connection;

        public UploadToSftpTests()
        {
            _connection = new ConnectionInfo(User, Password, Host, Port);
        }

        [Fact]
        public void UploadTest()
        {
            var operation = new Upload(_connection, _sourcePath, DestinationPath);
            operation.UploadToSftp();
            Assert.True(operation.UploadSuccess);
        }

        [Fact]
        public void UploadTestWithInvalidArgument()
        {
            var invalidPath = "path";
            var ex = Assert.Throws<ArgumentException>(() => new Upload(_connection, invalidPath, DestinationPath));
            Assert.Equal("Invalid path or file not found.", ex.Message);
        }
    }
}
