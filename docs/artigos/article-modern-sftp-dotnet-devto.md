---
title: Modern SFTP in .NET 8: Resilience, Dependency Injection, and Zero-Disk Streams
published: false
description: Stop using obsolete sync patterns. Learn how to build modern, resilient SFTP integrations in C# .NET 8 with Streams, DI, and exponential backoff.
tags: dotnet, csharp, softwareengineering, security
---

In the era of microservices, event streaming, and cloud-native architectures, many developers are surprised to discover that mission-critical enterprise workflows still run on... **SFTP**.

From banking reconciliation files (ACH/SEPA/CNAB) and healthcare records (HL7/EDI) to ERP batch processing (SAP/Oracle), SFTP remains the de facto protocol for secure, asynchronous B2B data exchange.

However, a quick search for C# SFTP tutorials reveals an unfortunate reality: most guides still promote patterns from a decade ago. They use synchronous blocking I/O, tightly couple business logic to concrete client classes, rely on temporary disk files, and lack automatic reconnects or transient fault handling.

In this article, we will examine the most common anti-patterns of SFTP implementations in C# and demonstrate how to build a clean, resilient, and testable SFTP integration in **.NET 8+** using the open-source library **[SftpWrapper.Sdk](https://www.nuget.org/packages/SftpWrapper.Sdk/)**.

---

## 4 Costly SFTP Anti-Patterns in .NET

```
┌─────────────────────────────────┐       ┌─────────────────────────────────┐
│        Common Anti-Pattern      │       │     Modern .NET 8 Solution      │
├─────────────────────────────────┤       ├─────────────────────────────────┤
│ 1. Synchronous Blocking I/O     │  ──>  │ Async/Await + CancellationToken │
│ 2. Concrete Client Coupling     │  ──>  │ ISftpService + DI Extension     │
│ 3. Temporary Disk Files         │  ──>  │ Direct Memory Streams           │
│ 4. No Transient Fault Retries   │  ──>  │ Exponential Backoff & Lazy Conn │
└─────────────────────────────────┘       └─────────────────────────────────┘
```

### 1. Synchronous Network I/O (Thread Pool Starvation)
Calling synchronous methods like `client.UploadFile(...)` or `client.Connect()` inside ASP.NET Core endpoints blocks worker threads while waiting for socket packets across the internet. Under moderate load, this causes **Thread Pool Starvation**, degrading API throughput and causing unrelated HTTP requests to queue.

### 2. Concrete Clients Without Interfaces (Zero Testability)
Directly instantiating `new SftpClient(...)` inside your domain services makes it impossible to write fast, isolated unit tests using mocks (such as Moq or NSubstitute). You are forced to spin up live SFTP containers even for basic business logic validation.

### 3. Unnecessary Disk I/O with Temporary Files
Writing payload data to local temporary files (`Path.GetTempFileName()`) before uploading introduces disk I/O bottlenecks in containerized environments (Kubernetes pods) and creates security liabilities with sensitive data persistence. Data should be streamed directly in-memory whenever possible.

### 4. Fragile Connections Without Retry Policies
SSH connections across corporate firewalls frequently experience socket resets, timeouts, and temporary blips. Without automatic reconnection and exponential backoff retries, batch jobs fail unnecessarily.

---

## The Solution: SftpWrapper.Sdk

To provide a modern and idiomatic C# experience on top of the battle-tested `SSH.NET` engine, **SftpWrapper.Sdk** provides:

* **`ISftpService`**: A clean, fully mockable interface for unit testing.
* **100% Async / Await**: Full support for asynchronous operations and cancellation tokens.
* **In-Memory Streams**: Stream directly to and from SFTP without temporary disk files.
* **Native Dependency Injection**: Register the service with a single line via `AddSftpService`.
* **Built-in Resilience**: Automatic reconnection and configurable exponential backoff retries.
* **Key-Based Authentication**: Seamless support for Passwords, Private Key files (`.pem`), and raw in-memory keys (retrieved from Azure Key Vault or AWS Secrets Manager).
* **Data-Loss Prevention**: By default, source files are never deleted unless explicitly instructed.

---

## Hands-On: Production-Grade Example in .NET 8

### 1. Install the Package

```bash
dotnet add package SftpWrapper.Sdk --version 2.1.0
```

---

### 2. Configure Dependency Injection in `Program.cs`

```csharp
using SftpWrapper.Sdk.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Register SftpService with strongly-typed options
builder.Services.AddSftpService(options =>
{
    options.Host = builder.Configuration["Sftp:Host"] ?? "sftp.partner.com";
    options.Port = builder.Configuration.GetValue("Sftp:Port", 22);
    options.UserName = builder.Configuration["Sftp:UserName"] ?? "app_user";
    
    // Option A: Password authentication
    options.Password = builder.Configuration["Sftp:Password"];

    // Option B: Private key authentication (Recommended for cloud/production)
    // options.PrivateKeyContent = builder.Configuration["Sftp:PrivateKeyRaw"];
    // options.PrivateKeyPassphrase = builder.Configuration["Sftp:Passphrase"];

    // Resilience & Retry policy
    options.MaxRetryAttempts = 3;
    options.RetryDelay = TimeSpan.FromSeconds(2);
});

var app = builder.Build();
```

---

### 3. Stream Uploads Without Disk I/O

Here is a real-world financial reconciliation service that generates report data in-memory and streams it directly to the remote SFTP server:

```csharp
using System.Text;
using SftpWrapper.Sdk.Abstractions;
using SftpWrapper.Sdk.Models;

public class ReconciliationService
{
    private readonly ISftpService _sftpService;
    private readonly ILogger<ReconciliationService> _logger;

    public ReconciliationService(ISftpService sftpService, ILogger<ReconciliationService> logger)
    {
        _sftpService = sftpService;
        _logger = logger;
    }

    public async Task ProcessDailyReconciliationAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Generating reconciliation payload...");

        // 1. Generate data directly in memory (e.g. CSV or EDI document)
        var csvContent = "TransactionId;Account;Amount;Status\nTX101;ACC-8821;14500.00;SETTLED\nTX102;ACC-9943;230.50;PENDING";
        var bytes = Encoding.UTF8.GetBytes(csvContent);

        using var memoryStream = new MemoryStream(bytes);

        // 2. Stream directly to remote SFTP path (Zero disk usage)
        var remotePath = $"/reconciliation/recon_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
        
        await _sftpService.UploadStreamAsync(
            memoryStream, 
            remotePath, 
            overwrite: true, 
            cancellationToken: cancellationToken);

        _logger.LogInformation("Reconciliation file {Path} successfully streamed to SFTP!", remotePath);

        // 3. List available response files
        var responseFiles = await _sftpService.ListFilesAsync(
            "/responses", 
            extensionFilter: ".ack", 
            cancellationToken: cancellationToken);

        foreach (var file in responseFiles)
        {
            _logger.LogInformation("Processing response file: {Name} ({Bytes} bytes)", file.Name, file.Length);

            // 4. Download directly into memory stream
            using var downloadStream = new MemoryStream();
            await _sftpService.DownloadStreamAsync(file.FullName, downloadStream, cancellationToken);
            
            // Process downloaded stream...
        }
    }
}
```

---

### 4. Writing Unit Tests with Mocks

Because the service relies on `ISftpService`, unit testing your business logic requires no real network connections or docker containers:

```csharp
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Microsoft.Extensions.Logging.Abstractions;
using SftpWrapper.Sdk.Abstractions;
using Xunit;

public class ReconciliationServiceTests
{
    [Fact]
    public async Task ProcessDailyReconciliation_InvokesUploadStreamAsync()
    {
        // Arrange
        var mockSftp = new Mock<ISftpService>();
        var logger = NullLogger<ReconciliationService>.Instance;

        mockSftp
            .Setup(s => s.UploadStreamAsync(
                It.IsAny<Stream>(), 
                It.Is<string>(path => path.StartsWith("/reconciliation/recon_")), 
                true, 
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = new ReconciliationService(mockSftp.Object, logger);

        // Act
        await service.ProcessDailyReconciliationAsync(CancellationToken.None);

        // Assert
        mockSftp.Verify(s => s.UploadStreamAsync(
            It.IsAny<Stream>(), 
            It.IsAny<string>(), 
            true, 
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
```

---

## Supply Chain Security: Trusted Publishing on NuGet

Modern engineering also requires securing package release pipelines. The package deployment workflow utilizes **NuGet Trusted Publishing via OpenID Connect (OIDC)** in GitHub Actions.

This eliminates long-lived static API keys from GitHub Secrets, replacing them with ephemeral tokens issued dynamically at build time, protecting the release pipeline from credential leakage.

---

## Conclusion

Using legacy protocols like SFTP does not mean you must settle for legacy code. By adopting clean interfaces, asynchronous streams, native dependency injection, and automatic retry policies, you can build reliable integrations that thrive in modern cloud environments.

* **NuGet Package**: [SftpWrapper.Sdk 2.1.0](https://www.nuget.org/packages/SftpWrapper.Sdk/)
* **GitHub Repository**: [rodrigodosanjosoliveira/sftpwrapper](https://github.com/rodrigodosanjosoliveira/sftpwrapper)

---
*How does your team handle legacy B2B file transfers in modern .NET apps? Let me know in the comments below!*
