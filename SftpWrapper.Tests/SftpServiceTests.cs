using System;
using System.IO;
using System.Threading.Tasks;
using SftpWrapper.Sdk.Models;
using SftpWrapper.Sdk.Services;
using Xunit;

namespace SftpWrapper.Tests
{
    public class SftpServiceTests
    {
        private readonly SftpOptions _validOptions = new SftpOptions("localhost", "user", "pass", 2222);

        [Fact]
        public void Constructor_NullOptions_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new SftpService((SftpOptions)null!));
        }

        [Fact]
        public void Constructor_NullConnectionInfo_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new SftpService((ConnectionInfo)null!));
        }

        [Fact]
        public async Task UploadFileAsync_NullOrEmptyLocalPath_ThrowsArgumentException()
        {
            using var service = new SftpService(_validOptions);
            await Assert.ThrowsAsync<ArgumentException>(() => service.UploadFileAsync("", "/remote/path"));
        }

        [Fact]
        public async Task UploadFileAsync_NonExistentFile_ThrowsFileNotFoundException()
        {
            using var service = new SftpService(_validOptions);
            await Assert.ThrowsAsync<FileNotFoundException>(() => service.UploadFileAsync("non_existent_file.xyz", "/remote/path"));
        }

        [Fact]
        public async Task UploadStreamAsync_NullStream_ThrowsArgumentNullException()
        {
            using var service = new SftpService(_validOptions);
            await Assert.ThrowsAsync<ArgumentNullException>(() => service.UploadStreamAsync(null!, "/remote/path"));
        }

        [Fact]
        public async Task DownloadFileAsync_NullOrEmptyRemotePath_ThrowsArgumentException()
        {
            using var service = new SftpService(_validOptions);
            await Assert.ThrowsAsync<ArgumentException>(() => service.DownloadFileAsync("", "/local/dest"));
        }

        [Fact]
        public async Task DownloadStreamAsync_NullStream_ThrowsArgumentNullException()
        {
            using var service = new SftpService(_validOptions);
            await Assert.ThrowsAsync<ArgumentNullException>(() => service.DownloadStreamAsync("/remote/file.txt", null!));
        }

        [Fact]
        public async Task ListFilesAsync_NullOrEmptyPath_ThrowsArgumentException()
        {
            using var service = new SftpService(_validOptions);
            await Assert.ThrowsAsync<ArgumentException>(() => service.ListFilesAsync(""));
        }

        [Fact]
        public async Task DeleteFileAsync_NullOrEmptyPath_ThrowsArgumentException()
        {
            using var service = new SftpService(_validOptions);
            await Assert.ThrowsAsync<ArgumentException>(() => service.DeleteFileAsync(""));
        }

        [Fact]
        public async Task CreateDirectoryAsync_NullOrEmptyPath_ThrowsArgumentException()
        {
            using var service = new SftpService(_validOptions);
            await Assert.ThrowsAsync<ArgumentException>(() => service.CreateDirectoryAsync(""));
        }

        [Fact]
        public async Task ExistsAsync_NullOrEmptyPath_ReturnsFalse()
        {
            using var service = new SftpService(_validOptions);
            var exists = await service.ExistsAsync("");
            Assert.False(exists);
        }

        [Fact]
        public async Task SftpService_ThrowsObjectDisposedException_WhenDisposed()
        {
            var service = new SftpService(_validOptions);
            service.Dispose();

            await Assert.ThrowsAsync<ObjectDisposedException>(() => service.ListFilesAsync("/test"));
        }
    }
}
