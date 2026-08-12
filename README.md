# secret.fix

`secret.fix` é um utilitário Windows voltado a centralizar ajustes de input e performance para jogadores de FiveM.

> Estado atual: **desenvolvimento / protótipo**. Este repositório é a fonte canônica do projeto a partir de agora.

## Objetivos do produto

- interface desktop preta/vermelha com animações e seleção visual de dispositivos;
- módulos de Mouse, Teclado, FiveM, Flick, Sensibilidade, Mira, Serviços e Display;
- backup/restauração antes de alterações de sistema;
- um único aplicativo com licenças por plano: **CORE**, **PULSE** e **APEX**;
- área Account com Username, License, Plan, Expiration, Device, App Version e Upgrade Plan;
- arquitetura segura: sem injeção de DLL no FiveM e sem mecanismos para contornar anti-cheat/regras de servidores.

## Stack proposta

- Windows 10/11 x64
- .NET 8
- WPF
- C#
- GitHub Actions para build

## Estrutura

```text
src/SecretFix.App/       Aplicativo Windows
docs/                    Produto, design, roadmap e decisões
.github/workflows/        CI de build para Windows
AGENTS.md                 Contexto e regras para o Codex
```

## Rodar localmente

Requer Windows e .NET 8 SDK.

```powershell
dotnet restore src/SecretFix.App/SecretFix.App.csproj
dotnet run --project src/SecretFix.App/SecretFix.App.csproj
```

## Rodar no VS Code

1. Instale o **.NET 8 SDK** para Windows.
2. Abra esta pasta no VS Code: `secret-fix-github`.
3. Instale as extensões recomendadas quando o VS Code pedir: **C# Dev Kit** e **C#**.
4. Use `Terminal > Run Task... > secret.fix: run`.
5. Para debug, pressione `F5` e escolha `secret.fix: debug WPF`.

Também existe o script:

```powershell
.\scripts\run-dev.ps1
```

## Build

```powershell
dotnet publish src/SecretFix.App/SecretFix.App.csproj -c Release -r win-x64 --self-contained true
```

Ou pelo VS Code: `Terminal > Run Task... > secret.fix: publish win-x64`.

O workflow do GitHub gera artifact versionado. Na versao atual, o artifact fica como `secret-fix-v0.1-win-x64` e o executavel dentro dele fica como `secret-fix-v0.1.exe`.

O executável publicado localmente ou pelo CI **não deve ser commitado** no repositório. Releases devem ser anexadas à área Releases do GitHub.

## Versionamento de builds

A cada atualizacao de teste, incremente a versao em:

- `.github/workflows/windows-build.yml`: `APP_VERSION` e `ARTIFACT_NAME`;
- `src/SecretFix.App/SecretFix.App.csproj`: `VersionPrefix` e `InformationalVersion`;
- `scripts/publish-win-x64.ps1`: `$version`.

## Próximas prioridades

1. reproduzir fielmente o design aprovado nas referências em `docs/reference/`;
2. completar MouseFix e TecladoFix com seleção real de dispositivos;
3. implementar as permissões CORE/PULSE/APEX;
4. criar a página Account e mock local de licença para desenvolvimento;
5. adicionar benchmark de input Antes × Depois;
6. separar recursos experimentais de recursos seguros.

Veja `docs/ROADMAP.md` e `AGENTS.md`.
