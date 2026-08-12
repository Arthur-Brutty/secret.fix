# Rodar sem instalar .NET SDK

Este projeto e um app WPF em .NET 8. Para compilar localmente, o Windows precisa ter o .NET 8 SDK instalado. Se a maquina nao permite instalar software, use uma destas opcoes.

## Opcao recomendada: GitHub Actions

1. Suba este repositorio para o GitHub.
2. Abra a aba `Actions`.
3. Escolha o workflow `Windows build`.
4. Clique em `Run workflow`.
5. Espere o build terminar.
6. Baixe o artifact versionado, por exemplo `secret-fix-v0.1-win-x64`.
7. Extraia o zip e rode o executavel versionado, por exemplo `secret-fix-v0.1.exe`.

O artifact e publicado como `self-contained`, entao nao precisa instalar .NET na maquina para executar.

## Opcao alternativa: build em outra maquina

Em uma maquina com permissao para instalar o .NET 8 SDK:

```powershell
dotnet publish src/SecretFix.App/SecretFix.App.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

Depois copie a pasta `publish` para a maquina restrita e rode `SecretFix.exe`.

## O que nao da para fazer

Nao da para compilar este projeto WPF apenas com VS Code, sem .NET SDK, MSBuild ou um executavel ja publicado.
