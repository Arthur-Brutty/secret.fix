# Contributing

`secret.fix` is source-available for portfolio review and is not an open-source project. Contributions may be discussed or accepted at the maintainer's discretion; the repository license continues to govern use of the code.

## Before opening an issue

- Search existing issues and documentation.
- Include Windows version, .NET SDK version, reproduction steps, expected behavior, and actual behavior.
- Remove account names, license values, device serials, paths containing personal data, and logs with credentials.
- Report security concerns privately according to [SECURITY.md](SECURITY.md), not in a public issue.

## Local workflow

```powershell
dotnet restore SecretFix.sln
dotnet build SecretFix.sln -c Release --no-restore
dotnet test tests/SecretFix.Tests/SecretFix.Tests.csproj -c Release --no-restore
```

## Pull requests and code style

- Keep changes focused and explain the user-visible or technical reason.
- Preserve the backup-before-apply, validation, restore, and logging flow for Windows changes.
- Use documented Windows APIs and clearly mark experiments.
- Do not add cheats, game injection, anti-cheat bypasses, combat macros, secrets, generated binaries, or local data.
- Prefer readable C# and small services with explicit responsibilities; comments should explain Windows behavior or trade-offs.
- Add or update tests when behavior changes.
