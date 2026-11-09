using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using SftpWrapper.Sdk.Models;
using SftpWrapper.Sdk.Services;
using Xunit;

namespace SftpWrapper.Tests
{
    [Trait("Category", "Integration")]
    public class SftpIntegrationTests
    {
        private const string Host = "localhost";
        private const int Port = 2222;
        private const string User = "foo";
        private const string Password = "pass";
        private const string RemoteDir = "/upload";

        private static bool IsSftpServerAvailable()
        {
            try
            {
                using var tcp = new TcpClient();
                var result = tcp.BeginConnect(Host, Port, null, null);
                var success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromMilliseconds(500));
                if (!success) return false;
                tcp.EndConnect(result);
                return true;
            }
            catch
            {
                return false;
            }
        }

        [SkippableFact]
        public async Task EndToEnd_StreamUpload_And_Download_Succeeds()
        {
            Skip.IfNot(IsSftpServerAvailable(), "SFTP server is not running on localhost:2222. Execute 'docker-compose up -d' to run integration tests.");

            var options = new SftpOptions(Host, User, Password, Port);
            using var service = new SftpService(options);

            var fileName = $"test_stream_{Guid.NewGuid():N}.txt";
            var remoteFilePath = $"{RemoteDir}/{fileName}";
            var content = "Integration test content over SFTP stream";
            using var uploadStream = new MemoryStream(Encoding.UTF8.GetBytes(content));

            // 1. Upload Stream
            await service.UploadStreamAsync(uploadStream, remoteFilePath);

            // 2. Check Exists
            var exists = await service.ExistsAsync(remoteFilePath);
            Assert.True(exists);

            // 3. Download Stream
            using var downloadStream = new MemoryStream();
            await service.DownloadStreamAsync(remoteFilePath, downloadStream);
            var downloadedContent = Encoding.UTF8.GetString(downloadStream.ToArray());
            Assert.Equal(content, downloadedContent);

            // 4. Delete File
            await service.DeleteFileAsync(remoteFilePath);
            var existsAfterDelete = await service.ExistsAsync(remoteFilePath);
            Assert.False(existsAfterDelete);
        }

        [SkippableFact]
        public async Task EndToEnd_ListFiles_ReturnsUploadedItem()
        {
            Skip.IfNot(IsSftpServerAvailable(), "SFTP server is not running on localhost:2222. Execute 'docker-compose up -d' to run integration tests.");

            var options = new SftpOptions(Host, User, Password, Port);
            using var service = new SftpService(options);

            var fileName = $"test_list_{Guid.NewGuid():N}.txt";
            var remoteFilePath = $"{RemoteDir}/{fileName}";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes("test list file"));

            await service.UploadStreamAsync(stream, remoteFilePath);

            var files = await service.ListFilesAsync(RemoteDir, extensionFilter: ".txt");
            Assert.Contains(files, f => f.Name == fileName);

            await service.DeleteFileAsync(remoteFilePath);
        }
    }
}
