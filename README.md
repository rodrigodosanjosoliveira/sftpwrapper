[![Gitpod Ready-to-Code](https://img.shields.io/badge/Gitpod-Ready--to--Code-blue?logo=gitpod)](https://gitpod.io/#https://github.com/rodrigodosanjosoliveira/sftpwrapper) 
[![SftpWrapper](https://github.com/rodrigodosanjosoliveira/sftpwrapper/actions/workflows/dotnet.yml/badge.svg?branch=master)](https://github.com/rodrigodosanjosoliveira/sftpwrapper/actions/workflows/dotnet.yml)

<!-- ALL-CONTRIBUTORS-BADGE:START - Do not remove or modify this section -->
[![All Contributors](https://img.shields.io/badge/all_contributors-2-orange.svg?style=flat-square)](#contributors-)
<!-- ALL-CONTRIBUTORS-BADGE:END -->

# SftpWrapper.Sdk

Biblioteca moderna e thread-safe para .NET (.NET Standard 2.0 / .NET 8+) construída sobre o [SSH.NET](https://github.com/sshnet/SSH.NET), projetada para operações assíncronas de upload, download, streaming e gerenciamento de arquivos via SFTP.

Nuget: https://www.nuget.org/packages/SftpWrapper.Sdk/

---

## Principais Recursos

- **Operações 100% Assíncronas (`async/await`)** com suporte a `CancellationToken`.
- **Suporte a Streams**: Faça upload e download diretamente via `Stream` em memória ou disco sem arquivos temporários intermediários.
- **Injeção de Dependência Nativa**: Integração simplificada com `Microsoft.Extensions.DependencyInjection` e `IOptions<SftpOptions>`.
- **Autenticação Flexível**: Suporte a Senha, Arquivo de Chave Privada (`.pem`/`.key`) ou Chave Privada em texto/base64 com passphrase.
- **Proteção contra Perda Acidental de Dados**: Por padrão, nenhum arquivo de origem é excluído após a transferência, a menos que explicitado via `PostTransferAction.DeleteSource`.
- **Observabilidade**: Suporte a logs estruturados com `ILogger<SftpService>`.
- **Retrocompatibilidade**: Mantém compatibilidade com consumidores das versões anteriores (`Upload` e `Download` marcados como `[Obsolete]`).

---

## Instalação

Package Manager:
```powershell
Install-Package SftpWrapper.Sdk -Version 2.1.0
```

.NET CLI:
```bash
dotnet add package SftpWrapper.Sdk --version 2.1.0
```

---

## Como Utilizar

### 1. Injeção de Dependência (Recomendado no ASP.NET Core e Worker Services)

Registre o serviço no `Program.cs`:

```csharp
using SftpWrapper.Sdk.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Configuração via Action ou IConfiguration
builder.Services.AddSftpService(options =>
{
    options.Host = builder.Configuration["Sftp:Host"] ?? "sftp.exemplo.com";
    options.Port = int.Parse(builder.Configuration["Sftp:Port"] ?? "22");
    options.UserName = builder.Configuration["Sftp:UserName"] ?? "usuario";
    options.Password = builder.Configuration["Sftp:Password"];
    
    // Opcional: autenticação por chave privada
    // options.PrivateKeyFilePath = "/caminho/para/id_rsa";
    // options.PrivateKeyPassphrase = "senha-da-chave";
});
```

Injete a interface `ISftpService` nos seus serviços ou controllers:

```csharp
using SftpWrapper.Sdk.Abstractions;
using SftpWrapper.Sdk.Models;

public class RelatorioService
{
    private readonly ISftpService _sftpService;

    public RelatorioService(ISftpService sftpService)
    {
        _sftpService = sftpService;
    }

    public async Task ProcessarAsync(CancellationToken ct)
    {
        // Upload de arquivo local para o servidor SFTP
        await _sftpService.UploadFileAsync("dados.csv", "/remoto/dados.csv", cancellationToken: ct);

        // Upload a partir de uma Stream em memória
        using var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes("conteudo"));
        await _sftpService.UploadStreamAsync(memoryStream, "/remoto/arquivo_stream.txt", cancellationToken: ct);

        // Listagem de arquivos remotos filtrando por extensão
        var arquivos = await _sftpService.ListFilesAsync("/remoto", extensionFilter: ".csv", cancellationToken: ct);
        foreach (var item in arquivos)
        {
            Console.WriteLine($"Arquivo: {item.Name} ({item.Length} bytes)");
        }

        // Download de arquivo remoto para disco local
        await _sftpService.DownloadFileAsync("/remoto/dados.csv", "download_local.csv", cancellationToken: ct);
    }
}
```

---

### 2. Uso Direto (Instanciação Manual)

```csharp
using SftpWrapper.Sdk.Models;
using SftpWrapper.Sdk.Services;

var options = new SftpOptions
{
    Host = "sftp.exemplo.com",
    Port = 22,
    UserName = "usuario",
    Password = "senha"
};

using var sftpService = new SftpService(options);

// Upload preservando o arquivo original (padrão seguro)
await sftpService.UploadFileAsync("local.txt", "/remoto/local.txt");

// Upload movendo (excluindo o arquivo local após o upload com sucesso)
var moveOptions = new SftpTransferOptions
{
    PostTransferAction = PostTransferAction.DeleteSource
};
await sftpService.UploadFileAsync("temp.txt", "/remoto/temp.txt", moveOptions);
```

---

## Executando os Testes

Para executar a suíte de testes unitários:

```bash
dotnet test
```

Para testes de integração com o servidor SFTP local via Docker:

```bash
docker-compose up -d
```

Credenciais padrão do ambiente Docker:
- **Host**: 127.0.0.1
- **Porta**: 2222
- **Usuário**: foo
- **Senha**: pass
- **Diretório**: `/upload/`

---

## Contributors ✨

Thanks goes to these wonderful people ([emoji key](https://allcontributors.org/docs/en/emoji-key)):

<!-- ALL-CONTRIBUTORS-LIST:START - Do not remove or modify this section -->
<!-- prettier-ignore-start -->
<!-- markdownlint-disable -->
<table>
  <tr>
    <td align="center"><a href="https://github.com/JCelento"><img src="https://avatars0.githubusercontent.com/u/22276748?v=4" width="100px;" alt=""/><br /><sub><b>Jamile Celento</b></sub></a><br /><a href="#infra-JCelento" title="Infrastructure (Hosting, Build-Tools, etc)">🚇</a> <a href="https://github.com/rodrigodosanjosoliveira/sftpwrapper/commits?author=JCelento" title="Tests">⚠️</a> <a href="https://github.com/rodrigodosanjosoliveira/sftpwrapper/commits?author=JCelento" title="Code">💻</a></td>
    <td align="center"><a href="https://github.com/rodrigodosanjosoliveira"><img src="https://avatars3.githubusercontent.com/u/657657?v=4" width="100px;" alt=""/><br /><sub><b>Rodrigo Oliveira</b></sub></a><br /><a href="#infra-rodrigodosanjosoliveira" title="Infrastructure (Hosting, Build-Tools, etc)">🚇</a> <a href="https://github.com/rodrigodosanjosoliveira/sftpwrapper/commits?author=rodrigodosanjosoliveira" title="Tests">⚠️</a> <a href="https://github.com/rodrigodosanjosoliveira/sftpwrapper/commits?author=rodrigodosanjosoliveira" title="Code">💻</a></td>
  </tr>
</table>

<!-- markdownlint-enable -->
<!-- prettier-ignore-end -->
<!-- ALL-CONTRIBUTORS-LIST:END -->

This project follows the [all-contributors](https://github.com/all-contributors/all-contributors) specification. Contributions of any kind welcome!
