[![Build Status](https://travis-ci.org/rodrigodosanjosoliveira/sftpwrapper.svg?branch=master)](https://travis-ci.org/rodrigodosanjosoliveira/sftpwrapper)


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

