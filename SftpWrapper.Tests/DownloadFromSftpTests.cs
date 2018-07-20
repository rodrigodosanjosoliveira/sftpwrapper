using System;
using System.Collections.Generic;
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
        private readonly ConnectionInfo _connection;

        public DownloadFromSftpTests()
        {
            _connection = new ConnectionInfo(User, Password, Host);
        }

        [Fact]
        public void DownloadTest()
        {
            var operation = new Download(_connection, SourcePath, _destinationPath);
            operation.DownloadFromSftp();
            Assert.True(operation.DownloadSuccess(_destinationPath));
        }

        [Fact]
        public void DownloadWithExtensionTest()
        {
            var validExtensions = new List<string> { ".rem", ".ret" };

            var operation = new Download(_connection, SourcePath, _destinationPath, validExtensions);
            operation.DownloadManyFromSftp();
            Assert.True(operation.DownloadSuccess(_destinationPath + "//teste.ret"));
        }

        [Fact]
        public void DownloadWithExtensionInValidFilesTest()
        {
            var validExtensions = new List<string> { ".rem", ".ret" };

            var operation = new Download(_connection, SourcePath, _destinationPath, validExtensions);
            var ex = Assert.Throws<SftpPathNotFoundException>(() => operation.DownloadManyFromSftp());
            Assert.Equal("No files found to download.", ex.Message);
        }

        [Fact]
        public void DownloadTestWithInvalidArgument()
        {
            const string invalidPath = "path";
            var ex = Assert.Throws<SftpPathNotFoundException>(() => new Download(_connection, invalidPath, _destinationPath));
            Assert.Equal("No such file", ex.Message);
        }

        [Fact]
        public void GetFileNameTest()
        {
            var download = new Download(_connection, SourcePath, _destinationPath);
            var file = download.GetFileName();
            Assert.NotNull(file);
            Assert.True(File.Exists(_destinationPath));
            Assert.True(new FileInfo(_destinationPath).Length > 0);
        }

        [Fact]
        public void GetFileNameIsNullTest()
        {
            var ex = Assert.Throws<SftpPathNotFoundException>(() => new Download(_connection, SourcePath, _destinationPath));
            Assert.Equal("File not found.", ex.Message);
        }

        [Fact]
        public void UsingOtherConstructorTest()
        {
            var download = new Download(_connection) { File = new SftpFileInfo("/upload/teste.ret", _destinationPath) };
            download.DownloadFromSftp();
            Assert.True(download.DownloadSuccess(_destinationPath));
        }
    }
}
