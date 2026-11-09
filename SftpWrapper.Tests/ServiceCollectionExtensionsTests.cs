using System;
using Microsoft.Extensions.DependencyInjection;
using SftpWrapper.Sdk.Abstractions;
using SftpWrapper.Sdk.Extensions;
using SftpWrapper.Sdk.Models;
using SftpWrapper.Sdk.Services;
using Xunit;

namespace SftpWrapper.Tests
{
    public class ServiceCollectionExtensionsTests
    {
        [Fact]
        public void AddSftpService_WithOptionsAction_ResolvesISftpService()
        {
            var services = new ServiceCollection();
            services.AddSftpService(options =>
            {
                options.Host = "sftp.example.com";
                options.Port = 2222;
                options.UserName = "tester";
                options.Password = "secret";
            });

            var provider = services.BuildServiceProvider();
            var service = provider.GetService<ISftpService>();

            Assert.NotNull(service);
            Assert.IsType<SftpService>(service);
        }

        [Fact]
        public void AddSftpService_WithDirectOptions_ResolvesISftpService()
        {
            var services = new ServiceCollection();
            var options = new SftpOptions("sftp.example.com", "tester", "secret", 2222);

            services.AddSftpService(options);

            var provider = services.BuildServiceProvider();
            var service = provider.GetService<ISftpService>();

            Assert.NotNull(service);
            Assert.IsType<SftpService>(service);
        }

        [Fact]
        public void AddSftpService_NullServices_ThrowsArgumentNullException()
        {
            IServiceCollection services = null!;
            Assert.Throws<ArgumentNullException>(() => services.AddSftpService(opt => { }));
        }

        [Fact]
        public void AddSftpService_NullAction_ThrowsArgumentNullException()
        {
            var services = new ServiceCollection();
            Assert.Throws<ArgumentNullException>(() => services.AddSftpService((Action<SftpOptions>)null!));
        }

        [Fact]
        public void SftpOptions_PrivateKeyProperties_StoreValuesCorrectly()
        {
            var options = new SftpOptions
            {
                Host = "localhost",
                UserName = "user",
                PrivateKeyFilePath = "/keys/id_rsa",
                PrivateKeyPassphrase = "pass",
                PrivateKeyContent = "-----BEGIN RSA PRIVATE KEY-----"
            };

            Assert.Equal("/keys/id_rsa", options.PrivateKeyFilePath);
            Assert.Equal("pass", options.PrivateKeyPassphrase);
            Assert.Equal("-----BEGIN RSA PRIVATE KEY-----", options.PrivateKeyContent);
        }
    }
}
