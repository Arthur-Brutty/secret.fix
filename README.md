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

## Build

```powershell
dotnet publish src/SecretFix.App/SecretFix.App.csproj -c Release -r win-x64 --self-contained true
```

O executável publicado fica em `src/SecretFix.App/bin/Release/.../publish/` e **não deve ser commitado** no repositório. Releases devem ser anexadas à área Releases do GitHub.

## Próximas prioridades

1. reproduzir fielmente o design aprovado nas referências em `docs/reference/`;
2. completar MouseFix e TecladoFix com seleção real de dispositivos;
3. implementar as permissões CORE/PULSE/APEX;
4. criar a página Account e mock local de licença para desenvolvimento;
5. adicionar benchmark de input Antes × Depois;
6. separar recursos experimentais de recursos seguros.

Veja `docs/ROADMAP.md` e `AGENTS.md`.
