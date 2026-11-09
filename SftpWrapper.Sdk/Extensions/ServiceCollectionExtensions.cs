using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SftpWrapper.Sdk.Abstractions;
using SftpWrapper.Sdk.Models;
using SftpWrapper.Sdk.Services;

namespace SftpWrapper.Sdk.Extensions
{
    /// <summary>
    /// Extension methods for configuring SFTP services in an <see cref="IServiceCollection"/>.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers <see cref="ISftpService"/> and <see cref="SftpService"/> with transient lifetime using configuration action.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configureOptions">Action to configure SFTP connection options.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddSftpService(this IServiceCollection services, Action<SftpOptions> configureOptions)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (configureOptions == null) throw new ArgumentNullException(nameof(configureOptions));

            services.Configure(configureOptions);
            services.TryAddTransient<ISftpService, SftpService>();

            return services;
        }

        /// <summary>
        /// Registers <see cref="ISftpService"/> and <see cref="SftpService"/> using pre-configured <see cref="SftpOptions"/>.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="options">The explicit SFTP options instance.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddSftpService(this IServiceCollection services, SftpOptions options)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (options == null) throw new ArgumentNullException(nameof(options));

            services.TryAddTransient<ISftpService>(_ => new SftpService(options));

            return services;
        }
    }
}
