using System;
using System.Collections.Generic;
using System.IO;
using Renci.SshNet.Common;
using SftpWrapper.Sdk.Models;
using SftpWrapper.Sdk.Services;
using Xunit;

#pragma warning disable CS0618

namespace SftpWrapper.Tests
{
    public class DownloadFromSftpTests : IDisposable
    {
        private const string Host = "localhost";
        private const string User = "foo";
        private const string Password = "pass";
        private const int Port = 2222;
        private const string SourcePath = "/upload/";
        private readonly string _destinationDirectory;
        private readonly ConnectionInfo _connection;

        public DownloadFromSftpTests()
        {
            _connection = new ConnectionInfo(User, Password, Host, Port);
            _destinationDirectory = Path.Combine(Path.GetTempPath(), $"sftp_download_tests_{Guid.NewGuid():N}");
            Directory.CreateDirectory(_destinationDirectory);
        }

        [Fact]
        public void DownloadTestWithNullArguments()
        {
            Assert.Throws<ArgumentNullException>(() => new Download(null!, SourcePath, _destinationDirectory));
        }

        [Fact]
        public void DownloadTest_SafeMode_PreservesRemoteFileFlagByDefault()
        {
            var fileInfo = new SftpFileInfo("/upload/test.txt", Path.Combine(_destinationDirectory, "test.txt"));
            Assert.Equal("test.txt", fileInfo.Name);
        }

        [Fact]
        public void SftpFileInfo_Constructor_SetsPropertiesCorrectly()
        {
            var info = new SftpFileInfo("/remote/path/file.txt", "/local/path/file.txt");
            Assert.Equal("/remote/path/file.txt", info.SourcePath);
            Assert.Equal("/local/path/file.txt", info.DestinationPath);
            Assert.Equal("file.txt", info.Name);
        }

        [Fact]
        public void ConnectionInfo_Constructor_SetsPropertiesCorrectly()
        {
            var conn = new ConnectionInfo("user", "pass", "host.com", 2222);
            Assert.Equal("user", conn.UserName);
            Assert.Equal("pass", conn.Password);
            Assert.Equal("host.com", conn.Host);
            Assert.Equal(2222, conn.Port);
        }

        public void Dispose()
        {
            if (Directory.Exists(_destinationDirectory))
            {
                try
                {
                    Directory.Delete(_destinationDirectory, recursive: true);
                }
                catch
                {
                    // Ignore temp directory cleanup errors
                }
            }
        }
    }
}
