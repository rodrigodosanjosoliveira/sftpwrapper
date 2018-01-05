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
        private const string SourcePath = "/upload/";
        private readonly string _destinationPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop));
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
        public void DownloadTestWithInvalidArgument()
        {
            const string invalidPath = "path";
            var ex = Assert.Throws<SftpPathNotFoundException>(() => new Download(_connection, invalidPath, _destinationPath));
            Assert.Equal("File not found.", ex.Message);
        }

        [Fact]
        public void GetFileNameTest()
        {
            var download = new Download(_connection, "/upload/", _destinationPath);
            var file = download.GetFileName();
            Assert.NotNull(file);
            Assert.Equal("teste.html", file);
        }

        [Fact]
        public void GetFileNameIsNullTest()
        {
            var download = new Download(_connection, "/upload/", _destinationPath);
            var file = download.GetFileName();
            Assert.Null(file);
        }

        [Fact]
        public void UsingOtherConstructorTest()
        {
            var download = new Download(_connection) {File = new SftpFileInfo("/upload/teste.html",_destinationPath)};
            download.DownloadFromSftp();
            Assert.True(download.DownloadSuccess);
        }
    }
}
