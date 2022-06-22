[![Gitpod Ready-to-Code](https://img.shields.io/badge/Gitpod-Ready--to--Code-blue?logo=gitpod)](https://gitpod.io/#https://github.com/rodrigodosanjosoliveira/sftpwrapper) 

![dotnet workflow](https://github.com/rodrigodosanjosoliveira/sftpwrapper/actions/workflows/dotnet.yml/badge.svg)
<!-- ALL-CONTRIBUTORS-BADGE:START - Do not remove or modify this section -->
[![All Contributors](https://img.shields.io/badge/all_contributors-2-orange.svg?style=flat-square)](#contributors-)
<!-- ALL-CONTRIBUTORS-BADGE:END -->


# SftpWrapper #
Pacote NuGet responsável por realizar operações de download e upload de arquivos utilizando servidores SFTP
https://www.nuget.org/packages/SftpWrapper.Sdk/

## Utilizando o nuget

Package Manager:
```
Install-Package SftpWrapper.Sdk -Version 1.0.6
```
.NET CLI
```
dotnet add package SftpWrapper.Sdk --version 1.0.6
```

## Executando os testes 

Deve-se executar o comando abaixo para iniciar o servidor SFTP a partir da imagem Docker.

``` docker-compose up -d ```

Dados de autenticação para acesso ao Servidor
* User: foo
* Password: pass
* Host: 127.0.0.1
* Port: 2222

## Dependências

https://github.com/sshnet/SSH.NET


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
